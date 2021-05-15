#include <tice.h>
#include <string.h>
#include <keypadc.h>
#include <stdio.h>

#define usb_callback_data_t usb_device_t

#include <usbdrvce.h>
#include <srldrvce.h>

/* Serial device struct */
srl_device_t srl;

bool has_device = false;

/* A buffer for internal use by the serial library */
uint8_t srl_buf[512];

// /* Handle USB events */
// static usb_error_t handle_usb_event(usb_event_t event, void* event_data,
// 	usb_callback_data_t* callback_data) {
// 	/* When a device is connected, or when connected to a computer */
// 	if ((event == USB_DEVICE_CONNECTED_EVENT && !(usb_GetRole() & USB_ROLE_DEVICE)) || event == USB_HOST_CONFIGURE_EVENT) {
// 		if (!has_device) {
// 			usb_device_t device = event_data;

// 			/* Initialize the serial library with the newly attached device */
// 			srl_error_t error = srl_Init(&srl, device, srl_buf, sizeof srl_buf, SRL_INTERFACE_ANY);

// 			if (error) {
// 				/* Print the error code to the homescreen */
// 				char buf[64];
// 				sprintf(buf, "Error %u initting serial", error);

// 				os_ClrHome();
// 				os_PutStrFull(buf);
// 			}
// 			else {
// 				has_device = true;
// 			}
// 		}
// 	}

// 	if (event == USB_DEVICE_DISCONNECTED_EVENT || event == USB_DEVICE_DEVICE_INTERRUPT || event == USB_DEVICE_SUSPEND_INTERRUPT)
// 	{
// 		has_device = false;
// 	}

// 	return USB_SUCCESS;
// }

static usb_error_t handle_usb_event(usb_event_t event, void *event_data,
                                    usb_callback_data_t *callback_data __attribute__((unused))) {
    /* Enable newly connected devices */
    if(event == USB_DEVICE_CONNECTED_EVENT && !(usb_GetRole() & USB_ROLE_DEVICE)) {
        usb_device_t device = event_data;
        usb_ResetDevice(device);
    }
    /* When a device is connected, or when connected to a computer */
    if((event == USB_DEVICE_ENABLED_EVENT && !(usb_GetRole() & USB_ROLE_DEVICE)) || event == USB_HOST_CONFIGURE_EVENT) {
        usb_device_t device;

        if(event == USB_HOST_CONFIGURE_EVENT) {
            device = usb_FindDevice(NULL, NULL, USB_SKIP_HUBS);
        } else {
            device = event_data;
        }

        if(device && !has_device) {
            /* Initialize the serial library with the newly attached device */
            srl_error_t error = srl_Init(&srl, device, srl_buf, sizeof srl_buf, SRL_INTERFACE_ANY);

            if(error) {
                /* Print the error code to the homescreen */
                char buf[64];
                sprintf(buf, "Error %u initting serial", error);

                os_ClrHome();
                os_PutStrFull(buf);
            } else {
                has_device = true;
            }
        }
    }
    if(event == USB_DEVICE_DISCONNECTED_EVENT) {
        has_device = false;
    }

    return USB_SUCCESS;
}

int main(void)
{
	os_ClrHome();

	os_PutStrLine("Starting...");
	os_NewLine();

	/* Initialize the USB driver with our event handler and the serial device descriptors */
	usb_error_t usb_error = usb_Init(handle_usb_event, NULL, srl_GetCDCStandardDescriptors(), USB_DEFAULT_INIT_FLAGS);
	if (usb_error) {
		usb_Cleanup();
		return 1;
	}

	/* Wait for a USB device to be connected */
	while (!has_device)
	{
		kb_Scan();

		/* Exit if clear is pressed */
		if (kb_IsDown(kb_KeyClear))
		{
			goto exit;
		}
		/* Handle any USB events that have occured */
		usb_HandleEvents();
	}

	os_NewLine();
	os_PutStrLine("USB Connected.");
	os_NewLine();
	os_PutStrLine("Waiting for program...");
	os_NewLine();

	do
	{
		kb_Scan();

		usb_HandleEvents();

		if (!has_device)
		{
			goto exit;
		}

		if (kb_IsDown(kb_KeyClear))
		{
			goto exit;
		}

		char in_buf[64];
		size_t bytes_read = srl_Read(&srl, in_buf, sizeof(in_buf));

		if (bytes_read)
		{
			if (strcmp(in_buf, "c"))
			{
				break;
			}
		}

	} while (true);

	os_PutStrLine("Sending keyboard data.");
	os_NewLine();

	do
	{
		kb_Scan();
		usb_HandleEvents();

		if (!has_device)
		{
			goto exit;
		}
		srl_Write_Blocking(&srl, "s", 2, 0);

		for (uint8_t i = 1; i < 8; i++)
		{
			char kb_Value[8] = "";
			sprintf(kb_Value, "%d", kb_Data[i]);
			srl_Write_Blocking(&srl, kb_Value, 8, 0);
			srl_Write_Blocking(&srl, ",", 2, 0);
		}

		srl_Write_Blocking(&srl, "\n", 2, 0);

		char in_buf[64];
		size_t bytes_read = srl_Read(&srl, in_buf, sizeof(in_buf));

		if (bytes_read)
		{
			if (in_buf[0] == 'd') 
			{
				has_device = false;
			}
		}

	} while (has_device);

	goto exit;

exit:
	usb_Cleanup();
	return 0;
}