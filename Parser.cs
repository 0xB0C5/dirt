using System;
using System.Collections.Generic;
using System.Text;

namespace dirt
{
    class Parser
    {
        private byte[] _code;
        private int _codeIndex;

        public Parser(byte[] code)
        {
            _code = code;
        }

        public SyntaxTree Parse()
        {
            SyntaxTree tree = ParseToParenOrEnd();

            if (_codeIndex != _code.Length)
            {
                Boom("Unmatched parentheses.");
            }

            return tree;
        }

        private SyntaxTree ParseToParenOrEnd()
        {
            SyntaxTree unionTree = null;
            SyntaxTree concatTree = null;

            while (_codeIndex < _code.Length && _code[_codeIndex] != ')')
            {
                if (_code[_codeIndex] == '|')
                {
                    _codeIndex++;

                    concatTree = concatTree ?? new SyntaxTree.Empty();

                    if (unionTree == null)
                    {
                        unionTree = concatTree;
                    }
                    else
                    {
                        unionTree = new SyntaxTree.Union { Left = unionTree, Right = concatTree };
                    }

                    concatTree = null;

                    continue;
                }
                else
                {
                    var term = ParseTerm();
                    
                    if (concatTree == null)
                    {
                        concatTree = term;
                    }
                    else
                    {
                        concatTree = new SyntaxTree.Concat { Left = concatTree, Right = term };
                    }
                }
            }

            concatTree = concatTree ?? new SyntaxTree.Empty();

            if (unionTree == null)
            {
                return concatTree;
            }

            unionTree = new SyntaxTree.Union { Left = unionTree, Right = concatTree };

            return unionTree;
        }

        private SyntaxTree ParseTerm()
        {
            SyntaxTree curTree = ParseUnstarredTerm();

            while (_codeIndex < _code.Length && _code[_codeIndex] == (byte)'*')
            {
                _codeIndex++;
                curTree = new SyntaxTree.Star { Sub = curTree };
            }

            return curTree;
        }

        private SyntaxTree ParseUnstarredTerm()
        {
            var readByte = Read();

            switch (readByte)
            {
                case (byte)'\\':
                    return new SyntaxTree.IO
                    {
                        In = true,
                        Out = true,
                        Byte = Read(),
                    };

                case (byte)'+':
                    return new SyntaxTree.IO
                    {
                        In = false,
                        Out = true,
                        Byte = Read(),
                    };

                case (byte)'-':
                    return new SyntaxTree.IO
                    {
                        In = true,
                        Out = false,
                        Byte = Read(),
                    };

                case (byte)'(':
                    var tree = ParseToParenOrEnd();
                    Read();
                    return tree;

                case (byte)'*':
                    Boom("Star not applied to anything.");
                    return null;

                case (byte)'|':
                    // Shouldn't happen - Callers should check for union.
                    Boom("Internal Error: Found union when parsing unstarred term.");
                    return null;

                default:
                    return new SyntaxTree.IO
                    {
                        In = true,
                        Out = true,
                        Byte = readByte,
                    };
            }
        }

        private byte Read()
        {
            if (_codeIndex >= _code.Length) Boom("Unexpected end of code.");

            return _code[_codeIndex++];
        }

        private void Boom(string message)
        {
            throw new Exception(message + "\nAt position " + _codeIndex + ".");
        }
    }
}
