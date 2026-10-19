# Les prérequis de maths discrètes pour la conception numérique

Tout le monde n'arrive pas avec le même bagage en maths. Cette leçon est une remise à niveau de 25 minutes sur les idées de maths discrètes qui reviennent sans arrêt en conception numérique, pour qu'une leçon plus tardive qui dit « voyez ça comme un ensemble » ou « l'espace d'états est le produit cartésien de… » ne vous arrête jamais net. Tout ce qui est ici correspond directement aux quatre unités du cours ESCOM *Matemáticas Discretas* — Logique, Théorie des ensembles, Entiers et Structures discrètes — et cette fois on lit chacune comme du matériel.

## La logique, c'est de la conception numérique avec d'autres symboles

La logique propositionnelle (l'unité I du cours) est la même algèbre qu'obéissent vos portes, juste écrite avec d'autres glyphes. Une **proposition** est un énoncé qui est soit vrai soit faux — exactement les deux valeurs qu'un fil peut porter.

| Maths        | Se lit         | VHDL            |
|--------------|----------------|-----------------|
| ∧ (et)       | « P et Q »     | `and`           |
| ∨ (ou)       | « P ou Q »     | `or`            |
| ¬ (non)      | « non P »      | `not`           |
| ⊕ (xor)      | « P xor Q »    | `xor`           |
| P ⇒ Q        | « si P alors Q » | `(not P) or Q` |

Cette dernière ligne mérite d'être mémorisée. Le conditionnel **P ⇒ Q est logiquement équivalent à ¬P ∨ Q** — il n'existe pas de porte `implies` parce que vous n'en avez jamais besoin. Deux autres équivalences que vous invoquerez constamment sont les **lois de De Morgan** :

- ¬(P ∧ Q) ≡ ¬P ∨ ¬Q  →  `not (a and b) = (not a) or (not b)`
- ¬(P ∨ Q) ≡ ¬P ∧ ¬Q  →  `not (a or b) = (not a) and (not b)`

De Morgan, c'est pourquoi on peut faire passer une bulle (une inversion) à travers une porte et échanger un ET contre un OU. Les outils de synthèse le font des milliers de fois pour faire tenir votre logique sur les cellules qu'une bibliothèque fournit réellement.

**Attention :** la réciproque (Q ⇒ P), l'inverse (¬P ⇒ ¬Q) et la contraposée (¬Q ⇒ ¬P) ne sont *pas* interchangeables. Seule la **contraposée est équivalente** à l'originale — une distinction que le cours martèle en unité I, et la raison pour laquelle un « if-else » négligent peut décrire le mauvais circuit.

## Algèbre de Boole : la logique que vous pouvez souder

L'unité IV appelle ça *álgebras y funciones booleanas*, et c'est le cœur formel de la conception combinatoire. Une **fonction booléenne** de n variables est une application de {0,1}ⁿ vers {0,1}. Il y en a exactement `2^(2^n)` pour n entrées — 16 fonctions distinctes à 2 entrées, ce qui explique pourquoi les six portes nommées plus les constantes couvrent tout.

Toute fonction de ce genre a deux formes canoniques :

- **Somme de minterms (SOP) :** on fait le OU d'un terme produit pour chaque ligne d'entrée qui donne un `1`.
- **Produit de maxterms (POS) :** on fait le ET d'un terme somme pour chaque ligne qui donne un `0`.

Exemple travaillé — la fonction majorité (sortie `1` quand deux ou trois entrées valent `1`) :

```
f(a,b,c) = (a and b) or (a and c) or (b and c)
```

Ces trois produits sont les minterms. On peut ensuite réduire l'expression avec les axiomes de Boole — identité, complément (`a or not a = 1`), distributivité, absorption (`a or (a and b) = a`) — ou avec une **table de Karnaugh**, le minimiseur visuel de l'unité IV. Moins de littéraux, c'est moins de transistors.

```vhdl
y <= (a and b) or (a and c) or (b and c);  -- trois portes, évaluées comme des fils
```

**Attention :** minimiser pour la *surface* n'est pas la même chose que minimiser pour la *vitesse*, et un synthétiseur peut défaire votre algèbre peaufinée à la main pour équilibrer les deux. Écrivez une logique claire ; laissez l'outil optimiser.

## Ensembles : le vocabulaire des signaux, des ports et des états

Un **ensemble** (unité II) est une collection non ordonnée d'éléments distincts : `{a, b, c}`. En conception numérique vous en croisez partout :

- Les **valeurs qu'un signal peut prendre.** `std_logic` est un ensemble à 9 éléments — `{U, X, 0, 1, Z, W, L, H, -}` — couvrant non initialisé, inconnu, les deux niveaux logiques forts, la haute impédance, les niveaux faibles et le don't-care.
- Les **ports** qu'une entity déclare, et les **états** qu'une machine à états finis peut occuper.

Les opérations qui reviennent sont l'**union** (∪), l'**intersection** (∩), la **différence** (∖) et le **complément**. Deux outils de dénombrement de l'unité II gagnent leur place :

- **Produit cartésien** A × B : l'ensemble de tous les couples ordonnés. Un signal 2 bits vit dans `{0,1} × {0,1}`, il a donc 4 valeurs ; un vecteur de n bits vit dans `{0,1}ⁿ`, ce qui donne **2ⁿ**. C'est littéralement pourquoi une adresse de 4 bits atteint 16 emplacements.
- **Ensemble des parties** 𝒫(A) : l'ensemble de tous les sous-ensembles de A, de cardinal **2^|A|**. La table de vérité d'une fonction à n entrées est une énumération de 𝒫 sur les entrées — les mêmes 2ⁿ lignes.

Pour dimensionner des ensembles qui se recouvrent, le **principe d'inclusion-exclusion** donne |A ∪ B| = |A| + |B| − |A ∩ B|.

## Fonctions et relations

Tout circuit combinatoire **est une fonction** : étant donné les entrées, il y a exactement une sortie correcte. `y <= a and b` réalise `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`. Deux propriétés (unité IV, *propiedades de las funciones*) que vous invoquerez sans les nommer :

- **Totale** — chaque entrée a une sortie. Dans un `process` combinatoire vous devez affecter la sortie sur *chaque* chemin, sinon l'outil infère un latch indésirable pour « se souvenir » du cas manquant.
- **Déterministe** — la même entrée donne toujours la même sortie, sans conditions de course.

La logique séquentielle élargit ça aux **relations** (unité IV) : une relation d'état suivant peut envoyer un état vers plusieurs, ce qui est exactement une MAE non déterministe — généralement un bug, parfois une spécification que vous raffinez en une machine déterministe.

## Entiers, bases et récurrence

L'unité III est la théorie des nombres, et sa section *sistemas de numeración* est du pur travail de fond numérique : l'algorithme de changement de base, le lien étroit entre binaire et hexadécimal (un chiffre hexa = un nibble de 4 bits), et l'arithmétique en base *b*. Si la conversion de base ou le complément à deux vous semble fragile, la leçon **Systèmes de numération** du module Fondamentaux du numérique la parcourt en détail.

La théorie des nombres vous met aussi en main des outils que vous utiliserez nommément :

- **Divisibilité et arithmétique modulaire.** Un compteur qui reboucle à K fait de l'arithmétique *mod K* ; les diviseurs d'horloge et le rebouclage d'adresses sont des congruences (unité IV, *enteros módulo n*).
- **Récurrence mathématique.** Pour prouver qu'un additionneur à propagation de retenue de N bits est correct, montrez que le cas 1 bit marche, puis que « si la version N bits marche, alors N+1 aussi ». C'est la forme de chaque boucle `for … generate` qui coud un chemin de données large à partir d'une tranche répétée.

## Compter les bits

Deux formules bouclent la boucle entre les quatre unités :

- Un port de largeur N contient **2^N** valeurs distinctes (produit cartésien / ensemble des parties).
- Une machine à K états tient dans **⌈log₂ K⌉** flip-flops en codage binaire, ou **K** flip-flops en one-hot.

C'est pourquoi une MAE de feu tricolore tient dans 2 flip-flops, et pourquoi un compteur 32 bits voit environ 4 milliards de fronts avant de reboucler.

## À retenir

- La logique propositionnelle *est* l'algèbre de Boole — mêmes opérateurs, et P ⇒ Q n'est que `(not P) or Q`. De Morgan vous laisse déplacer les inversions à travers les portes.
- Une fonction booléenne a des formes canoniques somme-de-minterms / produit-de-maxterms ; les tables de Karnaugh et les axiomes les réduisent.
- Les ensembles nomment les valeurs, les ports et les états d'un circuit ; **2ⁿ** (produit cartésien / ensemble des parties) et **⌈log₂ K⌉** sont les formules de largeur que vous réutiliserez toute votre vie.
- La logique combinatoire est une fonction *totale et déterministe* — affectez chaque sortie sur chaque chemin ou vous obtenez un latch accidentel.
