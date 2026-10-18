# Encodeur

Un **encodeur** est l'inverse du décodeur : à partir d'une entrée one-hot (une ligne active), il donne l'**adresse binaire** de cette ligne.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/13-encoder.html" title="Bloc combinatoire interactif sur un banc à module" loading="lazy"></iframe></div>

Déplace le curseur (ou les flèches) pour parcourir toutes les combinaisons d'entrée. Les poussoirs fixent les bits, le module calcule, les lampes de sortie s'allument et la **ligne courante de la table se surligne** pour lire la fonction directement.

En VHDL :

```vhdl
a <= "11" when i3 = '1' else
     "10" when i2 = '1' else
     "01" when i1 = '1' else
     "00";
```

C'est purement **combinatoire** : les sorties ne dépendent que des entrées actuelles, sans horloge ni mémoire. Un encodeur à **priorité** résout plusieurs entrées actives en prenant la plus haute — utile pour les contrôleurs d'interruptions qui doivent choisir une requête.
