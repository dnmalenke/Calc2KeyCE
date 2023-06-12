#pragma once
#include <usbdrvce.h>

void init_descriptors();
void cleanup_descriptors();
usb_standard_descriptors_t* get_descriptors();
