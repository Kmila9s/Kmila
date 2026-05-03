## BCD-to-7-segment decoder — one digit blanked

A standard BCD-to-7-segment decoder (active-high segments, order `a b c d e f g`) was specified for digits 0…9.

### Segment patterns

| digit | seg (`abcdefg`) |
|:-----:|:---------------:|
|   0   | `1111110` |
|   1   | `0110000` |
|   7   | `1110000` |
|   8   | `1111111` |
|   9   | `1111011` |

### What's wrong

Most digits decode correctly, but **digit 7** outputs `"0000000"` (display blank) instead of the expected `"1110000"` (segments `a`, `b`, `c` lit).

### Hint

Look at the `when "0111"` row of the selected assignment.
