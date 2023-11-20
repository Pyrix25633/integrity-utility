# C# Backup utility

## A simple utility to the integrity of files

### What is it:

Integrity utility creates an index with the hashes of selected files using the given algorithm.  
When you run it again it will recalculate the hashes and compare them giving you a feedback for possibly currupted files or changes.  
If a new file is detected an entry will be automatically created in the index. If a file is removed its entry will be automatically removed from the index.  
If an option is given the index will be updated with the newly calculated digests, otherwise it will be left unchanged (except for new and deleted files).

### How to use it:

**Usage: integrity-utility [ARGUMENTS]**

**Mandatory arguments**

`-p, --path [DIRECTORY] The folder to scan`

Optional arguments

`-a, --algorithm [ALGORITHM] The hashing algorithm, defaults to SHA3-512, [ALGORITHM] = MD5 | SHA1 | SHA-256 | SHA-384 | SHA-512 | SHA3-256 | SHA3-384 | SHA3-512`  
`-e, --extensions [FILENAME] File with the list of extensions to process (every file with a different extension will be ignored), [FILENAME] = 'all' stands for all extensions and is the default if -e is not specified`  
`-t, --threads [NUMBER] The number of files that can be processes concurrently, between 1 and 16, default 4`  
`-u, --update Updates the index with the newly calculated hashes`  
`-c, --compare [DIRECTORY] The directory containing another index that will be compared with the current one`  
`-s, --skip Every file already in the index is skipped`  
`-d, --delay [TIME] The delay time, e.g. 100 or 100s or 15m or 7h`  
`-l, --log Logs to file`  
`-f, --file [FILENAME] Saves the command to a script`  
`-h, --help Prints help message and exits`