# Calc2KeyCE

This is a C# program that reads serial input from a TI-84 Plus CE calculator and allows the user to bind calculator keys to keyboard keys or mouse actions.

## Installation Instructions:

First we need to build the usb branch of the CE Toolchain to get the libraries to install on the calculator ( because I don't think I'm allowed to distribute a prebuilt version of the usbdrvce lib )
`git clone --recurse-submodules https://github.com/CE-Programming/toolchain.git -b usbdrvce`

Follow instructions here to get the prerequisites build the toolchain:
https://ce-programming.github.io/toolchain/static/contributing.html#building-the-ce-c-toolchain

Navigate to `toolchain/src/usbdrvce`, open a command window and enter `make`

Install TI-Connect CE if you don't already have it

Download and Copy the CE C 'Standard' Libraries https://github.com/CE-Programming/libraries/releases/ to your calculator
Copy your newly built usbdrvce.8xv to your calculator
Copy CALC2KEY.8xp to your calculator and run the program

Run CALC2KEY on your calculator, then run Calc2KeyCE and hit connect.
Feel free to load example presets located in this repo.


Credits:
+ Thanks to the makers of the CE C Toolchain and those that continually update the usbdrvce library!
+ Inspired by this forum post: https://www.cemetech.net/forum/viewtopic.php?t=16647
+ zx7: (c) 2012-2013 by Einar Saukas.
