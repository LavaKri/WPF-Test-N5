@echo off
cd /d "%~dp0"
start "" "Net10\Debug\AccessTest.exe"
if errorlevel 1 pause
