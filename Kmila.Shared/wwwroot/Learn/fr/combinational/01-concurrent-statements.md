# Assignations concurrentes : `when/else` et `with/select`

La logique combinatoire en VHDL s'écrit avec des instructions concurrentes — chaque ligne hors d'un process décrit un morceau de matériel câblé en permanence. Il existe deux variantes syntaxiques pour le même travail : **conditionnelle** et **sélective**.

## `when/else` — conditionnelle

À utiliser quand tu as une liste de conditions priorisées, ou quand chaque branche vérifie un signal différent :

```vhdl
y <= '0' when reset = '1' else
     a   when mode  = "read" else
     b   when mode  = "write" else
     'Z';
```

La première condition qui matche l'emporte. L'ordre compte — cela se synthétise en un **multiplexeur à priorité** : la condition du haut a le chemin le plus direct vers `y`.

## `with/select` — sélective

À utiliser quand tu branches sur une seule expression (comme un `switch` en logiciel) :

```vhdl
with mode select
  y <=
    a   when "00",
    b   when "01",
    c   when "10",
    'Z' when others;
```

Tous les cas se comparent au même signal (`mode`), et `when others` est obligatoire pour couvrir tous les patterns possibles. Ce style se synthétise en un **multiplexeur équilibré** — aucune branche n'est privilégiée.

## Laquelle choisir ?

- **Même signal, plusieurs patterns :** `with/select` se lit comme une table de vérité. Utilise-la.
- **Signaux différents, logique de priorité :** `when/else`. Choix naturel.
- **Sortie de synthèse :** les deux produisent à peu près les mêmes portes sur un FPGA moderne — choisir la forme la plus claire au niveau du code est presque toujours le bon choix.

## Un petit piège

Les deux formes exigent que `y` finisse assigné dans **tous** les scénarios. Si tu oublies le dernier `else` d'un `when/else`, ou que tu sautes le `when others` d'un `with/select`, le compilateur t'avertira — et le synthétiseur pourra inférer un latch pour « mémoriser » la dernière valeur, ce qui n'est presque jamais ce que tu veux. C'est le même piège du latch que tu reverras dans l'exercice de correction du module séquentiel.

## Où ça apparaît ensuite

Tu utiliseras `with/select` dans l'exemple du décodeur 3 vers 8 juste après cette leçon, et `when/else` partout dans la FSM de feu tricolore plus tard. Les deux sont du pain quotidien en VHDL.
