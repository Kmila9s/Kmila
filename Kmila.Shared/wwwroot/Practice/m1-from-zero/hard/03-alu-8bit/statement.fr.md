## UAL 8 bits

Implémente une UAL combinatoire 8 bits. `op` (2 bits) choisit l'opération :

| `op` | Opération | Sortie `y`    |
|:----:|:----------|:--------------|
| `00` | ADD       | `a + b`       |
| `01` | SUB       | `a - b`       |
| `10` | AND       | `a and b`     |
| `11` | OR        | `a or b`      |

### Spécification

- Combinatoire uniquement — pas d'horloge.
- Utilise `IEEE.NUMERIC_STD` ; traite les opérandes en `unsigned`.
- Reconvertis en `STD_LOGIC_VECTOR` avec `std_logic_vector(...)`.

### Indice

Un `case op is ... when others =>` dans un process donne un sélecteur propre et synthétisable.
