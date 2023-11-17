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
    // TODO!!!!
    /// <summary>
    /// Function to search in the destination folder for the same file and compare them
    /// (<paramref name="destinationDictionary"/>, <paramref name="allExtensions"/>, <paramref name="extensions"/>)
    /// </summary>
    /// <param name="destinationDictionary">The dictionaty of all files in the destination folder</param>
    /// <param name="allExtensions">If all extensions have to be checked for content differencies</param>
    /// <param name="extensions">The list of the extensions to check for content differencies</param>
    /// <returns>Returns true if the file has to be copied</returns>
    public string Hash(bool allExtensions, string[] extensions) {
        // Check folder
        if((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
            // TODO
        }
        // Check extension
        if(!allExtensions) {
            if(!extensions.Contains(fileInfo.Extension)) return "";
        }

    }
}