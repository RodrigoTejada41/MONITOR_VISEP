"""Read scalar rows from a MySQL dump without executing any SQL."""
import re


class DumpFormatError(ValueError):
    pass


_TOKEN = re.compile(
    r"(?P<space>\s+)|(?P<comment>\#[^\n]*(?:\n|$)|--[^\n]*(?:\n|$)|/\*.*?\*/)|"
    r"(?P<string>'(?:\\.|''|[^'\\])*'|\"(?:\\.|\"\"|[^\"\\])*\")|"
    r"(?P<identifier>`(?:``|[^`])*`)|"
    r"(?P<number>(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?(?![\w$]))|"
    r"(?P<word>[\w$]+(?:\.[\w$]+)*)|"
    r"(?P<symbol>[^\s'\"`])", re.DOTALL)


def _tokens(stream):
    escapes = {'0': '\0', 'n': '\n', 'r': '\r', 't': '\t', 'b': '\b', 'Z': '\x1a'}
    pending = ''
    for line in stream:
        pending += line
        position = 0
        while position < len(pending):
            match = _TOKEN.match(pending, position)
            if not match or (pending.startswith('/*', position) and match.lastgroup != 'comment'):
                break
            kind, value = match.lastgroup, match.group()
            position = match.end()
            if kind in ('space', 'comment'):
                continue
            if kind in ('string', 'identifier'):
                quote = value[0]
                value = value[1:-1].replace(quote * 2, quote)
                if kind == 'string':
                    value = re.sub(r'\\(.)', lambda m: escapes.get(m[1], '\\' + m[1] if m[1] in '%_' else m[1]), value, flags=re.DOTALL)
            yield kind, value
        pending = pending[position:]
    if pending:
        raise DumpFormatError('Unterminated SQL quote or comment')


class _Reader:
    def __init__(self, tokens):
        self.tokens = iter(tokens)

    def next(self):
        return next(self.tokens, ('eof', ''))

    def expect(self, expected):
        token = self.next()
        if token[1].upper() != expected.upper() or (expected in '(),;' and token[0] != 'symbol'):
            raise DumpFormatError('Expected SQL ' + expected)
        return token

    def drain(self, token=None):
        token = token or self.next()
        while token != ('symbol', ';') and token[0] != 'eof':
            token = self.next()


def _schema(reader):
    token = reader.next()
    if token[1].upper() != 'TABLE':
        reader.drain(token)
        return None, None
    name = reader.next()[1]
    if name.upper() == 'IF':
        reader.expect('NOT')
        reader.expect('EXISTS')
        name = reader.next()[1]
    reader.expect('(')
    columns, depth, start = [], 1, True
    constraints = {'PRIMARY', 'KEY', 'UNIQUE', 'CONSTRAINT', 'FOREIGN', 'CHECK', 'INDEX', 'FULLTEXT', 'SPATIAL'}
    while depth:
        kind, value = reader.next()
        if kind == 'eof':
            raise DumpFormatError('Unterminated CREATE TABLE')
        if start:
            if kind == 'identifier' or (kind == 'word' and value.upper() not in constraints):
                columns.append(value)
            start = False
        if value == '(' and kind == 'symbol':
            depth += 1
        elif value == ')' and kind == 'symbol':
            depth -= 1
        elif value == ',' and depth == 1 and kind == 'symbol':
            start = True
    reader.drain()
    return name, columns


def _scalar(reader, token):
    kind, value = token
    if kind == 'string':
        return value
    if kind == 'word' and value.upper() == 'NULL':
        return None
    if kind == 'word' and re.fullmatch(r'0x(?:[0-9a-fA-F]{2})*', value):
        return bytes.fromhex(value[2:]).decode(reader.encoding, errors='strict')
    if value in ('+', '-'):
        value += reader.next()[1]
    if re.fullmatch(r'[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?', value):
        return value
    raise DumpFormatError('Unsupported SQL scalar expression')


def _insert(reader, schemas, selected):
    token = reader.next()
    if token[1].upper() != 'INTO':
        raise DumpFormatError('Unsupported INSERT syntax')
    table = reader.next()[1]
    if table not in selected:
        reader.drain()
        return
    token = reader.next()
    columns = schemas.get(table)
    if token[1] == '(':
        columns = []
        while True:
            kind, column = reader.next()
            if kind not in ('word', 'identifier'):
                raise DumpFormatError('Expected INSERT column')
            columns.append(column)
            token = reader.next()
            if token[1] == ')':
                break
            if token[1] != ',':
                raise DumpFormatError('Invalid INSERT column list')
        token = reader.next()
    if not columns or len(set(columns)) != len(columns):
        raise DumpFormatError('Missing or duplicate column metadata for ' + table)
    if token[1].upper() != 'VALUES':
        raise DumpFormatError('Expected VALUES for ' + table)
    first_row = True
    while True:
        opening = reader.next()
        if not first_row and opening == ('symbol', '\x05') and reader.allow_control_separator:
            reader.diagnostics['control_separators_ignored'] = reader.diagnostics.get('control_separators_ignored', 0) + 1
            opening = reader.next()
        if opening != ('symbol', '('):
            raise DumpFormatError('Expected row opening for ' + table)
        first_row = False
        values = []
        while True:
            values.append(_scalar(reader, reader.next()))
            token = reader.next()
            if token[1] == ')':
                break
            if token[1] != ',':
                raise DumpFormatError('Unsupported value expression for ' + table)
        if len(values) != len(columns):
            raise DumpFormatError('Column count mismatch for ' + table)
        yield table, dict(zip(columns, values))
        token = reader.next()
        if token[1] == ';':
            return
        if token[1] != ',':
            raise DumpFormatError('Unsupported INSERT suffix for ' + table)


def iter_rows(path, selected_tables, *, encoding='utf-8-sig', allow_control_separator=False, diagnostics=None):
    """Yield (table, column dictionary); encoding is explicit, never guessed.

    Exhaust this iterator before committing output: malformed trailing SQL raises.
    Supports SQL Manager/MySQL scalar INSERT VALUES dumps, not arbitrary SQL.
    """
    selected, schemas = set(selected_tables), {}
    with open(path, encoding=encoding, errors='strict') as stream:
        reader = _Reader(_tokens(stream))
        reader.encoding = encoding
        reader.allow_control_separator = allow_control_separator
        reader.diagnostics = diagnostics if diagnostics is not None else {}
        while True:
            kind, value = reader.next()
            if kind == 'eof':
                return
            if (kind, value) == ('symbol', ';'):
                continue
            if value.upper() == 'DELIMITER':
                delimiter = reader.next()
                if delimiter[1] == ';':
                    continue
                while True:
                    token = reader.next()
                    if token[0] == 'eof':
                        raise DumpFormatError('Unterminated DELIMITER block')
                    if token[0] == 'word' and token[1].upper() == 'DELIMITER':
                        reader.expect(';')
                        break
            elif value.upper() == 'CREATE':
                name, columns = _schema(reader)
                if name in selected:
                    schemas[name] = columns
            elif value.upper() == 'INSERT':
                yield from _insert(reader, schemas, selected)
            else:
                reader.drain()
