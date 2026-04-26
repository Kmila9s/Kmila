# Prérequis de maths discrètes pour le design numérique

Tous les étudiants n'arrivent pas avec le même bagage mathématique. Cette leçon est un rafraîchissement de 20 minutes sur les idées de maths discrètes qui apparaissent constamment en design numérique, pour que quand une leçon ultérieure dit « pense à ça comme un ensemble » ou « l'espace d'état est le produit cartésien de… », rien ne te stoppe net.

## Ensembles

Un **ensemble** est une collection non ordonnée d'éléments distincts : `{a, b, c}`. En design numérique, tu rencontreras des ensembles à trois endroits :

- L'ensemble des **valeurs possibles** qu'un signal peut prendre (`std_logic` est un ensemble de 9 éléments : `{U, X, 0, 1, Z, W, L, H, -}`).
- L'ensemble des **ports** qu'une entité déclare.
- L'ensemble des **états** dans lesquels une machine à états finis peut se trouver.

Opérations qui reviendront : **union** (fusion de deux ensembles), **intersection** (garder les communs), **différence** (retirer), et le **produit cartésien** — l'ensemble de toutes les paires ordonnées issues de deux ensembles. Un signal de 2 bits a `{0, 1} × {0, 1}` = 4 valeurs possibles.

## Fonctions

Une **fonction** associe chaque entrée à exactement une sortie. Tout circuit combinatoire est une fonction : étant données les entrées, il y a une sortie correcte. `y <= a and b` est la fonction `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`.

Deux propriétés que tu invoqueras sans y penser :

- **Totale** : chaque entrée a une sortie (dans un process combinatoire, tu dois assigner sur chaque chemin).
- **Déterministe** : même entrée → même sortie (pas de condition de course).

## Techniques de preuve

Trois sur lesquelles tu t'appuieras :

- **Tables de vérité** : vérification exhaustive. Fonctionne pour de petits espaces d'entrées ; notre Labo 1 utilise ça.
- **Algèbre de Boole** : simplification symbolique (De Morgan, distributivité, complément).
- **Induction sur la largeur de bit** : prouve qu'un additionneur N bits fonctionne en montrant le cas 1 bit et l'étape « si N fonctionne, N+1 fonctionne ». Apparaît dans les generics récursifs.

## Logique

La logique propositionnelle, c'est le design numérique avec une notation différente :

| Math        | VHDL           |
|-------------|----------------|
| ∧ (et)      | `and`          |
| ∨ (ou)      | `or`           |
| ¬ (non)     | `not`          |
| ⊕ (ou-ex)   | `xor`          |
| P ⇒ Q       | `(not P) or Q` |

Si tu as écrit une preuve en utilisant ces opérateurs, tu as écrit un circuit.

## Dénombrement

Deux formules que tu utiliseras :

- Un port de largeur N a **2^N** valeurs distinctes.
- Une machine à états de K états peut être encodée dans **⌈log₂ K⌉** flip-flops (encodage binaire) ou **K** flip-flops (one-hot).

C'est la raison pour laquelle une adresse 4 bits te donne 16 emplacements mémoire, pourquoi une FSM de feu tricolore tient dans 2 flip-flops, et pourquoi un compteur 32 bits couvre 4 milliards de fronts d'horloge avant de déborder.

## À retenir

- Ensembles, fonctions, produits cartésiens — le vocabulaire des circuits.
- L'algèbre de Boole est de la logique propositionnelle qu'on peut souder.
- `⌈log₂ K⌉` est la largeur en bits nécessaire pour K choses distinctes.
