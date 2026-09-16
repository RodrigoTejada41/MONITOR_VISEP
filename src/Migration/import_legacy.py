"""Read a MySQL dump as data; never connect to or execute SQL on a server."""
import argparse
from collections import Counter, defaultdict
from datetime import datetime, timezone
import hashlib
import heapq
import json
import os
from pathlib import Path
import xml.etree.ElementTree as ET

FIELDS = {
    'abmacodigos': 'ORDER_ID NOMBRE ID_RC ID_CL PARTICION CALLE NUMERO PISO DPTO CODIGOCIUD BORRADO BAJA_LOGICA CODIGOALAR',
    'abrltelefonos': 'ORDER_ID ORDER_RL CODIGO_ID DATOS01 DATOS02',
    'tlmapersonas': 'ORDER_ID NOMBRE NOMBRE_DOS',
    'tlrlpersonas': 'ORDER_ID ORDER_RL TELEFONO',
    'abrlzonas': 'ORDER_ID ORDER_RL N_ZONA NOMBRE',
    'abrlsensores': 'ORDER_ID ORDER_RL SENSOR_ID NOMBRE',
    'calles': 'ORDER_ID NOMBRE',
    'ciudad': 'CODIGOCIUD NOMBRE',
    'pamacodigos': 'ORDER_ID NOMBRE',
    'evmahistorico': 'ORDER_ID ORDER_RL EVENTO ZON_US FECHAHORA',
}


def clean(value, report):
    value = value or ''
    result = ''.join(c for c in value if c in '\t\n\r' or 32 <= ord(c) <= 0xD7FF or 0xE000 <= ord(c) <= 0xFFFD or 0x10000 <= ord(c) <= 0x10FFFF)
    if result != value:
        report['xml_texts_cleaned'] += 1
    return result


def build_state(rows, source_hash, history_limit, progress=None):
    if not 0 <= history_limit <= 10000:
        raise ValueError('History limit must be between 0 and 10000')
    counts, tables, history = Counter(), defaultdict(list), []
    report = Counter()
    for table, source in rows:
        if table not in FIELDS:
            continue
        counts[table] += 1
        if progress is not None and counts[table] % 250000 == 0:
            progress(table, counts[table])
        row = {key: clean(source.get(key), report) for key in FIELDS[table].split()}
        if table == 'evmahistorico':
            entry = (row['FECHAHORA'], counts[table], row)
            if len(history) < history_limit:
                heapq.heappush(history, entry)
            elif history_limit and entry[:2] > history[0][:2]:
                heapq.heapreplace(history, entry)
        else:
            tables[table].append(row)
    root = ET.Element('Visep', SchemaVersion='1', ImportSourceHash=source_hash, ImportMode='BYKOM_TEST')
    ET.SubElement(root, 'Users')
    clients = ET.SubElement(root, 'Clients')
    ET.SubElement(root, 'Incidents')
    audit = ET.SubElement(root, 'Audit')
    legacy = ET.SubElement(root, 'LegacyHistory')
    lookup = {}
    for table, key in [('calles', 'ORDER_ID'), ('ciudad', 'CODIGOCIUD'), ('pamacodigos', 'ORDER_ID'), ('tlmapersonas', 'ORDER_ID')]:
        lookup[table] = {r[key]: r['NOMBRE'] for r in tables[table]}
    relations = defaultdict(lambda: defaultdict(list))
    phones = defaultdict(list)
    for row in tables['tlrlpersonas']:
        phones[row['ORDER_RL']].append(row['TELEFONO'])
    for table in ('abrltelefonos', 'abrlzonas', 'abrlsensores'):
        for row in tables[table]:
            relations[row['ORDER_RL']][table].append(row)
    mapped = {}
    for row in tables['abmacodigos']:
        key = row['ORDER_ID']
        if not key or key in mapped:
            raise ValueError('Missing or duplicate source client key')
        account = 'BYKOM-' + key
        if len(account) > 64:
            raise ValueError('Source key exceeds account limit')
        name = row['NOMBRE'].strip() or account
        if not row['NOMBRE'].strip():
            report['client_names_missing'] += 1
        address = ' '.join(filter(None, [lookup['calles'].get(row['CALLE'], ''), row['NUMERO'], row['PISO'], row['DPTO'], lookup['ciudad'].get(row['CODIGOCIUD'], '')]))
        related = relations[key]
        contacts = '\n'.join(' | '.join(filter(None, [lookup['tlmapersonas'].get(r['CODIGO_ID'], ''), ', '.join(phones[r['CODIGO_ID']]), r['DATOS01'], r['DATOS02']])) for r in related['abrltelefonos'])
        zones = '\n'.join(' | '.join(filter(None, [r['N_ZONA'], r['NOMBRE']])) for r in related['abrlzonas'])
        equipment = 'BYKOM: receptor={}; conta={}; particao={}; BORRADO={}; BAJA_LOGICA={}'.format(row['ID_RC'], row['ID_CL'], row['PARTICION'], row['BORRADO'], row['BAJA_LOGICA'])
        equipment += '\n' + lookup['pamacodigos'].get(row['CODIGOALAR'], '')
        equipment += '\n' + '\n'.join(r['SENSOR_ID'] + ' | ' + r['NOMBRE'] for r in related['abrlsensores'])
        values = dict(Id='bykom-' + key, Name=name, Account=account, Address=address, Contacts=contacts, Equipment=equipment.strip(), Zones=zones)
        ET.SubElement(clients, 'Client', **values)
        mapped[key] = values
    if not mapped:
        raise ValueError('No source clients found; destination not published')
    for key, groups in relations.items():
        if key not in mapped:
            report['orphan_relations'] += sum(len(group) for group in groups.values())
    for _, _, row in sorted(history, reverse=True):
        client = mapped.get(row['ORDER_RL'])
        if client is None:
            report['history_orphan_clients'] += 1
        ET.SubElement(legacy, 'Event', Id=row['ORDER_ID'], SourceClientId=row['ORDER_RL'],
                      Account=client['Account'] if client else '', ClientName=client['Name'] if client else '',
                      Code=row['EVENTO'], Zone=row['ZON_US'], OccurredLocal=row['FECHAHORA'],
                      Detail='BYKOM evmahistorico; horario original, sem conversao; codigo de origem')
    now = datetime.now(timezone.utc).isoformat()
    ET.SubElement(audit, 'Entry', AtUtc=now, User='LegacyImporter', Action='ImportTest', Target=source_hash)
    report.update(clients_imported=len(mapped), history_rows_seen=counts['evmahistorico'],
                  history_imported=len(history), history_not_loaded=counts['evmahistorico'] - len(history))
    return root, dict(report, tables_read=dict(counts), source_sha256=source_hash,
                      mapping_version=1, created_utc=now, mode='TEST_ONLY', history_limit=history_limit,
                      accounts='BYKOM-ORDER_ID; original receiver/account/partition in Equipment',
                      history='Latest lexical source FECHAHORA; original timezone unverified; read-only sample',
                      excluded='Credentials, photos, free-form event detail, operators, programmable SQL and unselected tables')


