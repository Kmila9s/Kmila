## Comparateur "supérieur à" 4 bits — remplis le trou

L'entité fournit deux opérandes 4 bits `a` et `b` et une sortie `gt`.
L'architecture est vide hormis la zone éditable.

Tu dois affecter `gt` :

* `gt = '1'` quand `a > b` (non signé)
* `gt = '0'` sinon

### Spécification

* Traite `a` et `b` comme **non signés**.
* Pure logique combinatoire — pas d'horloge.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. L'entité est
verrouillée.

### Astuce

`gt <= '1' when unsigned(a) > unsigned(b) else '0';`
