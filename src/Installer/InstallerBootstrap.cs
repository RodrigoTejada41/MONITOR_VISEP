using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;

internal static class InstallerBootstrap
{
    internal static void Extract(Stream payload, string destination)
    {
        string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using (var archive = new ZipArchive(payload, ZipArchiveMode.Read, true))
        {
            foreach (var entry in archive.Entries)
            {
                string name = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(name) || name.IndexOf(':') >= 0)
                    throw new InvalidDataException("Caminho invalido no pacote.");
                string target = Path.GetFullPath(Path.Combine(root, name));
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Arquivo fora do pacote.");
                if (String.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(target); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using (var input = entry.Open())
                using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write)) input.CopyTo(output);
            }
        }
    }

    private static int Main(string[] args)
    {
        // Extraction-only is used to inspect the exact release without installing services.
        bool inspect = args.Length == 2 && args[0] == "--extract-only";
        string destination = inspect ? Path.GetFullPath(args[1]) : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VisepSetup", Guid.NewGuid().ToString("N"));
        try
        {
            if (args.Length != 0 && !inspect) throw new ArgumentException("Uso: VISEP-Setup.exe [--extract-only pasta-nova]");
            if (Directory.Exists(destination)) throw new IOException("Pasta de extracao ja existe.");
            Directory.CreateDirectory(destination);
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Visep.Payload.zip"))
            {
                if (resource == null) throw new InvalidDataException("Pacote incorporado ausente.");
                Extract(resource, destination);
            }
            Console.WriteLine("Pacote: " + destination);
            if (inspect) return 0;
            string script = Path.Combine(destination, "scripts", "Install.ps1");
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe"),
                "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -Interactive");
            start.UseShellExecute = false;
            start.WorkingDirectory = destination;
            using (var process = Process.Start(start))
            {
                process.WaitForExit();
                Console.WriteLine(process.ExitCode == 0 ? "Instalacao concluida." : "Instalacao falhou. Preserve esta pasta para diagnostico.");
                Console.WriteLine("Pressione Enter para fechar.");
                Console.ReadLine();
                return process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Instalacao interrompida: " + ex.Message);
            if (!inspect) { Console.WriteLine("Pressione Enter para fechar."); Console.ReadLine(); }
            return 1;
        }
    }
}
