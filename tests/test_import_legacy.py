import pathlib
import sys
import tempfile
import unittest

MODULE = pathlib.Path(__file__).resolve().parents[1] / 'src' / 'Migration'
sys.path.insert(0, str(MODULE))
from import_legacy import build_state, run, digest


class ImportTests(unittest.TestCase):
    def test_links_history_and_no_credentials(self):
        rows = [
            ('abmacodigos', {'ORDER_ID': '1', 'NOMBRE': 'Cliente & teste', 'CALLE': '7', 'NUMERO': '20', 'ID_CL': '99', 'PARTICION': '1', 'CONTRASENIA': 'secret'}),
            ('calles', {'ORDER_ID': '7', 'NOMBRE': 'Rua teste'}),
            ('tlmapersonas', {'ORDER_ID': '4', 'NOMBRE': 'Contato', 'PASS_WEB': 'secret'}),
            ('tlrlpersonas', {'ORDER_ID': '44', 'ORDER_RL': '4', 'TELEFONO': '5551234'}),
            ('abrltelefonos', {'ORDER_ID': '3', 'ORDER_RL': '1', 'CODIGO_ID': '4', 'DATOS01': '1111'}),
            ('abrlzonas', {'ORDER_ID': '5', 'ORDER_RL': '1', 'N_ZONA': '01', 'NOMBRE': 'Porta'}),
            ('evmahistorico', {'ORDER_ID': '10', 'ORDER_RL': '1', 'EVENTO': '130', 'FECHAHORA': '2020-01-01 10:00:00'}),
        ]
        root, report = build_state(iter(rows), 'abc', 10)
        client = root.find('./Clients/Client')
        self.assertEqual(client.get('Account'), 'BYKOM-1')
        self.assertIn('Rua teste', client.get('Address'))
        self.assertIn('Contato', client.get('Contacts'))
        self.assertIn('5551234', client.get('Contacts'))
        self.assertIn('Porta', client.get('Zones'))
        self.assertEqual(len(root.find('Users')), 0)
        self.assertEqual(len(root.find('Incidents')), 0)
        self.assertEqual(root.find('./LegacyHistory/Event').get('OccurredLocal'), '2020-01-01 10:00:00')
        self.assertEqual(report['clients_imported'], 1)
        self.assertNotIn('secret', str(root.attrib) + str(client.attrib))

    def test_history_bounded_latest_and_orphan_counted(self):
        rows = [('abmacodigos', {'ORDER_ID': '1', 'NOMBRE': 'A'})]
        rows += [('evmahistorico', {'ORDER_ID': str(i), 'ORDER_RL': '1', 'FECHAHORA': '2020-01-0%d 00:00:00' % i}) for i in range(1, 4)]
        rows += [('abrlzonas', {'ORDER_ID': '4', 'ORDER_RL': 'missing', 'NOMBRE': 'Orphan'})]
        root, report = build_state(iter(rows), 'abc', 2)
        self.assertEqual([e.get('Id') for e in root.find('LegacyHistory')], ['3', '2'])
        self.assertEqual(report['history_rows_seen'], 3)
        self.assertEqual(report['history_not_loaded'], 1)
        self.assertEqual(report['orphan_relations'], 1)

    def test_duplicate_source_fails(self):
        row = ('abmacodigos', {'ORDER_ID': '1', 'NOMBRE': 'A'})
        with self.assertRaises(ValueError):
            build_state(iter([row, row]), 'abc', 1)

    def test_missing_source_key_fails(self):
        with self.assertRaises(ValueError):
            build_state(iter([('abmacodigos', {'NOMBRE': 'A'})]), 'abc', 1)

    def test_publish_and_refuse_overwrite(self):
        with tempfile.TemporaryDirectory() as directory:
            source = pathlib.Path(directory) / 'source.sql'
            destination = pathlib.Path(directory) / 'test'
            source.write_text("CREATE TABLE abmacodigos (ORDER_ID int, NOMBRE text); INSERT INTO abmacodigos VALUES (1,'Cliente');", encoding='utf-8')
            original = digest(source)
            report = run(source, destination, 'utf-8-sig', 1)
            self.assertEqual(report['clients_imported'], 1)
            self.assertTrue((destination / 'data.xml').is_file())
            self.assertTrue((destination / 'import-report.json').is_file())
            result = digest(destination / 'data.xml')
            with self.assertRaises(ValueError):
                run(source, destination, 'utf-8-sig', 1)
            self.assertEqual(result, digest(destination / 'data.xml'))
            self.assertEqual(original, digest(source))

    def test_malformed_input_does_not_publish(self):
        with tempfile.TemporaryDirectory() as directory:
            source = pathlib.Path(directory) / 'source.sql'
            destination = pathlib.Path(directory) / 'test'
            source.write_text("INSERT INTO abmacodigos (ORDER_ID,NOMBRE) VALUES (1);", encoding='utf-8')
            with self.assertRaises(ValueError):
                run(source, destination, 'utf-8-sig', 1)
            self.assertFalse(destination.exists())


if __name__ == '__main__':
    unittest.main()
