# Porte NAND

La porte **NAND** est un ET suivi d'un NON : elle donne 0 seulement quand les deux entrées valent 1.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/06-nand.html" title="Porte logique interactive sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Le module calcule le résultat et la lampe de sortie **Y** s'allume selon la table.

En VHDL :

```vhdl
y <= a nand b;
```

Elle est purement **combinatoire** : la sortie ne dépend que des entrées actuelles, sans horloge ni mémoire. La bulle de sortie marque l'inversion. **NAND est universelle** : toute fonction logique peut être construite uniquement avec des portes NAND.
