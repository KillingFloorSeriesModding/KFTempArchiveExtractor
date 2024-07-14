using System;
using System.IO;
using System.Linq;

namespace TempArchiveExtractor
{
    public class Program
    {
        private static string _baseExtractionPath = string.Empty;

        public static void Main(string[] args)
        {
            Console.WriteLine("KF Temp Archive Extractor 1.2");

            if (args.Length != 0)
            {
                OriginalBehaviour(args);
                return;
            }

            DirectoryInfo kf = new DirectoryInfo(GetKFInstallDir());
            FileInfo[] archiveFiles = kf.GetFiles().Where(x => x.Name.StartsWith("TempArchive")).ToArray();

            if (archiveFiles.Length == 0)
            {
                Console.WriteLine("No file to process...");
                return;
            }

            SetAndCreateBaseExtractionPath();

            foreach (FileInfo archiveFile in archiveFiles)
            {
                Process(archiveFile);
            }
        }

        private static void OriginalBehaviour(string[] args)
        {
            if (0 == args.Length || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.Out.WriteLine("Usage: TempArchiveExtractor [TempArchiveFile]");
                return;
            }

            SetAndCreateBaseExtractionPath();

            FileInfo archiveFile = new FileInfo(args[0]);
            if (!Process(archiveFile))
            {
                Console.WriteLine($"File {archiveFile.Name} incorrectly processed.");
            }
        }

        private static void SetAndCreateBaseExtractionPath()
        {
            _baseExtractionPath =
                Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "KF Archive Files");

            if (!Directory.Exists(_baseExtractionPath))
            {
                Console.WriteLine("Creating base extraction directory...");
                Directory.CreateDirectory(_baseExtractionPath);
            }
        }

        private static string GetKFInstallDir()
        {
            return Microsoft.Win32.Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 1250",
                "InstallLocation",
                null).ToString();
        }

        private static bool Process(FileInfo archiveFile)
        {
            Console.WriteLine();

            if (!archiveFile.Exists)
            {
                Console.Out.WriteLine("No such file.");
                return false;
            }

            Console.WriteLine($"Processing file {archiveFile.Name}...");

            FileStream archiveStream = null;
            try
            {
                archiveStream = archiveFile.OpenRead();
                TempArchiveReader reader = new TempArchiveReader(archiveStream);

                int fileCount = reader.ReadInt32();
                Console.Out.WriteLine($"Archive contains {fileCount} files.");

                DirectoryInfo archiveOwnFolder = new DirectoryInfo(
                    Path.Combine(
                        _baseExtractionPath,
                        archiveFile.Name));

                if (!archiveOwnFolder.Exists)
                {
                    Console.WriteLine($"Creating {archiveOwnFolder.Name} folder.");
                    archiveOwnFolder.Create();
                }

                for (int i = 0; i < fileCount; i++)
                {
                    string filePath = reader.ReadFString();
                    int fileLen = reader.ReadFCompactIndex();

                    FileInfo fi = new FileInfo(Path.Combine(archiveOwnFolder.FullName, filePath));

                    DirectoryInfo di = fi.Directory;
                    if (!di.Exists)
                    {
                        di.Create();
                    }

                    using (FileStream fs = fi.OpenWrite())
                    {
                        reader.ReadIntoStream(fileLen, fs);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error reading archive.");
                Console.Out.WriteLine(ex);
                return false;
            }
            finally
            {
                archiveStream?.Close();
            }

            Console.WriteLine($"{archiveFile.Name} processed successfully.");
            return true;
        }
    }
}
