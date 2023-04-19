# memcardrex-cli

This is a very small command line frontend to the [memcardrex][1] Playstation memory card editor.

It can be used to list, export, import, convert or delete raw saves from a Playstation memory card image.

## Building

First, clone the repo with:

    git clone git@github.com:asveikau/memcardrex-cli.git

There is then a simple build script to fetch upstream memcardrex as a git submodule and build the executable.

### Microsoft Windows

Start a Visual Studio Command prompt, and ensure that GIT is on your path and working.  Then run:

    C:\blah\whatever\memcardrex-cli> build

[NB: On Windows 10, I frequently find `git` won't pull from `ssh` within scripts unless I run `set GIT_SSH=C:\windows\system32\openssh\ssh`]

### Unix with Mono

Ensure that `mono` and `mcs` are installed.  Then run:

    $ ./build-mono.sh

## Usage

Run `memcardrex-cli.exe` (or `mono memcardrex-cli.exe`) for command line usage examples.

    usage:  memcardrex-cli new card-filename [format]
            memcardrex-cli list card-filename
            memcardrex-cli export card-filename <index|name> [save-filename] [format]
            memcardrex-cli import card-filename save-filename dest-index
            memcardrex-cli delete card-filename <index|name>
            memcardrex-cli erase card-filename <index|name>
            memcardrex-cli convert src-card-filename dest-card-filename format
    
    Save formats: ActionReplay Mcs Raw Ps3 -- default is raw
    Card formats: Raw Gme Vgs Vmp Mcx

[1]: https://github.com/ShendoXT/memcardrex
