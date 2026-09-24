## Compteur synchrone 4 bits — combler le trou

Un compteur 4 bits est **presque** terminé. Le chemin de reset, la détection de front d'horloge et l'affectation de sortie sont déjà en place.

Implémente la **seule ligne** qui fait avancer le compteur à chaque front montant.

### Spécification

* Reset asynchrone : `q ← 0000`
* Au `rising_edge(clk)` (hors reset) : `q ← q + 1`
* Repasse de `1111` à `0000`

### Où écrire

Seule la ligne entre les commentaires `-- BEGIN_EDIT` et `-- END_EDIT` est modifiable. Le reste est la structure d'entité que l'évaluateur attend.
