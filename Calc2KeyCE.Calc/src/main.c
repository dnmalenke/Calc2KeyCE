typedef struct global global_t;
#define usb_callback_data_t global_t

#include <compression.h>
#include <tice.h>
#include <stdlib.h>
#include <string.h>
#include <usbdrvce.h>
#include "descriptors.c"
#include <keypadc.h>

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
void* buffer = 0;

int main(void)
{
	usb_error_t error;
	memset(&global, 0, sizeof(global_t));
	memset((void*)lcd_Ram, 0, LCD_SIZE);

	// https://wikiti.brandonw.net/index.php?title=84PCE:Ports:4000
	lcd_Control = 0b00000100100100111;

	init_descriptors();

	if ((error = usb_Init(handleUsbEvent, &global, &descriptors, USB_DEFAULT_INIT_FLAGS)) == USB_SUCCESS)
	{
		while ((error = usb_WaitForInterrupt()) == USB_SUCCESS)
		{
			if (connected)
			{
				//kb_Scan(); // use timer
				//uint8_t keys[] = { kb_Data[1], kb_Data[2], kb_Data[3], kb_Data[4], kb_Data[5], kb_Data[6], kb_Data[7] };
				//usb_BulkTransfer(global.in, &keys, 7, 0, NULL);	
			}
			else if (os_GetCSC())
			{
				break;
			}
		}
	}

	usb_Cleanup();

	lcd_Control = 0b00000100100101101;

	cleanup_descriptors();

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
				if (prog == 0) {
					memcpy((void*)lcd_Palette, data, 512);

					memcpy((void*)lcd_Ram, (void*)(data + 512), transferred - 512);
				}
				else {
					memcpy((void*)lcd_Ram + prog - 512, data, transferred);
				}

				if (prog + transferred >= screenSize)
				{
					screenSize = 4;
				}
			}
			else
			{
				memcpy((void*)lcd_Palette, data, 512);
				zx7_Decompress((void*)lcd_Ram, (void*)(data + 512));
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
		{
			break;
		}

		if (buffer == 0) {
			if (!(buffer = malloc(1024)))
			{
				error = USB_ERROR_NO_MEMORY;
				break;
			}
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
			error = usb_ResetDevice((usb_device_t)event_data);
		if (event == USB_DEVICE_ENABLED_EVENT)
			callback_data->device = (usb_device_t)event_data;
		break;
	default:
		break;
	}
	return error;
}
