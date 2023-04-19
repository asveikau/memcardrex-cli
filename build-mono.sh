#!/bin/sh
# Copyright (C) 2023 Andrew Sveikauskas
# GNU GPLv3, see LICENSE for details.

if [ ! -d submodules/memcardrex/MemcardRex ]; then
   git submodule update --init || exit $?
fi

files=MemcardRexCli.cs
files="$files submodules/memcardrex/MemcardRex/ps1card.cs"

mcs /out:memcardrex-cli.exe $files /reference:System.Drawing
