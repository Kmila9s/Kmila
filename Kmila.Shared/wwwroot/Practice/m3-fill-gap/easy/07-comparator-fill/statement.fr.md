## Comparateur d'égalité 4 bits — remplis le trou

Implémente un comparateur d'égalité 4 bits. La sortie `eq` vaut `1`
quand les deux vecteurs 4 bits `a` et `b` sont bit à bit égaux, et
`0` sinon.

### Comportement

* `eq <= '1'` quand `a = b`
* `eq <= '0'` sinon

### Indice

Utilise une affectation concurrente `when ... else` avec l'opérateur
`=` sur les vecteurs entiers.

### Où écrire

L'entité, les ports et le squelette de l'architecture sont en place.
Seule la ligne entre `-- BEGIN_EDIT` et `-- END_EDIT` est évaluée —
tout le reste est verrouillé.
