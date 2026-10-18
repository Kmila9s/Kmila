# Multiplexeur (MUX)

Un **multiplexeur** aiguille l'une de plusieurs entrées de données vers la sortie. Une ligne de **sélection** **S** choisit laquelle : ici Y = D1 si S = 1, sinon D0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/10-mux.html" title="Bloc combinatoire interactif sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Les poussoirs fixent les bits, le module calcule, les lampes de sortie s'allument et la **ligne courante de la table se surligne** pour lire la fonction directement.

En VHDL :

```vhdl
y <= d1 when s = '1' else d0;
```

C'est purement **combinatoire** : les sorties ne dépendent que des entrées actuelles, sans horloge ni mémoire. Chaque `if/then/else` et `case` en VHDL devient un MUX après synthèse. C'est aussi ainsi que marche le load-enable d'un registre : un MUX choisit entre la nouvelle valeur et conserver l'ancienne.
