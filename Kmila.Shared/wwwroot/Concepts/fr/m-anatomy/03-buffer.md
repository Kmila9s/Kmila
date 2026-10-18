# Tampon (BUF)

Le **tampon** recopie son entrée sur sa sortie sans la changer (Y = A). Il ne change rien à la logique — il sert à **attaquer** un signal : le renforcer ou ajouter un retard contrôlé.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/03-buffer.html" title="Porte logique interactive sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Le module calcule le résultat et la lampe de sortie **Y** s'allume selon la table.

En VHDL :

```vhdl
y <= a;
```

Elle est purement **combinatoire** : la sortie ne dépend que des entrées actuelles, sans horloge ni mémoire. Remarque l'absence de bulle d'inversion sur la sortie — c'est la seule différence avec une porte NON.
