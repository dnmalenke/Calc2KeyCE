#include <Windows.h>
#include <commdlg.h>
#include <ShObjIdl_core.h>

#include "sender.h"
#include "receiver.h"
#include <stdio.h>
#include <stdint.h>
#include <cmath>
#include <csignal>
#include <thread>
#include "keypad.h"

#include <libwdi.h>

#include "imgui.h"
#include "imgui_impl_win32.h"
#include "imgui_impl_dx11.h"
#include <d3d11.h>
#include <tchar.h>

#include <libusb-1.0/libusb.h>

volatile sig_atomic_t stop;

bool done = false;
bool bindingKeyState = false;

int calcState = 0;

uint8_t bindingKey = -1;
uint32_t newBinding = 0;

static ID3D11Device* g_pd3dDevice = nullptr;
static ID3D11DeviceContext* g_pd3dDeviceContext = nullptr;
static IDXGISwapChain* g_pSwapChain = nullptr;
static UINT                     g_ResizeWidth = 0, g_ResizeHeight = 0;
static ID3D11RenderTargetView* g_mainRenderTargetView = nullptr;

void CreateRenderTarget()
{
	ID3D11Texture2D* pBackBuffer;
	g_pSwapChain->GetBuffer(0, IID_PPV_ARGS(&pBackBuffer));
	g_pd3dDevice->CreateRenderTargetView(pBackBuffer, nullptr, &g_mainRenderTargetView);
	pBackBuffer->Release();
}

void CleanupRenderTarget()
{
	if (g_mainRenderTargetView) { g_mainRenderTargetView->Release(); g_mainRenderTargetView = nullptr; }
}

bool CreateDeviceD3D(HWND hWnd)
{
	// Setup swap chain
	DXGI_SWAP_CHAIN_DESC sd;
	ZeroMemory(&sd, sizeof(sd));
	sd.BufferCount = 2;
	sd.BufferDesc.Width = 0;
	sd.BufferDesc.Height = 0;
	sd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	sd.BufferDesc.RefreshRate.Numerator = 60;
	sd.BufferDesc.RefreshRate.Denominator = 1;
	sd.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH;
	sd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	sd.OutputWindow = hWnd;
	sd.SampleDesc.Count = 1;
	sd.SampleDesc.Quality = 0;
	sd.Windowed = TRUE;
	sd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

	UINT createDeviceFlags = 0;
	//createDeviceFlags |= D3D11_CREATE_DEVICE_DEBUG;
	D3D_FEATURE_LEVEL featureLevel;
	const D3D_FEATURE_LEVEL featureLevelArray[2] = { D3D_FEATURE_LEVEL_11_0, D3D_FEATURE_LEVEL_10_0, };
	HRESULT res = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, createDeviceFlags, featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
	if (res == DXGI_ERROR_UNSUPPORTED) // Try high-performance WARP software driver if hardware is not available.
		res = D3D11CreateDeviceAndSwapChain(nullptr, D3D_DRIVER_TYPE_WARP, nullptr, createDeviceFlags, featureLevelArray, 2, D3D11_SDK_VERSION, &sd, &g_pSwapChain, &g_pd3dDevice, &featureLevel, &g_pd3dDeviceContext);
	if (res != S_OK)
		return false;

	CreateRenderTarget();
	return true;
}

void CleanupDeviceD3D()
{
	CleanupRenderTarget();
	if (g_pSwapChain) { g_pSwapChain->Release(); g_pSwapChain = nullptr; }
	if (g_pd3dDeviceContext) { g_pd3dDeviceContext->Release(); g_pd3dDeviceContext = nullptr; }
	if (g_pd3dDevice) { g_pd3dDevice->Release(); g_pd3dDevice = nullptr; }
}

// Forward declare message handler from imgui_impl_win32.cpp
extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

// Win32 message handler
// You can read the io.WantCaptureMouse, io.WantCaptureKeyboard flags to tell if dear imgui wants to use your inputs.
// - When io.WantCaptureMouse is true, do not dispatch mouse input data to your main application, or clear/overwrite your copy of the mouse data.
// - When io.WantCaptureKeyboard is true, do not dispatch keyboard input data to your main application, or clear/overwrite your copy of the keyboard data.
// Generally you may always pass all inputs to dear imgui, and hide them from your application based on those two flags.
LRESULT WINAPI WndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
		return true;

	switch (msg)
	{
	case WM_SIZE:
		if (wParam == SIZE_MINIMIZED)
			return 0;
		g_ResizeWidth = (UINT)LOWORD(lParam); // Queue resize
		g_ResizeHeight = (UINT)HIWORD(lParam);
		return 0;
	case WM_KEYUP:
		if (bindingKeyState && newBinding & 0x10000)
		{
			newBinding = 0x10000 | (uint16_t)wParam;
		}
		break;
	case WM_CLOSE:
		done = true;
		stop = true;
		while (running)
		{
			Sleep(10);
		}
		break;
	case WM_DESTROY:
		::PostQuitMessage(0);
		return 0;
	}
	return ::DefWindowProcW(hWnd, msg, wParam, lParam);
}

