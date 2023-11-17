public class Arguments {
    /// <summary>
    /// Initializer
    /// </summary>
    public Arguments() {
        source = "";
        destination = "";
        errors = 0;
        repeat = false;
        log = false;
        help = false;
        allExtensions = false;
        backup = false;
    }
    public string source, destination;
    public string? removed, extensions;
    public Int32 time;
    public bool repeat, log, help, allExtensions, backup;
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
                    case "-s":
                    case "--source":
                        source = args[i + 1];
                        break;
                    case "-d":
                    case "--destination":
                        destination = args[i + 1];
                        break;
                    case "-r":
                    case "--removed":
                        removed = args[i + 1];
                        break;
                    case "-t":
                    case "--time":
                        string s = args[i + 1];
                        if(!char.IsNumber(s[s.Length - 1])) {
                            char unit = s[s.Length - 1];
                            s = s.Substring(0, s.Length - 1);
                            switch(unit) {
                                case 's':
                                    errors += (Int16)(Int32.TryParse(s, out time) ? 0 : 1);
                                    break;
                                case 'm':
                                    errors += (Int16)(Int32.TryParse(s, out time) ? 0 : 1);
                                    time *= 60;
                                    break;
                                case 'h':
                                    errors += (Int16)(Int32.TryParse(s, out time) ? 0 : 1);
                                    time *= 3600;
                                    break;
                                default:
                                    errors += 1;
                                    break;
                            }
                        }
                        else {
                            errors += (Int16)(Int32.TryParse(s, out time) ? 0 : 1);
                        }
                        repeat = true;
                        break;
                    case "-e":
                    case "--extensions":
                        extensions = args[i + 1];
                        allExtensions = (extensions == "all");
                        break;
                    case "-l":
                    case "--log":
                        log = true;
                        break;
                    case "-b":
                    case "--backup":
                        backup = true;
                        break;
                    case "-f":
                    case "--file":
                        string line = "backup-tool ";
                        foreach(string item in args) {
                            line += item + " ";
                        }
                        File.WriteAllTextAsync(args[i + 1], line);
                        break;
                    case "-h":
                    case "--help":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Usage: backup-utility [ARGUMENTS]");
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Mandatory arguments");
                        Console.WriteLine("  -s, --source        [DIRECTORY]      The source folder");
                        Console.WriteLine("  -d, --destination   [DIRECTORY]      The destination folder");
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Optional arguments");
                        Console.WriteLine("  -r, --removed       [DIRECTORY]      The folder for removed files");
                        Console.WriteLine("  -t, --time          [TIME]           The delay time, e.g. 100 or 100s or 15m or 7h");
                        Console.WriteLine("  -e, --extensions    [FILENAME]       File with the list of extensions to check for content differences,");
                        Console.WriteLine("                                       [FILENAME] = 'all' stands for all extensions");
                        Console.WriteLine("  -l, --log                            Logs to file");
                        Console.WriteLine("  -b, --backup                         Makes a compressed copy of the destination folder after the operation");
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
        if(source == "" || destination == "") errors += 1;
    }
}