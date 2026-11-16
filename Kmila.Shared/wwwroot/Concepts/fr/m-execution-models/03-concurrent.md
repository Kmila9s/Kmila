# Concurrent : tout se déclenche ensemble

En dehors de tout `process` — directement dans une `architecture`, entre `begin` et `end` — tu peux écrire des **affectations de signaux concurrentes**. Elles ressemblent à des affectations séquentielles mais suivent une règle complètement différente :

> **Toutes les sentences concurrentes sensibles à un événement se déclenchent dans le même cycle delta.** L'ordre dans le code source n'a aucune importance.

Imagine un immeuble avec quatre interrupteurs dans des pièces différentes. Quand le courant arrive, l'état de chaque interrupteur prend effet au même instant — peu importe la pièce que l'électricien a câblée en premier.

Le lecteur ci-dessous contient quatre sentences concurrentes qui pilotent quatre sorties différentes à partir des mêmes entrées `a` et `b`. Appuie sur play et regarde les quatre lignes de sortie se mettre à jour au même pas.

<!-- exec-fig:start -->
## Retour au restaurant : vérifier ou écouter les sonnettes

Il n'y a toujours **qu'une serveuse**, donc c'est de la concurrence, pas du parallélisme. Compare deux façons de jongler entre les tables :

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=poll,bell&mode=poll" title="Restaurant : une serveuse qui vérifie chaque table ou qui répond aux sonnettes" loading="lazy"></iframe></div>

- **1 ↻** — elle fait le tour et **vérifie** chaque table, prête ou non. Chaque ↻ de l'affichage est un trajet perdu.
- **1 <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M3 12H13M4.6 12C4.6 6 5.8 3 8 3S11.4 6 11.4 12M6.6 14.2A1.5 1.5 0 0 0 9.4 14.2"/></svg>** — elle prend toutes les commandes, puis **ne va que là où une sonnette retentit** (la sonnette de la table s'allume quand son plat est prêt).

Le simulateur VHDL fonctionne de la deuxième façon. Il ne ré-exécute pas toutes les instructions en permanence : il ne réveille que les process dont la **liste de sensibilité** a reçu un **événement**. La sonnette, c'est la liste de sensibilité ; qu'elle sonne, c'est l'événement. Même serveuse, mêmes tables, moins d'étapes (environ 23 s au lieu de 37 s, et 0 vérification perdue).
<!-- exec-fig:end -->

<!-- exec-fig2:start -->
## Le photographe : une photo, un flash

Pourquoi l'ordre du code ne compte-t-il pas ? Parce que dans un cycle delta, toutes les instructions concurrentes lisent la même **photo** des entrées, et personne ne voit le résultat des autres avant le **flash**.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-photo.html" title="Studio photo : quatre bureaux recopient une seule photo ; le flash retourne les quatre panneaux ensemble" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- L'appareil déclenche : la photo fige `a` et `b`.
- Chaque bureau (un par instruction) écrit son **brouillon** à partir de cette même photo.
- Le flash, c'est la fin du delta : les quatre panneaux de sortie changent **en même temps**.
<!-- exec-fig2:end -->

## Deux façons de le vérifier

- **Échange mentalement deux lignes** — le résultat change-t-il ? Non. Essaie.
- **Retire les entrées de l'architecture et regarde ce qui arrive à TOUTES les sorties.** Elles reviennent toutes à `U` en même temps, parce qu'il n'y a pas de séquence — c'est une seule évaluation parallèle par cycle delta.

## Concurrent ou séquentiel — quand choisir quoi ?

| Cas d'usage | Choisir |
|---|---|
| Logique combinatoire (portes, mux, arithmétique sans horloge) | **Concurrent** |
| Comportement synchrone (registres, FSM, compteurs) | **Séquentiel** (`process` avec `rising_edge(clk)`) |
| Lire plusieurs signaux, faire des calculs, produire un résultat | Les deux marchent — le concurrent se lit plus comme un circuit, le séquentiel se lit plus comme un programme |

> **Essaie ceci :** mets le lecteur en pause au moment où les quatre lignes sont surlignées. Cet instant unique est **un cycle delta**. Les quatre affectations sont évaluées exactement au même temps de simulation.
