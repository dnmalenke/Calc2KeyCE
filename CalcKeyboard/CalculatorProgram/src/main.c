typedef struct global global_t;
#define usb_callback_data_t global_t

#include <compression.h>
#include <usbdrvce.h>

#include <debug.h>
#include <keypadc.h>
#include <tice.h>

#include <inttypes.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

struct global
{
	usb_device_t device;
	usb_endpoint_t in, out;
	uint8_t type;
	usb_device_t host;
};

static usb_error_t handleBulkOut(usb_endpoint_t endpoint, usb_transfer_status_t status, size_t transferred, usb_transfer_data_t* data);
static usb_error_t handleUsbEvent(usb_event_t event, void* event_data, usb_callback_data_t* callback_data);

global_t global;
uint32_t screenSize = 4;
uint32_t prog = 0;
bool connected = false;

int main(void)
{
	usb_error_t error;
	memset(&global, 0, sizeof(global_t));
	memset((void*)lcd_Ram, 0, LCD_SIZE);

	usb_device_descriptor_t dev = { 0x12, 0x01, 0x200, 0x00, 0x00, 0x00, 0x40, 0x0451, 0xE009, 0x0220, 0x01, 0x02, 0x00, 0x03 };

	usb_configuration_descriptor_t* conf0 = malloc(0x0023);
	usb_configuration_descriptor_t* conf1 = malloc(0x0023);
	usb_configuration_descriptor_t* conf2 = malloc(0x0023);

	const usb_configuration_descriptor_t* confs[] = {
		conf0, conf1, conf2 };

	memcpy((void*)confs[0], (uint8_t[]) { 0x09, 0x02, 0x23, 0x00, 0x01, 0x01, 0x00, 0x80, 0xFA, 0x09, 0x04, 0x00, 0x00, 0x02, 0xFF, 0x01, 0x00, 0x00, 0x07, 0x05, 0x81, 0x02, 0x40, 0x00, 0x00, 0x07, 0x05, 0x02, 0x02, 0x40, 0x00, 0x00, 0x03, 0x09, 0x03 }, 35);
	memcpy((void*)confs[1], (uint8_t[]) { 0x09, 0x02, 0x23, 0x00, 0x01, 0x02, 0x00, 0xC0, 0x00, 0x09, 0x04, 0x00, 0x00, 0x02, 0xFF, 0x01, 0x00, 0x00, 0x07, 0x05, 0x81, 0x02, 0x40, 0x00, 0x00, 0x07, 0x05, 0x02, 0x02, 0x40, 0x00, 0x00, 0x03, 0x09, 0x03 }, 35);
	memcpy((void*)confs[2], (uint8_t[]) { 0x09, 0x02, 0x23, 0x00, 0x01, 0x03, 0x00, 0x80, 0x32, 0x09, 0x04, 0x00, 0x00, 0x02, 0xFF, 0x01, 0x00, 0x00, 0x07, 0x05, 0x81, 0x02, 0x40, 0x00, 0x00, 0x07, 0x05, 0x02, 0x02, 0x40, 0x00, 0x00, 0x03, 0x09, 0x03 }, 35);

	usb_string_descriptor_t* langids = malloc(sizeof(usb_string_descriptor_t) + sizeof(uint16_t));
	memcpy((void*)langids, (uint8_t[]) { 0x04, 0x03, 0x09, 0x04 }, 4);

	uint8_t numStrings = 238;

	usb_string_descriptor_t* string1 = malloc(0x3E);
	string1->bLength = 0x3E;
	string1->bDescriptorType = 0x03;

	usb_string_descriptor_t* string2 = malloc(0x1C);
	string2->bLength = 0x1C;
	string2->bDescriptorType = 0x03;

	usb_string_descriptor_t* osStr = malloc(0x12);

	const usb_string_descriptor_t* strings[238];
	strings[0] = string1;
	strings[1] = string2;
	strings[237] = osStr;

	memcpy((void*)strings[0]->bString, (wchar_t[]) { 'T', 'e', 'x', 'a', 's', ' ', 'I', 'n', 's', 't', 'r', 'u', 'm', 'e', 'n', 't', 's', ' ', 'I', 'n', 'c', 'o', 'r', 'p', 'o', 'r', 'a', 't', 'e', 'd' }, 60);
	memcpy((void*)strings[1]->bString, (wchar_t[]) { 'T', 'I', '-', '8', '4', ' ', 'P', 'l', 'u', 's', ' ', 'C', 'E' }, 26);
	memcpy((void*)strings[237], (uint8_t[]) { 0x12, 0x03, 0x4D, 0x00, 0x53, 0x00, 0x46, 0x00, 0x54, 0x00, 0x31, 0x00, 0x30, 0x00, 0x30, 0x00, 0x01, 0x01 }, 0x12);

	usb_standard_descriptors_t desc = { &dev, confs, langids, numStrings, strings };

	if ((error = usb_Init(handleUsbEvent, &global, &desc, USB_DEFAULT_INIT_FLAGS)) == USB_SUCCESS)
	{
		while ((error = usb_WaitForInterrupt()) == USB_SUCCESS)
		{
			if (connected)
			{
				kb_Scan();
				uint8_t keys[] = { kb_Data[1], kb_Data[2], kb_Data[3], kb_Data[4], kb_Data[5], kb_Data[6], kb_Data[7] };
				usb_BulkTransfer(global.in, &keys, 7, 0, NULL);
			}
			else if (os_GetCSC())
			{
				break;
			}
		}
	}

	usb_Cleanup();

	free(langids);
	free(string1);
	free(string2);
	free(osStr);
	free(conf0);
	free(conf1);
	free(conf2);

	return 0;
}

