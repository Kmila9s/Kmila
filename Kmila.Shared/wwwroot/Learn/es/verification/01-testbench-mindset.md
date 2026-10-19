# La mentalidad del banco de pruebas

En cada lección hasta aquí, Kmila ha sido tu banco de pruebas (testbench) — arrastrabas estímulos a la pestaña Puertos, Ejecutar corría el diseño y mirabas la forma de onda. Para aprender está bien. Pero no es así como funciona el diseño digital profesional.

## Qué es un banco de pruebas real

Un **banco de pruebas** es otra entidad VHDL cuyo único trabajo es ejercitar un *diseño bajo prueba* (DUT). No tiene puertos, porque nada lo alimenta; declara el DUT como componente, conecta sus propias señales internas a los puertos del DUT y luego usa un **process** para recorrer secuencias de estímulos en el tiempo.

Esqueleto:

```vhdl
entity tb_counter is
end entity tb_counter;

architecture sim of tb_counter is
    component counter
        port ( clk, reset : in  std_logic;
               q          : out std_logic_vector(3 downto 0) );
    end component;

    signal clk   : std_logic := '0';
    signal reset : std_logic := '1';
    signal q     : std_logic_vector(3 downto 0);
begin
    dut : counter port map (clk, reset, q);

    clk <= not clk after 10 ns;            -- 50 MHz

    process
    begin
        wait for 20 ns;
        reset <= '0';
        wait for 200 ns;
        assert q = "1010"
            report "El contador debería valer 10 a los 200 ns, se obtuvo " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

Tres ideas nuevas:

1. **`wait`** — el tiempo avanza solo cuando un proceso lo dice.
2. **`assert … report … severity`** — la auto-verificación. Convierte una forma de onda de 1000 líneas en un "pasa" o "falla".
3. **Entidad sin puertos** — los bancos de pruebas son auto-contenidos; el simulador los instancia directamente.

## Cómo generar un reloj

Esa sola línea, `clk <= not clk after 10 ns;`, es todo el reloj: se invierte cada 10 ns, dando un periodo de 20 ns (50 MHz). Como `clk` tiene un valor inicial de `'0'`, arranca limpio. Para un reloj con ciclo de trabajo distinto del 50 % o uno que necesites detener después, usa un proceso:

```vhdl
clk_gen : process
begin
    clk <= '0'; wait for 10 ns;
    clk <= '1'; wait for 10 ns;
    if done then wait; end if;   -- congela cuando termina la prueba
end process;
```

Mantén la generación del reloj y los estímulos en procesos **separados**. El reloj solo late; el proceso de estímulos maneja las entradas y revisa las salidas, normalmente sincronizándose con `wait until rising_edge(clk)` para alinearse con el DUT.

## Auto-verificación: assert, severity y valores de referencia

El punto de un banco de pruebas es que se verifica a *sí mismo*. `assert` evalúa una condición y, si es falsa, imprime la cadena de `report` en el `severity` elegido. Los cuatro niveles de severidad son **note**, **warning**, **error** y **failure** — y a la mayoría de los simuladores se les puede decir que se detengan en el momento en que dispara uno de un nivel dado. Así es como conviertes el "se veía bien" en un pasa/falla duro.

Para diseños no triviales, no escribas a mano cada valor esperado. Calcúlalo a partir de un **modelo de referencia** (golden model) — una referencia simple y obviamente correcta (incluso un cálculo estilo software dentro del banco de pruebas) — y compara el DUT contra ella en cada ciclo. Esa comparación contra una referencia es la idea central detrás de un **scoreboard**, y escala desde un contador hasta un CPU.

```vhdl
expected <= std_logic_vector(unsigned(expected) + 1) when rising_edge(clk);
assert q = expected
    report "discrepancia: se obtuvo " & to_string(q) & " se esperaba " & to_string(expected)
    severity error;
```

(`to_string` para `std_logic_vector` es una comodidad de VHDL-2008 — compila el banco de pruebas en modo 2008 para usarla.)

## Terminar la simulación de forma limpia

Una sesión de solo-forma-de-onda corre para siempre. Un banco de pruebas auto-verificable debería **detenerse solo** cuando se agotan los estímulos. En VHDL-2008 la forma limpia es `finish` del paquete `std.env`:

```vhdl
use std.env.all;
...
report "Todas las pruebas pasaron";
finish;          -- termina la simulación; stop la pausaría para inspeccionar
```

`finish` termina la corrida (ideal para uso automatizado/CI); `stop` la pausa para que husmees de forma interactiva. El truco viejo de forzar un `assert … severity failure` para detenerla también funciona en cualquier versión de VHDL, pero reporta una *falla* incluso en una corrida que pasa, lo que confunde a las herramientas de regresión — mejor usa `finish`.

## Estímulos dirigidos vs. aleatorizados

- Las pruebas **dirigidas** pegan en casos específicos elegidos a mano: el comportamiento del reset, el desbordamiento de un contador, una secuencia conocida como engañosa. Excelentes para acorralar un bug y para los casos de aceptación de una especificación.
- Las pruebas **aleatorizadas / de aleatorio-restringido** lanzan muchas entradas legales-pero-no-planeadas al DUT, atrapando rincones que jamás se te ocurriría escribir a mano. Se apoyan en el modelo de referencia, porque no puedes predecir a mano cada resultado.

La mayoría de la verificación real mezcla ambas: pruebas dirigidas para los casos que sabes que importan, aleatorizadas para los que no.

## Por qué importa esto

- **Regresión:** un banco de pruebas que escribiste ayer sigue corriendo hoy. Una forma de onda que miraste ayer ya no existe.
- **Cobertura:** puedes aplicar patrones de estímulo que una sesión interactiva nunca alcanza (entradas aleatorias, casos límite, frecuencias extremas).
- **Automatización:** un sistema de CI corre tu banco de pruebas en cada commit y lee el pasa/falla desde la salida. Las formas de onda no caben en CI.

## Relación con la asignatura

La verificación es la columna vertebral silenciosa de toda la secuencia de laboratorio de *Diseño de Sistemas Digitales*. Cada práctica — registros, sistemas secuenciales, contadores, memorias, máquinas ASM — es en realidad "constrúyelo en HDL, luego demuestra que funciona". Un banco de pruebas auto-verificable es cómo conviertes cada *reporte de práctica* de "aquí hay una captura de una forma de onda" a "aquí hay una prueba que pasa", que es exactamente el rigor que busca la evaluación basada en problemas de la unidad.

## Límites actuales de Kmila

El simulador de Kmila corre bancos de pruebas sin problemas si los escribes como entidades normales — lo único que falta hoy es la extracción automática de pasa/falla desde las sentencias `assert` hacia el log de Output. Eso está en la hoja de ruta.

## Qué recordar

- Un banco de pruebas es una entidad sin puertos que instancia el DUT, genera un reloj, maneja los estímulos y se **verifica a sí misma** con `assert`.
- Mantén el reloj y los estímulos en procesos separados; sincroniza las verificaciones con `wait until rising_edge(clk)`.
- Compara contra un modelo de referencia (un scoreboard) en lugar de escribir a mano cada valor esperado.
- Termina las corridas automatizadas con `std.env.finish`; usa los niveles de severidad para que el simulador pueda detenerse ante fallas reales.
- Mezcla pruebas dirigidas (casos conocidos) con aleatorizadas (los rincones que nunca adivinarías).
