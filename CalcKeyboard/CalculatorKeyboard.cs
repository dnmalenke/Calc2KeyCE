namespace CalcKeyboard
{
    public static class CalculatorKeyboard
    {
        public enum Group1
        {
            Graph = 1 << 0,
            Trace = 1 << 1,
            Zoom = 1 << 2,
            Window = 1 << 3,
            Yequ = 1 << 4,
            Second = 1 << 5,
            Mode = 1 << 6,
            Del = 1 << 7,
        }

        public enum Group2
        {
            Sto = 1 << 1,
            Ln = 1 << 2,
            Log = 1 << 3,
            Square = 1 << 4,
            Recip = 1 << 5,
            Math = 1 << 6,
            Alpha = 1 << 7,
        }

        public enum Group3
        {
            Zero = 1 << 0,
            One = 1 << 1,
            Four = 1 << 2,
            Seven = 1 << 3,
            Comma = 1 << 4,
            Sin = 1 << 5,
            Apps = 1 << 6,
            GraphVar = 1 << 7,
        }

        public enum Group4
        {
            DecPnt = 1 << 0,
            Two = 1 << 1,
            Five = 1 << 2,
            Eight = 1 << 3,
            LParen = 1 << 4,
            Cos = 1 << 5,
            Prgm = 1 << 6,
            Stat = 1 << 7,
        }

        public enum Group5
        {
            Chs = 1 << 0,
            Three = 1 << 1,
            Six = 1 << 2,
            Nine = 1 << 3,
            RParen = 1 << 4,
            Tan = 1 << 5,
            Vars = 1 << 6,
        }

        public enum Group6
        {
            Enter = 1 << 0,
            Add = 1 << 1,
            Sub = 1 << 2,
            Mul = 1 << 3,
            Div = 1 << 4,
            Power = 1 << 5,
            Clear = 1 << 6,
        }

        public enum Group7
        {
            Down = 1 << 0,
            Left = 1 << 1,
            Right = 1 << 2,
            Up = 1 << 3,
        }

        public enum AllKeys
        {
            Graph,
            Trace,
            Zoom,
            Window,
            Yequ,
            Second,
            Mode,
            Del,
            Sto,
            Ln,
            Log,
            Square,
            Recip,
            Math,
            Alpha,
            Zero,
            One,
            Four,
            Seven,
            Comma,
            Sin,
            Apps,
            GraphVar,
            DecPnt,
            Two,
            Five,
            Eight,
            LParen,
            Cos,
            Prgm,
            Stat,
            Chs,
            Three,
            Six,
            Nine,
            RParen,
            Tan,
            Vars,
            Enter,
            Add,
            Sub,
            Mul,
            Div,
            Power,
            Clear,
            Down,
            Left,
            Right,
            Up
        }
    }
}
