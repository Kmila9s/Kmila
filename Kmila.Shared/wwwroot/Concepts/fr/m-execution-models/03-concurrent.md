# Concurrent : tout se déclenche ensemble

En dehors de tout `process` — directement dans une `architecture`, entre
`begin` et `end` — tu peux écrire des **affectations de signaux
concurrentes**. Elles ressemblent à des affectations séquentielles mais
suivent une règle complètement différente :

> **Toutes les sentences concurrentes sensibles à un événement se
> déclenchent dans le même cycle delta.** L'ordre dans le code source
> n'a aucune importance.

Imagine un immeuble avec quatre interrupteurs dans des pièces différentes.
Quand le courant arrive, l'état de chaque interrupteur prend effet au
même instant — peu importe la pièce que l'électricien a câblée en
premier.

Le lecteur ci-dessous contient quatre sentences concurrentes qui pilotent
quatre sorties différentes à partir des mêmes entrées `a` et `b`. Appuie
sur play et regarde les quatre lignes de sortie se mettre à jour au même
pas.

## Deux façons de le vérifier

- **Échange mentalement deux lignes** — le résultat change-t-il ? Non.
  Essaie.
- **Retire les entrées de l'architecture et regarde ce qui arrive à
  TOUTES les sorties.** Elles reviennent toutes à `U` en même temps,
  parce qu'il n'y a pas de séquence — c'est une seule évaluation
  parallèle par cycle delta.

## Concurrent ou séquentiel — quand choisir quoi ?

| Cas d'usage | Choisir |
|---|---|
| Logique combinatoire (portes, mux, arithmétique sans horloge) | **Concurrent** |
| Comportement synchrone (registres, FSM, compteurs) | **Séquentiel** (`process` avec `rising_edge(clk)`) |
| Lire plusieurs signaux, faire des calculs, produire un résultat | Les deux marchent — le concurrent se lit plus comme un circuit, le séquentiel se lit plus comme un programme |

> **Essaie ceci :** mets le lecteur en pause au moment où les quatre
> lignes sont surlignées. Cet instant unique est **un cycle delta**.
> Les quatre affectations sont évaluées exactement au même temps de
> simulation.
