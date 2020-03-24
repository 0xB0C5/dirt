using System;
using System.Collections;
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

        public Expression Parse()
        {
            Expression tree = ParseToParenOrEnd();

            if (_codeIndex != _code.Length)
            {
                Boom("Unmatched parentheses.");
            }

            return tree;
        }

        private Expression ParseToParenOrEnd()
        {
            Expression unionTree = null;
            Expression concatTree = null;

            while (_codeIndex < _code.Length && _code[_codeIndex] != ')' && _code[_codeIndex] != '}')
            {
                if (_code[_codeIndex] == '|')
                {
                    _codeIndex++;

                    concatTree = concatTree ?? new Expression.Empty();

                    if (unionTree == null)
                    {
                        unionTree = concatTree;
                    }
                    else
                    {
                        unionTree = new Expression.Union { Left = unionTree, Right = concatTree };
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
                        concatTree = new Expression.Concat { Left = concatTree, Right = term };
                    }
                }
            }

            concatTree = concatTree ?? new Expression.Empty();

            if (unionTree == null)
            {
                return concatTree;
            }

            unionTree = new Expression.Union { Left = unionTree, Right = concatTree };

            return unionTree;
        }

        private Expression ParseTerm()
        {
            Expression curTree = ParseUnstarredTerm();

            while (_codeIndex < _code.Length && _code[_codeIndex] == (byte)'*')
            {
                _codeIndex++;
                curTree = new Expression.Star { Sub = curTree };
            }

            return curTree;
        }

        private Expression ParseUnstarredTerm()
        {
            var readByte = Read();

            switch (readByte)
            {
                case (byte)'\\':
                    return new Expression.IO
                    {
                        In = true,
                        Out = true,
                        Byte = Read(),
                    };

                case (byte)'\'':
                    return new Expression.IO
                    {
                        In = false,
                        Out = true,
                        Byte = Read(),
                    };

                case (byte)'`':
                    return new Expression.IO
                    {
                        In = true,
                        Out = false,
                        Byte = Read(),
                    };

                case (byte)'"':
                    return ReadQuotedOutput();

                case (byte)'(':
                    {
                        var tree = ParseToParenOrEnd();
                        if (Read() != ')') Boom("Expected close paren.");
                        return tree;
                    }

                case (byte)'{':
                    {
                        var tree = ParseToParenOrEnd();
                        if (Read() != '}') Boom("Expected close brace.");
                        return Silenced(tree);
                    }

                case (byte)'*':
                    Boom("Star not applied to anything.");
                    return null;

                case (byte)'|':
                    // Shouldn't happen - Callers should check for union.
                    Boom("Internal Error: Found union when parsing unstarred term.");
                    return null;

                case (byte)'[':
                    return ReadCharSet();

                default:
                    return new Expression.IO
                    {
                        In = true,
                        Out = true,
                        Byte = readByte,
                    };
            }
        }

        private Expression ReadQuotedOutput()
        {
            Expression expression = null;

            while (Peek() != '"')
            {
                var b = Read();

                if (b == '\\')
                {
                    b = Read();
                }

                var io = new Expression.IO { Byte = b, In = false, Out = true };

                if (expression == null)
                {
                    expression = io;
                }
                else
                {
                    expression = new Expression.Concat
                    {
                        Left = expression,
                        Right = io,
                    };
                }
            }

            _codeIndex++;

            return expression;
        }

        private Expression Silenced(Expression tree)
        {
            switch (tree)
            {
                case Expression.IO io:
                    if (io.In)
                    {
                        return new Expression.IO
                        {
                            Byte = io.Byte,
                            In = true,
                            Out = false,
                        };
                    }
                    else
                    {
                        return new Expression.Empty();
                    }

                case Expression.Empty empty:
                    return empty;

                case Expression.Star star:
                    return new Expression.Star { Sub = Silenced(star.Sub) };

                case Expression.Concat concat:
                    return new Expression.Concat { Left = Silenced(concat.Left), Right = Silenced(concat.Right) };

                case Expression.Union union:
                    return new Expression.Union { Left = Silenced(union.Left), Right = Silenced(union.Right) };

                case Expression.Nothing nothing:
                    return nothing;
            }

            Boom("Internal error : Unknown expression type " + tree.GetType().FullName);
            return null;
        }

        private Expression ReadCharSet()
        {
            bool inverted = false;

            if (Peek() == '^')
            {
                inverted = true;
                _codeIndex++;
            }

            BitArray bits = new BitArray(256, false);

            byte? prevB = null;

            while (Peek() != ']')
            {
                byte b = Read();

                if (b == '-' && prevB.HasValue && Peek() != ']')
                {
                    byte high = Read();

                    for (byte i = prevB.Value; ; i++)
                    {
                        bits.Set(i, true);

                        if (i == high) break;
                    }

                    continue;
                }

                if (b == '\\')
                {
                    b = Read();
                }

                bits.Set(b, true);

                prevB = b;
            }

            _codeIndex++;

            if (inverted) bits = bits.Not();

            Expression expression = null;

            for (int i = 0; i < 256; i++)
            {
                if (bits.Get(i))
                {
                    var io = new Expression.IO { Byte = (byte)i, In = true, Out = true };

                    if (expression == null)
                    {
                        expression = io;
                    }
                    else
                    {
                        expression = new Expression.Union
                        {
                            Left = expression,
                            Right = io,
                        };
                    }
                }
            }

            if (expression == null) expression = new Expression.Nothing();

            return expression;
        }

        private byte Peek()
        {
            if (_codeIndex >= _code.Length) Boom("Unexpected end of code.");

            return _code[_codeIndex];
        }

        private byte Read()
        {
            var b = Peek();
            _codeIndex++;
            return b;
        }

        private void Boom(string message)
        {
            throw new Exception(message + "\nAt position " + _codeIndex + ".");
        }
    }
}
