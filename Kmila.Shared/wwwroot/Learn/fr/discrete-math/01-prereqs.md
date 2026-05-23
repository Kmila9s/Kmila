# Prérequis de maths discrètes pour le design numérique

Les étudiants n'arrivent pas tous avec le même bagage mathématique. Cette leçon est un rappel de 20 minutes sur les idées de maths discrètes qui reviennent constamment en design numérique — pour qu'aucune phrase ultérieure du type « pense à ça comme un ensemble » ou « l'espace des états est le produit cartésien de … » ne te bloque.

## Ensembles

Un **ensemble** est une collection non ordonnée d'éléments distincts : `{a, b, c}`. En design numérique, les ensembles apparaissent à trois endroits :

- L'ensemble des **valeurs possibles** d'un signal (`std_logic` est un ensemble à 9 éléments : `{U, X, 0, 1, Z, W, L, H, -}`).
- L'ensemble des **ports** déclarés par une entity.
- L'ensemble des **états** dans lesquels une machine à états finis peut se trouver.

Opérations à connaître : **union** (fusionner deux ensembles), **intersection** (garder le commun), **différence** (retirer), et le **produit cartésien** — l'ensemble de tous les couples ordonnés tirés de deux ensembles. Un signal 2 bits a `{0, 1} × {0, 1}` = 4 valeurs possibles.

## Fonctions

Une **fonction** associe à chaque entrée exactement une sortie. Tout circuit combinatoire est une fonction : pour des entrées données, il existe une sortie correcte. `y <= a and b` est la fonction `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`.

Deux propriétés que tu invoqueras sans y penser :

- **Totale** : chaque entrée a une sortie (dans un process combinatoire, tu dois affecter sur chaque chemin).
- **Déterministe** : même entrée → même sortie (pas de conditions de course).

## Techniques de preuve

Trois sur lesquelles tu vas t'appuyer :

- **Tables de vérité** : vérification exhaustive. Marche pour les petits espaces d'entrées ; notre Lab 1 s'en sert.
- **Algèbre de Boole** : simplification symbolique (De Morgan, distributivité, complément).
- **Récurrence sur la largeur en bits** : démontrer qu'un additionneur N bits fonctionne en montrant le cas 1 bit puis l'étape « si N marche, N+1 marche ». On la retrouve dans les generics récursifs.

## Logique

La logique propositionnelle EST le design numérique avec une autre notation :

| Maths        | VHDL          |
|--------------|---------------|
| ∧ (et)       | `and`         |
| ∨ (ou)       | `or`          |
| ¬ (non)      | `not`         |
| ⊕ (xor)      | `xor`         |
| P ⇒ Q        | `(not P) or Q` |

Si tu as déjà écrit une preuve avec ces opérateurs, tu as écrit un circuit.

## Dénombrement

Deux formules qui te serviront :

- Un port de largeur N a **2^N** valeurs distinctes.
- Une machine à K états peut être codée sur **⌈log₂ K⌉** bascules (codage binaire) ou sur **K** bascules (one-hot).

C'est la raison pour laquelle une adresse de 4 bits donne 16 cases mémoire, pourquoi un FSM de feu tricolore tient en 2 bascules, et pourquoi un compteur 32 bits couvre 4 milliards de fronts d'horloge avant de boucler.

## À retenir

- Ensembles, fonctions, produits cartésiens — le vocabulaire des circuits.
- L'algèbre de Boole, c'est de la logique propositionnelle que l'on peut souder.
- `⌈log₂ K⌉` est la largeur en bits nécessaire pour K choses distinctes.
