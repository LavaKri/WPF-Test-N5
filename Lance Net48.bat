@echo off
cd /d "%~dp0"
start "" "Net48\Debug\AccessTest.exe"
if errorlevel 1 pause
