# dirt

The "irt" in "dirt" stands for "iterated regular transduction". The "d" stands for "don't use this for serious programming".

Language Overview
-----------------

dirt uses transduction expressions which are similar to regular expressions, except they are able to produce output.

Transduction expressions have the following syntax:

- Any non-special character `c` matches `c` and outputs `c`.

- `+c` matches the empty string and outputs `c`

- `-c` matches the character `c` and outputs the empty string.

- `X*` matches `X` 0 or more times, outputting `X`'s output that many times.

- `XY` matches the concatenation of `X` and `Y`, outputting their concatenated outputs.

- `M|N` matches the union of `M` and `N`. Outputs the output of whichever side was matched.

- `(` and `)` override precedence.

- `\c` escapes a character - matches and outputs `c`.

In cases where the input can be matched multiple ways, dirt chooses whichever way produces the least output.

A dirt program is just a transduction expression. dirt reads its input from stdin, then repeatedly transduces it doesn't match, then outputs it to stdout.

As an example, here's a program which removes leading zeros from a number:

    -0(0|1|2|3|4|5|6|7|8|9)*

The `-0` matches the first leading 0 but doesn't output it, then the rest matches and outputs the rest of the digits.
At each iteration, the first leading 0 is removed.
When all leading zeros have been removed, the expression no longer matches the number, so execution stops and the number is written to stdout.

Usage
-----

Build with `dotnet build` (I think?).

Run with `dirt [source-filename] < [input-filename]`. Use `-v` for verbose mode, where it prints every transduction.


brainbool interpreter
---------------------

Here's a brainbool interpreter in dirt. Give it a brainbool program in the format `^[program]#^0#[input]#` (where `input` is a string of `0`s and `1`s) and it will change it to `[program]^#[memory]##[output]`

    ([|]|<|>|,|.|\+)*(|-^.+^([|]|<|>|,|.|\+)*#(0|1)*^(0(0|1)*#(0|1)*#(0|1)*+0|1(0|1)*#(0|1)*#(0|1)*+1)|-^(\++^([|]|<|>|,|.|\+)*#(0|1)*^(-0+1|-1+0)|,+^([|]|<|>|,|.|\+)*#(0|1)*^((0|+0-1)(0|1)*#-0|(1|+1-0)(0|1)*#-1)|>+^([|]|<|>|,|.|\+)*#(0|1)*-^(0|1)+^((0|1)(0|1)*|+0)#|<+^([|]|<|>|,|.|\+)*#(0|1)*(-0^+0|-1^+1)|[+^([|]|<|>|,|.|\+)*#(0|1)*^1|[+}([|]|<|>|,|.|\+)*#(0|1)*^0)(0|1|#)*|(+[}}*+}-[|+]-}}}*-]|-}]+^|-^+{]|-]{{*+{+]|-[-{{{*+[|+^[-{|(+<}}*-<|+>}}*->|+,}}*-,|+.}}*-.|++}}*-+)|(-<{{*+<|->{{*+>|-,{{*+,|-.{{*+.|-+{{*++))([|]|<|>|,|.|\+|0|1|#|^)*)