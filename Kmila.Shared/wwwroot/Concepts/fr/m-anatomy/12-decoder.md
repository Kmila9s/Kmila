# Décodeur

Un **décodeur** transforme une adresse de n bits en une sortie **one-hot** : exactement une de ses 2ⁿ sorties vaut 1, celle que nomme l'adresse.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/12-decoder.html" title="Bloc combinatoire interactif sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Les poussoirs fixent les bits, le module calcule, les lampes de sortie s'allument et la **ligne courante de la table se surligne** pour lire la fonction directement.

En VHDL :

```vhdl
with a select y <=
  "0001" when "00",
  "0010" when "01",
  "0100" when "10",
  "1000" when others;
```

C'est purement **combinatoire** : les sorties ne dépendent que des entrées actuelles, sans horloge ni mémoire. Les décodeurs pilotent les lignes d'adresse : mémoires, afficheurs sept segments et distribution d'instructions choisissent ainsi une ligne parmi beaucoup.
