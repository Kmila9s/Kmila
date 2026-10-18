# Porte ET

La porte **ET** donne 1 seulement quand **les deux** entrées valent 1.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/04-and.html" title="Porte logique interactive sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Le module calcule le résultat et la lampe de sortie **Y** s'allume selon la table.

En VHDL :

```vhdl
y <= a and b;
```

Elle est purement **combinatoire** : la sortie ne dépend que des entrées actuelles, sans horloge ni mémoire. Pense à deux interrupteurs en série : le courant atteint la lampe seulement si les deux sont fermés.
