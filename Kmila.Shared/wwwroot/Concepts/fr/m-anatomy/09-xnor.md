# Porte XNOR (équivalence)

La porte **XNOR** donne 1 quand ses entrées sont **égales** — c'est un XOR avec bulle d'inversion.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/09-xnor.html" title="Porte logique interactive sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Le module calcule le résultat et la lampe de sortie **Y** s'allume selon la table.

En VHDL :

```vhdl
y <= a xnor b;
```

Elle est purement **combinatoire** : la sortie ne dépend que des entrées actuelles, sans horloge ni mémoire. XNOR répond « ces deux bits sont-ils égaux ? » — c'est la fonction d'égalité (comparateur) sur 1 bit.
