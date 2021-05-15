# Calc2KeyCE

This is a C# program that reads serial input from a TI-84 Plus CE calculator and allows the user to bind calculator keys to keyboard keys or mouse actions.

## Installation Instructions:

First we need to build the usb branch of the CE Toolchain to get the libraries to install on the calculator.
`git clone --recurse-submodules https://github.com/CE-Programming/toolchain.git -b usbdrvce`

Follow instructions here to build the toolchain:
https://ce-programming.github.io/toolchain/static/contributing.html#building-the-ce-c-toolchain

Install TI-Connect CE if you don't already have it

Copy all *.8xv files from toolchain/src/ to your calculator
Copy CALC2KEY.8xp to your calculator and run the program

Run CALC2KEY on your calculator, then run Calc2KeyCE and hit connect.
Feel free to load example presets located in this repo.


