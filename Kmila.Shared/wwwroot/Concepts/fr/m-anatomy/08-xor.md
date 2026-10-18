# Porte XOR (OU exclusif)

La porte **XOR** (OU exclusif) donne 1 quand ses entrées sont **différentes**.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/08-xor.html" title="Porte logique interactive sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Le module calcule le résultat et la lampe de sortie **Y** s'allume selon la table.

En VHDL :

```vhdl
y <= a xor b;
```

Elle est purement **combinatoire** : la sortie ne dépend que des entrées actuelles, sans horloge ni mémoire. XOR est au cœur de l'addition (le bit de somme), de la parité et de la comparaison : elle répond « ces deux bits sont-ils différents ? ».
