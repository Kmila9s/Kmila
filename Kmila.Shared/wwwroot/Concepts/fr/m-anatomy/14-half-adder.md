# Demi-additionneur

Un **demi-additionneur** additionne deux bits A et B, produisant une **somme** S et une **retenue** C.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/14-half-adder.html" title="Bloc combinatoire interactif sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Les poussoirs fixent les bits, le module calcule, les lampes de sortie s'allument et la **ligne courante de la table se surligne** pour lire la fonction directement.

En VHDL :

```vhdl
s <= a xor b;   -- sum
c <= a and b;   -- carry
```

C'est purement **combinatoire** : les sorties ne dépendent que des entrées actuelles, sans horloge ni mémoire. La somme est un **XOR** et la retenue un **ET** : un demi-additionneur, ce sont littéralement ces deux portes côte à côte. Il n'a pas de retenue entrante, donc ce n'est que le premier étage.
