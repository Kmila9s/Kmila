## FSM receptor UART

Un controlador UART simplificado. La máquina de 4 estados sigue el ciclo:

| Estado | Código | Significado            |
|:------:|:------:|:-----------------------|
| Idle   | `00`   | Espera `rx = '0'`      |
| Start  | `01`   | Captura bit de start   |
| Data   | `10`   | Muestrea bit de datos  |
| Stop   | `11`   | Captura bit de stop    |

### Especificación

- Desde idle, avanza a start sólo si `rx = '0'`.
- Desde start/data/stop avanza en cada flanco.
- Stop vuelve a idle.
- Reset → idle.

### Pista

`STD_LOGIC_VECTOR(1 downto 0)` y un `case` es la codificación más simple.
