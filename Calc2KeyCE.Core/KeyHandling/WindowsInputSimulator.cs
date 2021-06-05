using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;

namespace Calc2KeyCE.Core.KeyHandling
{
    public class WindowsInputSimulator : IInputSimulator
    {
        InputSimulator _inputSimulator = new();

        public MouseOperations.MousePoint GetCursorPosition()
        {
            return MouseOperations.GetCursorPosition();
        }

        public void MouseEvent(MouseOperations.MouseEventFlags value)
        {
            MouseOperations.MouseEvent(value);
        }

        public void MoveMouseBy(int pixelDeltaX, int pixelDeltaY)
        {
            _inputSimulator.Mouse.MoveMouseBy(pixelDeltaX, pixelDeltaY);
        }

        public void SendKey(Keyboard.DirectXKeyStrokes key, bool keyUp, Keyboard.InputType inputType)
        {
            Keyboard.SendKey(key, keyUp, inputType);
        }
    }
}
