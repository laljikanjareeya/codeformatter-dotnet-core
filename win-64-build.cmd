@ECHO OFF

SETLOCAL

dotnet publish -r win10-x64  --output="%~dp0bin\win10-x64"
