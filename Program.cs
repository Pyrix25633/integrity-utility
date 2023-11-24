using System;
using System.IO.Compression;
using System.Security.Cryptography;
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
        Dictionary<string, Dictionary<string, string>> indexDictionary = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, Dictionary<string, string>> indexDictionaryCopy = new Dictionary<string, Dictionary<string, string>>();
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        // Other variables
        SemaphoreSlim indexSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim loggerSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim threadSemaphore;
        SemaphoreSlim progressSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim logsSemaphore = new SemaphoreSlim(1, 1);
        SemaphoreSlim finishedSemaphore = new SemaphoreSlim(0, 1);
        ElementLog[] pendingLogs = new ElementLog[0];
        Int64 timestamp, elementsTimestamp;
        Int32 sleepTime, currentElements, totalElements, filesToCompute, itemsComputed, foldersToCompute;
        UInt64 sizeToCompute, sizeComputed;
        Int32 newFiles, newFolders, differentHashes, deletedFiles, deletedFolders;
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
        // Path
        Logger.Info("Path: " + arguments.path);
        if(!Directory.Exists(arguments.path)) {
            Logger.Error("Directory does not exist!");
            return;
        }
        // Getting full path
        arguments.path = new FileInfo(arguments.path).FullName;
        // Algorithm
        Logger.Info("Hashing algorithm: " + arguments.algorithm);
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
        threadSemaphore = new SemaphoreSlim(arguments.threads, arguments.threads);
        // Update
        if(arguments.update) Logger.Info("Update option enabled, the index will be updated");
        else Logger.Info("Update option not enabled, the index will not be modified");
        // Skip
        if(arguments.skip) Logger.Info("Skip option enabled, hashes of files already in the index will not be calculated");
        // Compare
        if(arguments.compare != null) {
            Logger.Info("Compare to: " + arguments.compare);
            if(!Directory.Exists(arguments.compare)) {
                Logger.Error("Directory does not exist!");
                return;
            }
            else {
                // Getting full path
                arguments.path = new FileInfo(arguments.path).FullName;
            }
        }
        // Delay
        if(arguments.repeat) Logger.Info("Delay time: " + arguments.delay + "s");
        else Logger.Info("Delay time not set, program will exit when hash calculation will be finished");
        // Log
        if(arguments.log) Logger.Info("Logging to file");
        while(true) {
            // Timestamp
            timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + arguments.delay;
            // Scan folders
            Logger.Info("Starting directory scan...");
            Task<string[]> pathTask = scanFolder(arguments.path, true);
            pathList = await pathTask;
            Logger.Success("Directory scanned: " + pathList.Length + " items found");
            // Build file info and load index
            Logger.Info("Building file info dictionary and loading main index...");
            Task<Dictionary<string, DirectoryEntry>> pathInfoDictionaryTask = buildInfoDictionary(pathList, arguments.path);
            Task<Dictionary<string, string>> indexDictionaryTask = loadIndex(arguments.path, arguments.algorithm, () => {
                Logger.Warning("Index not found for folder " + arguments.path + ", this is ok if it's the first scan");
            });
            pathInfoDictionary = await pathInfoDictionaryTask;
            indexDictionary[""] = await indexDictionaryTask;
            indexDictionaryCopy[""] = new Dictionary<string, string>(indexDictionary[""]);
            pathList = new string[0];
            Logger.Success("File info dictionary built and main index loaded");
            Logger.Info("Searching and loading subfolders indexes");
            currentElements = 0; totalElements = pathInfoDictionary.Count;
            filesToCompute = 0; foldersToCompute = 0; sizeToCompute = 0;
            elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            Logger.ProgressBarItemsOnly(currentElements, totalElements);
            foreach(KeyValuePair<string, DirectoryEntry> entry in pathInfoDictionary) {
                DirectoryEntry value = entry.Value;
                Int64 currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                if(currentTimestamp - elementsTimestamp >= 100) {
                    Logger.RemoveLine();
                    Logger.ProgressBarItemsOnly(currentElements, totalElements);
                    elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                }
                if(value.IsFolder()) {
                    // Should contain an index
                    indexDictionary[value.relativePath] = await loadIndex(value.path, arguments.algorithm, () => {
                        Logger.RemoveLine();
                        Logger.Warning("Index not found for folder " + entry.Key + ", this is ok if it's the first scan");
                        Logger.ProgressBarItemsOnly(currentElements, totalElements);
                    });
                    if(arguments.compare == null) {
                        indexDictionaryCopy[value.relativePath] = new Dictionary<string, string>(indexDictionary[value.relativePath]);
                        string? directoryName = Path.GetDirectoryName(value.relativePath);
                        directoryName = directoryName == null ? "" : directoryName;
                        try {
                            // Remove it from the index
                            indexDictionaryCopy[directoryName].Remove(value.fileInfo.Name);
                        }
                        catch(Exception) {}
                        if(arguments.skip) {
                            try {
                                // Is in index and must be skipped
                                string hash = indexDictionary[directoryName][value.fileInfo.Name];
                                pathInfoDictionary.Remove(entry.Key);
                            }
                            catch(Exception) {
                                foldersToCompute++;
                            }
                        }
                        else
                            foldersToCompute++;
                    }
                }
                else if(arguments.compare == null) {
                    if(value.fileInfo.Name == "integrity-utility.index." + arguments.algorithm + ".json") {
                        // It's an index and must be skipped
                        pathInfoDictionary.Remove(entry.Key);
                    }
                    else if(arguments.skip) {
                        try {
                            string? directoryName = Path.GetDirectoryName(value.relativePath);
                            directoryName = directoryName == null ? "" : directoryName;
                            string hash = indexDictionary[directoryName][value.fileInfo.Name];
                            // Is in index and must be skipped
                            pathInfoDictionary.Remove(entry.Key);
                            // Remove it from the index
                            indexDictionaryCopy[directoryName].Remove(value.fileInfo.Name);
                        }
                        catch(Exception) {
                            // Is not in index
                            sizeToCompute += (UInt64)value.fileInfo.Length;
                            filesToCompute++;
                        }
                    }
                    else if(value.IsToBeIgnored(arguments.allExtensions, extensionList)) {
                        pathInfoDictionary.Remove(entry.Key);
                        try {
                            string? directoryName = Path.GetDirectoryName(value.relativePath);
                            directoryName = directoryName == null ? "" : directoryName;
                            // Remove it from the index
                            indexDictionaryCopy[directoryName].Remove(value.fileInfo.Name);
                        }
                        catch(Exception) {}
                    }
                    else {
                        sizeToCompute += (UInt64)value.fileInfo.Length;
                        filesToCompute++;
                        try {
                            string? directoryName = Path.GetDirectoryName(value.relativePath);
                            directoryName = directoryName == null ? "" : directoryName;
                            // Remove it from the index
                            indexDictionaryCopy[directoryName].Remove(value.fileInfo.Name);
                        }
                        catch(Exception) {}
                    }
                }
                currentElements++;
            }
            Logger.RemoveLine();
            if(arguments.compare != null) {
                Logger.Info("Comparing to " + arguments.compare + "...");
                Logger.Info("Starting directory scan...");
                Task<string[]> compareTask = scanFolder(arguments.compare, true);
                string [] compareList = await compareTask;
                Logger.Success("Directory scanned: " + compareList.Length + " items found");
                // Build file info and load index
                Logger.Info("Building file info dictionary and loading main index...");
                Task<Dictionary<string, DirectoryEntry>> compareInfoDictionaryTask = buildInfoDictionary(compareList, arguments.compare);
                Task<Dictionary<string, string>> compareDictionaryTask = loadIndex(arguments.compare, arguments.algorithm, () => {
                    Logger.Warning("Index not found for folder " + arguments.compare + ", this is ok if it's the first scan");
                });
                Dictionary<string, DirectoryEntry> compareInfoDictionary = await compareInfoDictionaryTask;
                Dictionary<string, Dictionary<string, string>> compareDictionary = new Dictionary<string, Dictionary<string, string>>();
                compareDictionary[""] = await compareDictionaryTask;
                Logger.Success("File info dictionary built and main index loaded");
                Logger.Info("Searching and loading subfolders indexes");
                currentElements = 0; totalElements = compareInfoDictionary.Count;
                filesToCompute = 0; foldersToCompute = 0; sizeToCompute = 0;
                elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                Logger.ProgressBarItemsOnly(currentElements, totalElements);
                foreach(KeyValuePair<string, DirectoryEntry> entry in compareInfoDictionary) {
                    DirectoryEntry value = entry.Value;
                    Int64 currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    if(currentTimestamp - elementsTimestamp >= 100) {
                        Logger.RemoveLine();
                        elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                        Logger.ProgressBarItemsOnly(currentElements, totalElements);
                    }
                    if(value.IsFolder()) {
                        // Should contain an index
                        compareDictionary[value.relativePath] = await loadIndex(value.path, arguments.algorithm, () => {
                            Logger.RemoveLine();
                            Logger.Warning("Index not found for folder " + entry.Key + ", this is ok if it's the first scan");
                            Logger.ProgressBarItemsOnly(currentElements, totalElements);
                        });
                    }
                    currentElements++;
                }
                Logger.RemoveLine();
                Logger.Success("Subfolder indexes loaded");
                // Compare indexes
                Logger.Info("Comparing indexes...");
                currentElements = 0; totalElements = indexDictionary.Count;
                newFiles = 0; newFolders = 0; differentHashes = 0;
                elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                Logger.ProgressBarItemsOnly(currentElements, totalElements);
                foreach(KeyValuePair<string, Dictionary<string, string>> folder in indexDictionary) {
                    foreach(KeyValuePair<string, string> item in folder.Value) {
                        Int64 currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                        if(currentTimestamp - elementsTimestamp >= 100) {
                            Logger.RemoveLine();
                            foreach(ElementLog log in pendingLogs)
                                log.Log();
                            pendingLogs = new ElementLog[0];
                            elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                            Logger.ProgressBarItemsOnly(currentElements, totalElements);
                        }
                        try {
                            string hash = compareDictionary[folder.Key][item.Key];
                            if(hash != item.Value) {
                                // Different Hash
                                pendingLogs = pendingLogs.Append(new ElementLog(Path.Join(folder.Key, item.Key),
                                    LogType.DIFFERENT_HASH)).ToArray();
                                differentHashes++;
                            }
                        }
                        catch(Exception) {
                            // New file/folder
                            LogType type;
                            if(item.Value == "") {
                                type = LogType.NEW_FOLDER;
                                newFolders++;
                            }
                            else {
                                type = LogType.NEW_FILE;
                                newFiles++;
                            }
                            pendingLogs = pendingLogs.Append(new ElementLog(Path.Join(folder.Key, item.Key), type)).ToArray();
                        }
                        try {
                            compareDictionary[folder.Key].Remove(item.Key);
                        }
                        catch(Exception) {}
                        currentElements++;
                    }
                }
                Logger.RemoveLine();
                foreach(ElementLog log in pendingLogs)
                    log.Log();
                pendingLogs = new ElementLog[0];
                deletedFiles = 0; deletedFolders = 0;
                foreach(KeyValuePair<string, Dictionary<string, string>> folder in compareDictionary) {
                    foreach(KeyValuePair<string, string> item in folder.Value) {
                        LogType type;
                        if(item.Value == "") {
                            type = LogType.DELETED_FOLDER;
                            deletedFolders++;
                        }
                        else {
                            type = LogType.DELETED_FILE;
                            deletedFiles++;
                        }
                        new ElementLog(Path.Join(folder.Key, item.Key), type).Log();
                    }
                }
                Logger.Success("Detected " + newFolders + " new folder" + (newFolders == 1 ? "" : "s") + " and " +
                    newFiles + " new file" + (newFiles == 1 ? "" : "s") + ", " +
                    differentHashes + " different hash" + (differentHashes == 1 ? "" : "es, ") +
                    deletedFolders + " deleted folder" + (deletedFolders == 1 ? "" : "s") + " and " +
                    deletedFiles + " deleted file" + (deletedFiles == 1 ? "" : "s"));
                // Releasing useless instances
                pathInfoDictionary = new Dictionary<string, DirectoryEntry>();
                indexDictionary = new Dictionary<string, Dictionary<string, string>>();
                indexDictionaryCopy = new Dictionary<string, Dictionary<string, string>>();
                compareDictionary = new Dictionary<string, Dictionary<string, string>>();
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
            else {
                Logger.Success("Subfolder indexes loaded, " + foldersToCompute + " folder" + (foldersToCompute == 1 ? "" : "s") +
                    " and " + filesToCompute + " file" + (filesToCompute == 1 ? "" : "s") + " to compute (" +
                    Logger.HumanReadableSize(sizeToCompute)+ ")");
                itemsComputed = 0; sizeComputed = 0;
                newFiles = 0; newFolders = 0; differentHashes = 0;
                elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                Logger.ProgressBar(sizeComputed, sizeToCompute, itemsComputed, filesToCompute + foldersToCompute);
                if(pathInfoDictionary.Count == 0) finishedSemaphore.Release();
                foreach(KeyValuePair<string, DirectoryEntry> entry in pathInfoDictionary) {
                    await threadSemaphore.WaitAsync();
                    new Thread(async () => {
                        DirectoryEntry value = entry.Value;
                        Int64 currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                        await progressSemaphore.WaitAsync();
                        if(currentTimestamp - elementsTimestamp >= 100) {
                            await loggerSemaphore.WaitAsync();
                            Logger.RemoveLine();
                            await logsSemaphore.WaitAsync();
                            foreach(ElementLog log in pendingLogs)
                                log.Log();
                            pendingLogs = new ElementLog[0];
                            elementsTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                            logsSemaphore.Release();
                            Logger.ProgressBar(sizeComputed, sizeToCompute, itemsComputed, filesToCompute + foldersToCompute);
                            progressSemaphore.Release();
                            loggerSemaphore.Release();
                        }
                        else progressSemaphore.Release();
                        string? hash = entry.Value.Hash(arguments.allExtensions, extensionList, arguments.GetHashAlgorithmCopy());
                        if(hash == null) {
                            threadSemaphore.Release();
                            return;
                        }
                        string? directoryName = Path.GetDirectoryName(value.relativePath);
                        directoryName = directoryName == null ? "" : directoryName;
                        try {
                            string previousHash;
                            await indexSemaphore.WaitAsync();
                            previousHash = indexDictionary[directoryName][value.fileInfo.Name];
                            if(hash != previousHash) {
                                // Different hash
                                indexDictionary[directoryName][value.fileInfo.Name] = hash;
                                indexSemaphore.Release();
                                await logsSemaphore.WaitAsync();
                                pendingLogs = pendingLogs.Append(new ElementLog(value.relativePath, LogType.DIFFERENT_HASH)).ToArray();
                                differentHashes++;
                                logsSemaphore.Release();
                            }
                            else indexSemaphore.Release();
                        }
                        catch(Exception) {
                            // New file/folder
                            indexSemaphore.Release();
                            await indexSemaphore.WaitAsync();
                            indexDictionary[directoryName][value.fileInfo.Name] = hash;
                            indexSemaphore.Release();
                            await logsSemaphore.WaitAsync();
                            LogType type;
                            if(hash == "") {
                                type = LogType.NEW_FOLDER;
                                newFolders++;
                            }
                            else {
                                type = LogType.NEW_FILE;
                                newFiles++;
                            }
                            pendingLogs = pendingLogs.Append(new ElementLog(value.relativePath, type)).ToArray();
                            logsSemaphore.Release();
                        }
                        threadSemaphore.Release();
                        await progressSemaphore.WaitAsync();
                        itemsComputed++;
                        if(!value.IsFolder())
                            sizeComputed += (UInt64)value.fileInfo.Length;
                        if(itemsComputed == filesToCompute + foldersToCompute)
                            finishedSemaphore.Release();
                        progressSemaphore.Release();
                    }).Start();
                }
                await finishedSemaphore.WaitAsync();
                Logger.RemoveLine();
                foreach(ElementLog log in pendingLogs)
                    log.Log();
                pendingLogs = new ElementLog[0];
                deletedFiles = 0; deletedFolders = 0;
                foreach(KeyValuePair<string, Dictionary<string, string>> folder in indexDictionaryCopy) {
                    foreach(KeyValuePair<string, string> item in folder.Value) {
                        LogType type;
                        if(item.Value == "") {
                            type = LogType.DELETED_FOLDER;
                            deletedFolders++;
                        }
                        else {
                            type = LogType.DELETED_FILE;
                            deletedFiles++;
                        }
                        new ElementLog(Path.Join(folder.Key, item.Key), type).Log();
                        indexDictionary[folder.Key].Remove(item.Key);
                    }
                }
                Logger.Success("Detected " + newFolders + " new folder" + (newFolders == 1 ? "" : "s") + " and " +
                    newFiles + " new file" + (newFiles == 1 ? "" : "s") + ", " +
                    differentHashes + " different hash" + (differentHashes == 1 ? "" : "es, ") +
                    deletedFolders + " deleted folder" + (deletedFolders == 1 ? "" : "s") + " and " +
                    deletedFiles + " deleted file" + (deletedFiles == 1 ? "" : "s"));
                if(arguments.update) {
                    Logger.Info("Updating the index...");
                    foreach(KeyValuePair<string, Dictionary<string, string>> folder in indexDictionary) {
                        try {
                            string index = JsonConvert.SerializeObject(folder.Value, Formatting.Indented);
                            StreamWriter writer = File.CreateText(Path.Join(arguments.path, folder.Key,
                                "integrity-utility.index." + arguments.algorithm + ".json"));
                            writer.WriteLine(index);
                            writer.Close();
                            Logger.Success("Updated index for folder " + (folder.Key != "" ? folder.Key : arguments.path));
                        }
                        catch(Exception e) {
                            Logger.Error("Could not update the index for folder " + folder.Key + ", error: " + e);
                        }
                    }
                    Logger.Success("Index update finished");
                }
                // Releasing useless instances
                pathList = new string[0];
                pathInfoDictionary = new Dictionary<string, DirectoryEntry>();
                indexDictionary = new Dictionary<string, Dictionary<string, string>>();
                indexDictionaryCopy = new Dictionary<string, Dictionary<string, string>>();
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
    /// <returns>Returns the task of a string array</returns>
    public static async Task<Dictionary<string, DirectoryEntry>> buildInfoDictionary(string[] list, string path) {
        return await Task.Run<Dictionary<string, DirectoryEntry>>(() => {
            Dictionary<string, DirectoryEntry> dictionary = new Dictionary<string, DirectoryEntry>();
            try {
                foreach(string item in list) {
                    string relativePath = item.Substring(path.Length + 1);
                    dictionary[relativePath] = new DirectoryEntry(item, relativePath);
                }
            }
            catch(Exception e) {
                Logger.Error("Error while building " + " file info dictionary: " + e);
                Environment.Exit(2);
            }
            return dictionary;
        });
    }
    /// <summary>
    /// Function to load the index from the json file
    /// (<paramref name="path"/>, <paramref name="algorithm"/>)
    /// </summary>
    /// <param name="path">The folder path</param>
    /// <param name="algorithm">The algorithm argument</param>
    /// <returns>Returns the task of a Dictionary</returns>
    public static async Task<Dictionary<string, string>> loadIndex(string path, string algorithm, Action fails) {
        return await Task.Run<Dictionary<string, string>>(() => {
            Dictionary<string, string>? dictionary = new Dictionary<string, string>();
            try {
                string indexPath = Path.Join(path, "integrity-utility.index." + algorithm + ".json");
                if(File.Exists(indexPath)) {
                    string fileContent = File.ReadAllText(indexPath);
                    dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent);
                    if(dictionary == null) {
                        Logger.Warning("Unexpected index structure, using an empty index");
                        return new Dictionary<string, string>();
                    }
                }
                else {
                    fails();
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