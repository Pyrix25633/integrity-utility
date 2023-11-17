using System.IO.Compression;

public class Logger {
    private static string barFull = "█", barEmpty = " ";
    private static string? logfilename;
    private static StreamWriter? logstream;
    /// <summary>
    /// Function to initialize the file logging
    /// (<paramref name="log"/>)
    /// </summary>
    /// <param name="log">If the logger has to log to file</param>
    public static void InitializeLogging(bool log) {
        if(!log) return;
        logfilename = LongTimeString() + ".log";
        IEnumerable<string> logfiles = Directory.EnumerateFiles("./", "*.log");
        foreach(string item in logfiles) {
            Compress(item);
        }
        logstream = File.CreateText(logfilename);
    }
    /// <summary>
    /// Function to reopen the log stream
    /// </summary>
    public static void ReinitializeLogging() {
        if(logfilename != null) logstream = new StreamWriter(logfilename, append: true);
    }
    /// <summary>
    /// Function to close the log stream
    /// </summary>
    public static void TerminateLogging() {
        if(logstream != null) {
            logstream.Close();
        }
    }
    /// <summary>
    /// Function to output a success message
    /// (<paramref name="message"/>)
    /// </summary>
    /// <param name="message">The message to output</param>
    public static void Success(string message) {
        string time = TimeString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(time);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("(Success) ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
        if(logstream != null) logstream.WriteLineAsync(time + "(Success) " + message);
    }
    /// <summary>
    /// Function to output an info message
    /// (<paramref name="message"/>)
    /// </summary>
    /// <param name="message">The message to output</param>
    public static void Info(string message) {
        string time = TimeString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(time);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("(Info) ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    /// <summary>
    /// Function to output a warning message
    /// (<paramref name="message"/>)
    /// </summary>
    /// <param name="message">The message to output</param>
    public static void Warning(string message) {
        string time = TimeString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(time);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("(Warning) ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
        if(logstream != null) logstream.WriteLineAsync(time + "(Warning) " + message);
    }
    /// <summary>
    /// Function to output an error message
    /// (<paramref name="message"/>)
    /// </summary>
    /// <param name="message">The message to output</param>
    public static void Error(string message) {
        string time = TimeString();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(time);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("(Error) ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
        if(logstream != null) logstream.WriteLineAsync(time + "(Error) " + message);
    }
    /// <summary>
    /// Function to clear the last console line
    ///(<paramref name="line"/>)
    /// </summary>
    /// <param name="line">The line to remove, default 1</param>
    public static void RemoveLine(Int16 line = 1) {
        Int32 currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, currentLineCursor - line);
        for(Int32 i = 0; i < Console.WindowWidth; i++)
            Console.Write(" ");
        Console.SetCursorPosition(0, currentLineCursor - line);
    }
    /// <summary>
    /// Function to get the string time
    /// </summary>
    /// <returns>The string time, hh:mm:ss.msmsms</returns>
    public static string TimeString() {
        int hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, 
            second = DateTime.Now.Second, millisecond = DateTime.Now.Millisecond;
        return "[" + (hour < 10 ? "0" : "") + hour.ToString() + ":" + (minute < 10 ? "0" : "") + minute.ToString() + ":" +
               (second < 10 ? "0" : "") + second.ToString() + "." +
               (millisecond < 100 ? (millisecond < 10 ? "00" : "0") : "") + millisecond.ToString() + "] ";
    }
    /// <summary>
    /// Function to get the long string time
    /// </summary>
    /// <returns>The long string time, YYYY-MM-DD_hh.mm.ss.msmsms</returns>
    public static string LongTimeString() {
        int year = DateTime.Now.Year, month = DateTime.Now.Month, day = DateTime.Now.Day,
            hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, 
            second = DateTime.Now.Second, millisecond = DateTime.Now.Millisecond;
        return year.ToString() +  "-" + (month < 10 ? "0" : "") + month.ToString() + "-" +
               (day < 10 ? "0" : "") + day.ToString() + "_" + (hour < 10 ? "0" : "") + hour.ToString() + "." +
               (minute < 10 ? "0" : "") + minute.ToString() + "." + (second < 10 ? "0" : "") + second.ToString() + "." +
               (millisecond < 100 ? (millisecond < 10 ? "00" : "0") : "") + millisecond.ToString();
    }
    /// <summary>
    /// Function to print a progress bar string
    /// (<paramref name="currentSize"/>, <paramref name="totalSize"/>, <paramref name="currentElements"/>, <paramref name="totalElements"/>)
    /// </summary>
    /// <param name="currentSize">The current size</param>
    /// <param name="totalSize">The total size</param>
    /// <param name="currentElements">The current number of elements</param>
    /// <param name="totalElements">The total number of elements</param>
    public static void ProgressBar(UInt64 currentSize, UInt64 totalSize, Int32 currentElements, Int32 totalElements) {
        string bar = "[";
        Int16 percent = (Int16)((float)currentSize / totalSize * 100);
        for(Int16 i = 1; i <= percent; i++) {
            bar += barFull;
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(bar);
        bar = "";
        for(Int16 i = (Int16)(percent + 1); i <= 100; i++) {
            bar += barEmpty;
        }
        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.Write(bar);
        bar = "] " + percent.ToString() + "% (" + HumanReadableSize(currentSize) + "/" + HumanReadableSize(totalSize) + ") (" +
            currentElements + "/" + totalElements + ")";
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(bar);
        Console.ResetColor();
    }
    /// <summary>
    /// Function to print a message of the file that is being copied
    /// (<paramref name="reason"/>, <paramref name="file"/>)
    /// </summary>
    /// <param name="reason">The reason</param>
    /// <param name="file">The name of the file</param>
    public static void InfoReason(Reason reason, string file, UInt64? size = null) {
        string line = "";
        switch(reason) {
            case Reason.CopyNotThere:
                line = "Copying because not there: ";
                break;
            case Reason.CopyDifferentSize:
                line = "Copying because different size: ";
                break;
            case Reason.CopyDifferentContent:
                line = "Copying because different content: ";
                break;
            case Reason.Remove:
                line = "Removing: ";
                break;
        }
        Info(line + file + " (" + ((size != null) ? HumanReadableSize((UInt64)size) : "folder") + ")");
    }
    /// <summary>
    /// Function to print a message of the file that has been copied
    /// (<paramref name="reason"/>, <paramref name="file"/>)
    /// </summary>
    /// <param name="reason">The reason</param>
    /// <param name="file">The name of the file</param>
    public static void SuccessReason(Reason reason, string file, UInt64? size = null) {
        string line = "";
        switch(reason) {
            case Reason.CopyNotThere:
                line = "Copied because not there: ";
                break;
            case Reason.CopyDifferentSize:
                line = "Copied because different size: ";
                break;
            case Reason.CopyDifferentContent:
                line = "Copied because different content: ";
                break;
            case Reason.Remove:
                line = "Removed: ";
                break;
        }
        Success(line + file + " (" + ((size != null) ? HumanReadableSize((UInt64)size) : "folder") + ")");
    }
    /// <summary>
    /// Function to print a progress bar string
    /// (<paramref name="size"/>)
    /// </summary>
    /// <param name="size">The size in bytes</param>
    /// <returns>A string with size and unit</returns>
    public static string HumanReadableSize(UInt64 size) {
        UInt16 unit = 1024;
        // Bytes
        if(size < unit) return size.ToString() + "B";
        // KiBytes
        UInt64 KiBytes = (UInt64)Math.Floor((float)size / unit);
        UInt16 Bytes = (UInt16)(size % unit);
        if(KiBytes < unit) return KiBytes.ToString() + "KiB&" + Bytes.ToString() + "B";
        // MiBytes
        UInt32 MiBytes = (UInt32)Math.Floor((float)KiBytes / unit);
        KiBytes %= unit;
        if(MiBytes < unit) return MiBytes.ToString() + "MiB&" + KiBytes.ToString() + "KiB";
        UInt16 GiBytes = (UInt16)Math.Floor((float)MiBytes / unit);
        MiBytes %= unit;
        return GiBytes.ToString() + "GiB&" + MiBytes.ToString() + "MiB";
    }
    /// <summary>
    /// Function to compress a file to .gz
    /// (<paramref name="filename"/>)
    /// </summary>
    /// <param name="filename">The name of the file to compress</param>
    public static void Compress(string filename) {
        FileInfo fi = new FileInfo(filename);
        // Get the stream of the source file.
        using (FileStream inFile = fi.OpenRead()) {
            // Prevent compressing hidden and 
            // already compressed files.
            if((File.GetAttributes(fi.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden) {
                // Create the compressed file.
                using (FileStream outFile = File.Create(fi.FullName + ".gz")) {
                    using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress)) {
                        // Copy the source file into 
                        // the compression stream.
                        inFile.CopyTo(Compress);
                    }
                }
            }
        }
        try {
            File.Delete(filename);
            Info("Compressed " + filename);
        }
        catch(Exception e) {
            Warning("Could not compress " + filename + ", error: " + e);
        }
    }
}