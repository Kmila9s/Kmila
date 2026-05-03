## ALU de 8 bits

Implementa una ALU combinacional de 8 bits. `op` (2 bits) selecciona la operación:

| `op` | Operación | Salida `y`     |
|:----:|:----------|:---------------|
| `00` | ADD       | `a + b`        |
| `01` | SUB       | `a - b`        |
| `10` | AND       | `a and b`      |
| `11` | OR        | `a or b`       |

### Especificación

- Sólo combinacional — sin reloj, sin flip-flops.
- Usa `IEEE.NUMERIC_STD` y trata los operandos como `unsigned`.
- Devuelve el resultado a `STD_LOGIC_VECTOR` con `std_logic_vector(...)`.

### Pista

`case op is ... when others =>` dentro de un proceso da un selector limpio y sintetizable.
