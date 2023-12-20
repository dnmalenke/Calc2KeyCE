#include <compression.h>
#include <keypadc.h>
#include <string.h>
#include <sys/basicusb.h>
#include <sys/lcd.h>
#include <sys/timers.h>
#include <ti/getcsc.h>
#include <usbdrvce.h>

#define BUFFER_SIZE 1024
#define BUFFER2_SIZE 60032
#define TIMER_FREQ      32768 // Frequency of timer in Hz
#define KEY_RATE      (TIMER_FREQ /16)

static usb_device_descriptor_t _dev_descriptor = { 0x12, USB_DEVICE_DESCRIPTOR, 0x200, 0x00, 0x00, 0x00, 0x40, 0x0451, 0xE009, 0x0240, 0x00, 0x00, 0x00, 0x01 };
static uint8_t _config_descriptors[] = { 0x09, 0x02, 0x20, 0x00, 0x01, 0x01, 0x00, 0x80, 0xFA, 0x09, 0x04, 0x00, 0x00, 0x02, 0xFF, 0x01, 0x01, 0x00, 0x07, 0x05, 0x81, 0x02, 0x40, 0x00, 0x00, 0x07, 0x05, 0x02, 0x02, 0x40, 0x00, 0x00 };
static const usb_configuration_descriptor_t* _config_descriptors_array[] = { (usb_configuration_descriptor_t*)&_config_descriptors };

static usb_standard_descriptors_t _std_descriptor =
{
	.device = &_dev_descriptor,
	.configurations = _config_descriptors_array,
	.langids = NULL,
	.numStrings = 0,
	.strings = NULL,
};

static usb_error_t event_handler(usb_event_t event, void* event_data, usb_callback_data_t* callback_data);
static usb_error_t key_transfer_callback(usb_endpoint_t endpoint, usb_transfer_status_t status, size_t transferred, usb_transfer_data_t* data);
static usb_error_t screen_transfer_callback(usb_endpoint_t endpoint, usb_transfer_status_t status, size_t transferred, usb_transfer_data_t* data);

static usb_endpoint_t _out_endpoint = NULL;
static usb_endpoint_t _in_endpoint = NULL;

static bool _connected = false;
static bool _transfer_scheduled = false;
static bool _key_transfer_complete = true;

static uint32_t _transfer_progress = 0;
static uint32_t _screen_offset = 0;
static int32_t _screen_size = 0;
static size_t _request_size = 64;

static uint8_t* _palette = lcd_CrsrImage;
static uint8_t _keys[7];
static uint8_t _in_buffer[BUFFER_SIZE];
static uint8_t _compressed_buffer[BUFFER2_SIZE];

static void* _buffer_location;

extern void lcd_Configure();
extern void lcd_Reset();
extern void zx0_DecompressMega(void* dst, const void* src);

int main(void)
{
	if (usb_Init(event_handler, NULL, &_std_descriptor, USB_DEFAULT_INIT_FLAGS))
	{
		usb_Cleanup();
		return 1;
	}

	timer_Disable(1);

	timer_Set(1, KEY_RATE);
	timer_SetReload(1, KEY_RATE);

	timer_Enable(1, TIMER_32K, TIMER_0INT, TIMER_DOWN);

	kb_EnableOnLatch();
	kb_ClearOnLatch();

	// https://wikiti.brandonw.net/index.php?title=84PCE:Ports:4000
	memset(lcd_Palette, 0, 512);
	lcd_Control = (uint24_t)0b00000100000100111; // 8bpp palette mode
	lcd_Configure();

	memset(lcd_Ram, 0, 320 * 240 * 2);

	for (;;)
	{
		if (kb_On)
		{
			break;
		}

		if (usb_HandleEvents() != USB_SUCCESS)
		{
			break;
		}

		if (_connected)
		{
			if (!_transfer_scheduled)
			{
				usb_ScheduleTransfer(_out_endpoint, &_in_buffer, _request_size, screen_transfer_callback, &_in_buffer);

				_transfer_scheduled = true;
			}

			if (timer_ChkInterrupt(1, TIMER_RELOADED))
			{
				kb_Scan();

				if (_keys[0] != kb_Data[1] || _keys[1] != kb_Data[2] || _keys[2] != kb_Data[3] ||
					_keys[3] != kb_Data[4] || _keys[4] != kb_Data[5] || _keys[5] != kb_Data[6] || _keys[6] != kb_Data[7])
				{
					if (_key_transfer_complete)
					{
						_keys[0] = kb_Data[1];
						_keys[1] = kb_Data[2];
						_keys[2] = kb_Data[3];
						_keys[3] = kb_Data[4];
						_keys[4] = kb_Data[5];
						_keys[5] = kb_Data[6];
						_keys[6] = kb_Data[7];

						_key_transfer_complete = false; 
						usb_ScheduleTransfer(_in_endpoint, _keys, 7, key_transfer_callback, NULL);
					}
				}

				timer_AckInterrupt(1, TIMER_RELOADED);
			}
		}
	}

	lcd_Reset();

	lcd_UpBase = (uint24_t)lcd_Ram; // reset lcd buffer location
	lcd_Control = 0b00000100100101101; // back to rgb 565 mode

	usb_Cleanup();

	// because we're using pixelShadow as the base of heap, clear it out to eliminate screen artifacts on exit
	memset((void*)(0x0D031F6), 0, 8400);

	return 0;
}

