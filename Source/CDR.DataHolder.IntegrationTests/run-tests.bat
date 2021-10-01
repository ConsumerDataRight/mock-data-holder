@echo off
cls
echo Run tests?
pause
dotnet test -c Release --logger "console;verbosity=detailed" > _temp/18.log