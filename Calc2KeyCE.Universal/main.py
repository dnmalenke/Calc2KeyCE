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

if __name__ == "__main__":
    if getattr(os.sys, 'frozen', False):
        basedir = os.sys._MEIPASS
    else:
        basedir = os.path.dirname(os.path.abspath(__file__))
        basedir = str(basedir)

    def capture_screenshot():
        with mss() as sct:
            monitor = sct.monitors[1]
            sct_img = sct.grab(monitor)
            return Image.frombytes('RGB', sct_img.size, sct_img.bgra, 'raw', 'BGRX')

    rt = get_coreclr("./Calc2KeyCE.runtimeconfig.json")
    set_runtime(rt)

    import clr

    clr.AddReference("Calc2KeyCE.Compression")

    from Calc2KeyCE.Compression import *

    while True:
        dev = usb.core.find(idVendor=0x0451, idProduct=0xe009)
        if dev is None:
            print("Searching for Calculator")
            time.sleep(1)
        else:
            print("Found")
            break

    run = True

    while run:
        image = capture_screenshot()
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

@atexit.register
def onexit():
    run = False
    dev.write(2,[0x00,0x00,0x00])

