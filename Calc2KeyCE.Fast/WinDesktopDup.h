#pragma once
#pragma comment(lib,"d3d11.lib")
#include <SDKDDKVer.h>

#define WIN32_LEAN_AND_MEAN // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>

// C RunTime Header Files
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <tchar.h>

#include <stdint.h>

#include <string>
#include <functional>
#include <vector>

#include <d3d11.h>
#include <dxgi1_2.h>
#include <DirectXMath.h>

#include "PixelShader.h"
#include "VertexShader.h"

typedef std::string Error;

//
// A vertex with a position and texture coordinate
//
typedef struct _VERTEX
{
	DirectX::XMFLOAT3 Pos;
	DirectX::XMFLOAT2 TexCoord;
} VERTEX;

// WinDesktopDup hides the gory details of capturing the screen using the
// Windows Desktop Duplication API
class WinDesktopDup
{
public:
	int    OutputNumber = 0;

	~WinDesktopDup();

	bool Initialize();
	void  Close();
	bool  CaptureNext(uint8_t* dest);

private:
	ID3D11Device* D3DDevice = nullptr;
	ID3D11DeviceContext* D3DDeviceContext = nullptr;
	IDXGIOutputDuplication* DeskDupl = nullptr;
	bool                    HaveFrameLock = false;
	ID3D11Texture2D* m_resizeTexture = nullptr;
	ID3D11RenderTargetView* m_RTV = nullptr;
	ID3D11SamplerState* m_SamplerLinear = nullptr;
	ID3D11BlendState* m_BlendState = nullptr;
	ID3D11VertexShader* m_VertexShader = nullptr;
	ID3D11PixelShader* m_PixelShader = nullptr;
	ID3D11InputLayout* m_InputLayout = nullptr;
	ID3D11ShaderResourceView* m_SrcSrv = nullptr;
};