void runner()
{
	libusb_init_context(NULL, NULL, 0);

	while (!done)
	{
		calcState = 0;

		libusb_device_handle* devHandle = NULL;
		while (devHandle == NULL && !done)
		{
			devHandle = libusb_open_device_with_vid_pid(NULL, 0x0451, 0xE009);

			if (devHandle == NULL)
			{
				struct wdi_device_info* device, * list;
				struct wdi_options_create_list opts = {};
				opts.list_all = true;
				if (wdi_create_list(&list, &opts) == WDI_SUCCESS)
				{
					bool found = false;
					for (device = list; device != NULL; device = device->next)
					{
						if ((device->vid == 0x0451) && (device->pid == 0xE009) && device->driver == NULL)
						{
							calcState = 2;
						}

						if ((device->vid == 0x0451) && (device->pid == 0xE008))
						{
							calcState = 1;
						}
					}

					wdi_destroy_list(list);
				}
			}
			else
			{
				calcState = 3;
				break;
			}

			Sleep(100);
		}

		if (done)
		{
			libusb_exit(NULL);
			return;
		}

		libusb_set_auto_detach_kernel_driver(devHandle, 1);
		libusb_claim_interface(devHandle, 0);

		std::thread tRecv(receiveThread, devHandle, &stop);
		std::thread tSend(sendThread, devHandle, &stop);

		tRecv.join();
		tSend.join();

		libusb_close(devHandle);

		stop = false;
	}

	libusb_exit(NULL);
}

static void drawStats()
{
	ImGui::Text("Frame Times:");
	float sum = 0;
	int count = 64;
	float frameRates[64] = { 0 };
	for (int i = 0; i < 64; i++)
	{
		float t = getFrameTime(NULL, i);
		sum += t;

		if (!t)
		{
			count--;
		}
		else
		{
			frameRates[i] = 1000.0 / t;
		}
	}

	if (count == 0) count = 1;

	char textBuf[64];
	float latestTime = getFrameTime(NULL, 63);
	sprintf_s(textBuf, 64, "Average: %.2f ms\nLatest: %.2f ms", sum / count, getFrameTime(NULL, 63));

	ImGui::PlotLines("", getFrameTime, NULL, 64, 0, textBuf, 0, 350, ImVec2(0, 128.0f));

	ImGui::Text("Frame Rate:");

	if (count == 0) sum = 1;

	if (latestTime == 0) latestTime = 1000;

	sprintf_s(textBuf, 64, "Average: %.2f fps\nLatest: %.2f fps", 1000 * count / sum, 1000.0 / latestTime);

	ImGui::PlotLines("", frameRates, 64, 0, textBuf, 0, 60, ImVec2(0, 128.0f));
}

