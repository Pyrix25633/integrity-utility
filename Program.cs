using System;
using System.IO.Compression;

public class Program {
    private static Arguments arguments = new Arguments();
    private static EnumerationOptions enumOptions = new EnumerationOptions();
    static async Task Main(string[] args) {
        // Version
        string version = "1.6.1";
        // Lists and dictionaries
        string[] sourceList = new string[0], destinationList = new string[0], extensionList = new string[0];
        Dictionary<string, DirectoryEntry> sourceInfoDictionary = new Dictionary<string, DirectoryEntry>();
        Dictionary<string, DirectoryEntry> destinationInfoDictionary = new Dictionary<string, DirectoryEntry>();
        DirectoryEntry[] toCopyList = new DirectoryEntry[0], toRemoveFileList = new DirectoryEntry[0], toRemoveFolderList = new DirectoryEntry[0];
        enumOptions.RecurseSubdirectories = true; enumOptions.AttributesToSkip = default;
        // Other variables
        Int32 length, filesToCopy, filesCopied, foldersToCopy, foldersCopied,
            filesToRemove, filesRemoved, foldersToRemove, foldersRemoved, sleepTime;
        UInt64 sizeToCopy, sizeCopied, sizeToRemove, sizeRemoved;
        Int64 timestamp;
        string backupFolder = "";
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
        Logger.Info("Backup utility " + version);
        // Source
        Logger.Info("Source folder: " + arguments.source);
        if(!Directory.Exists(arguments.source)) {
            Logger.Error("Source folder does not exist!");
            return;
        }
        // Destination
        Logger.Info("Destination folder: " + arguments.destination);
        if(!Directory.Exists(arguments.destination)) {
            Logger.Warning("Destination folder does not exist, attempting creation");
            try {
                Directory.CreateDirectory(arguments.destination);
                Logger.Success("Destination directory created");
            }
            catch(Exception e) {
                Logger.Error("Destination directory creation failed, error: " + e);
                return;
            }
        }
        // Getting full path
        arguments.source = new FileInfo(arguments.source).FullName;
        arguments.destination = new FileInfo(arguments.destination).FullName;
        // Removed
        if(arguments.removed != null) {
            Logger.Info("Folder for removed files: " + arguments.removed);
            if(!Directory.Exists(arguments.removed)) {
                Logger.Warning("Folder for removed files does not exist, attempting creation");
                try {
                    Directory.CreateDirectory(arguments.removed);
                    Logger.Success("Directory for removed files created");
                }
                catch(Exception e) {
                    Logger.Error("Directory for removed files creation failed, error: " + e);
                    return;
                }
            }
            // Getting full path
            if(Directory.Exists(arguments.removed)) arguments.removed = new FileInfo(arguments.removed).FullName;
        }
        else Logger.Info("Folder for removed files is not set, they will be permanently removed");
        // Delay time
        if(arguments.repeat) Logger.Info("Delay time: " + arguments.time.ToString() + "s");
        else Logger.Info("Delay time not set, program will exit when backup will be finished");
        // Log
        if(arguments.log) Logger.Info("Logging to file");
        // Extensions
        if(arguments.extensions != null) {
            if(arguments.allExtensions) {
                Logger.Info("All extensions will be checked for content changes");
            }
            else {
                Logger.Info("File with the list of extensions to check for content changes: " + arguments.extensions);
                if(!File.Exists(arguments.extensions)) {
                    Logger.Error("File with the list of extensions to check for content changes does not exist!");
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
            Logger.Info("Extension list not set, only file size will be used to compare files");
        }
        // Compressed Backup
        if(arguments.backup) {
            Logger.Info("Compressed backup: yes");
            backupFolder = arguments.destination + "-backups";
            if(!Directory.Exists(backupFolder)) {
                Logger.Warning("Folder for backups " + backupFolder + " does not exist, attempting creation");
                try {
                    Directory.CreateDirectory(backupFolder);
                    Logger.Success("Created folder for backups");
                }
                catch(Exception e) {
                    Logger.Error("Could not create folder for backups, error: " + e);
                }
            }
        }
        else {
            Logger.Info("Compressed backup: no");
        }
        while(true) {
            // Timestamp
            timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + arguments.time;
            // Scan folders
            Logger.Info("Starting source and destination folders scan...");
            Task<string[]> sourceTask = scanFolder(arguments.source, true);
            Task<string[]> destinationTask = scanFolder(arguments.destination, false);
            sourceList = await sourceTask;
            destinationList = await destinationTask;
            Logger.Success("Source and destination folders scanned: " + sourceList.Length + " and " + destinationList.Length + " items found");
            // Build file info
            Logger.Info("Building source and destination file info dictionaries...");
            Task<Dictionary<string, DirectoryEntry>> sourceDictionaryTask = buildInfoDictionary(sourceList, arguments.source, true);
            Task<Dictionary<string, DirectoryEntry>> destinationDictionaryTask = buildInfoDictionary(destinationList, arguments.destination, false);
            sourceInfoDictionary = await sourceDictionaryTask;
            destinationInfoDictionary = await destinationDictionaryTask;
            Logger.Success("Source and destination file info dictionaries built");
            sourceList = new string[0];
            destinationList = new string[0];
            // Items to copy
            Logger.Info("Determining items to copy...");
            filesToCopy = 0; foldersToCopy = 0; sizeToCopy = 0;
            foreach(KeyValuePair<string, DirectoryEntry> entry in sourceInfoDictionary) {
                DirectoryEntry value = entry.Value;
                if(value.ToCopy(ref destinationInfoDictionary, arguments.allExtensions, extensionList)) {
                    toCopyList = toCopyList.Append(value).ToArray();
                    if((value.fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                        foldersToCopy++;
                    }
                    else {
                        filesToCopy++;
                        sizeToCopy += (UInt64)value.fileInfo.Length;
                    }
                }
            }
            Logger.Success(foldersToCopy.ToString() + " folder" + (foldersToCopy == 1 ? "" : "s") + " and " +
                filesToCopy.ToString() + " file" + (filesToCopy == 1 ? "" : "s") + " to copy (" +
                Logger.HumanReadableSize(sizeToCopy) + ")");
            // Items to remove
            Logger.Info("Determining items to remove...");
            filesToRemove = 0; foldersToRemove = 0; sizeToRemove = 0;
            foreach(KeyValuePair<string, DirectoryEntry> entry in destinationInfoDictionary) {
                DirectoryEntry value = entry.Value;
                if(value.ToRemove(ref sourceInfoDictionary)) {
                    if((value.fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                        toRemoveFolderList = toRemoveFolderList.Append(value).ToArray();
                        foldersToRemove++;
                    }
                    else {
                        toRemoveFileList = toRemoveFileList.Append(value).ToArray();
                        filesToRemove++;
                        sizeToRemove += (UInt64)value.fileInfo.Length;
                    }
                }
            }
            Logger.Success(foldersToRemove.ToString() + " folder" + (foldersToRemove == 1 ? "" : "s") + " and " +
                filesToRemove.ToString() + " file" + (filesToRemove == 1 ? "" : "s") + " to remove (" +
                Logger.HumanReadableSize(sizeToRemove) + ")");
            // Clear info lists
            sourceInfoDictionary = new Dictionary<string, DirectoryEntry>();
            destinationInfoDictionary = new Dictionary<string, DirectoryEntry>();
            // Copy files
            length = toCopyList.Length;
            filesCopied = 0; foldersCopied = 0; sizeCopied = 0;
            for(Int32 i = 0; i < length; i++) {
                DirectoryEntry e = toCopyList[i];
                bool isDirectory = (e.fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                UInt64 fileSize = (isDirectory ? 0 : (UInt64)e.fileInfo.Length);
                Logger.InfoReason(e.reason, e.relativePath, (isDirectory ? null : fileSize));
                Logger.ProgressBar(sizeCopied, sizeToCopy, foldersCopied + filesCopied, foldersToCopy + filesToCopy);
                string destinationPath = arguments.destination + Path.DirectorySeparatorChar + e.relativePath;
                if(isDirectory) { // Copy folder
                    try {
                        Directory.CreateDirectory(destinationPath);
                        foldersCopied++;
                        Logger.RemoveLine(); Logger.RemoveLine();
                        Logger.SuccessReason(e.reason, e.relativePath);
                    }
                    catch(Exception exc) {
                        foldersToCopy--;
                        Logger.RemoveLine(); Logger.RemoveLine();
                        Logger.Error("Could not copy: " + e.relativePath + ", error: " + exc);
                    }
                }
                else { // Copy file
                    try {
                        File.Copy(e.fileInfo.FullName, destinationPath, true);
                        filesCopied++;
                        sizeCopied += fileSize;
                        Logger.RemoveLine(); Logger.RemoveLine();
                        Logger.SuccessReason(e.reason, e.relativePath, fileSize);
                    }
                    catch(Exception exc) {
                        filesToCopy--;
                        try {sizeToCopy -= fileSize;}
                        catch(Exception) {}
                        Logger.RemoveLine(); Logger.RemoveLine();
                        Logger.Error("Could not copy: " + e.relativePath + ", error: " + exc);
                    }
                }
            }
            toCopyList = new DirectoryEntry[0];
            // Remove files
            length = toRemoveFileList.Length;
            filesRemoved = 0; sizeRemoved = 0; foldersRemoved = 0;
            for(Int32 i = 0; i < length; i++) {
                DirectoryEntry e = toRemoveFileList[i];
                UInt64 fileSize = (UInt64)e.fileInfo.Length;
                bool err;
                Logger.InfoReason(e.reason, e.relativePath, fileSize);
                Logger.ProgressBar(sizeRemoved, sizeToRemove, foldersRemoved + filesRemoved, foldersToRemove + filesToRemove);
                if(arguments.removed != null) { // Move
                    string newPath = arguments.removed + Path.DirectorySeparatorChar + e.relativePath;
                    try {
                        File.Move(e.fileInfo.FullName, newPath, true);
                        err = false;
                    }
                    catch(Exception exc1) {
                        try {
                            Directory.CreateDirectory(newPath.Substring(0, newPath.Length - e.fileInfo.Name.Length));
                            File.Move(e.fileInfo.FullName, newPath);
                            err = false;
                        }
                        catch(Exception exc2) {
                            Logger.RemoveLine(); Logger.RemoveLine();
                            Logger.Error("Could not remove: " + e.relativePath + ", error 1: " + exc1 + ", error 2: " + exc2);
                            err = true;
                        }
                    }
                }
                else { // Completely remove
                    try {
                        File.Delete(e.fileInfo.FullName);
                        err = false;
                    }
                    catch(Exception exc) {
                        Logger.RemoveLine(); Logger.RemoveLine();
                        Logger.Error("Could not remove: " + e.relativePath + ", error: " + exc);
                        err = true;
                    }
                }
                if(!err) {
                    sizeRemoved += fileSize;
                    filesRemoved++;
                    Logger.RemoveLine(); Logger.RemoveLine();
                    Logger.SuccessReason(e.reason, e.relativePath, fileSize);
                }
                else {
                    filesToRemove--;
                    try {sizeToRemove -= fileSize;}
                    catch(Exception) {}
                }
            }
            toRemoveFileList = new DirectoryEntry[0];
            // Remove folders
            length = toRemoveFolderList.Length;
            for(Int32 i = length - 1; i >= 0; i--) {
                DirectoryEntry e = toRemoveFolderList[i];
                bool err;
                Logger.InfoReason(e.reason, e.relativePath);
                if(arguments.removed != null) { // Move
                    string newPath = arguments.removed + Path.DirectorySeparatorChar + e.relativePath;
                    try {
                        Directory.CreateDirectory(newPath);
                        Directory.Delete(e.fileInfo.FullName, true);
                        err = false;
                    }
                    catch(Exception exc) {
                        Logger.RemoveLine();
                        Logger.Error("Could not remove: " + e.relativePath + ", error: " + exc);
                        err = true;
                    }
                }
                else { // Completely remove
                    try {
                        Directory.Delete(e.fileInfo.FullName, true);
                        err = false;
                    }
                    catch(Exception exc) {
                        Logger.RemoveLine();
                        Logger.Error("Could not remove: " + e.relativePath + ", error: " + exc);
                        err = true;
                    }
                }
                if(!err) {
                    foldersRemoved++;
                    Logger.RemoveLine();
                    Logger.SuccessReason(e.reason, e.relativePath);
                }
                else foldersToRemove--;
            }
            toRemoveFolderList = new DirectoryEntry[0];
            // Log copied and removed items
            Logger.Success(foldersCopied.ToString() + " folder" + (foldersRemoved == 1 ? "" : "s") + " and " +
                filesCopied.ToString() + " file" + (filesCopied == 1 ? "" : "s") + " copied (" + Logger.HumanReadableSize(sizeCopied) +
                "), " + foldersRemoved.ToString() + " folder" + (foldersRemoved == 1 ? "" : "s") + " and " +
                filesRemoved.ToString() + " file" + (filesRemoved == 1 ? "" : "s") + " removed (" + Logger.HumanReadableSize(sizeRemoved) +
                "), delta: " + (sizeCopied >= sizeRemoved ? "+" : "-") + Logger.HumanReadableSize((UInt64)Math.Abs((Int64)sizeCopied - (Int64)sizeRemoved)));
            // Compressed backup
            if(arguments.backup) {
                string backupFileName = Logger.LongTimeString() + ".zip";
                Logger.Info("Creating compressed backup: " + backupFileName);
                backupFileName = backupFolder + Path.DirectorySeparatorChar + backupFileName;
                try {
                    ZipFile.CreateFromDirectory(arguments.source, backupFileName);
                    Logger.Success("Created compressed backup (" + Logger.HumanReadableSize((UInt64)new FileInfo(backupFileName).Length) + ")");
                }
                catch(Exception e) {
                    Logger.Error("Could not create compressed backup, error: " + e);
                }
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
}