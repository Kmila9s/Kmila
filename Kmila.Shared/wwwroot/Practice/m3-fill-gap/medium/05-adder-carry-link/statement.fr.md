## Additionneur ripple 2 bits — remplis la chaîne de retenue

Deux additionneurs complets sont partiellement écrits. Les bits de
somme et la retenue finale sont déjà câblés ; seule la **retenue
interne** `c1` entre le bit-0 et le bit-1 manque.

Tu dois affecter `c1` à la retenue sortante de l'additionneur du bit-0 :

```
c1 = (a0·b0) + cin·(a0 ⊕ b0)
```

### Spécification

* Entrées : `a`, `b` (2 bits chacun) et `cin`.
* Sorties : `sum` (2 bits) et `cout`.
* Les autres affectations sont déjà fournies — cet exercice ne touche
  qu'au câblage de `c1`.

### Où écrire

Édite uniquement entre `-- BEGIN_EDIT` et `-- END_EDIT`. Le reste est
verrouillé.

### Astuce

`c1 <= (a(0) and b(0)) or (cin and (a(0) xor b(0)));`