const char* getCalcKeyName(uint8_t i)
{
	uint8_t dataIdx = (i >> 4) & 7;
	uint8_t keyShift = i & 0xF;
	uint8_t key = 1 << keyShift;
	switch (dataIdx)
	{
	case 0:
		if (key & calc_Graph) return "Graph";
		if (key & calc_Trace) return "Trace";
		if (key & calc_Zoom) return "Zoom";
		if (key & calc_Window) return "Window";
		if (key & calc_Yequ) return "Yequ";
		if (key & calc_Second) return "Second";
		if (key & calc_Mode) return "Mode";
		if (key & calc_Del) return "Del";
		break;
	case 1:
		if (key & calc_Sto) return "Sto";
		if (key & calc_Ln) return "Ln";
		if (key & calc_Log) return "Log";
		if (key & calc_Square) return "Square";
		if (key & calc_Recip) return "Recip";
		if (key & calc_Math) return "Math";
		if (key & calc_Alpha) return "Alpha";
	case 2:
		if (key & calc_Zero) return "Zero";
		if (key & calc_One) return "One";
		if (key & calc_Four) return "Four";
		if (key & calc_Seven) return "Seven";
		if (key & calc_Comma) return "Comma";
		if (key & calc_Sin) return "Sin";
		if (key & calc_Apps) return "Apps";
		if (key & calc_GraphVar) return "GraphVar";
		break;
	case 3:
		if (key & calc_DecPnt) return "DecPnt";
		if (key & calc_Two) return "Two";
		if (key & calc_Five) return "Five";
		if (key & calc_Eight) return "Eight";
		if (key & calc_LParen) return "LParen";
		if (key & calc_Cos) return "Cos";
		if (key & calc_Prgm) return "Prgm";
		if (key & calc_Stat) return "Stat";
		break;
	case 4:
		if (key & calc_Chs) return "Chs";
		if (key & calc_Three) return "Three";
		if (key & calc_Six) return "Six";
		if (key & calc_Nine) return "Nine";
		if (key & calc_RParen) return "RParen";
		if (key & calc_Tan) return "Tan";
		if (key & calc_Vars) return "Vars";
		break;
	case 5:
		if (key & calc_Enter) return "Enter";
		if (key & calc_Add) return "Add";
		if (key & calc_Sub) return "Sub";
		if (key & calc_Mul) return "Mul";
		if (key & calc_Div) return "Div";
		if (key & calc_Power) return "Power";
		if (key & calc_Clear) return "Clear";
		break;
	case 6:
		if (key & calc_Down) return "Down";
		if (key & calc_Left) return "Left";
		if (key & calc_Right) return "Right";
		if (key & calc_Up) return "Up";
		break;
	}

	return "";
}

void drawKeyboardKeyName(const char* format, uint16_t keyCode)
{
	char buf[64];

	if ((keyCode >= 0x30 && keyCode <= 0x39) || (keyCode >= 0x41 && keyCode <= 0x5A))
	{
		sprintf_s(buf, 64, "%c", keyCode);
	}
	else
	{
		sprintf_s(buf, 64, "Key Code: 0x%x", keyCode);
	}

	ImGui::Text(format, buf);
}

