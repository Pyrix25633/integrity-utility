# C# Backup utility
## A simple utility to backup folders copying only the necessary files
> If the file in the source folder is the same as the one in the destination folder it will not be copied because it is not necessary, saving time and disk life
## This program is the continuation of the [C++ backup utility](https://github.com/Pyrix25633/backup-utility-cpp), the first `C#` version is `1.4.0`

### What is it:
Backup Utility is a program to keep a "real-time" copy of a folder, so it is a **backup** program, but **more efficient**: instead of copying the entire folder, it will make a list of all items in source folder, a list of all items in destination folder, compare the two, make a list of items to copy, a list of items to remove and then copy and remove those items, it will then wait a certain time and continue to do so.
How does it function:
It will check if a file exists in the destination folder, if not it will sign it as to copy, if it exists, it will compare the file sizes, if they are different it will sign it as to copy, if they are the same and the extension is in a list of extensions to scan for differences, it will compare the contents, if they are different it will sign it as to copy.
If a file no longer exists in the source folder, it will be moved from the destination folder to a "deleted" folder, so you can recover it.
### How to use it:
**Usage: backup-utility [ARGUMENTS]**
**Mandatory arguments**
`-s, --source [DIRECTORY] The source folder`
`-d, --destination [DIRECTORY] The destination folder`
Optional arguments
`-r, --removed [DIRECTORY] The folder for removed files`
`-t, --time [TIME] The delay time, e.g. 100 or 100s or 15m or 7h`
`-e, --extensions [FILENAME] File with the list of extensions to check for content differences, [FILENAME] = 'all' stands for all extensions`
`-l, --log Logs to file`
`-b, --backup Makes a compressed copy of the destination folder after the operation`
`-f, --file [FILENAME] Saves the command to a script`
`-h, --help Prints help message and exits`