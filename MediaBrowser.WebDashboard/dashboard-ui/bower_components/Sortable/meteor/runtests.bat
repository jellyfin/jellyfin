@echo off
REM Test Meteor package before publishing to Atmospherejs.com

REM Sanity check: make sure we're in the directory of the script
set DIR=%~dp0
cd %DIR%

meteor test-packages ./ %*
