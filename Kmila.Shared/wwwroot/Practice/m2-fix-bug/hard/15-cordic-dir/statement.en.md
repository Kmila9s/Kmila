## CORDIC — rotation direction reversed

A single CORDIC iteration should rotate counter-clockwise when `dir = '1'`:
* `x' = x - y`
* `y' = y + x`

And clockwise when `dir = '0'` (signs flipped).

The starter wires `dir` the wrong way — `dir = '1'` produces clockwise rotation. Find and fix the conditional that selects the formulas.