static usb_error_t key_transfer_callback(usb_endpoint_t endpoint, usb_transfer_status_t status, size_t transferred, usb_transfer_data_t* data)
{
	(void)endpoint;
	(void)status;
	(void)transferred;
	(void)data;
	
	_key_transfer_complete = true;

	return USB_SUCCESS;
}

static usb_error_t screen_transfer_callback(usb_endpoint_t endpoint, usb_transfer_status_t status, size_t transferred, usb_transfer_data_t* data)
{
	(void)endpoint;
	(void)status;

	if (transferred != 64 || _transfer_progress != 0)
	{
		if (_transfer_progress)
		{
			memcpy(_buffer_location + _transfer_progress - 512, data, transferred);
		}
		else
		{
			memcpy((void*)_palette, data, 512);
			memcpy(_buffer_location, ((char*)data + 512), transferred - 512);
		}

		_transfer_progress += transferred;

		if (_transfer_progress >= (uint32_t)_screen_size)
		{
			_transfer_progress = 0;
			_request_size = 64;

			if (_screen_size <= BUFFER2_SIZE + 512)
			{
				zx0_DecompressMega((void*)lcd_Ram + _screen_offset, _compressed_buffer);
			}

			memcpy((void*)lcd_Palette, _palette, 512);

			lcd_UpBase = (uint24_t)(lcd_Ram + _screen_offset);
			_screen_offset = _screen_offset == 0 ? LCD_WIDTH * LCD_HEIGHT : 0;
		}
		else
		{
			if (_screen_size - _transfer_progress < BUFFER_SIZE)
			{
				_request_size = _screen_size - _transfer_progress;
			}
		}
	}
	else
	{
		memcpy(&_screen_size, data, sizeof(int32_t));

		if (_screen_size == -1)
		{
			memset(lcd_Ram, 0, 320 * 240 * 2);

			_transfer_progress = 0;
			_request_size = 64;
			_screen_size = 0;
			_transfer_scheduled = false;

			return USB_SUCCESS;
		}

		_request_size = _screen_size > BUFFER_SIZE ? BUFFER_SIZE : _screen_size;

		if (_screen_size <= BUFFER2_SIZE)
		{
			memset(_compressed_buffer + (_screen_size - 512), 0, BUFFER2_SIZE - (_screen_size - 512)); // so the decompression doesn't see leftover data from last frame
			_buffer_location = (void*)_compressed_buffer;
		}
		else
		{
			_buffer_location = lcd_Ram + _screen_offset;
		}
	}

	_transfer_scheduled = false;

	return USB_SUCCESS;
}

static usb_error_t event_handler(usb_event_t event, void* event_data, usb_callback_data_t* callback_data)
{
	(void)event_data;
	(void)callback_data;

	if (event == USB_HOST_CONFIGURE_EVENT)
	{
		usb_device_t host_device = usb_FindDevice(NULL, NULL, USB_SKIP_HUBS);

		if (!host_device)
		{
			return USB_ERROR_NO_DEVICE;
		}

		_out_endpoint = usb_GetDeviceEndpoint(host_device, 0x02);
		_in_endpoint = usb_GetDeviceEndpoint(host_device, 0x81);

		_connected = true;
	}

	return USB_SUCCESS;
}