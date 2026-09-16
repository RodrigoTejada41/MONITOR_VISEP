import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'src' / 'Migration'))
from sql_dump import iter_rows, DumpFormatError


class SqlDumpTests(unittest.TestCase):
    def parse(self, sql, encoding='utf-8-sig'):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'dump.sql'
            path.write_bytes(sql.encode(encoding))
            return list(iter_rows(path, {'clients'}, encoding=encoding))

    def test_create_order_and_multiple_rows(self):
        rows = self.parse('CREATE TABLE `clients` (`id` int, `name` varchar(50), PRIMARY KEY (`id`)); INSERT INTO `clients` VALUES (1,\'Ana\'),(2,NULL);')
        self.assertEqual(rows, [('clients', {'id': '1', 'name': 'Ana'}), ('clients', {'id': '2', 'name': None})])

    def test_explicit_columns_escapes_and_comments(self):
        rows = self.parse("-- heading\nINSERT INTO `clients` (`name`,`id`) VALUES ('O''Neil; \\' \\\\ \\n',-2); # done\n")
        self.assertEqual(rows[0][1], {'name': "O'Neil; ' \\ \n", 'id': '-2'})

    def test_ignore_unselected_and_routines(self):
        rows = self.parse('INSERT INTO other VALUES (FUNC(1));\nDELIMITER $$\nCREATE PROCEDURE p() BEGIN INSERT INTO clients VALUES (1); END$$\nDELIMITER ;\nINSERT INTO clients (id) VALUES (3);')
        self.assertEqual(rows, [('clients', {'id': '3'})])

    def test_reject_missing_schema_expressions_and_row_mismatch(self):
        for sql in ['INSERT INTO clients VALUES (1);', 'INSERT INTO clients (id) VALUES (NOW());', 'INSERT INTO clients (id) VALUES (1,2);', 'INSERT INTO clients (id) VALUES (1) ON DUPLICATE KEY UPDATE id=2;', 'INSERT INTO clients (id) VALUES (1']:
            with self.subTest(sql=sql), self.assertRaises(DumpFormatError):
                self.parse(sql)

    def test_encoding_is_explicit(self):
        self.assertEqual(self.parse("INSERT INTO clients (name) VALUES ('João');", 'latin-1')[0][1]['name'], 'João')
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'dump.sql'
            path.write_bytes(b"INSERT INTO clients (name) VALUES ('\xe3');")
            with self.assertRaises(UnicodeDecodeError):
                list(iter_rows(path, {'clients'}))

    def test_mysql_hex_literal_uses_explicit_encoding(self):
        self.assertEqual(self.parse('INSERT INTO clients (name) VALUES (0x4ae36f);', 'latin-1')[0][1]['name'], 'Jão')

    def test_multiline_quotes_and_scientific_number(self):
        rows = self.parse("INSERT INTO clients (name,id) VALUES ('first\nDELIMITER $$\nlast',-1.2e-3);")
        self.assertEqual(rows[0][1], {'name': 'first\nDELIMITER $$\nlast', 'id': '-1.2e-3'})

    def test_comments_and_quoted_column_names(self):
        rows = self.parse("/* start\nend */ CREATE TABLE clients (`key` int, name text); INSERT INTO clients VALUES (1,'a');")
        self.assertEqual(rows[0][1], {'key': '1', 'name': 'a'})

    def test_unselected_punctuation_strings_cannot_end_statement(self):
        rows = self.parse("INSERT INTO other VALUES (';', 'CREATE', 'INSERT'); INSERT INTO clients (id) VALUES (1);")
        self.assertEqual(rows, [('clients', {'id': '1'})])

    def test_control_separator_requires_explicit_opt_in(self):
        sql = "INSERT INTO clients (name) VALUES ('a\x05'),\x05('b');"
        with self.assertRaises(DumpFormatError):
            self.parse(sql)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'dump.sql'
            path.write_text(sql, encoding='utf-8')
            diagnostics = {}
            rows = list(iter_rows(path, {'clients'}, allow_control_separator=True, diagnostics=diagnostics))
            self.assertEqual(rows, [('clients', {'name': 'a\x05'}), ('clients', {'name': 'b'})])
            self.assertEqual(diagnostics, {'control_separators_ignored': 1})
            path.write_text("INSERT INTO clients (name) VALUES \x05('b');", encoding='utf-8')
            with self.assertRaises(DumpFormatError):
                list(iter_rows(path, {'clients'}, allow_control_separator=True))


if __name__ == '__main__':
    unittest.main()
