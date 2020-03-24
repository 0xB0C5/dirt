using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dirt
{
    class Transducer
    {
        private State[] _states;

        private Transducer() { }

        public static Transducer Empty()
        {
            return new Transducer
            {
                _states = new State[]
                {
                    new State
                    {
                        Transitions = new Dictionary<byte, Transition[]>(),
                        IsAccept = true,
                        StartOutput = new byte[0],
                    }
                },
            };
        }

        public static Transducer Out(byte b)
        {
            return new Transducer
            {
                _states = new State[]
                {
                    new State
                    {
                        Transitions = new Dictionary<byte, Transition[]>(),
                        IsAccept = true,
                        StartOutput = new byte[] { b },
                    }
                },
            };
        }

        public static Transducer In(byte b)
        {
            return new Transducer
            {
                _states = new State[]
                {
                    new State {
                        IsAccept = false,
                        StartOutput = new byte[0],
                        Transitions = new Dictionary<byte, Transition[]>
                        {
                            { b, new Transition[] { new Transition { NextState = 1, Output = new byte[0] } } }
                        },
                    },
                    new State
                    {
                        IsAccept = true,
                        StartOutput = null,
                        Transitions = new Dictionary<byte, Transition[]>(),
                    },
                },
            };
        }

        public static Transducer InOut(byte b)
        {
            return new Transducer
            {
                _states = new State[]
                {
                    new State {
                        IsAccept = false,
                        StartOutput = new byte[0],
                        Transitions = new Dictionary<byte, Transition[]>
                        {
                            { b, new Transition[] { new Transition { NextState = 1, Output = new[] { b } } } }
                        },
                    },
                    new State
                    {
                        IsAccept = true,
                        StartOutput = null,
                        Transitions = new Dictionary<byte, Transition[]>(),
                    },
                },
            };
        }

        public static Transducer Star(Transducer sub)
        {
            var states = new State[sub._states.Length + 1];
            for (int i = 0; i < sub._states.Length; i++)
            {
                var subState = sub._states[i];

                states[i] = new State
                {
                    IsAccept = false,
                    StartOutput = subState.StartOutput,
                    Transitions = new Dictionary<byte, Transition[]>(),
                };

                foreach (var entry in subState.Transitions)
                {
                    var (input, subTransitions) = entry;

                    var transitions = new List<Transition>(subTransitions);

                    var acceptString = subTransitions
                        .Where(transition => sub._states[transition.NextState].IsAccept)
                        .Select(transition => transition.Output)
                        .OrderBy(output => output.Length)
                        .FirstOrDefault();

                    if (acceptString != null)
                    {
                        for (int nextStateIndex = 0; nextStateIndex < sub._states.Length; nextStateIndex++)
                        {
                            if (sub._states[nextStateIndex].StartOutput != null)
                            {
                                transitions.Add(new Transition
                                {
                                    Output = acceptString.Concat(sub._states[nextStateIndex].StartOutput).ToArray(),
                                    NextState = nextStateIndex,
                                });
                            }
                        }

                        transitions.Add(new Transition
                        {
                            Output = acceptString,
                            NextState = sub._states.Length,
                        });
                    }

                    states[i].Transitions[input] = transitions.ToArray();
                }
            }

            states[sub._states.Length] = new State
            {
                StartOutput = new byte[0],
                Transitions = new Dictionary<byte, Transition[]>(),
                IsAccept = true,
            };

            return new Transducer
            {
                _states = states,
            };
        }

        public static Transducer Union(Transducer a, Transducer b)
        {
            var states = new State[a._states.Length + b._states.Length];

            for (int i = 0; i < a._states.Length; i++)
            {
                states[i] = new State
                {
                    StartOutput = a._states[i].StartOutput,
                    IsAccept = a._states[i].IsAccept,
                    Transitions = new Dictionary<byte, Transition[]>(),
                };

                foreach (var pair in a._states[i].Transitions)
                {
                    var (input, transitions) = pair;

                    states[i].Transitions[input] = transitions
                        .Select(transition => new Transition
                        {
                            NextState = transition.NextState,
                            Output = transition.Output,
                        })
                        .ToArray();
                }
            }

            for (int i = 0; i < b._states.Length; i++)
            {
                states[a._states.Length + i] = new State
                {
                    StartOutput = b._states[i].StartOutput,
                    IsAccept = b._states[i].IsAccept,
                    Transitions = new Dictionary<byte, Transition[]>(),
                };

                foreach (var pair in b._states[i].Transitions)
                {
                    var (input, transitions) = pair;

                    states[i + a._states.Length].Transitions[input] = transitions
                        .Select(transition => new Transition
                        {
                            NextState = transition.NextState + a._states.Length,
                            Output = transition.Output,
                        })
                        .ToArray();
                }
            }

            return new Transducer
            {
                _states = states,
            };
        }

        public static Transducer Concat(Transducer a, Transducer b)
        {
            var states = new State[a._states.Length + b._states.Length];

            for (int i = 0; i < a._states.Length; i++)
            {
                states[i] = new State
                {
                    StartOutput = a._states[i].StartOutput,
                    IsAccept = false,
                    Transitions = new Dictionary<byte, Transition[]>(),
                };

                foreach (var pair in a._states[i].Transitions)
                {
                    var (input, aTransitions) = pair;

                    var transitions = new List<Transition>();

                    transitions.AddRange(
                        aTransitions
                            .Select(transition => new Transition
                            {
                                NextState = transition.NextState,
                                Output = transition.Output,
                            })
                    );

                    byte[] acceptOutput =
                        aTransitions
                            .Where(transition => a._states[transition.NextState].IsAccept)
                            .Select(transition => transition.Output)
                            .OrderBy(output => output.Length)
                            .FirstOrDefault();

                    if (acceptOutput != null)
                    {
                        for (int bState = 0; bState < b._states.Length; bState++)
                        {
                            if (b._states[bState].StartOutput != null)
                            {
                                transitions.Add(new Transition
                                {
                                    Output = acceptOutput.Concat(b._states[bState].StartOutput).ToArray(),
                                    NextState = bState + a._states.Length,
                                });
                            }
                        }
                    }

                    states[i].Transitions[input] = transitions.ToArray();
                }
            }

            var aEmptyAcceptOutput = a._states
                .Where(s => s.StartOutput != null && s.IsAccept)
                .Select(s => s.StartOutput)
                .OrderBy(output => output.Length)
                .FirstOrDefault();

            for (int i = 0; i < b._states.Length; i++)
            {
                states[a._states.Length + i] = new State
                {
                    StartOutput = aEmptyAcceptOutput != null && b._states[i].StartOutput != null
                        ? aEmptyAcceptOutput.Concat(b._states[i].StartOutput).ToArray()
                        : null,
                    IsAccept = b._states[i].IsAccept,
                    Transitions = new Dictionary<byte, Transition[]>(),
                };

                foreach (var pair in b._states[i].Transitions)
                {
                    var (input, transitions) = pair;

                    states[i + a._states.Length].Transitions[input] = transitions
                        .Select(transition => new Transition
                        {
                            NextState = transition.NextState + a._states.Length,
                            Output = transition.Output,
                        })
                        .ToArray();
                }
            }

            return new Transducer
            {
                _states = states,
            };
        }

        public static Transducer FromSyntaxTree(Expression tree)
        {
            switch (tree)
            {
                case Expression.Empty _:
                    return Empty();

                case Expression.IO io:
                    if (io.In && io.Out)
                    {
                        return InOut(io.Byte);
                    }
                    else if (io.In)
                    {
                        return In(io.Byte);
                    }
                    else if (io.Out)
                    {
                        return Out(io.Byte);
                    }
                    else
                    {
                        return Empty();
                    }

                case Expression.Concat concat:
                    return Concat(FromSyntaxTree(concat.Left), FromSyntaxTree(concat.Right));

                case Expression.Union union:
                    return Union(FromSyntaxTree(union.Left), FromSyntaxTree(union.Right));

                case Expression.Star star:
                    return Star(FromSyntaxTree(star.Sub));

                case Expression.Nothing _:
                    return new Transducer { _states = new State[0] };
            }

            throw new Exception("Internal error: unknown SyntaxTree type " + tree.GetType().FullName);
        }

        public byte[] GetMinimumOutput(byte[] input)
        {
            byte[][] stateOutputs = new byte[_states.Length][];

            for (int i = 0; i < _states.Length; i++)
            {
                stateOutputs[i] = _states[i].StartOutput;
            }

            foreach (byte b in input)
            {
                byte[][] newStateOutputs = new byte[_states.Length][];

                for (int i = 0; i < _states.Length; i++)
                {
                    if (stateOutputs[i] != null)
                    {
                        if (!_states[i].Transitions.TryGetValue(b, out var transitions))
                        {
                            continue;
                        }

                        foreach (var transition in transitions)
                        {
                            var dest = transition.NextState;

                            var newOutput = stateOutputs[i].Concat(transition.Output).ToArray();

                            if (newStateOutputs[dest] == null
                                || CompareOutputs(newStateOutputs[dest], newOutput) > 0)
                            {
                                newStateOutputs[dest] = newOutput;
                            }
                        }
                    }
                }

                stateOutputs = newStateOutputs;
            }

            byte[] output = null;
            for (int i = 0; i < _states.Length; i++)
            {
                if (_states[i].IsAccept && stateOutputs[i] != null)
                {
                    if (output == null || CompareOutputs(output, stateOutputs[i]) > 0)
                    {
                        output = stateOutputs[i];
                    }
                }
            }

            return output;
        }
        
        private static int CompareOutputs(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return a.Length.CompareTo(b.Length);
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return a[i].CompareTo(b[i]);
                }
            }

            return 0;
        }

        private class State
        {
            public Dictionary<byte, Transition[]> Transitions;
            public byte[] StartOutput;
            public bool IsAccept;
        }

        private class Transition
        {
            public byte[] Output;
            public int NextState;
        }
    }
}
