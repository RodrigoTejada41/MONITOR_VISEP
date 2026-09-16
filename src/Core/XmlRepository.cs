using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
namespace Visep
{
    internal sealed class XmlRepository
    {
        readonly string path;
        public XmlRepository(string path) { this.path = path; }

        public T Access<T>(bool write, Func<XElement, T> action)
        {
            using (AcquireLock())
            {
                var root = Load();
                var result = action(root);
                if (write) Save(root);
                return result;
            }
        }

        FileStream AcquireLock()
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (true)
            {
                try { return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException)
                {
                    if (DateTime.UtcNow >= deadline) throw new TimeoutException("Store busy.");
                    Thread.Sleep(50);
                }
            }
        }

        XElement Load()
        {
            if (!File.Exists(path))
                return new XElement("Visep", new XAttribute("SchemaVersion", "1"),
                    new XElement("Users"), new XElement("Clients"),
                    new XElement("Incidents"), new XElement("Audit"));
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 64 * 1024 * 1024
            };
            XElement root;
            using (var reader = XmlReader.Create(path, settings)) root = XElement.Load(reader);
            if (root.Name != "Visep" || (string)root.Attribute("SchemaVersion") != "1")
                throw new InvalidDataException("Unsupported data schema version.");
            foreach (var section in new[] { "Users", "Clients", "Incidents", "Audit" })
                if (root.Elements(section).Count() != 1) throw new InvalidDataException("Expected one data section: " + section);
            return root;
        }

        void Save(XElement root)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    root.Save(stream);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
