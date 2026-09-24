## FIFO — corrigez le bouclage des pointeurs

Une FIFO de profondeur 4 perd des données quand le tampon boucle.
La logique de `count`/`full`/`empty` semble bonne, mais les
pointeurs de lecture et d'écriture rebouclent une case trop tard,
si bien que la FIFO se comporte comme si elle avait cinq cases au
lieu de quatre.

### Spécification

* FIFO synchrone profondeur 4, reset actif-haut.
* Les pointeurs prennent les valeurs `0..3` et **doivent reboucler
  à 0 après 3**.
* `full = '1'` quand `count = 4`, `empty = '1'` quand `count = 0`.
* Les push sont ignorés en plein, les pop quand vide.

### Indice

Si `DEPTH = 4`, le domaine légal du pointeur est `0..DEPTH-1`.
Reboucler à `DEPTH` (ou pire, `DEPTH+1`) crée une case fantôme.
