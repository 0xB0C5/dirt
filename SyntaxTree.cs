using System;
using System.Collections.Generic;
using System.Text;

namespace dirt
{
    public class SyntaxTree
    {
        private SyntaxTree() { }

        public sealed class Concat : SyntaxTree
        {
            public SyntaxTree Left { get; set; }
            public SyntaxTree Right { get; set; }

            public override string ToString()
            {
                return '(' + Left.ToString() + ")(" + Right.ToString() + ")";
            }
        }

        public sealed class Union : SyntaxTree
        {
            public SyntaxTree Left { get; set; }
            public SyntaxTree Right { get; set; }

            public override string ToString()
            {
                return Left.ToString() + "|" + Right.ToString();
            }
        }

        public sealed class Star : SyntaxTree
        {
            public SyntaxTree Sub { get; set; }

            public override string ToString()
            {
                return "(" + Sub.ToString() + ")*";
            }
        }

        public sealed class IO : SyntaxTree
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
                else
                {
                    return "+" + ((char)Byte).ToString();
                }
            }
        }

        public sealed class Empty : SyntaxTree
        {
            public override string ToString()
            {
                return "()";
            }
        }
    }
}
