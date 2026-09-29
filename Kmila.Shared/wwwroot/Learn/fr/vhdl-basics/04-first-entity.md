# Exemple : votre première entity (MUX 2 vers 1)

Un **multiplexeur** est le dispositif combinatoire le plus simple qui ait un « choix ». La version 2 vers 1 sélectionne entre deux entrées selon un bit `sel` :

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

Le VHDL pour ça tient sur une ligne :

```vhdl
y <= a when sel = '0' else b;
```

## Ce que fait l'exemple

Ouvrez l'exemple et vous obtenez un projet nommé **Your first entity** avec :

- `mux21.vhd` (le design ci-dessus)
- Une config de simulation compagnon qui pilote `sel`, `a` et `b` sur 40 microsecondes

Quand vous cliquez sur Exécuter, l'onglet Formes d'onde affiche quatre lignes — les trois entrées et la sortie. Regardez `y` basculer entre `a` et `b` au gré de `sel`.

## Choses à remarquer

L'affectation `y <= a when sel = '0' else b;` est une **instruction concurrente** : elle vit hors de tout process et décrit un câblage permanent. Il n'y a pas de « quand cette ligne s'exécute ? » — elle est, simplement.

La condition dans un `when/else` peut être n'importe quelle expression booléenne :

```vhdl
y <= a when sel = '0' else
     b when enable = '1' else
     'Z';
```

Ce genre de chaîne se synthétise en multiplexeur prioritaire (le cas du dessus gagne les égalités).

## À vous : étendez-le

Une fois que vous avez vu l'exemple marcher :

1. Changez l'entity en un **MUX 4 vers 1** — `sel` devient 2 bits, et vous choisissez entre `a`, `b`, `c`, `d`.
2. Ajoutez un cinquième port `output_enable`. Quand il vaut `'0'`, pilotez `y <= 'Z'` (haute impédance) quel que soit `sel`.
3. Comparez `when/else` avec la version `with/select` que vous rencontrerez au module suivant.

L'éditeur de Kmila met en évidence les mots-clés VHDL et connaît `std_logic` — survolez `when` pour voir la doc intégrée.
