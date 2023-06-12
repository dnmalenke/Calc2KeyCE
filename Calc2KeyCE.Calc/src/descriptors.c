#include "descriptors.h"
#include <usbdrvce.h>
#include <string.h>
#include <stdlib.h>

static const usb_string_descriptor_t langids = {
	.bLength = sizeof(langids) + sizeof(wchar_t) * 1,
	.bDescriptorType = USB_STRING_DESCRIPTOR,
	.bString = {
		[0] = 0x0409u,
	},
};

static const struct configuration1
{
	usb_configuration_descriptor_t configuration;
	struct configuration1_interface0
	{
		usb_interface_descriptor_t interface;
		usb_endpoint_descriptor_t endpoints[2];
	} interface0;
} configuration1 = {
	.configuration = {
		.bLength = sizeof(configuration1.configuration),
		.bDescriptorType = USB_CONFIGURATION_DESCRIPTOR,
		.wTotalLength = sizeof(configuration1),
		.bNumInterfaces = 1u,
		.bConfigurationValue = 1u,
		.iConfiguration = 0,
		.bmAttributes = USB_CONFIGURATION_ATTRIBUTES,
		.bMaxPower = 500u / 2u,
	},
	.interface0 = {
		.interface = {
			.bLength = sizeof(configuration1.interface0.interface),
			.bDescriptorType = USB_INTERFACE_DESCRIPTOR,
			.bInterfaceNumber = 0u,
			.bAlternateSetting = 0u,
			.bNumEndpoints = 2,
			.bInterfaceClass = USB_VENDOR_SPECIFIC_CLASS,
			.bInterfaceSubClass = 1u,
			.bInterfaceProtocol = 1u,
			.iInterface = 0x00u,
		},
		.endpoints = { [0] = {
						  .bLength = sizeof(configuration1.interface0.endpoints[0]),
						  .bDescriptorType = USB_ENDPOINT_DESCRIPTOR,
						  .bEndpointAddress = 0x81u,
						  .bmAttributes = USB_BULK_TRANSFER,
						  .wMaxPacketSize = 0x0040u,
						  .bInterval = 0u,
					  },
					  [1] = {
						  .bLength = sizeof(configuration1.interface0.endpoints[1]),
						  .bDescriptorType = USB_ENDPOINT_DESCRIPTOR,
						  .bEndpointAddress = 0x02u,
						  .bmAttributes = USB_BULK_TRANSFER,
						  .wMaxPacketSize = 0x0040u,
						  .bInterval = 0u,
					  }},
	},
};
static const usb_configuration_descriptor_t* configurations[] = {
	[0] = &configuration1.configuration };

static usb_device_descriptor_t device = {
	.bLength = sizeof(device),
	.bDescriptorType = USB_DEVICE_DESCRIPTOR,
	.bcdUSB = 0x200u,
	.bDeviceClass = USB_INTERFACE_SPECIFIC_CLASS,
	.bDeviceSubClass = 0u,
	.bDeviceProtocol = 0u,
	.bMaxPacketSize0 = 0x40u,
	.idVendor = 0x0451u,
	.idProduct = 0xE009u,
	.bcdDevice = 0x240u,
	.iManufacturer = (uint8_t)0x0451u,
	.iProduct = (uint8_t)0xE009u,
	.iSerialNumber = (uint8_t)0x0220u,
	.bNumConfigurations = 1,
};

usb_standard_descriptors_t descriptors = {
	.device = &device,
	.configurations = configurations,
	.langids = &langids,
	.numStrings = 2,
	.strings = NULL,
};

static usb_string_descriptor_t* string1;
static usb_string_descriptor_t* string2;
static const usb_string_descriptor_t* strings[2];

void init_descriptors() {
	string1 = (usb_string_descriptor_t*)malloc(0x3E);
	string1->bLength = 0x3E;
	string1->bDescriptorType = 0x03;

	string2 = (usb_string_descriptor_t*)malloc(0x1C);
	string2->bLength = 0x1C;
	string2->bDescriptorType = 0x03;

	strings[0] = string1;
	strings[1] = string2;
	//strings[237] = osStr;

	memcpy((void*)strings[0]->bString, (wchar_t[60]) { 'T', 'e', 'x', 'a', 's', ' ', 'I', 'n', 's', 't', 'r', 'u', 'm', 'e', 'n', 't', 's', ' ', 'I', 'n', 'c', 'o', 'r', 'p', 'o', 'r', 'a', 't', 'e', 'd' }, 60);
	memcpy((void*)strings[1]->bString, (wchar_t[26]) { 'T', 'I', '-', '8', '4', ' ', 'P', 'l', 'u', 's', ' ', 'C', 'E' }, 26);
	//	memcpy((void*)strings[237], (uint8_t[]){0x12, 0x03, 0x4D, 0x00, 0x53, 0x00, 0x46, 0x00, 0x54, 0x00, 0x31, 0x00, 0x30, 0x00, 0x30, 0x00, 0x01, 0x01}, 0x12);

	descriptors.strings = strings;
}

void cleanup_descriptors() {
	free(string1);
	free(string2);
}

usb_standard_descriptors_t* get_descriptors() {
	return &descriptors;
}
