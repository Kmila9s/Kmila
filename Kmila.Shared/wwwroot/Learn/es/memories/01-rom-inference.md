# Ejemplo: ROM síncrona inferida

Toda FPGA tiene **block RAM** (BRAM) — bloques de memoria dedicados que el sintetizador usa cuando reconoce un patrón de ROM o RAM en tu código. No instancias primitivas del fabricante; escribes VHDL sencillo en una forma que la herramienta sabe cómo mapear. "Inferencia" es el nombre de ese reconocimiento: la herramienta lee tu arreglo-y-proceso e *infiere* que te referías a una memoria.

Este ejemplo es el patrón de ROM portable de libro de texto. Todo sintetizador serio (Xilinx/AMD Vivado, Intel/Altera Quartus, Lattice Radiant, Yosys de código abierto) lo reconocerá y asignará BRAM automáticamente.

## El patrón

```vhdl
type rom_t is array(0 to 15) of std_logic_vector(7 downto 0);
constant ROM : rom_t := (
  x"00", x"01", x"04", x"09", x"10", x"19", x"24", x"31",
  x"40", x"51", x"64", x"79", x"90", x"A9", x"C4", x"E1"
);

process(clk)
begin
  if rising_edge(clk) then
    data <= ROM(to_integer(unsigned(addr)));
  end if;
end process;
```

Tres cosas hacen que esto sea una ROM amigable con la BRAM:

1. **El almacenamiento es una constante** (`constant ROM : rom_t := (...)`). Los datos están fijos en tiempo de síntesis.
2. **La lectura es síncrona.** Una lectura de ROM combinacional solo puede mapearse a LUTs — eso está bien para una ROM chiquita de 16 entradas, pero cualquier cosa más grande necesita el patrón con reloj para que el sintetizador asigne BRAM.
3. **`addr` se usa puramente como índice.** Sin lógica combinacional costosa entre `addr` y el acceso a memoria.

## Combinacional vs. síncrona, y la latencia de lectura

La lectura con reloj es la clave de todo. Escribe la lectura como una asignación concurrente simple —

```vhdl
data <= ROM(to_integer(unsigned(addr)));   -- combinacional: ROM distribuida (LUTs)
```

— y la herramienta construye la ROM con LUTs (memoria "distribuida"). Eso da cero latencia de lectura, pero cuesta lógica general, y no escala: unos cuantos kilobytes de ROM en LUTs ahogarán un dispositivo pequeño. Registra la lectura (el `process` de arriba) y la herramienta puede meter toda la tabla en una block RAM, que es mucho más densa.

El precio de la lectura registrada es **un ciclo de latencia**: presentas `addr` en un flanco, y `data` aparece en el *siguiente* flanco. Eso no es un bug — es la característica que define a una memoria síncrona, y el registro de salida interno de la BRAM es lo que hace que el diseño cumpla el timing a altas frecuencias de reloj. Muchas FPGA ofrecen un segundo registro de salida opcional para un timing aún mejor, a costa de un segundo ciclo de latencia.

## El ancho de la dirección debe coincidir con la profundidad

`to_integer(unsigned(addr))` nunca debe indexar más allá del arreglo. Para una ROM de 16 entradas, `addr` debería ser exactamente de 4 bits; una `addr` más ancha puede producir un índice fuera de rango que falla en simulación y se comporta de forma impredecible tras la síntesis. Mantén `2**addr'length = DEPTH`, o acota/protege el índice. Amarra juntas la profundidad, el ancho y el tamaño de la dirección y todo queda consistente.

## ¿Por qué estos valores?

La ROM de ejemplo guarda `n²` para `n = 0..15`. Útil para una unidad de elevar al cuadrado por tabla de consulta — más rápida que un multiplicador completo cuando solo necesitas operandos pequeños. También podrías guardar:

- Una tabla de onda senoidal (para generadores de señal DDS)
- Polinomios CRC
- Tablas de consulta de protocolo
- Fuentes de caracteres

Una ROM en realidad no es más que una tabla de verdad congelada en silicio: cualquier función combinacional de los bits de dirección se puede precalcular y almacenar, intercambiando lógica por memoria.

## Qué hace el ejemplo

La simulación pasa `addr` por 5 valores específicos: 0, 1, 4, 8, 15. Tras el flanco de reloj, `data` muestra la entrada de ROM correspondiente, un ciclo después (esa es la latencia de lectura de una ROM síncrona — pides la dirección, recibes el dato en el **siguiente** reloj).

## Te toca

- **Hazla parametrizable.** Reemplaza el tamaño fijo de 16 entradas con `generic (DEPTH : integer := 16; WIDTH : integer := 8)`, y dimensiona `addr` como `std_logic_vector(log2(DEPTH)-1 downto 0)`. El resto del código debería seguir funcionando.
- **Lee el contenido de un archivo.** Los proyectos reales cargan el contenido de la ROM vía una función de inicialización o un paquete separado de constantes. Intenta que el VHDL lea un archivo de texto de valores hex (`std.textio` + `hread`) en una función impura que devuelva el arreglo — la mayoría de las herramientas la ejecutan en la elaboración y hornean el resultado en la BRAM.
- **Agrega un segundo puerto de lectura.** Para cosas como la memoria de instrucciones/datos en un CPU monociclo. Verás este patrón en el proyecto de CPU más adelante en el plan de estudios.

## Relación con la asignatura

Esto es la Unidad IV, *Memorias*: la familia ROM (ROM, PROM, EPROM, EEPROM, Flash) y, específicamente, el *manejo de arreglos y diseño de memorias con lenguaje de descripción de hardware*. La constante `type … array … of` es exactamente el "manejo de arreglos" que pide la unidad; el ejercicio de inicialización desde archivo se adelanta al trabajo de la Unidad V con archivos, funciones y paquetes.

## Qué recordar

- Inferencia significa escribir la memoria en una forma que la herramienta reconoce — sin necesidad de primitivas del fabricante.
- Un arreglo **constante** leído dentro de un proceso con reloj es el patrón portable de ROM síncrona; se mapea a block RAM.
- Lectura con reloj = un ciclo de latencia y BRAM densa; lectura combinacional = cero latencia pero hambrienta de LUTs y no escalable.
- Haz coincidir el ancho de la dirección con la profundidad para que el índice nunca se salga del final del arreglo.
