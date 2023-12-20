#include "sender.h"
#include <stdint.h>
#include "leptonica/allheaders.h"
#include <math.h>
#include "compression.h"

#define CALC_WIDTH 320
#define CALC_HEIGHT 240
#define BITS_PER_PIXEL 32

#define BUF_LEN 512 + CALC_WIDTH * CALC_HEIGHT
constexpr double COLOR_MULT = 31 / 255.0;

constexpr int STATS_BUF_SIZE = 64;
bool running = false;

static uint16_t ConvertColor(uint32_t color)
{
	return (uint16_t)(((int)round((color & 0xFF) * COLOR_MULT) << 10) + ((int)round(((color >> 8) & 0xFF) * COLOR_MULT) << 5) + ((int)round(((color >> 16) & 0xFF) * COLOR_MULT) << 0));
}

void pixFunc(PIX* pix, uint8_t* sendBuf)
{
	PIX* qPic = pixOctreeColorQuant(pix, 240, 0);
	PIX* rPic = pixRotate90(qPic, 1);

	uint32_t* picData = pixGetData(rPic);
	uint32_t* colors = *(uint32_t**)pixGetColormap(rPic);

	for (int i = 0; i < 256; i++)
	{
		*(uint16_t*)(sendBuf + 2 * i) = ConvertColor(colors[i]);
	}

	for (size_t i = 0; i < CALC_WIDTH * CALC_HEIGHT / 4; i++)
	{
		*(uint32_t*)(sendBuf + 512 + 4 * i) = ((picData[i] & 0xFF) << 24) | (((picData[i] >> 8) & 0xFF) << 16) | (((picData[i] >> 16) & 0xFF) << 8) | ((picData[i] >> 24) & 0xFF);
	}

	pixDestroy(&qPic);
	pixDestroy(&rPic);
}

static float timeBuf[STATS_BUF_SIZE] = { 0 };
static int curBufIdx = 0;

float getFrameTime(void* data, int idx)
{
	return timeBuf[(curBufIdx + idx) % STATS_BUF_SIZE];
}

void sendThread(libusb_device_handle* devHandle, volatile sig_atomic_t* stop)
{
	running = true;
	WinDesktopDup dup;
	dup.Initialize();

	int sendLen = BUF_LEN;
	uint8_t sizeBuf[64] = { 0 };
	uint8_t* sendBuf = (uint8_t*)malloc(BUF_LEN);

	if (sendBuf == NULL)
	{
		throw;
	}

	PIX* picture = pixCreateHeader(320, 240, BITS_PER_PIXEL);

	uint8_t* bits = (uint8_t*)malloc(320 * 240 * 4 + 1);
	pixSetData(picture, (uint32_t*)bits);

	LARGE_INTEGER frequency;        // ticks per second
	LARGE_INTEGER t1, t2, t3;           // ticks

	QueryPerformanceFrequency(&frequency);

	while (!*stop)
	{
		if (!dup.CaptureNext(bits + 1))
		{
			printf("Error: failed to capture frame.\n");
			Sleep(10);
			dup.Close();
			Sleep(10);
			dup.Initialize();
		}

		QueryPerformanceCounter(&t1);

		pixFunc(picture, sendBuf);

		int outSize;
		sendLen = BUF_LEN;

		uint8_t* compBuf = compress(optimize(sendBuf + 512, BUF_LEN - 512, 0, 1, 25), sendBuf + 512, BUF_LEN - 512, 0, 0, 1, &outSize);

		if (compBuf != NULL)
		{
			if (outSize <= 60032)
			{
				sendLen = outSize + 512;
				if (sendLen % 64 != 0)
				{
					sendLen += 64 - (sendLen % 64);
				}

				memcpy(sendBuf + 512, compBuf, outSize);
				memset(sendBuf + outSize + 512, 0, (size_t)sendLen - outSize - 512);
			}
			else
			{
				printf("kinda compressed to: %d\n", outSize);
			}

			free(compBuf);
		}

		memcpy(sizeBuf, &sendLen, sizeof(int));

		QueryPerformanceCounter(&t2);

		int sentLen = 0;

		libusb_bulk_transfer(devHandle, 2, sizeBuf, 64, &sentLen, 0);

		if (sentLen != 64)
		{
			*stop = 1;
			printf("Failed to send. Exiting...\n");
			break;
		}


		for (size_t i = 0; i < sendLen; i += 64)
		{
			libusb_bulk_transfer(devHandle, 2, sendBuf + i, 64, &sentLen, 0);
		}

		QueryPerformanceCounter(&t3);
		float t = (t3.QuadPart - t1.QuadPart) * 1000.0 / frequency.QuadPart;
		long l = sendLen + 64;

		timeBuf[curBufIdx++] = t;
		if (curBufIdx == STATS_BUF_SIZE)
		{
			curBufIdx = 0;
		}

	}

	sendLen = -1;
	memcpy(sizeBuf, &sendLen, sizeof(int));
	libusb_bulk_transfer(devHandle, 2, sizeBuf, 64, NULL, 0);

	pixDestroy(&picture);

	dup.Close();
	running = false;
}