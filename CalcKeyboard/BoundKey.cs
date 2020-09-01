using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CalcKeyboard
{
    public class BoundKey
    {
        public CalculatorKeyboard.AllKeys CalcKey { get; set; }
        public Keys? KeyboardAction { get; set; }
        public MouseButtons? MouseButtonAction { get; set; }
        public MouseOperations.MouseMoveActions? MouseMoveAction { get; set; }
    }
}
