#pragma once
#include <libusb-1.0/libusb.h>
#include <signal.h>

extern uint32_t keyBindings[128];
extern int mouseSpeed;
extern uint8_t lastCalcKeyUp;

void receiveThread(libusb_device_handle* devHandle, volatile sig_atomic_t* stop);