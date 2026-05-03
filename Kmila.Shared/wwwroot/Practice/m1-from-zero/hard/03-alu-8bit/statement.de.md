## 8-Bit-ALU

Implementiere eine kombinatorische 8-Bit-ALU. `op` (2 Bit) wählt die Operation:

| `op` | Operation | Ausgabe `y`   |
|:----:|:----------|:--------------|
| `00` | ADD       | `a + b`       |
| `01` | SUB       | `a - b`       |
| `10` | AND       | `a and b`     |
| `11` | OR        | `a or b`      |

### Spezifikation

- Nur kombinatorisch — kein Takt, keine Flip-Flops.
- Verwende `IEEE.NUMERIC_STD` und betrachte die Operanden als `unsigned`.
- Wandele für die Ausgabe via `std_logic_vector(...)` zurück.

### Hinweis

Ein `case op is ... when others =>` in einem Prozess ergibt einen sauberen, synthetisierbaren Selektor.
