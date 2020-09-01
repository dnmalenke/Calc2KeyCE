#include <tice.h>
#include <string.h>
#include <keypadc.h>
#include <stdio.h>

#define usb_callback_data_t usb_device_t

#include <usbdrvce.h>
#include <srldrvce.h>

/* Handle USB events */
static usb_error_t handle_usb_event(usb_event_t event, void *event_data,
                                    usb_callback_data_t *callback_data)
{
    /* When a device is connected, or when connected to a computer */
    if (event == USB_DEVICE_CONNECTED_EVENT || event == USB_HOST_CONFIGURE_EVENT)
    {
        if (!*callback_data)
        {
            /* Set the USB device */
            *callback_data = event_data;
        }
        return USB_SUCCESS;
    }

    if (event == USB_DEVICE_DISCONNECTED_EVENT || event == USB_DEVICE_DEVICE_INTERRUPT || event == USB_DEVICE_SUSPEND_INTERRUPT)
    {
        *callback_data = NULL;
        return USB_SUCCESS;
    }
    return srl_HandleEvent(event, event_data);
}

int main(void)
{
    usb_error_t usb_error;
    srl_error_t srl_error;

    usb_device_t usb_device = NULL;
    srl_device_t srl;

    /* A buffer for internal use by the serial library */
    uint8_t srl_buf[2048];

    os_ClrHome();

    os_PutStrLine("Starting...");
    os_NewLine();

    /* Initialize the USB driver with our event handler and the serial device descriptors */
    usb_error = usb_Init(handle_usb_event, &usb_device, srl_GetCDCStandardDescriptors(), USB_DEFAULT_INIT_FLAGS);
    if (usb_error)
        goto exit;

    /* Wait for a USB device to be connected */
connectUsb:
    while (!usb_device)
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

    /* Initialize the serial library with the USB device */
    srl_error = srl_Init(&srl, usb_device, srl_buf, sizeof(srl_buf), SRL_INTERFACE_ANY);
    if (srl_error)
        goto exit;

    os_PutStrLine("Serial Connected.");
    os_NewLine();
    os_PutStrLine("Waiting for program...");
    os_NewLine();

    do
    {
        usb_HandleEvents();

        if (!usb_device)
        {
            goto exit;
        }

        if (kb_IsDown(kb_KeyClear))
        {
            goto exit;
        }

        char in_buf[64];
        size_t bytes_read;

        bytes_read = srl_Read(&srl, in_buf, sizeof(in_buf));

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

        srl_Write_Blocking(&srl, "s", 2, 100);

        for (uint8_t i = 1; i < 8; i++)
        {
            char kb_Value[8] = "";
            sprintf(kb_Value, "%d", kb_Data[i]);
            srl_Write_Blocking(&srl, kb_Value, 8, 100);
            srl_Write_Blocking(&srl, ",", 2, 100);
        }

        srl_Write_Blocking(&srl, "\n", 2, 100);

        char in_buf[64];
        size_t bytes_read;

        bytes_read = srl_Read(&srl, in_buf, sizeof(in_buf));

        if (bytes_read)
        {
            if (strcmp(in_buf, "d"))
            {
                goto exit;
            }
        }

        if (!usb_device)
        {
            goto exit;
        }

    } while (usb_device);

    goto exit;

exit:
    usb_Cleanup();
    return 0;
}