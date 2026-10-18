# Comparateur

Un **comparateur** compare deux nombres A et B et indique le cas : A < B, A = B ou A > B.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/16-comparator.html" title="Bloc combinatoire interactif sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Les poussoirs fixent les bits, le module calcule, les lampes de sortie s'allument et la **ligne courante de la table se surligne** pour lire la fonction directement.

En VHDL :

```vhdl
lt <= '1' when a < b else '0';
eq <= '1' when a = b else '0';
gt <= '1' when a > b else '0';
```

C'est purement **combinatoire** : les sorties ne dépendent que des entrées actuelles, sans horloge ni mémoire. Exactement une des trois sorties vaut 1. Les comparateurs de magnitude s'étendent bit à bit, du plus significatif vers le bas.