static void drawKeyBindings()
{
	if (ImGui::BeginTable("table", 2))
	{
		ImGui::TableSetupColumn("", ImGuiTableColumnFlags_WidthFixed, 0);
		ImGui::TableNextRow();
		ImGui::TableSetColumnIndex(0);

		if (ImGui::Button("Bind key"))
		{
			lastCalcKeyUp = -1;
			bindingKeyState = true;
			bindingKey = -1;
		}

		if (bindingKeyState)
		{
			if (lastCalcKeyUp == (uint8_t)-1)
			{
				ImGui::Text("Press a calculator key to bind");
			}
			else
			{
				if (bindingKey == (uint8_t)-1)
				{
					bindingKey = lastCalcKeyUp;
					newBinding = keyBindings[bindingKey];

					if (newBinding == 0)
					{
						newBinding = 0x10000;
					}
				}

				ImGui::Text("Binding Key: %s", getCalcKeyName(bindingKey));
			}

			if (bindingKey != (uint8_t)-1)
			{
				ImGui::Text("Input Type:");
				if (ImGui::RadioButton("Keyboard", newBinding & 0x10000))
				{
					newBinding = 0x10000;
				}

				if (ImGui::RadioButton("Mouse", !(newBinding & 0x10000)))
				{
					newBinding = 1;
				}

				if (newBinding & 0x10000)
				{
					if (!(newBinding & 0xFFFF))
					{
						ImGui::Text("Press a keyboard key to bind");
					}
					else
					{
						drawKeyboardKeyName("Seleted Key: %s", (uint16_t)newBinding);
					}
				}
				else
				{
					ImGui::Separator();

					uint8_t mType = newBinding;
					if (ImGui::RadioButton("Move Up", newBinding == 1))
						newBinding = 1;
					else if (ImGui::RadioButton("Move Down", newBinding == (1 | (1 << 8))))
						newBinding = 1 | (1 << 8);
					else if (ImGui::RadioButton("Move Left", newBinding == (1 | (2 << 8))))
						newBinding = 1 | (2 << 8);
					else if (ImGui::RadioButton("Move Right", newBinding == (1 | (3 << 8))))
						newBinding = 1 | (3 << 8);
					else if (ImGui::RadioButton("Left Click", newBinding == MOUSEEVENTF_LEFTDOWN))
						newBinding = MOUSEEVENTF_LEFTDOWN;
					else if (ImGui::RadioButton("Right Click", newBinding == MOUSEEVENTF_RIGHTDOWN))
						newBinding = MOUSEEVENTF_RIGHTDOWN;
				}

				if ((newBinding & 0x10000 && newBinding & 0xFFFF) || !(newBinding & 0x10000))
				{
					if (ImGui::Button("Bind"))
					{
						keyBindings[bindingKey] = newBinding;
						bindingKeyState = false;
					}
				}
			}

			if (ImGui::Button("Cancel"))
			{
				bindingKeyState = false;
			}
		}
		else
		{
			if (ImGui::Button("Save Keybindings"))
			{
				OPENFILENAMEA file = {};
				char buf[256] = { 0 };
				file.lStructSize = sizeof(OPENFILENAME);
				file.nFilterIndex = 1;
				file.lpstrFile = buf;
				file.nMaxFile = 256;
				file.lpstrTitle = "Open key things";
				file.Flags = OFN_CREATEPROMPT | OFN_PATHMUSTEXIST;

				if (GetSaveFileNameA(&file))
				{
					FILE* saveFile = NULL;
					fopen_s(&saveFile, file.lpstrFile, "w");
					if (saveFile != NULL)
					{
						fwrite(keyBindings, sizeof(uint32_t), 128, saveFile);
						fclose(saveFile);
					}
				}
			}

			if (ImGui::Button("Load Keybindings"))
			{
				OPENFILENAMEA file = {};
				char buf[256] = { 0 };
				file.lStructSize = sizeof(OPENFILENAME);
				file.nFilterIndex = 1;
				file.lpstrFile = buf;
				file.nMaxFile = 256;
				file.lpstrTitle = "Open key things";
				file.Flags = OFN_CREATEPROMPT | OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;

				if (GetOpenFileNameA(&file))
				{
					FILE* loadFile = NULL;
					fopen_s(&loadFile, file.lpstrFile, "r");
					if (loadFile != NULL)
					{
						fread(keyBindings, sizeof(uint32_t), 128, loadFile);
						fclose(loadFile);
					}
				}
			}
		}

		ImGui::TableSetColumnIndex(1);

		if (ImGui::BeginTable("boundTable", 3))
		{
			ImGui::TableSetupColumn("Calc Key");
			ImGui::TableSetupColumn("Type");
			ImGui::TableSetupColumn("Action");
			ImGui::TableHeadersRow();

			for (uint8_t i = 0; i < 128; i++)
			{
				if (!keyBindings[i]) continue;

				ImGui::TableNextRow();

				ImGui::TableSetColumnIndex(0);
				ImGui::Text(getCalcKeyName(i));

				if (keyBindings[i] & 0x10000)
				{
					ImGui::TableSetColumnIndex(1);
					ImGui::Text("Keyboard");

					ImGui::TableSetColumnIndex(2);

					uint16_t wVk = (uint16_t)keyBindings[i];
					drawKeyboardKeyName("%s", wVk);
				}
				else
				{
					ImGui::TableSetColumnIndex(1);
					ImGui::Text("Mouse");

					ImGui::TableSetColumnIndex(2);
					uint8_t mType = keyBindings[i];

					if (mType == MOUSEEVENTF_MOVE)
					{
						uint8_t dir = keyBindings[i] >> 8;
						switch (dir)
						{
						case 0:
							ImGui::Text("Move Up");
							break;
						case 1:
							ImGui::Text("Move Down");
							break;
						case 2:
							ImGui::Text("Move Left");
							break;
						case 3:
							ImGui::Text("Move Right");
							break;
						}
					}
					else
					{
						if (mType == MOUSEEVENTF_LEFTDOWN)
						{
							ImGui::Text("Left Click");
						}
						else if (mType == MOUSEEVENTF_RIGHTDOWN)
						{
							ImGui::Text("Right Click");
						}
					}
				}
			}
			ImGui::EndTable();
		}

		ImGui::EndTable();
	}

	ImGui::SliderInt("Mouse Speed", &mouseSpeed, 1, 100);

}

