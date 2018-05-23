@echo off
echo Registering WasatchNET.DLL...
cd \Windows
\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /codebase /tlb:WasatchNET.tlb WasatchNET.dll
echo.
echo If no errors appear in the above output, press return or close the window.
echo.
pause
