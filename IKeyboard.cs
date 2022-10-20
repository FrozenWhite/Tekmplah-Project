namespace Teknomli
{
    internal class Keyboard
    {
        enum KeyCode
        {
            Left = 3,
            Right = 4,
            Up = 5,
            Down = 6,
            Control = 7,
            Alt = 8,
            Shift = 9,
            Space = 10,
            Return = 11,
            Tab = 12,
            Esc = 13,
            BackSpace = 14,
            A = 15,
            B = 16,
            C = 17,
            D = 18,
            E = 19,
            F = 20,
            G = 21,
            H = 22,
            I = 23,
            J = 24,
            K = 25,
            L = 26,
            M = 27,
            N = 28,
            O = 29,
            P = 30,
            Q = 31,
            R = 32,
            S = 33,
            T = 34,
            U = 35,
            V = 36,
            W = 37,
            X = 38,
            Y = 39,
            Z = 40,
            D0 = 41,
            D1 = 42,
            D2 = 43,
            D3 = 44,
            D4 = 45,
            D5 = 46,
            D6 = 47,
            D7 = 48,
            D8 = 49,
            D9 = 50,
            Minus = 51,
            Caret = 52,
            Pipe = 53,
            AtMark = 54,
            OpenBrackets = 55,
            CloseBrackets = 56,
            SemiColon = 57,
            Colon = 58,
            Slash = 59,
            BackSlash = 60,
            Period = 61,
            Comma = 62,
        }

        public static byte? Translate(Keys key, bool caps, bool shift, bool control, bool alt)
        {
            byte? code = null;
            if (alt)
            {
                return null;
            }

            switch (key)
            {
                case Keys.Left:
                    code = 3;
                    break;
                case Keys.Right:
                    code = 4;
                    break;
                case Keys.Up:
                    code = 5;
                    break;
                case Keys.Down:
                    code = 6;
                    break;
                case Keys.Control:
                    code = 7;
                    break;
                case Keys.Alt:
                    code = 8;
                    break;
                case Keys.Space:
                    code = 10;
                    break;
                case Keys.Enter:
                    code = 11;
                    break;
                case Keys.Tab:
                    code = 12;
                    break;
                case Keys.Escape:
                    code = 13;
                    break;
                case Keys.Back:
                    code = 14;
                    break;
                default:
                    {
                        if (!control)
                        {
                            if (shift)
                            {
                                code = key switch
                                {
                                    >= Keys.D1 and <= Keys.D9 => (byte)(key - Keys.D1 + 63),
                                    >= Keys.A and <= Keys.Z => (byte)(key - Keys.A + 72),
                                    Keys.OemMinus => 98,
                                    Keys.Oem7 => 99,
                                    Keys.Oem5 => 100,
                                    Keys.Oemtilde => 101,
                                    Keys.OemOpenBrackets => 102,
                                    Keys.Oem6 => 103,
                                    Keys.Oemplus => 104,
                                    Keys.Oem1 => 105,
                                    Keys.OemQuestion => 106,
                                    Keys.OemBackslash => 107,
                                    Keys.OemPeriod => 108,
                                    Keys.Oemcomma => 109,
                                    _ => null
                                };
                            }
                            else
                            {
                                code = key switch
                                {
                                    >= Keys.A and <= Keys.Z => (byte)(key - Keys.A + 15),
                                    >= Keys.D0 and <= Keys.D9 => (byte)(key - Keys.D0 + 41),
                                    Keys.OemMinus => 51,
                                    Keys.Oem7 => 52,
                                    Keys.Oem5 => 53,
                                    Keys.Oemtilde => 54,
                                    Keys.OemOpenBrackets => 55,
                                    Keys.Oem6 => 56,
                                    Keys.Oemplus => 57,
                                    Keys.Oem1 => 58,
                                    Keys.OemQuestion => 59,
                                    Keys.OemBackslash => 60,
                                    Keys.OemPeriod => 61,
                                    Keys.Oemcomma => 62,
                                    _ => null
                                };
                            }
                        }
                        break;
                    }
            }

            return code;
        }
    }
}
