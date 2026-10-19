# Ejemplo: flip-flop D con reset asíncrono

El flip-flop D (DFF) es la celda de memoria más común del diseño digital — la estrella de los "elementos de memoria tipo Flip-Flop" de la Unidad IV. Un FPGA moderno tiene decenas o cientos de miles. Cada registro, contador y etapa de pipeline está hecho de DFFs.

## Flip-flop vs. latch (la distinción que hay que clavar)

Ambos almacenan un bit, pero escuchan distinto:

- Un **latch** es *sensible a nivel*: mientras su enable está en alto, la salida sigue a la entrada (es "transparente"); solo retiene cuando el enable está en bajo.
- Un **flip-flop** es *sensible a flanco*: muestrea la entrada en un instante — el flanco de reloj — y retiene por el resto del ciclo.

Esta lección construye un flip-flop. El siguiente ejercicio de arreglar muestra qué pasa cuando construyes un latch *por accidente*. Ten presente la diferencia: disparado por flanco = flip-flop, disparado por nivel = latch.

## Qué hace

En cada flanco de subida de `clk`, el valor en `d` se copia a `q`. Entre flancos, `q` mantiene su valor aunque `d` cambie. Cuando `reset` está en alto, `q` se fuerza a `'0'` de inmediato — este es el reset **asíncrono**.

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

- **Lista de sensibilidad `(clk, reset)`** — el proceso despierta con cualquiera de los dos. Listar `reset` es lo que hace el reset asíncrono: actúa en el momento en que `reset` sube, no en el siguiente flanco de reloj.
- **`if reset = '1'`** — el reset tiene prioridad. Si el pin de reset está en alto, no nos importa qué hagan el reloj o `d`.
- **`elsif rising_edge(clk)`** — solo cuando el reloj pasa de 0 a 1 se muestrea `d` hacia `q`.

## Setup y hold: por qué los flancos necesitan margen

Un flip-flop solo puede capturar `d` de forma confiable si `d` está estable durante una pequeña ventana **antes** del flanco (tiempo de setup) y **después** de él (tiempo de hold). Viola esa ventana y la salida puede volverse *metaestable* — oscilando entre `'0'` y `'1'` antes de estabilizarse de forma impredecible. No modelas estos tiempos en este ejemplo, pero son la razón por la que los diseños reales registran las entradas asíncronas a través de dos flip-flops (un "sincronizador") antes de usarlas. Bueno saber el nombre desde ahora; lo volverás a encontrar.

## Recorrido por la forma de onda

El ejemplo mantiene `reset = '1'` los primeros 5 µs, luego lo libera. `d` alterna en 10, 30 y 50 µs. Cambia a la pestaña de formas de onda tras pulsar Ejecutar y observa:

- Antes de los 5 µs, `q = '0'` (retenido por el reset).
- Tras liberar el reset, `q` solo cambia en los flancos de subida del reloj — el valor de `d` *en el flanco* queda capturado.
- Los cambios de `d` entre flancos de reloj no tienen efecto sobre `q`.

## Reset síncrono vs. asíncrono

Hay dos escuelas, y la elección tiene consecuencias reales:

**Reset asíncrono** (`if reset = '1'` fuera del bloque del flanco, como arriba):
- Pros: resetea aunque el reloj no esté corriendo; maneja limpio el encendido.
- Contras: la *liberación* del reset no está alineada al reloj, así que distintos flip-flops pueden salir del reset en flancos distintos — un riesgo de metaestabilidad — y puede limitar la frecuencia máxima de reloj.

**Reset síncrono** (`if rising_edge(clk) then if reset = '1' then …`):
- Pros: se comporta como cualquier otra entrada síncrona; se mapea directo al reset integrado del flip-flop y se lleva bien con el cierre de timing.
- Contras: necesita el reloj corriendo para surtir efecto.

Para *aprender*, el reset asíncrono es más fácil de razonar, por eso este ejemplo lo usa. Para *trabajo de producción en FPGA*, la guía de los fabricantes (por ejemplo el white paper "Get Smart About Reset" de AMD/Xilinx y las guías de metodología posteriores) generalmente favorece los resets **síncronos**, y algo de lógica de libre corrido no necesita reset en absoluto. Sigue la recomendación de tu dispositivo objetivo.

## Los otros flip-flops

El curso también cubre los flip-flops SR, JK y T. Todos son un flip-flop D con un poco de lógica combinacional al frente que calcula el siguiente estado:

- **T (toggle):** `d = q xor t` → retiene cuando `t=0`, cambia cuando `t=1`.
- **JK:** `d = (j and not q) or (not k and q)`.
- **SR:** set/reset, con el caso `s=r=1` definido aparte.

Así que una vez que puedes escribir un flip-flop D limpio, puedes construir cualquiera de ellos.

## Tu turno

- Agrega una entrada **enable**: `q` debe actualizarse en el flanco de subida solo cuando `enable = '1'`.
- Haz que **registre un bus de 4 bits**: cambia `d` y `q` a `std_logic_vector(3 downto 0)` y el valor de reset a `(others => '0')`.
- Agrega una salida `q_not` que siempre sea `not q`. Bonus: cablea `q_not` de vuelta a `d` y míralo convertirse en un reloj **dividido entre dos** — un flip-flop T disfrazado.
- Convierte el reset a **síncrono** y compara las formas de onda en la liberación del reset.

## Qué recordar

- Un flip-flop D muestrea `d` en el flanco de reloj y retiene entre flancos — disparado por flanco, a diferencia de un latch sensible a nivel.
- `process(clk, reset)` con el reset probado *antes* de `rising_edge` da un reset asíncrono; ponlo *dentro* del flanco para uno síncrono.
- Respeta setup/hold; sincroniza las entradas asíncronas para evitar la metaestabilidad.
- Los flip-flops SR, JK y T son solo un flip-flop D más lógica de siguiente estado.
