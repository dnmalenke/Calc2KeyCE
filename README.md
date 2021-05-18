# Calc2KeyCE

This is a C# program that reads usb input from a TI-84 Plus CE calculator and allows the user to bind calculator keys to keyboard keys or mouse actions. It can also cast your screen to your calculator's screen.

## Installation Instructions:

Install TI-Connect CE if you don't already have it

Download and Copy the CE C 'Standard' Libraries https://github.com/CE-Programming/libraries/releases/ to your calculator <br/>
Copy usbdrvce.8xv to your calculator<br/>
Copy CALC2KEY.8xp to your calculator and run the program

If it is your first time, use a program like Zadig https://zadig.akeo.ie/ to install the libusb-win32 driver for USB ID 0451 E009

Then run Calc2KeyCE and hit connect.
Feel free to load example presets located in this repo.


Credits:
+ Thanks to the makers of the CE C Toolchain and those that continually update the usbdrvce library!
+ Inspired by this forum post: https://www.cemetech.net/forum/viewtopic.php?t=16647
+ zx7: (c) 2012-2013 by Einar Saukas.
