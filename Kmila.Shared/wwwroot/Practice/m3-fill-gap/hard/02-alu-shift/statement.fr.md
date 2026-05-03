## Décalage ALU à gauche — complète le trou

Un barrel shifter est **presque** terminé. L'entité déclare déjà
l'entrée 8 bits `a`, l'amplitude `sh` (3 bits) et la sortie `y`.
Le `process(a, sh)` qui encapsule ton code est déjà en place.

Ton rôle : affecter à `y` la valeur de `a` décalée vers la
**gauche** de `sh` positions, en remplissant les bits bas avec
des zéros.

### Spécification

* `numeric_std` est déjà importé.
* Convertis `a` en `unsigned`, le nombre de positions par
  `to_integer(unsigned(sh))`, et appelle `shift_left(...)`.
* Reconvertis le résultat via `std_logic_vector(...)`.
* Les bits qui dépassent le MSB sont perdus; les LSB deviennent `0`.

### Indice

Une seule ligne suffit :

```vhdl
y <= std_logic_vector(shift_left(unsigned(a), to_integer(unsigned(sh))));
```

### Où écrire

Seules les lignes entre `-- BEGIN_EDIT` et `-- END_EDIT` sont
évaluées.