int APIENTRY WinMain(HINSTANCE hInst, HINSTANCE hInstPrev, PSTR cmdline, int cmdshow)
{
	if (!strcmp(cmdline, "--install-driver"))
	{
		struct wdi_device_info* device, * list;

		if (wdi_create_list(&list, NULL) == WDI_SUCCESS)
		{
			for (device = list; device != NULL; device = device->next)
			{
				if ((device->vid == 0x0451) && (device->pid == 0xE009))
				{
					struct wdi_options_prepare_driver opts = {};
					opts.driver_type = WDI_LIBUSB0;
					if (wdi_prepare_driver(device, "driver", "device.inf", &opts) == WDI_SUCCESS)
					{
						wdi_install_driver(device, "driver", "device.inf", NULL);
					}
				}
			}
			wdi_destroy_list(list);
		}

		return 0;
	}

	// Create application window
	ImGui_ImplWin32_EnableDpiAwareness();

	WNDCLASSEXW wc = { sizeof(wc), CS_CLASSDC, WndProc, 0L, 0L, hInst, nullptr, nullptr, nullptr, nullptr, L"Calc2KeyCE", nullptr };
	::RegisterClassExW(&wc);
	HWND hwnd = ::CreateWindowW(wc.lpszClassName, L"Calc2KeyCE", WS_OVERLAPPEDWINDOW, 100, 100, 500, 500, nullptr, nullptr, wc.hInstance, nullptr);

	// Initialize Direct3D
	if (!CreateDeviceD3D(hwnd))
	{
		CleanupDeviceD3D();
		::UnregisterClassW(wc.lpszClassName, wc.hInstance);
		return 1;
	}

	// Show the window
	::ShowWindow(hwnd, SW_SHOWDEFAULT);
	::UpdateWindow(hwnd);

	// Setup Dear ImGui context
	IMGUI_CHECKVERSION();
	ImGui::CreateContext();
	ImGuiIO& io = ImGui::GetIO(); (void)io;
	io.IniFilename = NULL;
	io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;     // Enable Keyboard Controls

	// Setup Dear ImGui style
	ImGui::StyleColorsDark();

	// Setup Platform/Renderer backends
	ImGui_ImplWin32_Init(hwnd);
	ImGui_ImplDX11_Init(g_pd3dDevice, g_pd3dDeviceContext);

	std::thread runThread(runner);

	// Main loop
	while (!done)
	{
		// Poll and handle messages (inputs, window resize, etc.)
		// See the WndProc() function below for our to dispatch events to the Win32 backend.
		MSG msg;
		while (::PeekMessage(&msg, nullptr, 0U, 0U, PM_REMOVE))
		{
			::TranslateMessage(&msg);
			::DispatchMessage(&msg);
			if (msg.message == WM_QUIT)
			{
				done = true;
				stop = true;
			}

		}
		if (done)
			break;

		// Handle window resize (we don't resize directly in the WM_SIZE handler)
		if (g_ResizeWidth != 0 && g_ResizeHeight != 0)
		{
			CleanupRenderTarget();
			g_pSwapChain->ResizeBuffers(0, g_ResizeWidth, g_ResizeHeight, DXGI_FORMAT_UNKNOWN, 0);
			g_ResizeWidth = g_ResizeHeight = 0;
			CreateRenderTarget();
		}

		// Start the Dear ImGui frame
		ImGui_ImplDX11_NewFrame();
		ImGui_ImplWin32_NewFrame();
		ImGui::NewFrame();

		ImGui::Begin("Main Window", NULL, ImGuiWindowFlags_NoDecoration | ImGuiWindowFlags_NoMove);

		ImGui::SetWindowSize(ImVec2(io.DisplaySize.x, io.DisplaySize.y));
		ImGui::SetWindowPos(ImVec2(0, 0));

		switch (calcState)
		{
		case 0:
			ImGui::Text("Calculator not detected.");
			break;
		case 1:
			ImGui::Text("Calculator found, but Calc2Key not running on device.");
			break;
		case 2:
			ImGui::Text("Calculator found, but needs driver.");
			if (ImGui::Button("Install Driver"))
			{
				char buf[256];
				GetModuleFileNameA(NULL, buf, 256);
				ShellExecuteA(NULL,
					"runas",
					buf,
					" --install-driver",
					NULL,
					SW_HIDE
				);
			}
			break;
		case 3:
			ImGui::Text("Connected to calculator.");
		default:
			break;
		}

		ImGui::Separator();

		if (ImGui::TreeNode("Stats"))
		{
			drawStats();
			ImGui::TreePop();
		}

		if (ImGui::TreeNode("Keybinding"))
		{
			drawKeyBindings();
			ImGui::TreePop();
		}

		ImGui::End();

		// Rendering
		ImGui::Render();
		const float clear_color_with_alpha[4] = { 0,0,0,0 };
		g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, nullptr);
		g_pd3dDeviceContext->ClearRenderTargetView(g_mainRenderTargetView, clear_color_with_alpha);
		ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());

		g_pSwapChain->Present(1, 0); // Present with vsync
	}

	// Cleanup
	ImGui_ImplDX11_Shutdown();
	ImGui_ImplWin32_Shutdown();
	ImGui::DestroyContext();

	CleanupDeviceD3D();
	::DestroyWindow(hwnd);
	::UnregisterClassW(wc.lpszClassName, wc.hInstance);

	runThread.join();

	return 0;
}
