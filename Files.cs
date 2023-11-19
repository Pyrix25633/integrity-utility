using System.Security.Cryptography;
using System.Text;

public class DirectoryEntry {
    /// <summary>
    /// Initializer
    /// </summary>
    public DirectoryEntry(string path, string relativePath) {
        this.path = path;
        fileInfo = new FileInfo(path);
        this.relativePath = relativePath;
    }
    public string path, relativePath;
    public FileInfo fileInfo;
    // TODO!!!!
    /// <summary>
    /// Function to search in the destination folder for the same file and compare them
    /// (<paramref name="allExtensions"/>, <paramref name="extensions"/>, <paramref name="hashAlgorithm"/>)
    /// </summary>
    /// <param name="allExtensions">If all extensions have to be checked for content differencies</param>
    /// <param name="extensions">The list of the extensions to check for content differencies</param>
    /// <param name="hashAlgorithm">The hash algorithm</param>
    /// <returns>Returns true if the file has to be copied</returns>
    public string? Hash(bool allExtensions, string[] extensions, HashAlgorithm hashAlgorithm) {
        // Check folder
        if((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
            return null;
        }
        // Check extension
        if(!allExtensions) {
            if(!extensions.Contains(fileInfo.Extension)) return "";
        }
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