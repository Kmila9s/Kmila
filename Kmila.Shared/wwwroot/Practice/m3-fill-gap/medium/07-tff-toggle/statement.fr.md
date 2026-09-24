## Bascule T — remplis la ligne de basculement

Une bascule T est **presque** complète : reset, détection de front
montant et sortie `q <= qi` sont déjà câblés.

Tu dois implémenter le comportement de basculement à l'intérieur du
front montant :

* si `t = '1'` → `qi` change de valeur
* si `t = '0'` → `qi` reste identique

### Spécification

* Reset asynchrone : `qi ← '0'`.
* Sur `rising_edge(clk)` : `qi ← qi xor t`.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le reset et
la sortie sont verrouillés.

### Astuce

`qi <= qi xor t;`
