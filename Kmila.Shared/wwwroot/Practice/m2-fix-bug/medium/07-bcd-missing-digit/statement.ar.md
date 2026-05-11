## BCD-to-7-segment decoder, one digit blanked

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

A standard BCD-to-7-segment decoder for digits 0…9 (active-high segments, order `a b c d e f g`).

Most digits decode correctly, but digit 7 outputs `"0000000"` (blank display) instead of `"1110000"` (segments a, b, c lit).

**Hint:** check the `when "0111"` row of the selected assignment.
