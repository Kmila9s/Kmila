## Diviseur d'horloge — mauvais diviseur

Un diviseur par 4 était demandé.

### Spécification

* À chaque `rising_edge(clk)` on incrémente un compteur 3 bits `cnt`.
* `clk_out` correspond au **bit 1** du compteur, donc `clk_out` bascule tous les 2 cycles d'entrée → période de sortie = 4 cycles d'entrée → division par 4.
* Reset asynchrone actif haut met `cnt` à zéro.

### Ce qui cloche

Le starter prend `cnt(2)` au lieu de `cnt(1)`. Cela divise par **8** et non par 4 — `clk_out` bascule tous les 4 cycles.

### Indice

Dans un diviseur en puissance de deux à partir d'un compteur libre, prendre le bit `n` divise par `2^(n+1)`. Choisis le bit qui donne une période de 4.
