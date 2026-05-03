## ALU 4 bits — opération manquante

Une ALU à 4 opérations sélectionnées par `op` :

| `op` | opération |
|:----:|-----------|
| `00` | `a + b`   |
| `01` | `a - b`   |
| `10` | `a and b` |
| `11` | `a xor b` |

### Ce qui cloche

Pour `op = "11"` la sortie est figée à `"0000"` au lieu de calculer `a xor b`. L'addition (`00`), la soustraction (`01`) et le AND (`10`) sont corrects.

### Indice

Regarde la branche `when others` du `case`. Le résultat attendu est un `xor` bit à bit, pas une constante.
