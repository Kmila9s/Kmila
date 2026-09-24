## Compteur up/down — direction ignorée

Un compteur up/down 4 bits commandé par `dir` :

* `dir = '1'` → compte vers le haut : `count <- count + 1`.
* `dir = '0'` → compte vers le bas : `count <- count - 1`.
* Reset asynchrone actif haut met `count` à zéro.

### Ce qui cloche

Le starter ne fait qu'incrémenter — `dir` n'a aucun effet. Avec `dir = '0'`, on a toujours `0 → 1 → 2 → …` au lieu de `0 → 15 → 14 → …`.

### Indice

Ajoute un `if dir = '1' then count <= count + 1; else count <= count - 1; end if;` à l'intérieur de la branche front montant.
