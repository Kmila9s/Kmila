# Porte NON (inverseur)

La **porte NON** est la logique la plus simple qui soit : une entrée, une sortie, et la sortie vaut toujours le **contraire** de l'entrée — 0 en entrée donne 1 en sortie, 1 donne 0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/02-not.html" title="Porte NON interactive — une balançoire qui inverse son entrée" loading="lazy"></iframe></div>

**Déplace le curseur** pour mettre l'entrée **A** à 0 ou 1. La figure est une balançoire à bras égaux : pousse l'entrée (A) vers le bas et la sortie (Y) monte, et inversement — égales et opposées. Le petit cercle sur la sortie est la bulle d'inversion.

## La bulle

Ce **cercle** sur la sortie est la *bulle d'inversion*, le symbole du NON dans tout schéma. Chaque fois que tu le vois sur la sortie d'une porte (NON-ET, NON-OU, NON-OU exclusif…), il signifie la même chose : « et en plus, inverse ».

| A | Y |
|---|---|
| 0 | 1 |
| 1 | 0 |

En VHDL, l'inverseur est l'opérateur `not` :

```vhdl
y <= not a;
```

Elle est purement **combinatoire** : la sortie ne dépend que de l'entrée actuelle — ni horloge ni mémoire. Dès que l'entrée change, la sortie s'inverse après un court délai de propagation.
