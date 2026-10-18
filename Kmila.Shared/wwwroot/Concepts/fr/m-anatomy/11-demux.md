# Démultiplexeur (DEMUX)

Un **démultiplexeur** est l'inverse du MUX : il aiguille une entrée **D** vers l'une de plusieurs sorties, choisie par la ligne de **sélection** S.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/11-demux.html" title="Bloc combinatoire interactif sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Les poussoirs fixent les bits, le module calcule, les lampes de sortie s'allument et la **ligne courante de la table se surligne** pour lire la fonction directement.

En VHDL :

```vhdl
y0 <= d when s = '0' else '0';
y1 <= d when s = '1' else '0';
```

C'est purement **combinatoire** : les sorties ne dépendent que des entrées actuelles, sans horloge ni mémoire. Les sorties non sélectionnées restent à 0. Décodeurs et démultiplexeurs sont cousins : un démux est un décodeur dont l'entrée de données valide la sortie active.
