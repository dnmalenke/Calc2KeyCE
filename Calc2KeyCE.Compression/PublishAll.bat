@echo off

dotnet publish -c Release -r win-x64
copy /Y ".\bin\Release\net5.0\win-x64\publish\Calc2KeyCE.Compression.dll" "..\Calc2KeyCE.Universal\win-x64\"
dotnet publish -c Release -r linux-x64
copy /Y ".\bin\Release\net5.0\linux-x64\publish\Calc2KeyCE.Compression.dll" "..\Calc2KeyCE.Universal\linux-x64\"
dotnet publish -c Release -r osx-x64
copy /Y ".\bin\Release\net5.0\osx-x64\publish\Calc2KeyCE.Compression.dll" "..\Calc2KeyCE.Universal\osx-x64\"