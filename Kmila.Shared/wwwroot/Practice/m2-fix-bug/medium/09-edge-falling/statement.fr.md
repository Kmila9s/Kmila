## Détecteur de front — mauvais front

On voulait un détecteur de **front montant** sur `d`.

### Spécification

* Échantillonne `d` à chaque `rising_edge(clk)` dans `d_prev`.
* `pulse = '1'` pendant un cycle quand `d` valait `'0'` au cycle précédent et vaut `'1'` maintenant.
* Reset asynchrone actif haut met `d_prev` à `'0'`.

### Ce qui cloche

L'expression combinatoire de `pulse` teste la mauvaise polarité : elle s'active quand `d` passe de `'1'` à `'0'`, c'est-à-dire sur le front **descendant**. Du coup, lorsque `d` monte (cible de la spec), `pulse` reste à `'0'`, et lorsqu'il descend, `pulse` s'active par erreur.

### Indice

Inverse les deux égalités dans l'affectation conditionnelle de `pulse`.