def digest(path):
    with open(path, 'rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def run(source, destination, encoding, history_limit, allow_control_separator=False):
    from sql_dump import iter_rows
    source, destination = Path(source).resolve(), Path(destination).resolve()
    if destination.exists():
        raise ValueError('Destination already exists; use a new isolated folder')
    if not source.is_file():
        raise ValueError('SQL file not found')
    before = digest(source)
    diagnostics = {}
    options = {'encoding': encoding}
    if allow_control_separator:
        options.update(allow_control_separator=True, diagnostics=diagnostics)
    root, report = build_state(iter_rows(source, set(FIELDS), **options), before, history_limit,
                               lambda table, count: print('Reading {}: {} rows'.format(table, count), flush=True))
    if digest(source) != before:
        raise ValueError('Source changed during import')
    payload = ET.tostring(root, encoding='utf-8', xml_declaration=True)
    if len(payload) > 60 * 1024 * 1024:
        raise ValueError('Test state exceeds supported size; reduce history limit')
    ET.fromstring(payload)
    report['encoding'] = encoding
    report['allow_control_separator'] = allow_control_separator
    report['parser_diagnostics'] = diagnostics
    report['data_sha256'] = hashlib.sha256(payload).hexdigest()
    destination.mkdir(parents=True, exist_ok=False)
    try:
        if os.name == 'nt':
            import subprocess
            identity = subprocess.run(['whoami', '/user', '/fo', 'csv', '/nh'], capture_output=True, text=True, check=True)
            import csv
            sid = next(csv.reader(identity.stdout.splitlines()))[1]
            subprocess.run(['icacls.exe', str(destination), '/inheritance:r', '/grant:r', '*S-1-5-18:(OI)(CI)F', '*S-1-5-32-544:(OI)(CI)F', '*' + sid + ':(OI)(CI)F'], capture_output=True, check=True)
        (destination / 'data.xml.tmp').write_bytes(payload)
        (destination / 'import-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        (destination / 'data.xml.tmp').rename(destination / 'data.xml')
    except Exception:
        # Leave the failed staging directory for diagnosis; never publish data.xml on partial failure.
        raise
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True)
    parser.add_argument('--destination', required=True)
    parser.add_argument('--encoding', default='utf-8-sig', choices=['utf-8-sig', 'latin-1', 'cp1252'])
    parser.add_argument('--history-limit', type=int, default=1000)
    parser.add_argument('--allow-control-separator', action='store_true', help='Tolerate only known 0x05 artifact between row tuples; record count')
    args = parser.parse_args()
    try:
        result = run(args.source, args.destination, args.encoding, args.history_limit, args.allow_control_separator)
        print(json.dumps({k: result[k] for k in ('clients_imported', 'history_rows_seen', 'history_imported', 'history_not_loaded', 'source_sha256')}))
    except Exception as error:
        print('Import failed: ' + type(error).__name__ + '. Destination not ready; original SQL unchanged.')
        raise SystemExit(1)
