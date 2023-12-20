#include "receiver.h"
#include <stdio.h>
#include "keypad.h"
#include <thread>

static uint8_t moveState = 0;
int mouseSpeed = 10;

uint32_t keyBindings[128];
uint8_t lastCalcKeyUp = -1;

uint8_t recData[7];
uint8_t prevRecData[7] = { 0 };

void receiveThread(libusb_device_handle* devHandle, volatile sig_atomic_t* stop)
{
	int received = 0;
	int inCount = 1;
	INPUT inputs[128] = { 0 };

	while (!*stop)
	{
		libusb_bulk_transfer(devHandle, 0x81, recData, 7, &received, 10);

		inCount = 0;

		if (received)
		{
			for (uint16_t i = 0; i < 128; i++)
			{
				if (!keyBindings[i]) continue;

				uint8_t dataIdx = (i >> 4) & 7;
				uint8_t keyShift = i & 0xF;
				uint8_t key = 1 << keyShift;

				if (dataIdx >= 0 && dataIdx <= 7)
				{
					if (keyBindings[i] & 0x10000)
					{
						inputs[inCount].type = INPUT_KEYBOARD;
						inputs[inCount].ki.wVk = (uint16_t)keyBindings[i];

						if ((recData[dataIdx] & key) && !(prevRecData[dataIdx] & key))
						{
							inputs[inCount].ki.dwFlags = 0;
							inCount++;
						}
						else if (!(recData[dataIdx] & key) && (prevRecData[dataIdx] & key))
						{
							inputs[inCount].ki.dwFlags = KEYEVENTF_KEYUP;
							inCount++;
						}
					}
					else
					{
						uint8_t mType = keyBindings[i];

						if (mType == MOUSEEVENTF_MOVE)
						{
							uint8_t dir = keyBindings[i] >> 8;

							if ((recData[dataIdx] & key) && !(prevRecData[dataIdx] & key))
							{
								moveState |= (1 << dir);
							}
							else if (!(recData[dataIdx] & key) && (prevRecData[dataIdx] & key))
							{
								moveState &= ~(1 << dir);
							}
						}
						else
						{
							inputs[inCount].type = INPUT_MOUSE;
							inputs[inCount].mi.dwFlags = mType;
							if ((recData[dataIdx] & key) && !(prevRecData[dataIdx] & key))
							{
								inCount++;
							}
							else if (!(recData[dataIdx] & key) && (prevRecData[dataIdx] & key))
							{
								inputs[inCount].mi.dwFlags <<= 1; // mouseeventf_UP
								inCount++;
							}
						}
					}
				}
			}

			for (int i = 0; i < 7; i++)
			{
				uint8_t diff = prevRecData[i] & ~(prevRecData[i] & recData[i]);
				if (diff)
				{
					if (diff == 0b1)
						lastCalcKeyUp = (i << 4) | 0;
					else if (diff == 0b10)
						lastCalcKeyUp = (i << 4) | 1;
					else if (diff == 0b100)
						lastCalcKeyUp = (i << 4) | 2;
					else if (diff == 0b1000)
						lastCalcKeyUp = (i << 4) | 3;
					else if (diff == 0b10000)
						lastCalcKeyUp = (i << 4) | 4;
					else if (diff == 0b100000)
						lastCalcKeyUp = (i << 4) | 5;
					else if (diff == 0b1000000)
						lastCalcKeyUp = (i << 4) | 6;
					else if (diff == 0b10000000)
						lastCalcKeyUp = (i << 4) | 7;
				}
			}

			memcpy(prevRecData, recData, received);
		}

		if (moveState)
		{
			inputs[inCount].type = INPUT_MOUSE;
			inputs[inCount].mi.dwFlags = MOUSEEVENTF_MOVE;
			inputs[inCount].mi.dy = (moveState & 0b1) * -mouseSpeed  + ((moveState & 0b10) >> 1) * mouseSpeed;
			inputs[inCount].mi.dx = ((moveState & 0b100) >> 2) * -mouseSpeed + ((moveState & 0b1000) >> 3) * mouseSpeed;
			inCount++;
		}

		SendInput(inCount, inputs, sizeof(INPUT));
		
	}

	printf("receive thread exited.\n");
}