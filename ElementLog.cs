public class ElementLog {
    /// <summary>
    /// Initializer
    /// (<paramref name="relativePath"/>, <paramref name="type"/>)
    /// </summary>
    /// <param name="relativePath">The relative path</param>
    /// <param name="type">The log type</param>
    public ElementLog(string relativePath, LogType type) {
        this.relativePath = relativePath;
        this.type = type;
    }
    public string relativePath;
    public LogType type;
    /// <summary>
    /// Function to print it to console
    /// </summary>
    public void Log() {
        switch(type) {
            case LogType.NEW_FILE:
                Logger.Success("Detected new file: " + relativePath);
                break;
            case LogType.NEW_FOLDER:
                Logger.Success("Detected new folder: " + relativePath);
                break;
            case LogType.DELETED_FILE:
                Logger.Warning("Detected deleted file: " + relativePath);
                break;
            case LogType.DELETED_FOLDER:
                Logger.Warning("Detected deleted folder: " + relativePath);
                break;
            case LogType.DIFFERENT_HASH:
                Logger.Warning("Detected different hash for file: " + relativePath);
                break;
        }
    }
}

public enum LogType {
    NEW_FILE = 1,
    NEW_FOLDER = 2,
    DELETED_FILE = 3,
    DELETED_FOLDER = 4,
    DIFFERENT_HASH = 5
}