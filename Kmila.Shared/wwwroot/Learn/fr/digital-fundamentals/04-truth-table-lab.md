# Labo : explorateur de table de vérité

Ce labo est la première fois que tu vas piloter un vrai design VHDL. L'entité a deux entrées (`a`, `b`) et six sorties — une par porte élémentaire à 2 entrées.

## Ce qui se passe quand tu cliques sur *Ouvrir dans l'éditeur*

1. Kmila crée un projet jetable appelé quelque chose comme « Labo : explorateur de table de vérité ».
2. Il l'amorce avec `gates.vhd` contenant les six assignations concurrentes.
3. Il charge une **configuration de simulation** qui stimule `a` et `b` à travers chaque combinaison.
4. L'atelier éditeur s'ouvre, avec la config déjà sélectionnée dans la pastille en haut.

À partir de là :

- Clique sur **Build** pour parser et élaborer le design.
- Clique sur **Run** pour simuler.
- Passe à l'onglet **Waveforms** dans le dock du bas.

Tu verras sept rangées de signaux carrés : tes deux entrées en haut, et chaque sortie qui réagit en temps réel.

## Ce qu'il faut observer

Utilise la barre de défilement ou le clavier pour aligner les fronts d'entrée avec les transitions de sortie. Essaie de vérifier le comportement de chaque porte sans revenir aux tables de vérité :

- `o_and` : reste bas jusqu'à ce que `a` et `b` valent 1 — puis saute à haut.
- `o_nand` : l'inverse. Normalement haut, passe bas seulement quand les deux entrées sont à 1.
- `o_xor` : haut exactement quand les deux entrées diffèrent.

Si l'une d'elles ne correspond pas à ce que tu attends, ouvre l'onglet **Output** — les warnings de build y atterrissent.

## Essaie par toi-même

Une fois que l'exemple fonctionne, expérimente :

- Ajoute une troisième entrée `c` à l'entité et calcule `a xor b xor c` (parité à trois entrées).
- Renomme une sortie et rebuild — l'onglet **Ports** se mettra à jour pour suivre.
- Ajoute une autre ligne à la config : bascule `a` et `b` simultanément et vois quelles sorties changent.

Plus tu manipules ces leviers avant d'écrire ton propre design, moins VHDL ressemblera à de la devinette.
