## Registre à décalage 8 bits — mauvais sens

On voulait un registre série-entrée / parallèle-sortie qui décale **vers la gauche** à chaque coup d'horloge.

### Spécification

* Sur `rising_edge(clk)` : `r <- r(6 downto 0) & din` — `din` entre par le bit 0, le MSB est éjecté.
* `reset` asynchrone actif haut met `r` à zéro.
* `q` reflète `r`.

### Ce qui cloche

La concaténation est inversée : `din` est inséré côté MSB, donc après quelques cycles avec `din = '1'` ce sont les bits hauts qui se remplissent d'abord, pas les bits bas.

### Indice

Dans `r <= a & b`, l'opérande de **gauche** devient les MSB. Pour décaler à gauche, jette le MSB et place `din` à droite.
