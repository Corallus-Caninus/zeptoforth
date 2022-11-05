# FAT32 Tools

zeptoforth comes with a variety of words for manipulating files and directories in FAT32 filesystems. Directories can be created, listed, removed, and renamed, and files can be created, appended, overwritten, dumped, removed, and renamed.

zeptoforth includes support for including code for execution within FAT32 filesystems. This includes support for handling nested included files, up to a maximum of eight included files. Note that including code is intended to only be done from within the main task, and undefined results may occur if done from within any other task.

### `fat32-tools`

The `fat32-tools` module contains the following words:

##### `x-fs-not-set`
( -- )

Current filesystem not set exception.

##### `x-include-stack-overflow`
( -- )

Include stack overflow exception, raised if the number of nested includes exceeds eight includes.

##### `current-fs!`
( fs -- )

Set the current FAT32 filesystem. This filesystem is a subclass of `<base-fat32-fs>` in the `fat32` module.

##### `current-fs@`
( -- fs )

Get the current FAT32 filesystem. This filesystem is a subclass of `<base-fat32-fs>` in the `fat32` module.

##### `load-file`
( file -- )

Load code from a file in the FAT32 filesystem. Note that the file object will be duplicated in the process.

##### `included`
( path-addr path-u -- )

Load code from a file with the specified path in the current include FAT32 filesystem.

##### `include`
( "path" -- )

Load code from a file with the specified path as a token in the current include FAT32 filesystem.

##### `list-dir`
( path-addr path-u -- )

List a directory at the specified path.

##### `create-file`
( data-addr data-u path-addr path-u -- )

Create a file at the specified path and write data to it.

##### `create-dir`
( path-addr path-u -- )

Create a directory at the specified path.

##### `append-file`
( data-addr data-u path-addr path-u -- )

Write data to the end of a file at the specified path.

##### `write-file`
( data-addr data-u path-addr path-u -- )

Overwrite a file at the specified path with data and then truncate it afterwards.

##### `dump-file`
( path-addr path-u -- )

Dump the contents of a file at the specified path to the console.

##### `remove-file`
( path-addr path-u -- )

Remove a file at the specified path.

##### `remove-dir`
( path-addr path-u -- )

Remove a directory at the specified path. Note that it must be empty aside from the `.` and `..` entries.

##### `rename`
( path-addr path-u new-name-addr new-name-u -- )

Rename a file or directory at the specified path to a new *name* (not path). Note that files' and directories' parent directories cannot be changed with this word.