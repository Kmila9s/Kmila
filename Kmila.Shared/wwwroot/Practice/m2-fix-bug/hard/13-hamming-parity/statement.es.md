## Hamming(7,4) — paridad cambiada

El codificador Hamming(7,4) produce 7 bits donde los bits de paridad ocupan posiciones 7, 6 y 4 (desde MSB), y los datos las posiciones 5, 3, 2, 1.

Orden esperado (MSB primero): `p1 & p2 & d(0) & p3 & d(1) & d(2) & d(3)`.

El starter intercambia accidentalmente `p1` y `p3` en la concatenación final. Encuentra y corrige.
