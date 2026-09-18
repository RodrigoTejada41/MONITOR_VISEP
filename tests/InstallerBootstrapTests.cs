using System;
using System.IO;
using System.IO.Compression;
using System.Text;

internal static class InstallerBootstrapTests
{
    private static int checks;
    private static void Assert(bool value) { if (!value) throw new Exception("Bootstrap assertion failed"); checks++; }
    private static MemoryStream Archive(string name)
    {
        var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        using (var writer = new StreamWriter(zip.CreateEntry(name).Open(), Encoding.UTF8)) writer.Write("synthetic");
        buffer.Position = 0;
        return buffer;
    }
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "visep-bootstrap-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using (var zip = Archive("scripts/Install.ps1")) InstallerBootstrap.Extract(zip, root);
            Assert(File.Exists(Path.Combine(root, "scripts", "Install.ps1")));
            foreach (string name in new[] { "../escape.txt", "scripts/../../escape.txt", "C:/escape.txt", "/escape.txt" })
            {
                bool rejected = false;
                try { using (var zip = Archive(name)) InstallerBootstrap.Extract(zip, root); }
                catch (InvalidDataException) { rejected = true; }
                Assert(rejected);
            }
            bool duplicate = false;
            try { using (var zip = Archive("scripts/Install.ps1")) InstallerBootstrap.Extract(zip, root); }
            catch (IOException) { duplicate = true; }
            Assert(duplicate);
            Console.WriteLine("Bootstrap: " + checks + " assertions passed.");
            return 0;
        }
        finally { Directory.Delete(root, true); }
    }
}
