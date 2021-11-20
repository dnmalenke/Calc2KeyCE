pyinstaller -F main.py --add-data "Calc2KeyCE.Compression.dll:." --add-data "Calc2KeyCE.runtimeconfig.json:." --exclude-module tkinter
