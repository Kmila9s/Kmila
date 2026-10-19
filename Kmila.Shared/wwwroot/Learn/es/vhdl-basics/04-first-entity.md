# Ejemplo: tu primera entidad (MUX 2 a 1)

Un **multiplexor** es el dispositivo combinacional más simple que incluye una "decisión". La versión 2 a 1 escoge entre dos entradas según un bit de `sel`:

```
  sel = 0  →  y = a
  sel = 1  →  y = b
```

En VHDL es una sola línea:

```vhdl
y <= a when sel = '0' else b;
```

## El archivo completo

Para que veas las cuatro secciones de las lecciones anteriores en un mismo lugar:

```vhdl
library IEEE;
use IEEE.STD_LOGIC_1164.ALL;

entity mux21 is
  port (
    a   : in  std_logic;
    b   : in  std_logic;
    sel : in  std_logic;
    y   : out std_logic
  );
end entity mux21;

architecture rtl of mux21 is
begin
  y <= a when sel = '0' else b;
end architecture rtl;
```

## Qué hace el ejemplo

Al abrirlo obtienes un proyecto llamado **Tu primera entidad** con:

- `mux21.vhd` (el diseño de arriba)
- Una configuración de simulación acompañante que estimula `sel`, `a` y `b` durante 40 microsegundos

Al pulsar Ejecutar, la pestaña de formas de onda muestra cuatro filas — las tres entradas y la salida. Observa cómo `y` alterna entre `a` y `b` cada vez que `sel` cambia.

## Lo que hay que notar

La asignación `y <= a when sel = '0' else b;` es una **sentencia concurrente**: vive fuera de cualquier proceso y describe un cableado permanente. No hay "¿cuándo se ejecuta esta línea?" — simplemente *es*. Si `a`, `b` o `sel` cambian, `y` se vuelve a estabilizar tras un retraso de propagación.

La condición dentro de un `when/else` puede ser cualquier expresión booleana, y puedes encadenarlas:

```vhdl
y <= a   when sel = '0' else
     b   when enable = '1' else
     'Z';
```

Cadenas así se sintetizan como un multiplexor con **prioridad** (la condición de más arriba gana los empates).

## La teoría por debajo: la expansión de Shannon

Esa sola línea no es solo sintaxis cómoda — es la forma en hardware de un teorema del álgebra de Boole que el curso llama el **teorema de expansión de Shannon**. Cualquier función de una variable `s` se puede partir como:

```
f(s, …) = (s' · f(0, …)) + (s · f(1, …))
```

Un mux 2 a 1 calcula exactamente `y = sel'·a + sel·b`. En otras palabras, un multiplexor es la realización física de la expansión de Shannon alrededor del bit de selección — por eso las funciones más grandes se construyen tan seguido apilando muxes. Guárdatelo en el bolsillo; es la idea detrás de la unidad de multiplexor/demultiplexor más adelante en el curso.

## Tu turno: extiéndelo

Cuando ya hayas visto funcionar el ejemplo:

1. Cambia la entidad a un **MUX 4 a 1** — `sel` se vuelve un `std_logic_vector` de 2 bits, y eliges entre `a`, `b`, `c`, `d`. (Una sentencia `with sel select` se lee muy limpia aquí — la conocerás en el módulo combinacional.)
2. Agrega un quinto puerto `output_enable`. Cuando valga `'0'`, manda `y <= 'Z'` (alta impedancia) sin importar `sel`.
3. Construye un **demultiplexor** — la imagen en espejo. Una entrada, `sel` y varias salidas; la entrada se enruta a exactamente una salida mientras las demás se quedan en `'0'`.

El editor de Kmila resalta las keywords de VHDL y conoce `std_logic` — pasa el ratón por `when` para ver la documentación incorporada.

## Qué recordar

- Una **entidad** nombra la interfaz; la **arquitectura** da el comportamiento. El mux es un ejemplo completo y mínimo de ambos.
- `y <= a when sel = '0' else b;` es una asignación condicional concurrente — cableado permanente, no una línea que "corre".
- Un multiplexor es la expansión de Shannon en silicio: `y = sel'·a + sel·b`. Esa sola idea escala a todos los muxes más grandes que vayas a construir.
