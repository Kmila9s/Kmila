# Exemple : ta première entité (MUX 2 vers 1)

Un **multiplexeur** est le dispositif combinatoire le plus simple qui comporte un « choix ». La version 2 vers 1 sélectionne entre deux entrées selon un bit `sel` :

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

Le VHDL pour ça tient en une ligne :

```vhdl
y <= a when sel = '0' else b;
```

## Ce que fait l'exemple

Ouvre l'exemple et tu obtiendras un projet nommé **Your first entity** contenant :

- `mux21.vhd` (le design ci-dessus)
- Une configuration de simulation associée qui pilote `sel`, `a` et `b` sur 40 microsecondes

Quand tu lances Run, l'onglet Waveforms montre quatre rangées — les trois entrées et la sortie. Regarde `y` basculer entre `a` et `b` quand `sel` bascule.

## Choses à remarquer

L'assignation `y <= a when sel = '0' else b;` est une **instruction concurrente** : elle vit hors de tout process, et elle décrit un câblage permanent. Il n'y a pas de « quand cette ligne s'exécute-t-elle ? » — elle *est*, tout simplement.

La condition dans un `when/else` peut être n'importe quelle expression booléenne :

```vhdl
y <= a when sel = '0' else
     b when enable = '1' else
     'Z';
```

Des chaînes comme ça se synthétisent en un multiplexeur à priorité (le cas du haut l'emporte en cas d'égalité).

## À toi : étends-le

Une fois que tu as vu l'exemple fonctionner :

1. Transforme l'entité en **MUX 4 vers 1** — `sel` devient 2 bits, et tu choisis entre `a`, `b`, `c`, `d`.
2. Ajoute un cinquième port `output_enable`. Quand il vaut `'0'`, force `y <= 'Z'` (haute impédance) quel que soit `sel`.
3. Compare `when/else` à la version `with/select` que tu rencontreras dans le module suivant.

L'éditeur Kmila surligne les mots-clés VHDL et connaît `std_logic` — passe sur `when` pour voir la documentation intégrée.
