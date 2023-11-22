using System.Security.Cryptography;
using System.Text;

public class DirectoryEntry {
    /// <summary>
    /// Initializer
    /// (<paramref name="path"/>, <paramref name="relativePath"/>)
    /// </summary>
    /// <param name="path">The absolute entry path</param>
    /// <param name="relativePath">The entry path relative to the working folder</param>
    public DirectoryEntry(string path, string relativePath) {
        this.path = path;
        fileInfo = new FileInfo(path);
        this.relativePath = relativePath;
    }
    public string path, relativePath;
    public FileInfo fileInfo;
    /// <summary>
    /// Function to check if it is a folder
    /// </summary>
    /// <returns>True if the entry is a folder</returns>
    public bool IsFolder() {
        return (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
    }
    /// <summary>
    /// Function to check if the entry is to be ignored
    /// </summary>
    /// <param name="allExtensions"></param>
    /// <param name="extensions"></param>
    /// <returns></returns>
    public bool IsToBeIgnored(bool allExtensions, string[] extensions) {
        if(!allExtensions) {
            if(!extensions.Contains(fileInfo.Extension)) return true;
        }
        return false;
    }
    /// <summary>
    /// Function to compute the file hash
    /// (<paramref name="allExtensions"/>, <paramref name="extensions"/>, <paramref name="hashAlgorithm"/>)
    /// </summary>
    /// <param name="allExtensions">If all extensions have to be checked for content differencies</param>
    /// <param name="extensions">The list of the extensions to check for content differencies</param>
    /// <param name="hashAlgorithm">The hash algorithm</param>
    /// <returns>Returns the computed hash, an empty string if it is a folder or null if its extension is not in the extension list</returns>
    public string? Hash(bool allExtensions, string[] extensions, HashAlgorithm hashAlgorithm) {
        // Check folder
        if(IsFolder()) return "";
        // Check extension
        if(IsToBeIgnored(allExtensions, extensions)) return null;
        // Read file and compute hash
        try {
            FileStream file = File.OpenRead(path);
            byte[] hashBytes = hashAlgorithm.ComputeHash(file);
            StringBuilder builder = new StringBuilder();
            foreach(byte b in hashBytes)
                builder.AppendFormat("{0:x2}", b);
            return builder.ToString();
        }
        catch(Exception e) {
            Logger.Error("Could not compute file hash, error: " + e);
            return "";
        }
    }
}