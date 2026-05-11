## هامينغ(7,4) — تبديل بتي تماثل

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

The Hamming(7,4) encoder produces a 7-bit code where parity bits land at positions 7, 6, and 4 (from MSB) and data at 5, 3, 2, 1.

Expected order (MSB-first): `p1 & p2 & d(0) & p3 & d(1) & d(2) & d(3)`.

The starter accidentally swaps `p1` and `p3` in the final concatenation. Find and fix.
