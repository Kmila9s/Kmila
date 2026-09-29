# Ejemplo: flip-flop D con reset asíncrono

El flip-flop D (DFF) es la celda de memoria más común del diseño digital. Un FPGA moderno tiene decenas o cientos de miles. Cada registro, contador y etapa de pipeline está hecho de DFFs.

## Qué hace

En cada flanco de subida de `clk`, el valor en `d` se copia a `q`. Entre flancos, `q` mantiene su valor aunque `d` cambie.

Cuando `reset` es alto, `q` se fuerza a `'0'` de inmediato — este es el reset **asíncrono**.

## El código

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Línea por línea:

- **Lista `(clk, reset)`** — el proceso despierta con cualquiera de los dos. Eso hace el reset asíncrono.
- **`if reset = '1'`** — el reset tiene prioridad. Si está alto, no importa lo que hagan el reloj o `d`.
- **`elsif rising_edge(clk)`** — solo cuando el reloj pasa de 0 a 1 se muestrea `d`.

## Recorrido por la forma de onda

El ejemplo mantiene `reset = '1'` los primeros 5 µs, luego lo libera. `d` alterna en 10, 30 y 50 µs. Cambia a la pestaña de formas de onda tras pulsar Ejecutar y observa:

- Antes de los 5 µs, `q = '0'` (retenido por el reset).
- Tras liberar el reset, `q` solo cambia en flancos de reloj — el valor de `d` en el flanco queda capturado.
- Los cambios de `d` entre flancos no tienen efecto.

## Reset síncrono vs asíncrono

Dos escuelas:

**Reset asíncrono** (`if reset = '1'` fuera del bloque de flanco, como arriba):
- Pros: resetea aunque no haya reloj. Limpia el power-on.
- Contras: más difícil cerrar timing a frecuencias muy altas; la recomendación de Xilinx para 7-series en adelante es evitarlo.

**Reset síncrono** (`if rising_edge(clk) then if reset = '1'…`):
- Pros: se comporta como cualquier otra entrada en el flanco; se lleva mejor con el sintetizador.
- Contras: necesita el reloj en marcha para resetear.

Para aprender, el asíncrono es más fácil. Para producción en FPGA, sigue la guía del fabricante.

## Tu turno

- Agrega una entrada **enable**. `q` solo debe actualizarse en el flanco cuando `enable = '1'`.
- Haz que **registre un bus de 4 bits** en lugar de un solo bit. Cambia `d` y `q` a `std_logic_vector(3 downto 0)` y ajusta el valor de reset a `(others => '0')`.
- Agrega `q_not` siempre como inverso de `q`. Bonus: cablea `q_not` de vuelta a `d` y míralo convertirse en un divisor de frecuencia por dos.
