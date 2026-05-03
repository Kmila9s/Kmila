## Convertisseur binaire→Gray 3 bits — propriété Gray cassée

Un convertisseur binaire→Gray standard. La recette classique pour `g`, étant donné le binaire `b`, est :

* `g(2) = b(2)`
* `g(1) = b(2) xor b(1)`
* `g(0) = b(1) xor b(0)`

Elle garantit que des valeurs binaires adjacentes (`b` et `b+1`) ne diffèrent que d'un seul bit Gray.

| `b`  | Gray |
|:----:|:----:|
| 000  | 000  |
| 001  | 001  |
| 010  | 011  |
| 011  | 010  |
| 100  | 110  |

### Ce qui cloche

Le starter calcule mal `g(2)` en y mélangeant `b(0)`. En passant de `b = 000` à `b = 001`, `g(0)` **et** `g(2)` basculent en même temps — deux bits changent, exactement ce que le code de Gray cherche à éviter.

### Indice

`g(2)` ne doit dépendre que du **MSB** de l'entrée binaire.
