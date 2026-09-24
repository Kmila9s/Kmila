## Hamming(7,4) — parité échangée

L'encodeur Hamming(7,4) produit 7 bits où les bits de parité sont en positions 7, 6, 4 (depuis le MSB), et les données aux positions 5, 3, 2, 1.

Ordre attendu (MSB d'abord) : `p1 & p2 & d(0) & p3 & d(1) & d(2) & d(3)`.

Le starter échange accidentellement `p1` et `p3` dans la concaténation finale. Trouve et corrige.
