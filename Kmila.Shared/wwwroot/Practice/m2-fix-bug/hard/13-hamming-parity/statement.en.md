## Hamming(7,4) — parity swap

The Hamming(7,4) encoder produces a 7-bit code where parity bits land at positions 7, 6, and 4 (counting from MSB), and data at positions 5, 3, 2, 1.

Expected output ordering (MSB-first): `p1 & p2 & d(0) & p3 & d(1) & d(2) & d(3)`.

The starter has the parity bits `p1` and `p3` accidentally swapped in the final concatenation. Find and fix it.
