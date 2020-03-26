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
            Transducer transducer;

            switch (tree)
            {
                case Expression.Empty _:
                    transducer = Empty();
                    break;

                case Expression.IO io:
                    if (io.In && io.Out)
                    {
                        transducer = InOut(io.Byte);
                    }
                    else if (io.In)
                    {
                        transducer = In(io.Byte);
                    }
                    else if (io.Out)
                    {
                        transducer = Out(io.Byte);
                    }
                    else
                    {
                        transducer = Empty();
                    }
                    break;

                case Expression.Concat concat:
                    transducer = Concat(FromSyntaxTree(concat.Left), FromSyntaxTree(concat.Right));
                    break;

                case Expression.Union union:
                    transducer = Union(FromSyntaxTree(union.Left), FromSyntaxTree(union.Right));
                    break;

                case Expression.Star star:
                    transducer = Star(FromSyntaxTree(star.Sub));
                    break;

                case Expression.Nothing _:
                    transducer = new Transducer { _states = new State[0] };
                    break;

                default:
                    throw new Exception("Internal error: unknown SyntaxTree type " + tree.GetType().FullName);
            }

            transducer.RemoveUselessStates();

            return transducer;
        }

        private void RemoveUselessStates()
        {
            // TODO : this is too slow.
            var reachableStates = new HashSet<int>();

            for (int i = 0; i < _states.Length; i++)
            {
                if (_states[i].IsAccept)
                {
                    reachableStates.Add(i);
                }
            }

            var freshStates = new HashSet<int>(reachableStates);

            while (freshStates.Count > 0)
            {
                var newFreshStates = new HashSet<int>();

                for (int i = 0; i < _states.Length; i++)
                {
                    if (reachableStates.Contains(i)) continue;

                    foreach (var transitions in _states[i].Transitions.Values)
                    {
                        foreach (var transition in transitions)
                        {
                            if (reachableStates.Contains(transition.NextState))
                            {
                                reachableStates.Add(i);
                                newFreshStates.Add(i);
                            }
                        }
                    }
                }

                freshStates = newFreshStates;
            }

            int[] stateMap = new int[_states.Length];

            Array.Fill(stateMap, -1);

            int newStateIndex = 0;

            foreach (var state in reachableStates.OrderBy(i => i))
            {
                stateMap[state] = newStateIndex++;
            }

            var newStates = new State[reachableStates.Count];

            for (int oldState = 0; oldState < _states.Length; oldState++)
            {
                int newState = stateMap[oldState];

                if (newState == -1) continue;

                newStates[newState] = new State
                {
                    StartOutput = _states[oldState].StartOutput,
                    IsAccept = _states[oldState].IsAccept,
                    Transitions = _states[oldState].Transitions
                        .ToDictionary(
                            pair => pair.Key,
                            pair => pair.Value
                                .Where(transition => stateMap[transition.NextState] != -1)
                                .Select(transition => new Transition
                                {
                                    NextState = stateMap[transition.NextState],
                                    Output = transition.Output
                                })
                                .ToArray()
                        ),
                };
            }

            _states = newStates;
        }

        public byte[] GetMinimumOutput(byte[] input)
        {
            LinkedByteArrayListNode[] stateOutputs = new LinkedByteArrayListNode[_states.Length];

            for (int i = 0; i < _states.Length; i++)
            {
                if (_states[i].StartOutput != null)
                {
                    stateOutputs[i] = new LinkedByteArrayListNode
                    {
                        Output = _states[i].StartOutput,
                        ByteCount = _states[i].StartOutput.Length,
                    };
                }
            }

            foreach (byte b in input)
            {
                LinkedByteArrayListNode[] newStateOutputs = new LinkedByteArrayListNode[_states.Length];

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

                            var newOutput = new LinkedByteArrayListNode
                            {
                                Output = transition.Output,
                                PrevNode = stateOutputs[i],
                                ByteCount = stateOutputs[i].ByteCount + transition.Output.Length,
                            };

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

            LinkedByteArrayListNode outputNode = null;
            for (int i = 0; i < _states.Length; i++)
            {
                if (_states[i].IsAccept && stateOutputs[i] != null)
                {
                    if (outputNode == null || CompareOutputs(outputNode, stateOutputs[i]) > 0)
                    {
                        outputNode = stateOutputs[i];
                    }
                }
            }

            if (outputNode == null)
            {
                return null;
            }

            byte[] output = new byte[outputNode.ByteCount];
            int outputIndex = output.Length;
            while (outputNode != null)
            {
                outputIndex -= outputNode.Output.Length;
                Array.Copy(outputNode.Output, 0, output, outputIndex, outputNode.Output.Length);
                outputNode = outputNode.PrevNode;
            }

            if (outputIndex != 0)
            {
                throw new Exception("Internal error: ByteCount of LinkedByteArrayListNode was wrong?");
            }

            return output;
        }
        
        private static int CompareOutputs(LinkedByteArrayListNode a, LinkedByteArrayListNode b)
        {
            if (a.ByteCount != b.ByteCount)
            {
                return a.ByteCount.CompareTo(b.ByteCount);
            }

            // TODO : implement lexicographical ordering.

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

        private class LinkedByteArrayListNode
        {
            public LinkedByteArrayListNode PrevNode;
            public byte[] Output;
            public int ByteCount;
        }
    }
}
