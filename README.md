# dirt

The "irt" in "dirt" stands for "iterated regular transduction". The "d" stands for "don't use this for serious programming".

Language Overview
-----------------

dirt uses transduction expressions which are similar to regular expressions, except they are able to produce output.

Transduction expressions have the following regex syntax:

- Any non-special character matches itself.

- `X*` matches `X` 0 or more times.

- `X+` matches `X` 1 or more times.

- `XY` matches the concatenation of `X` and `Y`. Lower precedence than `*` and `+`.

- `X|Y` matches the union of `X` and `Y`. Lower precedence than concatenation.

- `(` and `)` override precedence.

- `\` escapes the next character.

- `[...]` matches one of the character between the `[]`s.

- `[a-b]` matches character in the range from `a` to `b`. Can have multiple ranges and additional non-range characters (e.g. `[a-zA-Z0-9_]`)

- `[^...]` matches any character *not* included between the `[]`s

Any expression using only the above syntax, when run on a string which it matches, will output the same string.

To allow different output than the input string, the following syntax can be used:

- `'c` matches the empty string and outputs a single character `c` when matched. Special characters don't need to be escaped (e.g. `'\` outputs a backslash).

- `"..."` matches the empty string and outputs everything between the 2 `"` characters when matched. `"`s and `\`s inside need to be escaped.

- `` `c `` matches a single character `c` and outputs nothing. Special characters don't need to be escaped.

- `{X}` matches whatever `X` matches but outputs nothing.

In cases where the input can be matched multiple ways, dirt chooses whichever way produces the least output.

A dirt program is just a transduction expression. dirt reads its input from stdin, then repeatedly transduces it until it doesn't match, then outputs it to stdout.

Note: by "character" I mean "byte". Non-printable characters are perfectly valid in a dirt program.

Usage
-----

Build with `dotnet build` (I think?).

Run with `dirt [source-filename] < [input-filename]`. Use `-v` for verbose mode, where it prints every transduction.

# Examples

Hello World
-----------

Here's a Hello World program

    `"Hello, World!"`


brainbool interpreter
---------------------

I had a working brainbool interpreter but I changed the syntax of dirt and now it doesn't work.