## FSM de 3 estados — estado inalcanzable

Se pedía una máquina con estados `S0` (`"00"`), `S1` (`"01"`), `S2` (`"10"`).

### Especificación

* Reset asíncrono activo en alto → `S0`.
* En `rising_edge(clk)`:
  * `S0 -> S1` siempre.
  * `S1 -> S2` cuando `x = '1'`, en otro caso se queda en `S1`.
  * `S2 -> S0` siempre.
* `state` saca la codificación del estado actual.

### Qué falla

La transición `S1 -> S2` nunca se dispara. Por mucho que mantengas `x` en alto, la FSM se queda en `S1` para siempre: `S2` es **inalcanzable**. El bug está en la condición que protege esa transición.

### Pista

Una señal no puede valer dos cosas distintas a la vez. Mira la expresión booleana: es una contradicción.
