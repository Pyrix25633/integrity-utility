using System;
using System.IO.Compression;
using Newtonsoft.Json;

public class Program {
    private static Arguments arguments = new Arguments();
    private static EnumerationOptions enumOptions = new EnumerationOptions();
    static async Task Main(string[] args) {
        // Version
        string version = "1.0.0";
        // Lists and dictionaries
        string[] pathList = new string[0], extensionList = new string[0];
        Dictionary<string, DirectoryEntry> pathInfoDictionary = new Dictionary<string, DirectoryEntry>();
        Dictionary<string, string> indexDictionary = new Dictionary<string, string>();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        // Other variables
        Int64 timestamp;
        Int32 sleepTime;
        // Parsing arguments
        try {
            arguments.Parse(args);
            if(arguments.help) return;
            if(arguments.errors != 0) {
                if(arguments.errors == 255)
                    Logger.Error("Missing arguments!");
                else
                    Logger.Error("Wrong arguments!");
                return;
            }
        }
        catch(Exception e) {
            Logger.Error("Exception parsing arguments: " + e);
            return;
        }
        // Initialize logger
        Logger.InitializeLogging(arguments.log);
        // Logging Info
        Logger.Info("Integrity utility " + version);
        // Source
        Logger.Info("Path: " + arguments.path);
        if(!Directory.Exists(arguments.path)) {
            Logger.Error("Directory does not exist!");
            return;
        }
        // Getting full path
        arguments.path = new FileInfo(arguments.path).FullName;
        // Algorithm
        Logger.Info("Hashing algorithm: " + arguments.algorithm);
        // Log
        if(arguments.log) Logger.Info("Logging to file");
        // Extensions
        if(arguments.extensions != null) {
            if(arguments.allExtensions) {
                Logger.Info("All extensions will be processed");
            }
            else {
                Logger.Info("File with the list of extensions to be processed: " + arguments.extensions);
                if(!File.Exists(arguments.extensions)) {
                    Logger.Error("File with the list of extensions to be processed does not exist!");
                    return;
                }
                try {
                    extensionList = File.ReadAllLines(arguments.extensions);
                    Logger.Success("Extension list retrieved from file");
                }
                catch(Exception e) {
                    Logger.Error("Could not retrieve extension list from file, error: " + e);
                    return;
                }
            }
        }
        else {
            Logger.Info("Extension list not set, every file will be processed");
        }
        // Threads
        Logger.Info("Number of threads: " + arguments.threads);
        // Compare
        if(arguments.compare != null) {
            Logger.Info("Compare: " + arguments.compare);
            if(!Directory.Exists(arguments.compare)) {
                Logger.Error("Directory does not exist!");
                return;
            }
        }
        while(true) {
            // Timestamp
            timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + arguments.delay;
            // Scan folders
            Logger.Info("Starting directory scan...");
            Task<string[]> pathTask = scanFolder(arguments.path, true);
            pathList = await pathTask;
            Logger.Success("Directory scanned: " + pathList.Length + " items found");
            // Build file info and load index
            Logger.Info("Building file info dictionary and loading index...");
            Task<Dictionary<string, DirectoryEntry>> sourceDictionaryTask = buildInfoDictionary(pathList, arguments.path, true);
            Task<Dictionary<string, string>> indexDictionaryTask = loadIndex(arguments.path);
            pathInfoDictionary = await sourceDictionaryTask;
            indexDictionary = await indexDictionaryTask;
            pathInfoDictionary.Remove("integrity-utility.index.json");
            Logger.Success("File info dictionary built and index loaded");
            foreach(KeyValuePair<string, string> indexEntry in indexDictionary) {
                Logger.Success(indexEntry.Key + ": " + indexEntry.Value);
            }
            // Close log stream
            Logger.TerminateLogging();
            if(!arguments.repeat) break;
            sleepTime = (Int32)(timestamp - new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
            if(sleepTime > 0) {
                Logger.Info("Waiting " + sleepTime + " seconds from now, process can be terminated with 'Ctrl + C' before the next scan");
                Thread.Sleep(sleepTime * 1000);
            }
            // Reopen log stream
            Logger.ReinitializeLogging();
        }
    }

    /// <summary>
    /// Function to get the list of files in a folder
    /// (<paramref name="path"/>, <paramref name="type"/>)
    /// </summary>
    /// <param name="path">The path</param>
    /// <param name="type">True if source folder, false if destination folder</param>
    /// <returns>Returns the task of a string array</returns>
    public static async Task<string[]> scanFolder(string path, bool type) {
        return await Task.Run<string[]>(() => {
            string[] array;
            try {
                array = Directory.EnumerateFileSystemEntries(path, "*", enumOptions).ToArray();
            }
            catch(Exception e) {
                Logger.Error("Error while scanning " + (type ? "source" : "destination") + " folder: " + e);
                Environment.Exit(1);
                array = new string[0];
            }
            return array;
        });
    }

    /// <summary>
    /// Function to get the list of files in a folder
    /// (<paramref name="path"/>, <paramref name="type"/>)
    /// </summary>
    /// <param name="path">The path</param>
    /// <param name="type">True if source folder, false if destination folder</param>
    /// <returns>Returns the task of a string array</returns>
    public static async Task<Dictionary<string, DirectoryEntry>> buildInfoDictionary(string[] list, string path, bool type) {
        return await Task.Run<Dictionary<string, DirectoryEntry>>(() => {
            Dictionary<string, DirectoryEntry> dictionary = new Dictionary<string, DirectoryEntry>();
            try {
                foreach(string item in list) {
                    string relativePath = item.Substring(path.Length + 1);
                    dictionary[relativePath] = new DirectoryEntry(item, relativePath);
                }
            }
            catch(Exception e) {
                Logger.Error("Error while building " + (type ? "source" : "destination") + " file info dictionary: " + e);
                Environment.Exit(2);
            }
            return dictionary;
        });
    }
    /// <summary>
    /// Function to load the index from the json file
    /// (<paramref name="path"/>)
    /// </summary>
    /// <param name="path">The folder path</param>
    /// <returns>Returns the task of a Dictionary</returns>
    public static async Task<Dictionary<string, string>> loadIndex(string path) {
        return await Task.Run<Dictionary<string, string>>(() => {
            Dictionary<string, string>? dictionary = new Dictionary<string, string>();
            try {
                string indexPath = Path.Join(path, "integrity-utility.index.json");
                if(File.Exists(indexPath)) {
                    string fileContent = File.ReadAllText(indexPath);
                    dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent);
                    if(dictionary == null) {
                        Logger.Warning("Unexpected index structure, using an empty index");
                        return new Dictionary<string, string>();
                    }
                }
                else {
                    Logger.Warning("Index not found, this is ok if it's the first scan");
                }
            }
            catch(Exception e) {
                Logger.Error("Error while loading index of folder " + path + " : " + e);
                Environment.Exit(2);
            }
            return dictionary;
        });
    }
}