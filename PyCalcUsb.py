import numpy as np
from PIL import ImageGrab # Windows and Mac
#from pyscreenshot import ImageGrab # Linux

image = ImageGrab.grab() # Windows and Mac
# image = ImageGrab.grab(backend="mss", childprocess=False) # Linux
    (width,height) = image.size

    image = image.resize((320,240))

    im = np.array(image)
    
    R5 = (im[...,0] >> 3).astype(np.uint16) << 11
    G6 = (im[...,1] >> 2).astype(np.uint16) << 5
    B5 = (im[...,2] >> 3).astype(np.uint16)

    RGB565 = R5 | G6 | B5

    pic = RGB565.flatten().tolist()

    pp = []

    for pixel in pic:
        pp.extend([pixel & 0xff,pixel >> 8])

print(pp)
    