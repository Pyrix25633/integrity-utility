public class DirectoryEntry {
    /// <summary>
    /// Initializer
    /// </summary>
    public DirectoryEntry(string path, string relativePath) {
        fileInfo = new FileInfo(path);
        this.relativePath = relativePath;
    }
    public string relativePath;
    public FileInfo fileInfo;
    public Reason reason;
    /// <summary>
    /// Function to search in the destination folder for the same file and compare them
    /// (<paramref name="destinationDictionary"/>, <paramref name="allExtensions"/>, <paramref name="extensions"/>)
    /// </summary>
    /// <param name="destinationDictionary">The dictionaty of all files in the destination folder</param>
    /// <param name="allExtensions">If all extensions have to be checked for content differencies</param>
    /// <param name="extensions">The list of the extensions to check for content differencies</param>
    /// <returns>Returns true if the file has to be copied</returns>
    public bool ToCopy(ref Dictionary<string, DirectoryEntry> destinationDictionary, bool allExtensions, string[] extensions) {
        DirectoryEntry e;
        try {e = destinationDictionary[relativePath];}
        catch(Exception) {
            // Does not exist
            reason = Reason.CopyNotThere;
            return true;
        }
        // Remove from destination dictionary to speed up the search for files to remove
        destinationDictionary.Remove(relativePath);
        // Skip if directory
        if((e.fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            return false;
        // Check size
        if(fileInfo.Length != e.fileInfo.Length) {
            reason = Reason.CopyDifferentSize;
            return true;
        }
        if(!allExtensions) {
            if(!extensions.Contains(fileInfo.Extension)) return false;
        }
        // Content differences
        try {
            FileStream stream1 = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read),
                    stream2 = new FileStream(e.fileInfo.FullName, FileMode.Open, FileAccess.Read);
            Int32 byte1, byte2;
            do {
                byte1 = stream1.ReadByte();
                byte2 = stream2.ReadByte();
            } while((byte1 == byte2) && (byte1 != -1));
            stream1.Close();
            stream2.Close();
            bool toCopy = (byte1 != byte2);
            if(toCopy) {
                reason = Reason.CopyDifferentContent;
            }
            return toCopy;
        }
        catch(Exception exc) {
            Logger.Error("Could not check the file " + relativePath + " for content differences, error: " + exc);
            return false;
        }
    }
    /// <summary>
    /// Function to know of a file has to be removed
    /// (<paramref name="sourceDictionary"/>)
    /// </summary>
    /// <param name="sourceDictionary">The dictionary of files in the source folder</param>
    /// <returns>Returns true if the file has to be removed</returns>
    public bool ToRemove(ref Dictionary<string, DirectoryEntry> sourceDictionary) {
        DirectoryEntry e;
        try {
            e = sourceDictionary[relativePath];
            return false;
        }
        catch(Exception) {
            reason = Reason.Remove;
            return true;
        }
    }
}

public enum Reason {
    CopyNotThere = 0,
    CopyDifferentSize = 1,
    CopyDifferentContent = 2,
    Remove = 3
}