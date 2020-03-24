using System;
using System.Collections.Generic;
using System.Text;

namespace dirt
{
    public class Expression
    {
        private Expression() { }

        public sealed class Concat : Expression
        {
            public Expression Left { get; set; }
            public Expression Right { get; set; }

            public override string ToString()
            {
                return '(' + Left.ToString() + ")(" + Right.ToString() + ")";
            }
        }

        public sealed class Union : Expression
        {
            public Expression Left { get; set; }
            public Expression Right { get; set; }

            public override string ToString()
            {
                return Left.ToString() + "|" + Right.ToString();
            }
        }

        public sealed class Star : Expression
        {
            public Expression Sub { get; set; }

            public override string ToString()
            {
                return "(" + Sub.ToString() + ")*";
            }
        }

        public sealed class IO : Expression
        {
            public bool In { get; set; }

            public bool Out { get; set; }

            public byte Byte { get; set; }

            public override string ToString()
            {
                if (In && Out)
                {
                    return "\\" + ((char)Byte).ToString();
                }
                else if (In)
                {
                    return "-" + ((char)Byte).ToString();
                }
                else if (Out)
                {
                    return "+" + ((char)Byte).ToString();
                }
                else
                {
                    return "()";
                }
            }
        }

        public sealed class Empty : Expression
        {
            public override string ToString()
            {
                return "()";
            }
        }

        public sealed class Nothing : Expression
        {
            public override string ToString()
            {
                return "!";
            }
        }
    }
}
