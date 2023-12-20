#pragma once
#include "WinDesktopDup.h"
#include <libusb-1.0/libusb.h>
#include <signal.h>

void sendThread(libusb_device_handle* devHandle, volatile sig_atomic_t* stop);

float getFrameTime(void* data, int idx);

extern bool running;