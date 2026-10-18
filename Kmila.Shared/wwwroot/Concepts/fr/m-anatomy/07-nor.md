# Porte NOR

La porte **NOR** est un OU suivi d'un NON : elle donne 1 seulement quand les deux entrées valent 0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/07-nor.html" title="Porte logique interactive sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Le module calcule le résultat et la lampe de sortie **Y** s'allume selon la table.

En VHDL :

```vhdl
y <= a nor b;
```

Elle est purement **combinatoire** : la sortie ne dépend que des entrées actuelles, sans horloge ni mémoire. La bulle marque l'inversion. **NOR est aussi universelle** : tout circuit peut être construit uniquement avec des portes NOR.
