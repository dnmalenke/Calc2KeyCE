using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Calc2KeyCE.KeyHandling
{
    /// <summary>
    /// StackOverflow question as reference: https://stackoverflow.com/questions/35138778/sending-keys-to-a-directx-game
    /// http://www.gamespp.com/directx/directInputKeyboardScanCodes.html
    /// </summary>
    public class Keyboard
    {
        [Flags]
        public enum InputType
        {
            Mouse = 0,
            Keyboard = 1,
            Hardware = 2
        }

        [Flags]
        public enum KeyEventF
        {
            KeyDown = 0x0000,
            ExtendedKey = 0x0001,
            KeyUp = 0x0002,
            Unicode = 0x0004,
            Scancode = 0x0008,
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();

        /// <summary>
        /// DirectX key list collected out from the gamespp.com list by me.
        /// </summary>
        public enum DirectXKeyStrokes
        {
            ESCAPE = 0x01,
            D1 = 0x02,
            D2 = 0x03,
            D3 = 0x04,
            D4 = 0x05,
            D5 = 0x06,
            D6 = 0x07,
            D7 = 0x08,
            D8 = 0x09,
            D9 = 0x0A,
            D0 = 0x0B,
            MINUS = 0x0C,
            EQUALS = 0x0D,
            BACK = 0x0E,
            TAB = 0x0F,
            Q = 0x10,
            W = 0x11,
            E = 0x12,
            R = 0x13,
            T = 0x14,
            Y = 0x15,
            U = 0x16,
            I = 0x17,
            O = 0x18,
            P = 0x19,
            LBRACKET = 0x1A,
            RBRACKET = 0x1B,
            RETURN = 0x1C,
            LCONTROLKEY = 0x1D,
            CONTROLKEY = 0x1D,
            A = 0x1E,
            S = 0x1F,
            D = 0x20,
            F = 0x21,
            G = 0x22,
            H = 0x23,
            J = 0x24,
            K = 0x25,
            L = 0x26,
            SEMICOLON = 0x27,
            APOSTROPHE = 0x28,
            GRAVE = 0x29,
            LSHIFTKEY = 0x2A,
            SHIFTKEY = 0x2A,
            BACKSLASH = 0x2B,
            Z = 0x2C,
            X = 0x2D,
            C = 0x2E,
            V = 0x2F,
            B = 0x30,
            N = 0x31,
            M = 0x32,
            OEMCOMMA = 0x33,
            PERIOD = 0x34,
            SLASH = 0x35,
            RSHIFTKEY = 0x36,
            MULTIPLY = 0x37,
            LMENU = 0x38,
            MENU = 0x38,
            SPACE = 0x39,
            CAPITAL = 0x3A,
            F1 = 0x3B,
            F2 = 0x3C,
            F3 = 0x3D,
            F4 = 0x3E,
            F5 = 0x3F,
            F6 = 0x40,
            F7 = 0x41,
            F8 = 0x42,
            F9 = 0x43,
            F10 = 0x44,
            NUMLOCK = 0x45,
            SCROLL = 0x46,
            NUMPAD7 = 0x47,
            NUMPAD8 = 0x48,
            NUMPAD9 = 0x49,
            SUBTRACT = 0x4A,
            NUMPAD4 = 0x4B,
            NUMPAD5 = 0x4C,
            NUMPAD6 = 0x4D,
            ADD = 0x4E,
            NUMPAD1 = 0x4F,
            NUMPAD2 = 0x50,
            NUMPAD3 = 0x51,
            NUMPAD0 = 0x52,
            DECIMAL = 0x53,
            F11 = 0x57,
            F12 = 0x58,
            F13 = 0x64,
            F14 = 0x65,
            F15 = 0x66,
            KANA = 0x70,
            CONVERT = 0x79,
            NOCONVERT = 0x7B,
            YEN = 0x7D,
            NUMPADEQUALS = 0x8D,
            CIRCUMFLEX = 0x90,
            AT = 0x91,
            COLON = 0x92,
            UNDERLINE = 0x93,
            KANJI = 0x94,
            STOP = 0x95,
            AX = 0x96,
            UNLABELED = 0x97,
            NUMPADENTER = 0x9C,
            RCONTROL = 0x9D,
            NUMPADCOMMA = 0xB3,
            DIVIDE = 0xB5,
            SYSRQ = 0xB7,
            RMENU = 0xB8,
            HOME = 0xC7,
            UP = 0xC8,
            PRIOR = 0xC9,
            LEFT = 0xCB,
            RIGHT = 0xCD,
            END = 0xCF,
            DOWN = 0xD0,
            NEXT = 0xD1,
            INSERT = 0xD2,
            DELETE = 0xD3,
            LWIN = 0xDB,
            RWIN = 0xDC,
            APPS = 0xDD,
            BACKSPACE = BACK,
            NUMPADSTAR = MULTIPLY,
            LALT = LMENU,
            ALT = MENU,
            CAPSLOCK = CAPITAL,
            NUMPADMINUS = SUBTRACT,
            NUMPADPLUS = ADD,
            NUMPADPERIOD =DECIMAL,
            NUMPADSLASH = DIVIDE,
            RALT = RMENU,
            UPARROW = UP,
            PGUP = PRIOR,
            LEFTARROW = LEFT,
            RIGHTARROW = RIGHT,
            DOWNARROW = DOWN,
            PGDN = NEXT,

            // Mined these out of nowhere.
            DIK_LEFTMOUSEBUTTON = 0x100,
            DIK_RIGHTMOUSEBUTTON = 0x101,
            DIK_MIDDLEWHEELBUTTON = 0x102,
            DIK_MOUSEBUTTON3 = 0x103,
            DIK_MOUSEBUTTON4 = 0x104,
            DIK_MOUSEBUTTON5 = 0x105,
            DIK_MOUSEBUTTON6 = 0x106,
            DIK_MOUSEBUTTON7 = 0x107,
            DIK_MOUSEWHEELUP = 0x108,
            DIK_MOUSEWHEELDOWN = 0x109,
        }

        /// <summary>
        /// Sends a directx key.
        /// http://www.gamespp.com/directx/directInputKeyboardScanCodes.html
        /// </summary>
        /// <param name="key"></param>
        /// <param name="KeyUp"></param>
        /// <param name="inputType"></param>
        public static void SendKey(DirectXKeyStrokes key, bool KeyUp, InputType inputType)
        {
            uint flagtosend;
            if (KeyUp)
            {
                flagtosend = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode);
            }
            else
            {
                flagtosend = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode);
            }

            Input[] inputs =
            {
            new Input
            {
                type = (int) inputType,
                u = new InputUnion
                {
                    ki = new KeyboardInput
                    {
                        wVk = 0,
                        wScan = (ushort) key,
                        dwFlags = flagtosend,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            }
        };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        /// <summary>
        /// Sends a directx key.
        /// http://www.gamespp.com/directx/directInputKeyboardScanCodes.html
        /// </summary>
        /// <param name="key"></param>
        /// <param name="KeyUp"></param>
        /// <param name="inputType"></param>
        public static void SendKey(ushort key, bool KeyUp, InputType inputType)
        {
            uint flagtosend;
            if (KeyUp)
            {
                flagtosend = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode);
            }
            else
            {
                flagtosend = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode);
            }

            Input[] inputs =
            {
            new Input
            {
                type = (int) inputType,
                u = new InputUnion
                {
                    ki = new KeyboardInput
                    {
                        wVk = 0,
                        wScan = key,
                        dwFlags = flagtosend,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            }
        };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        public struct Input
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public readonly MouseInput mi;
            [FieldOffset(0)] public KeyboardInput ki;
            [FieldOffset(0)] public readonly HardwareInput hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public readonly int dx;
            public readonly int dy;
            public readonly uint mouseData;
            public readonly uint dwFlags;
            public readonly uint time;
            public readonly IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public readonly uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HardwareInput
        {
            public readonly uint uMsg;
            public readonly ushort wParamL;
            public readonly ushort wParamH;
        }
    }
}
