import os
import time
import numpy
import atexit
import usb.core
from mss import mss
from PIL import Image
from pythonnet import set_runtime
from clr_loader import get_coreclr
from PIL.Image import FLOYDSTEINBERG, MEDIANCUT

if os.sys.platform == "linux" and os.geteuid() != 0:
    os.sys.exit("This program must run as root")

# setup path for dll files when it is a pyinstaller executable
if getattr(os.sys, 'frozen', False):
    basedir = os.sys._MEIPASS
else:
    basedir = os.path.dirname(os.path.abspath(__file__))
    basedir = str(basedir)

rt = get_coreclr(f"{basedir}/Calc2KeyCE.runtimeconfig.json")
set_runtime(rt)

import clr

clr.AddReference("Calc2KeyCE.Compression")

from Calc2KeyCE.Compression import *

run = True

@atexit.register
def onexit():
    run = False
    if dev is not None:
        dev.write(2,[0x00,0x00,0x00])

def capture_screenshot(monitor):
    with mss() as sct:
        sct_img = sct.grab(monitor)
        return Image.frombytes('RGB', sct_img.size, sct_img.bgra, 'raw', 'BGRX')

if __name__ == "__main__":
    print("Calc2KeyCE Universal version 1.5.0 by David Malenke\n")
    if len(mss().monitors) > 2:
        i = 1
        for monitor in mss().monitors[1:]:
            print(f"{i}. {monitor}")
            i += 1
        valid_monitor = False
        while not valid_monitor:
            try:
                input_str = input("Multiple monitors detected, please enter the desired monitor number (default: 1): ")
                if input_str == '':
                    input_str = "1"

                monitor_num = int(input_str)
                selected_monitor = mss().monitors[monitor_num]
                valid_monitor = True
            except:
                print("Invalid monitor number. Please try again...\n")
    else:
        selected_monitor = mss().monitors[1]
        
    dots = "    "
    while True:
        dev = usb.core.find(idVendor=0x0451, idProduct=0xe009)
        if dev is None:
            os.sys.stdout.write("\rSearching for calculator" + dots)
            os.sys.stdout.flush()
            time.sleep(1)
            dots = dots.replace(' ','.',1)
            if dots == "....":
                dots = "    "
        else:
            print("\nCalculator Found!")
            break
     
    fps = 0.0
    while run:
        start_time = time.time()

        image = capture_screenshot(selected_monitor)
        image = image.resize((320,240))
        image = image.quantize(colors=256, method=MEDIANCUT,dither=FLOYDSTEINBERG)
        palette = numpy.array(image.getpalette(),dtype=numpy.uint8).reshape((256, 3))

        colors = []
        for entry in palette:
            c = (numpy.int16(int(round(entry[0] * 31 / 255.0, 0) * (2 ** 10) + round(entry[1] * 31 / 255.0, 0) * (2 ** 5) + round(entry[2] * 31 / 255.0, 0)))).tobytes()
            colors.append(c[0])
            colors.append(c[1])

        imageBytes = bytearray(list(image.getdata()))

        optimize = Optimize.optimize(imageBytes, len(imageBytes), 0, None)
        compImage = Compress.compress(optimize, imageBytes, len(imageBytes), 0, 0, 0)
        data = colors + list(compImage[0])

        if(len(data) > 51200):
            data = colors + list(image.getdata())

        dev.write(2, len(data).to_bytes(4, 'little'))
        dev.write(2, data)

        fps = (1 / (time.time() - start_time))
        os.sys.stdout.write("\rFps: " + str(round(fps,2)))
        os.sys.stdout.flush()
