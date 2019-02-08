@ECHO OFF

SETLOCAL

dotnet publish -r ubuntu.16.10-x64  --output="%~dp0bin\ubuntu.16.10-x64"
