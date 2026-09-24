## Parité paire 4 bits — remplis le trou

Le bit de parité paire `p` d'un mot 4 bits `d` vaut `1` quand le
nombre de bits à `1` dans `d` est **impair**, et `0` sinon — c'est
exactement le XOR des quatre bits d'entrée.

### Comportement

`p <= d(0) xor d(1) xor d(2) xor d(3)`

### Exemples

| `d`   | uns | `p` |
|:------|:---:|:---:|
| 0000  |  0  |  0  |
| 0001  |  1  |  1  |
| 0011  |  2  |  0  |
| 1110  |  3  |  1  |
| 1111  |  4  |  0  |

### Où écrire

L'entité, les ports et le squelette sont en place. Seule la ligne
entre `-- BEGIN_EDIT` et `-- END_EDIT` est évaluée — tout le reste
est verrouillé.
