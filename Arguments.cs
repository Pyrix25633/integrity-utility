public class Arguments {
    /// <summary>
    /// Initializer
    /// </summary>
    public Arguments() {
        path = "";
        algorithm = "SHA3-512";
        extensions = "all";
        threads = 4;
        repeat = false;
        log = false;
        help = false;
        allExtensions = true;
        errors = 0;
    }
    public string path, algorithm, extensions;
    public Int16 threads;
    public string? compare;
    public Int32 delay;
    public bool repeat, log, help, allExtensions;
    public Int16 errors;

    /// <summary>
    /// Function to parse the arguments
    /// </summary>
    public void Parse(string[] args) {
        Int16 length = (Int16)args.Length;
        if(length == 0) {
            errors = 255;
            return;
        }
        for(Int16 i = 0; i < length; i++) {
            if(args[i][0] == '-') {
                switch(args[i]) {
                    case "-p":
                    case "--path":
                        path = args[i + 1];
                        break;
                    case "-a":
                    case "--algorithm":
                        algorithm = args[i + 1];
                        break;
                    case "-e":
                    case "--extensions":
                        extensions = args[i + 1];
                        allExtensions = extensions == "all";
                        break;
                    case "-t":
                    case "--threads":
                        string t = args[i + 1];
                        errors += (Int16)(Int16.TryParse(t, out threads) ? 0 : 1);
                        break;
                    case "-c":
                    case "--compare":
                        compare = args[i + 1];
                        break;
                    case "-d":
                    case "--delay":
                        string s = args[i + 1];
                        if(!char.IsNumber(s[s.Length - 1])) {
                            char unit = s[s.Length - 1];
                            s = s.Substring(0, s.Length - 1);
                            switch(unit) {
                                case 's':
                                    errors += (Int16)(Int32.TryParse(s, out delay) ? 0 : 1);
                                    break;
                                case 'm':
                                    errors += (Int16)(Int32.TryParse(s, out delay) ? 0 : 1);
                                    delay *= 60;
                                    break;
                                case 'h':
                                    errors += (Int16)(Int32.TryParse(s, out delay) ? 0 : 1);
                                    delay *= 3600;
                                    break;
                                default:
                                    errors += 1;
                                    break;
                            }
                        }
                        else {
                            errors += (Int16)(Int32.TryParse(s, out delay) ? 0 : 1);
                        }
                        repeat = true;
                        break;
                    case "-l":
                    case "--log":
                        log = true;
                        break;
                    case "-f":
                    case "--file":
                        string line = "integrity-utility ";
                        foreach(string item in args) {
                            line += item + " ";
                        }
                        File.WriteAllTextAsync(args[i + 1], line);
                        break;
                    case "-h":
                    case "--help":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Usage: integrity-utility [ARGUMENTS]");
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Mandatory arguments");
                        Console.WriteLine("  -p, --path          [DIRECTORY]      The folder to scan");
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Optional arguments");
                        Console.WriteLine("  -a, --algorithm     [ALGORITHM]      The hashing algorithm, defaults to SHA3-512,");
                        Console.WriteLine("                                       overridden by the algorithm used to create the index file if it exists");
                        Console.WriteLine("                                       [ALGORITHM] = SHA-256 | SHA-512 | SHA3-256 | SHA3-512");
                        Console.WriteLine("  -e, --extensions    [FILENAME]       File with the list of extensions to process");
                        Console.WriteLine("                                       (every file with a different extension will be ignored),");
                        Console.WriteLine("                                       [FILENAME] = 'all' stands for all extensions");
                        Console.WriteLine("  -t, --threads       [NUMBER]         The number of files that can be processes concurrently,");
                        Console.WriteLine("                                       between 1 and 16, default 4");
                        Console.WriteLine("  -c, --compare       [DIRECTORY]      The directory containing another index that will be compared");
                        Console.WriteLine("                                       with the current one");
                        Console.WriteLine("  -d, --delay         [TIME]           The delay time, e.g. 100 or 100s or 15m or 7h");
                        Console.WriteLine("  -l, --log                            Logs to file");
                        Console.WriteLine("  -f, --file          [FILENAME]       Saves the command to a script");
                        Console.WriteLine("  -h, --help                           Prints help message and exits");
                        Console.ResetColor();
                        help = true;
                        break;
                    case "--":
                        break;
                    default:
                        errors += 1;
                        break;
                }
            }
        }
        if(path == "") errors += 1;
    }
}