using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace Visep.Migration
{
    public static class InspectProgram
    {
        public static int Main(string[] args)
        {
            if (args.Length != 1) { Console.Error.WriteLine("Uso: Visep.LegacyInspector.exe arquivo.sql"); return 2; }
            try
            {
                Inspect(args[0]); return 0;
            }
            catch (Exception) { Console.Error.WriteLine("Inspecao falhou. Verifique arquivo, permissao e formato. Conteudo nao exibido."); return 1; }
        }
        private static string Identifier(string input)
        {
            string value = input.Trim('`', '"', '[', ']');
            return Regex.IsMatch(value, @"\A[A-Za-z_][A-Za-z0-9_$]{0,127}\z") ? value : "[identificador omitido]";
        }
        private static string ReadBoundedLine(StreamReader reader)
        {
            var text = new StringBuilder();
            int value;
            bool any = false;
            while ((value = reader.Read()) >= 0)
            {
                any = true;
                if (value == '\n') break;
                if (text.Length < 65536 && value != '\r') text.Append((char)value);
            }
            return any ? text.ToString() : null;
        }
        private static void Inspect(string path)
        {
            long lines = 0, inserts = 0, dangerous = 0, objects = 0, indexes = 0, columns = 0, tables = 0;
            bool inTable = false;
            var charsets = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var versions = new SortedSet<string>();
            using (var reader = new StreamReader(path))
            {
                string line;
                while ((line = ReadBoundedLine(reader)) != null)
                {
                    lines++;
                    // Only structural tokens are emitted; row values and defaults are never printed.
                    if (Regex.IsMatch(line, @"^\s*(?:INSERT|REPLACE)\s", RegexOptions.IgnoreCase)) { inserts++; continue; }
                    if (Regex.IsMatch(line, @"^\s*(?:DROP|TRUNCATE|DELETE|UPDATE|GRANT|REVOKE|LOAD\s+DATA|CALL)\b", RegexOptions.IgnoreCase)) dangerous++;
                    if (Regex.IsMatch(line, @"^\s*CREATE\s+(?:(?:OR\s+REPLACE|DEFINER\s*=\s*\S+)\s+)*(?:VIEW|TRIGGER|PROCEDURE|FUNCTION|EVENT)\b", RegexOptions.IgnoreCase)) objects++;
                    Match version = Regex.Match(line, @"^--\s*Server version\s+(\d+(?:\.\d+){1,3})", RegexOptions.IgnoreCase);
                    if (version.Success) versions.Add(version.Groups[1].Value);
                    Match table = Regex.Match(line, "^\\s*CREATE\\s+TABLE\\s+(?:IF\\s+NOT\\s+EXISTS\\s+)?(`[^`]+`|[A-Za-z_][A-Za-z0-9_$]*)", RegexOptions.IgnoreCase);
                    if (table.Success) { tables++; inTable = true; Console.WriteLine("TABLE " + Identifier(table.Groups[1].Value)); }
                    else if (inTable)
                    {
                        Match column = Regex.Match(line, "^\\s*(`[^`]+`|[A-Za-z_][A-Za-z0-9_$]*)\\s+(BIGINT|INT|INTEGER|SMALLINT|TINYINT|MEDIUMINT|VARCHAR|CHAR|TEXT|LONGTEXT|MEDIUMTEXT|TINYTEXT|BLOB|LONGBLOB|VARBINARY|BINARY|DECIMAL|NUMERIC|DOUBLE|FLOAT|REAL|DATE|DATETIME|TIMESTAMP|TIME|YEAR|BIT|BOOLEAN|BOOL|ENUM|SET|JSON)\\b", RegexOptions.IgnoreCase);
                        if (column.Success) { columns++; Console.WriteLine("  COLUMN " + Identifier(column.Groups[1].Value) + " " + column.Groups[2].Value.ToUpperInvariant()); }
                        if (Regex.IsMatch(line, @"^\s*(?:PRIMARY\s+KEY|UNIQUE\s+(?:KEY|INDEX)|KEY|INDEX|FULLTEXT\s+KEY|SPATIAL\s+KEY)\b", RegexOptions.IgnoreCase)) indexes++;
                    }
                    if (inTable)
                    {
                        Match charset = Regex.Match(line, @"\b(?:CHARSET\s*=|CHARACTER\s+SET\s+)\s*([A-Za-z0-9_]{1,32})\b", RegexOptions.IgnoreCase);
                        if (charset.Success) charsets.Add(charset.Groups[1].Value);
                        if (line.TrimEnd().EndsWith(";", StringComparison.Ordinal)) inTable = false;
                    }
                }
            }
            Console.WriteLine("Linhas: " + lines + "; tabelas: " + tables + "; colunas: " + columns + "; indices: " + indexes);
            Console.WriteLine("Comandos INSERT/REPLACE: " + inserts + "; objetos programaveis/views: " + objects + "; comandos potencialmente destrutivos: " + dangerous);
            Console.WriteLine("Versoes declaradas: " + String.Join(", ", versions));
            Console.WriteLine("Charsets declarados: " + String.Join(", ", charsets));
            Console.WriteLine("Inspecao textual somente. Contagens por linha; SQL multiline/compactado pode exigir revisao. Nenhum SQL executado. Nenhum mapeamento inferido.");
        }
    }
}
