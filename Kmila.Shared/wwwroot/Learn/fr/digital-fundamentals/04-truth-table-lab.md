# Lab : explorateur de table de vérité

Ce lab est la première fois que tu vas piloter un vrai design VHDL. L'entity a deux entrées (`a`, `b`) et six sorties — une par porte élémentaire à 2 entrées.

## Ce qui se passe quand tu cliques *Ouvrir dans l'éditeur*

1. Kmila crée un projet jetable appelé un truc comme « Lab : explorateur de table de vérité ».
2. Il l'amorce avec `gates.vhd` contenant les six affectations concurrentes.
3. Il charge une **configuration de simulation** qui stimule `a` et `b` sur toutes les combinaisons.
4. L'éditeur s'ouvre, avec la config déjà sélectionnée dans la pillule en haut.

À partir de là :

- Clique **Build** pour parser et élaborer le design.
- Clique **Run** pour simuler.
- Passe à l'onglet **Waveforms** dans le dock en bas.

Tu verras sept lignes de signaux carrés : tes deux entrées en haut, et chaque sortie réagissant en temps réel.

## À quoi regarder

Utilise la barre de défilement ou le clavier pour aligner les fronts d'entrée avec les transitions de sortie. Essaie de vérifier le comportement de chaque porte sans regarder les tables de vérité :

- `o_and` : reste bas jusqu'à ce que `a` et `b` soient à 1 — alors il monte.
- `o_nand` : l'inverse. Habituellement haut, il ne descend que quand les deux entrées sont à 1.
- `o_xor` : haut exactement quand les deux entrées diffèrent.

Si l'une d'elles ne correspond pas à ce que tu attendais, ouvre l'onglet **Output** — les avertissements de build atterrissent là.

## Essaie par toi-même

Une fois que l'exemple marche, expérimente :

- Ajoute une troisième entrée `c` à l'entity et calcule `a xor b xor c` (parité à 3 entrées).
- Renomme une sortie et reconstruis — l'onglet **Ports** se mettra à jour.
- Ajoute une autre ligne à la config : bascule `a` et `b` simultanément et vois quelles sorties changent.

Plus tu manipules ces leviers avant d'écrire ton propre design, moins VHDL aura l'air d'être de la devinette.
