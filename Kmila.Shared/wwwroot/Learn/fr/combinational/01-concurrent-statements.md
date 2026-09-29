# Affectations concurrentes : `when/else` et `with/select`

La logique combinatoire en VHDL s'écrit avec des instructions concurrentes — chaque ligne hors d'un process décrit un morceau de matériel câblé en dur. Deux saveurs syntaxiques pour le même job : affectation **conditionnelle** et **sélective**.

## `when/else` — conditionnel

Bien quand vous avez une liste priorisée de conditions, ou quand chaque branche teste un signal différent :

```vhdl
y <= '0' when reset = '1' else
     a   when mode  = "read" else
     b   when mode  = "write" else
     'Z';
```

La première condition vraie gagne. L'ordre compte — ça compile vers un **MUX prioritaire** : la condition la plus haute a le chemin le plus direct vers `y`.

## `with/select` — sélectif

Bien quand vous branchez sur une seule expression (comme un `switch` logiciel) :

```vhdl
with mode select
  y <=
    a   when "00",
    b   when "01",
    c   when "10",
    'Z' when others;
```

Tous les cas comparent au même signal (`mode`), et `when others` est obligatoire pour couvrir tous les motifs possibles. Ce style compile vers un **MUX équilibré** — aucune branche n'est privilégiée.

## Lequel choisir ?

- **Même signal, plusieurs motifs :** `with/select` se lit comme une table de vérité. Prenez-le.
- **Signaux différents, logique prioritaire :** `when/else`. Naturel.
- **Sortie de synthèse :** les deux produisent à peu près les mêmes portes sur un FPGA moderne — choisir la forme la plus claire au niveau source est presque toujours la bonne décision.

## Un petit piège

Les deux formes exigent que `y` soit assigné dans **chaque** scénario. Si vous oubliez le `else` final d'un `when/else`, ou si vous sautez le `when others` d'un `with/select`, le compilateur vous avertit — et le synthétiseur peut inférer un verrou pour « se souvenir » de la dernière valeur, ce qui n'est presque jamais ce que vous voulez. C'est le même piège du verrou que vous reverrez dans l'exercice fix-it du module séquentiel.

## Où ça apparaît ensuite

Vous utiliserez `with/select` dans le décodeur 3 vers 8 juste après cette leçon, et `when/else` un peu partout dans le FSM de feu tricolore plus tard. Tous deux sont le pain quotidien du VHDL.
