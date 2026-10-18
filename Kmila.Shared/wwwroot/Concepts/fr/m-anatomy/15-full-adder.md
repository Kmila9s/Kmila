# Additionneur complet

Un **additionneur complet** additionne trois bits — A, B et une retenue entrante **Ci** — produisant une somme S et une retenue sortante **Co**. Chaînés, ils additionnent de grands nombres.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/15-full-adder.html" title="Bloc combinatoire interactif sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Les poussoirs fixent les bits, le module calcule, les lampes de sortie s'allument et la **ligne courante de la table se surligne** pour lire la fonction directement.

En VHDL :

```vhdl
s  <= a xor b xor ci;
co <= (a and b) or (ci and (a xor b));
```

C'est purement **combinatoire** : les sorties ne dépendent que des entrées actuelles, sans horloge ni mémoire. L'additionneur complet est la brique de tout additionneur binaire : chaîne-en n, retenue à retenue, et tu additionnes deux nombres de n bits. Il se construit avec deux demi-additionneurs et un OU.
