## Decodificador BCD a 7 segmentos — un dígito en blanco

Se pedía un decodificador estándar BCD a 7 segmentos (segmentos activos en alto, orden `a b c d e f g`) para los dígitos 0…9.

### Patrones de segmentos

| dígito | seg (`abcdefg`) |
|:------:|:---------------:|
|   0    | `1111110` |
|   1    | `0110000` |
|   7    | `1110000` |
|   8    | `1111111` |
|   9    | `1111011` |

### Qué falla

Casi todos los dígitos se decodifican bien, pero **el dígito 7** saca `"0000000"` (pantalla apagada) en vez del esperado `"1110000"` (segmentos `a`, `b`, `c` encendidos).

### Pista

Revisa la línea `when "0111"` del selected assignment.
