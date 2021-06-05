using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using static Calc2KeyCE.Core.KeyHandling.Keyboard;
using static Calc2KeyCE.MouseOperations;

namespace Calc2KeyCE.Core.KeyHandling
{
    public interface IInputSimulator
    {
        public void MoveMouseBy(int pixelDeltaX, int pixelDeltaY);
        public MousePoint GetCursorPosition();
        public void MouseEvent(MouseEventFlags value);
        public void SendKey(DirectXKeyStrokes key, bool KeyUp, InputType inputType);
    }
}
