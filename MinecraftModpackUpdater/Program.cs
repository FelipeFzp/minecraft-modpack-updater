using MinecraftModpackUpdater.Extensions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Web;

class Program
{
    private static readonly string UNZIP_TEMP_FOLDER_NAME = "temp-unzipped";
    private static async Task Main(string[] args)
    {
        //https://www.dropbox.com/s/m47lu79rqexfjag/Forge%20QuickfastModded.zip?dl=1
        try
        {
            ShowWelcomeMessage();
            var modpackUrl = GetModpackUrl();
            var modpackFileStream = await DownloadModpack(modpackUrl);

            var (unzippedModpackPath, modpackName) = ExtractModpackToVersionsFolder(modpackFileStream);
            BackupOldModpack(modpackName);
            CopyNewModpack(unzippedModpackPath, modpackName);
            CleanupFiles();
            ShowFinishMessage(modpackName);
        }
        catch (Exception e)
        {
            Console.WriteLine("\r\n[ERROR] Unexpected error: " + e.Message);
            Console.WriteLine("\r\n[ERROR] Press any key to close program");
            Console.ReadKey();
            throw;
        }
    }

    //STEPS
    private static void ShowWelcomeMessage()
    {
        Console.WriteLine("========================================================\r\n" +
            "Welcome to Modpack Uploader (by: Felipe Zaniol)\r\n" +
            "This software will download the lastest version of Quickfast modpack and paste on your minecraft folder\r\n" +
            "========================================================");
        Console.WriteLine();
    }

    private static async Task<FileStream> DownloadModpack(string modpackUrl)
    {
        var modpackFileStream = File.Create($"quickfast-temp.zip");
        var finished = false;
        await new HttpClient() { Timeout = TimeSpan.FromHours(1) }
            .DownloadAsync(modpackUrl, modpackFileStream, new Progress<float>((value) =>
            {
                if (finished) return;

                var progress = Math.Round(value * 100, 0);

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"\r[DOWNLOAD] Downloading modpack, please wait... {progress}%");
                if(progress == 100)
                {
                    Console.SetCursorPosition(0, Console.CursorTop + 1);
                    finished = true;
                };
            }));


        return modpackFileStream;
    }

    private static void BackupOldModpack(string modpackName)
    {
        var versionsDirectory = GetMinecraftVersionsDirectory(modpackName);
        if (!Directory.Exists(versionsDirectory)) return;

        Console.WriteLine("[BACKUP] Creating backup of older version...");
        var bkpFolderPrefix = $"{DateTime.Now.Hour}h{DateTime.Now.Minute}m_{DateTime.Now.Day}{DateTime.Now.Month}{DateTime.Now.Year}_";
        var bkpFolderName = bkpFolderPrefix + modpackName;
        var bkpFolderPath = GetMinecraftVersionsDirectory(bkpFolderName);

        Console.WriteLine($"[BACKUP] Backup done, older version is available at \"{bkpFolderPath}\"");
        Directory.Move(versionsDirectory, bkpFolderPath);
    }

    private static void CopyNewModpack(string unzippedModpackPath, string modpackName)
    {
        Console.WriteLine("[COPYING] Copying new modpack files to versions folder...");
        var versionsDirectory = GetMinecraftVersionsDirectory(modpackName);
        if (Directory.Exists(versionsDirectory)) throw new Exception("Conflicting names copying modpack to folder, cancelling copy to avoid override versions without backup!!!");

        Directory.Move(unzippedModpackPath, versionsDirectory);
        Console.WriteLine($"[COPYING] New modpack is available at {versionsDirectory}");
    }

    private static void CleanupFiles()
    {
        Console.WriteLine($"[CLEANING] Cleaning remaining download files...");
        Directory.Delete(GetMinecraftVersionsDirectory(UNZIP_TEMP_FOLDER_NAME));
    }

    private static void ShowFinishMessage(string modpackName)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("========================================================\r\n" +
             "Thank you to use Modpack Uploader to update our server\r\n" +
            $"Now on TLauncher you should use {modpackName} profile\r\n" +
             "========================================================");
        Console.ReadKey(false);
    }


    //UTILITIES
    private static (string unzippedModpackPath, string modpackName) ExtractModpackToVersionsFolder(FileStream modpackFileStream)
    {
        var extractToPath = GetMinecraftVersionsDirectory(UNZIP_TEMP_FOLDER_NAME);
        if (Directory.Exists(extractToPath))
            Directory.Delete(extractToPath, true);

        var modpackZip = new ZipArchive(modpackFileStream, ZipArchiveMode.Read, false);
        modpackZip.ExtractToDirectory(extractToPath);

        var modpackName = modpackZip.Entries.First().FullName.Split("/")[0];
        return (extractToPath+ modpackName, modpackName);
    }
    private static string GetModpackUrl()
    {
        Console.WriteLine("[PREPARATION] Please enter modpack url (should start with: \"https://www.dropbox.com/\")");

        var modpackUrl = Console.ReadLine();
        Console.WriteLine();
        if (string.IsNullOrEmpty(modpackUrl) || !modpackUrl.StartsWith("https://www.dropbox.com/"))
        {
            Console.WriteLine("[PREPARATION] Invalid url");
            return GetModpackUrl();
        }

        return modpackUrl;
    }
    private static string GetMinecraftVersionsDirectory(string fileName = "")
    {
        var appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var versionsFolderPath = $"{appDataFolderPath}\\.minecraft\\versions\\{fileName}\\";

        return versionsFolderPath;
    }
}