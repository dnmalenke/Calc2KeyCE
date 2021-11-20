# Calc2KeyCE

This is a C# program that reads usb input from a TI-84 Plus CE calculator and allows the user to bind calculator keys to keyboard keys or mouse actions. It can also cast your screen to your calculator's screen.

Demo:
https://www.youtube.com/watch?v=Afdhxlz6EIk

## Installation Instructions:
### Windows 11 may not be supported at the moment
Install TI-Connect CE if you don't already have it

Download and Copy the CE C 'Standard' Libraries https://github.com/CE-Programming/libraries/releases/ to your calculator <br/>
Copy usbdrvce.8xv to your calculator<br/>
Copy CALC2KEY.8xp to your calculator and run the program

### Windows Specific:

If it is your first time, use a program like Zadig https://zadig.akeo.ie/ to install the libusb-win32 driver for USB ID `0451 E009` <br>
![zadig](https://user-images.githubusercontent.com/21128448/118577897-d55f6e80-b750-11eb-9c48-049f8778a3a5.png)

Then run Calc2KeyCE and hit connect.
Feel free to load example presets located in this repo.

### Universal Specific:

Install .NET Runtime
https://dotnet.microsoft.com/download/dotnet/5.0
Visit this link and select the installer for your OS under the .NET Runtime section

macOS:<br/>
`brew install libusb`<br/>
Linux:<br/>
Calc2KeyCE.Universal needs to be run as sudo to access usb

Run Calc2KeyCE from a terminal and everything should work! (hopefully)

If you run into any issues or have questions, Contact me on Discord: `Darkmasterr#3498`


Credits:
+ Thanks to the makers of the CE C Toolchain and those that continually update the usbdrvce library!
+ Inspired by this forum post: https://www.cemetech.net/forum/viewtopic.php?t=16647
+ zx7: (c) 2012-2013 by Einar Saukas.
