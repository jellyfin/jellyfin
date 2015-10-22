@echo off
REM Sanity check: make sure we're in the directory of the script
set DIR=%~dp0
cd %DIR%

set PACKAGE_DIRS=..\..\
meteor run %*