static usb_error_t handleBulkOut(usb_endpoint_t endpoint, usb_transfer_status_t status, size_t transferred, usb_transfer_data_t* data)
{
	if (prog != UINT32_MAX)
	{
		if (prog >= screenSize)
		{
			prog = 0;
		}

		if (transferred == 4)
		{
			connected = true;
			memcpy(&screenSize, data, sizeof(int));
		}
		else if (transferred == 3)
		{
			connected = !connected;
			if (!connected)
			{
				return USB_ERROR_NO_DEVICE;
			}
		}
		else
		{
			if (screenSize >= 51200)
			{
				memcpy((void*)lcd_Ram + prog, data, transferred);

				if (prog + transferred >= screenSize)
				{
					screenSize = 4;
				}
			}
			else
			{
				zx7_Decompress((void*)lcd_Ram, data);
				screenSize = 4;
			}
		}
	}

	if (status == USB_TRANSFER_COMPLETED)
	{
		if (prog != UINT32_MAX)
		{
			if (transferred != 4)
			{
				prog += transferred;
			}

			if (screenSize >= 51200)
			{
				if (screenSize - prog >= 51200)
				{
					return usb_ScheduleBulkTransfer(endpoint, data, 51200, handleBulkOut, data);
				}
				else
				{
					return usb_ScheduleBulkTransfer(endpoint, data, screenSize - prog, handleBulkOut, data);
				}
			}

			return usb_ScheduleBulkTransfer(endpoint, data, screenSize, handleBulkOut, data);
		}

		return usb_ScheduleBulkTransfer(endpoint, data, 1024, handleBulkOut, data);
	}

	free(data);
	return USB_SUCCESS;
}

static usb_error_t handleUsbEvent(usb_event_t event, void* event_data, usb_callback_data_t* callback_data)
{
	usb_error_t error = USB_SUCCESS;
	switch ((unsigned)event)
	{
	case USB_HOST_CONFIGURE_EVENT:
	{
		callback_data->host = usb_FindDevice(NULL, NULL, USB_SKIP_HUBS);
		if (!callback_data->host)
		{
			error = USB_ERROR_NO_DEVICE;
			break;
		}

		callback_data->out = usb_GetDeviceEndpoint(callback_data->host, 0x02);
		callback_data->in = usb_GetDeviceEndpoint(callback_data->host, 0x81);

		global.in = callback_data->in;

		if (!callback_data->in || !callback_data->out)
		{
			error = USB_ERROR_SYSTEM;
			break;
		}
		usb_SetEndpointFlags(callback_data->in, USB_AUTO_TERMINATE);
		if (error != USB_SUCCESS)
			break;
		void* buffer;
		if (!(buffer = malloc(1024)))
		{
			error = USB_ERROR_NO_MEMORY;
			break;
		}
		prog = UINT32_MAX;
		handleBulkOut(callback_data->out, USB_TRANSFER_COMPLETED, 0, buffer);
		prog = 0;
		break;
	}
	case USB_DEVICE_DISCONNECTED_EVENT:
		if (callback_data->device == event_data)
		{
			callback_data->device = NULL;
			callback_data->in = callback_data->out = NULL;
			connected = false;
		}
		__attribute__((fallthrough));
	case USB_DEVICE_CONNECTED_EVENT:
	case USB_DEVICE_DISABLED_EVENT:
	case USB_DEVICE_ENABLED_EVENT:
		if (event == USB_DEVICE_CONNECTED_EVENT && !(usb_GetRole() & USB_ROLE_DEVICE))
			error = usb_ResetDevice(event_data);
		if (event == USB_DEVICE_ENABLED_EVENT)
			callback_data->device = event_data;
		break;
	case USB_DEFAULT_SETUP_EVENT:
	case USB_DEVICE_INTERRUPT:
	case USB_DEVICE_DEVICE_INTERRUPT:
	case USB_DEVICE_CONTROL_INTERRUPT:
	case USB_DEVICE_WAKEUP_INTERRUPT:
	case USB_HOST_INTERRUPT:
		break;
	default:
		break;
	}
	return error;
}
