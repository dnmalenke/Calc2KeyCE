using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Calc2KeyCE.KeyHandling;
using WindowsInput;

namespace Calc2KeyCE
{
    public static class KeyHandler
    {
        private const double _mouseMoveIncrement = 0.05;
        private static double _mouseMoveX = 0;
        private static double _mouseMoveY = 0;

        private static List<string> _currentMouseActions = new();

        private static InputSimulator _inputSimulator = new();

        public static void HandleBoundKeys(List<BoundKey> boundKeys, Dictionary<string, int> currentKeys, List<string> previousKeys, List<string> addedKeys)
        {
            foreach (var boundKey in boundKeys.Where(bk => previousKeys.Except(addedKeys).Contains(Enum.GetName(typeof(CalculatorKeyboard.AllKeys), bk.CalcKey))))
            {
                //keyup

                if (boundKey.KeyboardAction != null)
                {
                    Keyboard.SendKey(GetDirectXKeyStroke(boundKey.KeyboardAction.Value), true, Keyboard.InputType.Keyboard);
                }

                if (boundKey.MouseButtonAction != null)
                {
                    MouseOperations.MouseEvent(GetMouseEventFlag(boundKey.MouseButtonAction.Value, true));

                    _currentMouseActions.Remove(boundKey.MouseButtonAction.ToString());
                }

                if (boundKey.MouseMoveAction != null)
                {
                    switch (boundKey.MouseMoveAction.Value)
                    {
                        case MouseOperations.MouseMoveActions.MoveDown:
                            _mouseMoveY = 0;
                            break;
                        case MouseOperations.MouseMoveActions.MoveUp:
                            _mouseMoveY = 0;
                            break;
                        case MouseOperations.MouseMoveActions.MoveLeft:
                            _mouseMoveX = 0;
                            break;
                        case MouseOperations.MouseMoveActions.MoveRight:
                            _mouseMoveX = 0;
                            break;
                        default:
                            break;
                    }
                }

                currentKeys.Remove(boundKey.CalcKey.ToString());
            }

            foreach (var boundKey in boundKeys.Where(bk => currentKeys.ContainsKey(Enum.GetName(typeof(CalculatorKeyboard.AllKeys), bk.CalcKey))))
            {
                //keydown

                if (boundKey.KeyboardAction != null)
                {
                    if (currentKeys[boundKey.CalcKey.ToString()] == 1 || currentKeys[boundKey.CalcKey.ToString()] > 50)
                    {
                        Keyboard.SendKey(GetDirectXKeyStroke(boundKey.KeyboardAction.Value), false, Keyboard.InputType.Keyboard);
                    }
                }

                if (boundKey.MouseButtonAction != null && !_currentMouseActions.Contains(boundKey.MouseButtonAction.ToString()))
                {
                    MouseOperations.MouseEvent(GetMouseEventFlag(boundKey.MouseButtonAction.Value, false));

                    _currentMouseActions.Add(boundKey.MouseButtonAction.ToString());
                }

                if (boundKey.MouseMoveAction != null)
                {
                    switch (boundKey.MouseMoveAction.Value)
                    {
                        case MouseOperations.MouseMoveActions.MoveDown:
                            if (_mouseMoveY < double.MaxValue)
                            {
                                _mouseMoveY += _mouseMoveIncrement;
                            }
                            break;
                        case MouseOperations.MouseMoveActions.MoveUp:
                            if (_mouseMoveY > double.MinValue)
                            {
                                _mouseMoveY -= _mouseMoveIncrement;
                            }
                            break;
                        case MouseOperations.MouseMoveActions.MoveLeft:
                            if (_mouseMoveX > double.MinValue)
                            {
                                _mouseMoveX -= _mouseMoveIncrement;
                            }
                            break;
                        case MouseOperations.MouseMoveActions.MoveRight:
                            if (_mouseMoveX < double.MaxValue)
                            {
                                _mouseMoveX += _mouseMoveIncrement;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (Math.Round(_mouseMoveX) != 0 || Math.Round(_mouseMoveY) != 0)
            {
                var mousePosition = MouseOperations.GetCursorPosition();
                _inputSimulator.Mouse.MoveMouseBy((int)Math.Round(_mouseMoveX), (int)Math.Round(_mouseMoveY));
            }
        }

        private static Keyboard.DirectXKeyStrokes GetDirectXKeyStroke(Keys keyboardKey)
        {
            return (Keyboard.DirectXKeyStrokes)Enum.Parse(typeof(Keyboard.DirectXKeyStrokes), keyboardKey.ToString(), true);
        }

        private static MouseOperations.MouseEventFlags GetMouseEventFlag(MouseButtons mouseAction, bool mouseUp = false)
        {
            string mouseActionString = mouseAction.ToString();

            if (mouseUp)
            {
                mouseActionString += "Up";
            }
            else
            {
                mouseActionString += "Down";
            }

            return (MouseOperations.MouseEventFlags)Enum.Parse(typeof(MouseOperations.MouseEventFlags), mouseActionString, true);
        }
    }
}
