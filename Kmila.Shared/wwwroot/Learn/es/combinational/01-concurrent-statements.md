# Asignaciones concurrentes: `when/else` y `with/select`

La lógica combinacional en VHDL se escribe con sentencias concurrentes — cada línea fuera de un proceso describe una pieza de hardware cableada permanentemente. Existen dos sabores de sintaxis para el mismo trabajo: asignación **condicional** (`when/else`) y **selectiva** (`with/select`). Estas son las "sentencias concurrentes" de la unidad de HDL, y son la columna vertebral de todo circuito combinacional del curso: sumadores, convertidores de código, comparadores, multiplexores, codificadores, decodificadores.

## `when/else` — condicional

Bien cuando tienes una lista priorizada de condiciones, o cuando cada rama revisa una señal *distinta*:

```vhdl
y <= (others => '0') when reset = '1' else
     a               when sel = "00"  else
     b               when sel = "01"  else
     c               when sel = "10"  else
     d;
```

Gana la primera condición que se cumple. El orden importa — compila a un **multiplexor con prioridad**: la condición de más arriba tiene el camino más directo a `y`. Nota que todas las expresiones de valor (`a`, `b`, `c`, `d`, el agregado de puros ceros) deben tener el **mismo tipo y ancho** que `y`, y el `else` final no lleva condición — es el atrapa-todo.

## `with/select` — selectiva

Bien cuando estás ramificando sobre una sola expresión (como lo hace un `switch`/`case` de software):

```vhdl
with sel select
  y <= a   when "00",
       b   when "01",
       c   when "10",
       'Z' when others;
```

Todas las opciones se comparan contra la misma expresión (`sel`), las ramas se separan con **comas** (no con `else`), y `when others` es **obligatorio** para cubrir todos los valores posibles. Este estilo compila a un **multiplexor balanceado** — ninguna rama es privilegiada.

### Por qué `when others` es innegociable

`sel` aquí es un `std_logic_vector(1 downto 0)`. Pensarías que son cuatro casos — `"00"`, `"01"`, `"10"`, `"11"`. Pero cada bit es un `std_logic` de *nueve valores*, así que el compilador cuenta 9² = 81 patrones posibles (`"0X"`, `"ZU"`, …). No puedes enumerarlos, por eso `when others` es necesario para que la sentencia sea total. En síntesis los metavalores se colapsan y obtienes el mux limpio de 4 vías que pretendías.

También puedes agrupar opciones con el operador de barra:

```vhdl
with opcode select
  result <= a + b    when "000" | "001",   -- dos códigos, misma salida
            a - b    when "010",
            (others => '0') when others;
```

## ¿Cuál deberías elegir?

- **Misma señal, muchos patrones** (decodificador, mux, convertidor de código): `with/select` se lee como una tabla de verdad. Úsalo.
- **Señales distintas, lógica con prioridad** (el reset le gana al enable, que le gana a los datos): `when/else`. Encaja natural.
- **Salida de síntesis:** en un FPGA moderno ambos producen más o menos las mismas compuertas, así que elegir la forma más clara a nivel de código fuente casi siempre es lo correcto. Recurre a `when/else` solo cuando de verdad quieras prioridad; si no, `with/select` le señala "sin prioridad" al siguiente lector.

## La trampa del latch (adelanto)

Ambas formas requieren que `y` termine asignada en **todos** los escenarios. Un `when/else` o `with/select` concurrente no puede, de hecho, dejar `y` sin asignar — la sintaxis obliga un `else` / `when others` final. Pero el equivalente **if/case dentro de un proceso** sí puede, y *ahí* es donde el sintetizador infiere un latch para "recordar" el último valor, lo cual casi nunca es lo que quieres. Es la misma trampa del latch que enfrentarás de lleno en el ejercicio de arreglar del módulo secuencial, así que conserva el hábito: asigna la salida en cada camino.

## Un pequeño amarre resuelto: un comparador de 2 bits

El comparador de magnitud del curso sale directo de `when/else`:

```vhdl
-- gt/eq/lt son salidas std_logic individuales
gt <= '1' when unsigned(a) > unsigned(b) else '0';
eq <= '1' when a = b else '0';
lt <= '1' when unsigned(a) < unsigned(b) else '0';
```

Tres sentencias concurrentes independientes, tres comparadores, todos vivos a la vez.

## Dónde aparece después

Usarás `with/select` en el ejemplo del decodificador 3 a 8 justo después de esta lección, y `when/else` a lo largo del trabajo de sumadores, comparadores y muxes. Ambas son pan de todos los días en VHDL.

## Qué recordar

- `when/else` = condicional, priorizada, se permiten señales distintas → mux con prioridad.
- `with/select` = un selector, opciones separadas por comas, `when others` obligatorio → mux balanceado.
- Cada rama de valor debe coincidir con el tipo y ancho del destino.
- Las formas concurrentes no pueden dejar caer la salida, pero las versiones basadas en proceso con `if`/`case` sí — y ahí es donde se cuelan los latches.
