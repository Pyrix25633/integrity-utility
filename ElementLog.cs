public class ElementLog {
    // TODO!!
    public ElementLog(string relativePath, LogType type) {
        this.relativePath = relativePath;
        this.type = type;
    }
    public string relativePath;
    public LogType type;
    public void Log() {
        switch(type) {
            case LogType.NEW_FILE:
                Logger.Success("New file: " + relativePath);
                break;
            case LogType.DELETED_FILE:
                Logger.Warning("Deleted file: " + relativePath);
                break;
            case LogType.DIFFERENT_HASH:
                Logger.Warning("Different hash for file: " + relativePath);
                break;
        }
    }
}

public enum LogType {
    NEW_FILE = 1,
    DELETED_FILE = 2,
    DIFFERENT_HASH = 3
}