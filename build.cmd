@echo off

REM Copyright (C) 2023 Andrew Sveikauskas
REM GNU GPLv3, see LICENSE for details.

where csc 2>NUL >NUL || goto :no-dotnet

if exist submodules\memcardrex\MemcardRex goto :skip-git
where git 2>NUL >NUL || goto :no-git
git submodule update --init || exit /b %ERRORLEVEL%
:skip-git

set FILES=MemcardRexCli.cs
set FILES=%FILES% submodules\memcardrex\MemcardRex\ps1card.cs

csc /out:memcardrex-cli.exe %FILES%

exit /b %ERRORLEVEL%

:no-dotnet
echo Could not find C# compiler.  Please run from Visual Studio command prompt. 1>&2
exit /b 1
:no-git
echo Could not find git in PATH. 1>&2
exit /b 1
