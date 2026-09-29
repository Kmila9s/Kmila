# Lab : explorateur de table de vérité

Ce lab est la première fois que vous allez piloter un vrai design VHDL. L'entity a deux entrées (`a`, `b`) et six sorties — une par porte élémentaire à 2 entrées.

## Ce qui se passe quand vous cliquez *Ouvrir dans l'éditeur*

1. Kmila crée un projet jetable appelé un truc comme « Lab : explorateur de table de vérité ».
2. Il l'amorce avec `gates.vhd` contenant les six affectations concurrentes.
3. Il charge une **configuration de simulation** qui stimule `a` et `b` sur toutes les combinaisons.
4. L'éditeur s'ouvre, avec la config déjà sélectionnée dans la pillule en haut.

À partir de là :

- Cliquez **Compiler** pour parser et élaborer le design.
- Cliquez **Exécuter** pour simuler.
- Passez à l'onglet **Formes d'onde** dans le dock en bas.

Vous verrez sept lignes de signaux carrés : vos deux entrées en haut, et chaque sortie réagissant en temps réel.

## À quoi regarder

Utilisez la barre de défilement ou le clavier pour aligner les fronts d'entrée avec les transitions de sortie. Essayez de vérifier le comportement de chaque porte sans regarder les tables de vérité :

- `o_and` : reste bas jusqu'à ce que `a` et `b` soient à 1 — alors il monte.
- `o_nand` : l'inverse. Habituellement haut, il ne descend que quand les deux entrées sont à 1.
- `o_xor` : haut exactement quand les deux entrées diffèrent.

Si l'une d'elles ne correspond pas à ce que vous attendiez, ouvrez l'onglet **Sortie** — les avertissements de compilation atterrissent là.

## Essayez par vous-même

Une fois que l'exemple marche, expérimentez :

- Ajoutez une troisième entrée `c` à l'entity et calculez `a xor b xor c` (parité à 3 entrées).
- Renommez une sortie et reconstruisez — l'onglet **Ports** se mettra à jour.
- Ajoutez une autre ligne à la config : basculez `a` et `b` simultanément et voyez quelles sorties changent.

Plus vous manipulez ces leviers avant d'écrire votre propre design, moins VHDL aura l'air d'être de la devinette.
