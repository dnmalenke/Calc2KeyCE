#include "WinDesktopDup.h"

// https://github.com/bmharper/WindowsDesktopDuplicationSample

static VERTEX Vertices[4] =
{
	{DirectX::XMFLOAT3(-1.0f, -1.0f, 0), DirectX::XMFLOAT2(0.0f, 1.0f)},
	{DirectX::XMFLOAT3(-1.0f, 1.0f, 0), DirectX::XMFLOAT2(0.0f, 0.0f)},
	{DirectX::XMFLOAT3(1.0f, -1.0f, 0), DirectX::XMFLOAT2(1.0f, 1.0f)},
	{DirectX::XMFLOAT3(1.0f, 1.0f, 0), DirectX::XMFLOAT2(1.0f, 0.0f)},
};

WinDesktopDup::~WinDesktopDup()
{
	Close();
}

bool WinDesktopDup::Initialize()
{
	// Get desktop
	HDESK hDesk = OpenInputDesktop(0, FALSE, GENERIC_ALL);
	if (!hDesk)
		return false;

	// Attach desktop to this thread (presumably for cases where this is not the main/UI thread)
	bool deskAttached = SetThreadDesktop(hDesk) != 0;
	CloseDesktop(hDesk);
	hDesk = nullptr;
	if (!deskAttached)
		return false;

	// Initialize DirectX
	HRESULT hr = S_OK;

	// Driver types supported
	D3D_DRIVER_TYPE driverTypes[] = {
		D3D_DRIVER_TYPE_HARDWARE,
		D3D_DRIVER_TYPE_WARP,
		D3D_DRIVER_TYPE_REFERENCE,
	};
	auto numDriverTypes = ARRAYSIZE(driverTypes);

	// Feature levels supported
	D3D_FEATURE_LEVEL featureLevels[] = {
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0,
		D3D_FEATURE_LEVEL_9_1 };
	auto numFeatureLevels = ARRAYSIZE(featureLevels);

	D3D_FEATURE_LEVEL featureLevel;

	// Create device
	for (size_t i = 0; i < numDriverTypes; i++)
	{
		hr = D3D11CreateDevice(nullptr, driverTypes[i], nullptr, D3D11_CREATE_DEVICE_BGRA_SUPPORT, featureLevels, (UINT)numFeatureLevels,
			D3D11_SDK_VERSION, &D3DDevice, &featureLevel, &D3DDeviceContext);
		if (SUCCEEDED(hr))
			break;
	}
	if (FAILED(hr))
		return false;

	// Initialize the Desktop Duplication system
	//m_OutputNumber = Output;

	// Get DXGI device
	IDXGIDevice* dxgiDevice = nullptr;
	hr = D3DDevice->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgiDevice);
	if (FAILED(hr))
		return false;

	// Get DXGI adapter
	IDXGIAdapter* dxgiAdapter = nullptr;
	hr = dxgiDevice->GetParent(__uuidof(IDXGIAdapter), (void**)&dxgiAdapter);
	dxgiDevice->Release();
	dxgiDevice = nullptr;
	if (FAILED(hr))
	{
		return false;
	}

	// Get output
	IDXGIOutput* dxgiOutput = nullptr;
	hr = dxgiAdapter->EnumOutputs(OutputNumber, &dxgiOutput);
	dxgiAdapter->Release();
	dxgiAdapter = nullptr;
	if (FAILED(hr))
	{
		return false;
	}

	// QI for Output 1
	IDXGIOutput1* dxgiOutput1 = nullptr;
	hr = dxgiOutput->QueryInterface(__uuidof(dxgiOutput1), (void**)&dxgiOutput1);
	dxgiOutput->Release();
	dxgiOutput = nullptr;
	if (FAILED(hr))
		return false;

	// Create desktop duplication
	hr = dxgiOutput1->DuplicateOutput(D3DDevice, &DeskDupl);
	dxgiOutput1->Release();
	dxgiOutput1 = nullptr;
	if (FAILED(hr))
	{
		return false;
	}

	D3D11_TEXTURE2D_DESC targetDesc = { 0 };
	targetDesc.Width = 320;
	targetDesc.Height = 240;
	targetDesc.MipLevels = 1;
	targetDesc.ArraySize = 1;
	targetDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	targetDesc.SampleDesc.Count = 1;
	targetDesc.Usage = D3D11_USAGE_DEFAULT;
	targetDesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
	targetDesc.CPUAccessFlags = 0;
	targetDesc.MiscFlags = 0;

	D3DDevice->CreateTexture2D(&targetDesc, nullptr, &m_resizeTexture);

	if (m_resizeTexture == NULL)
	{
		return false;
	}

	D3DDevice->CreateRenderTargetView(m_resizeTexture, nullptr, &m_RTV);
	D3DDeviceContext->OMSetRenderTargets(1, &m_RTV, nullptr);

	D3D11_VIEWPORT VP{};
	VP.Width = static_cast<FLOAT>(320);
	VP.Height = static_cast<FLOAT>(240);
	VP.MinDepth = 0.0f;
	VP.MaxDepth = 1.0f;
	VP.TopLeftX = 0;
	VP.TopLeftY = 0;
	D3DDeviceContext->RSSetViewports(1, &VP);

	D3D11_SAMPLER_DESC SampDesc;
	memset(&SampDesc, 0, sizeof(SampDesc));
	SampDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	SampDesc.AddressU = D3D11_TEXTURE_ADDRESS_CLAMP;
	SampDesc.AddressV = D3D11_TEXTURE_ADDRESS_CLAMP;
	SampDesc.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
	SampDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	SampDesc.MinLOD = 0;
	SampDesc.MaxLOD = D3D11_FLOAT32_MAX;
	hr = D3DDevice->CreateSamplerState(&SampDesc, &m_SamplerLinear);

	D3D11_BLEND_DESC BlendStateDesc{};
	BlendStateDesc.AlphaToCoverageEnable = FALSE;
	BlendStateDesc.IndependentBlendEnable = FALSE;
	BlendStateDesc.RenderTarget[0].BlendEnable = TRUE;
	BlendStateDesc.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_ALPHA;
	BlendStateDesc.RenderTarget[0].DestBlend = D3D11_BLEND_INV_SRC_ALPHA;
	BlendStateDesc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
	BlendStateDesc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_ONE;
	BlendStateDesc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_ZERO;
	BlendStateDesc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
	BlendStateDesc.RenderTarget[0].RenderTargetWriteMask = D3D11_COLOR_WRITE_ENABLE_ALL;
	hr = D3DDevice->CreateBlendState(&BlendStateDesc, &m_BlendState);

	UINT Size = ARRAYSIZE(g_VS);
	hr = D3DDevice->CreateVertexShader(g_VS, Size, nullptr, &m_VertexShader);

	D3D11_INPUT_ELEMENT_DESC Layout[] =
	{
		{"POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D11_INPUT_PER_VERTEX_DATA, 0},
		{"TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 12, D3D11_INPUT_PER_VERTEX_DATA, 0}
	};
	UINT NumElements = ARRAYSIZE(Layout);
	hr = D3DDevice->CreateInputLayout(Layout, NumElements, g_VS, Size, &m_InputLayout);

	D3DDeviceContext->IASetInputLayout(m_InputLayout);

	Size = ARRAYSIZE(g_PS);
	hr = D3DDevice->CreatePixelShader(g_PS, Size, nullptr, &m_PixelShader);

	Sleep(10);

	return true;
}

void WinDesktopDup::Close()
{
	if (DeskDupl)
		DeskDupl->Release();

	if (D3DDeviceContext)
		D3DDeviceContext->Release();

	if (D3DDevice)
		D3DDevice->Release();

	DeskDupl = nullptr;
	D3DDeviceContext = nullptr;
	D3DDevice = nullptr;
	HaveFrameLock = false;

	if (m_resizeTexture)
		m_resizeTexture->Release();

	if (m_RTV)
		m_RTV->Release();

	if (m_SamplerLinear)
		m_SamplerLinear->Release();

	if (m_BlendState)
		m_BlendState->Release();

	if (m_VertexShader)
		m_VertexShader->Release();

	if (m_PixelShader)
		m_PixelShader->Release();

	if (m_InputLayout)
		m_InputLayout->Release();

	if (m_SrcSrv)
		m_SrcSrv->Release();

	m_resizeTexture = nullptr;
	m_RTV = nullptr;
	m_SamplerLinear = nullptr;
	m_BlendState = nullptr;
	m_VertexShader = nullptr;
	m_PixelShader = nullptr;
	m_InputLayout = nullptr;
	m_SrcSrv = nullptr;
}

bool WinDesktopDup::CaptureNext(uint8_t* dest)
{
	if (!DeskDupl)
		return false;

	HRESULT hr;

	if (HaveFrameLock)
	{
		HaveFrameLock = false;
		hr = DeskDupl->ReleaseFrame();
	}

	IDXGIResource* deskRes = nullptr;
	DXGI_OUTDUPL_FRAME_INFO frameInfo;

	while (true)
	{
		hr = DeskDupl->AcquireNextFrame(0, &frameInfo, &deskRes);
		if (hr != DXGI_ERROR_WAIT_TIMEOUT)
		{
			break;
		}
		Sleep(1);
	}
	if (FAILED(hr))
	{
		return false;
	}

	HaveFrameLock = true;

	ID3D11Texture2D* gpuTex = nullptr;
	hr = deskRes->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&gpuTex);
	deskRes->Release();
	deskRes = nullptr;
	if (FAILED(hr))
	{
		return false;
	}

	ID3D11Texture2D* lGDIImage = nullptr;
	D3D11_TEXTURE2D_DESC desc;
	gpuTex->GetDesc(&desc);
	desc.CPUAccessFlags = 0;
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_FLAG::D3D11_BIND_RENDER_TARGET | D3D11_BIND_FLAG::D3D11_BIND_SHADER_RESOURCE;
	desc.MiscFlags = D3D11_RESOURCE_MISC_GDI_COMPATIBLE;

	hr = D3DDevice->CreateTexture2D(&desc, NULL, &lGDIImage);
	if (FAILED(hr) || lGDIImage == nullptr)
	{
		return false;
	}

	D3DDeviceContext->CopyResource(lGDIImage, gpuTex);

	IDXGISurface1* dxgiSurface1 = nullptr;
	lGDIImage->QueryInterface(__uuidof(IDXGISurface1), (void**)&dxgiSurface1);

	CURSORINFO lCursorInfo = { 0 };

	lCursorInfo.cbSize = sizeof(lCursorInfo);

	BOOL ciRes = GetCursorInfo(&lCursorInfo);
	if (ciRes == TRUE && lCursorInfo.flags == CURSOR_SHOWING)
	{
		POINT lCursorPosition = lCursorInfo.ptScreenPos;

		DWORD lCursorSize = lCursorInfo.cbSize;

		HDC  lHDC;

		dxgiSurface1->GetDC(FALSE, &lHDC);

		DrawIconEx(
			lHDC,
			lCursorPosition.x,
			lCursorPosition.y,
			lCursorInfo.hCursor,
			0,
			0,
			0,
			0,
			DI_NORMAL | DI_DEFAULTSIZE);

		dxgiSurface1->ReleaseDC(nullptr);
	}

	hr = D3DDevice->CreateShaderResourceView(lGDIImage, NULL, &m_SrcSrv);


	// Set resources
	UINT Stride = sizeof(VERTEX);
	UINT Offset = 0;
	D3DDeviceContext->OMSetBlendState(nullptr, nullptr, 0xffffffff);
	D3DDeviceContext->OMSetRenderTargets(1, &m_RTV, nullptr);
	D3DDeviceContext->VSSetShader(m_VertexShader, nullptr, 0);
	D3DDeviceContext->PSSetShader(m_PixelShader, nullptr, 0);
	D3DDeviceContext->PSSetShaderResources(0, 1, &m_SrcSrv);
	D3DDeviceContext->PSSetSamplers(0, 1, &m_SamplerLinear);
	D3DDeviceContext->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP);

	D3D11_BUFFER_DESC BufferDesc;
	RtlZeroMemory(&BufferDesc, sizeof(BufferDesc));
	BufferDesc.Usage = D3D11_USAGE_DEFAULT;
	BufferDesc.ByteWidth = sizeof(VERTEX) * 4;
	BufferDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
	BufferDesc.CPUAccessFlags = 0;
	D3D11_SUBRESOURCE_DATA InitData;
	RtlZeroMemory(&InitData, sizeof(InitData));
	InitData.pSysMem = Vertices;

	ID3D11Buffer* VertexBuffer = nullptr;

	// Create vertex buffer
	hr = D3DDevice->CreateBuffer(&BufferDesc, &InitData, &VertexBuffer);
	if (FAILED(hr))
	{
		m_SrcSrv->Release();
		m_SrcSrv = nullptr;
		return false;
	}
	D3DDeviceContext->IASetVertexBuffers(0, 1, &VertexBuffer, &Stride, &Offset);

	// Draw textured quad onto render target
	D3DDeviceContext->Draw(4, 0);

	VertexBuffer->Release();
	VertexBuffer = nullptr;

	// Release shader resource
	m_SrcSrv->Release();
	m_SrcSrv = nullptr;

	bool ok = true;

	m_resizeTexture->GetDesc(&desc);
	desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE | D3D11_CPU_ACCESS_READ;
	desc.Usage = D3D11_USAGE_STAGING;
	desc.BindFlags = 0;
	desc.MiscFlags = 0;
	ID3D11Texture2D* cpuTex = nullptr;
	hr = D3DDevice->CreateTexture2D(&desc, nullptr, &cpuTex);
	if (SUCCEEDED(hr) && cpuTex != NULL)
	{
		D3DDeviceContext->CopyResource(cpuTex, m_resizeTexture);
	}
	else
	{
		ok = false;
	}

	if (ok)
	{
		D3D11_MAPPED_SUBRESOURCE sr;
		hr = D3DDeviceContext->Map(cpuTex, 0, D3D11_MAP_READ, 0, &sr);
		if (SUCCEEDED(hr))
		{
			memcpy(dest, sr.pData, 320 * 240 * 4);
			D3DDeviceContext->Unmap(cpuTex, 0);
		}
		else
		{
			ok = false;
		}
	}

	dxgiSurface1->Release();
	lGDIImage->Release();
	cpuTex->Release();
	gpuTex->Release();

	return ok;
}
