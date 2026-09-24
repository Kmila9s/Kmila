# Ejemplo: ROM síncrona inferida

Todo FPGA tiene **block RAM** (BRAM) — bloques de memoria dedicados que el sintetizador usa cuando reconoce un patrón de ROM o RAM en tu código. No instancias primitivas del fabricante; escribes VHDL en una forma que la herramienta sabe mapear.

Este ejemplo es el patrón de ROM portable de libro de texto. Todo sintetizador serio (Xilinx Vivado, Intel Quartus, Lattice Radiant, Yosys) lo reconoce y asigna BRAM automáticamente.

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

Tres cosas hacen que esto sea una ROM amigable con BRAM:

1. **El almacenamiento es una constante** (`constant ROM : rom_t := (...)`). Los datos están fijos en tiempo de síntesis.
2. **La lectura es síncrona.** Una ROM combinacional solo puede mapearse a LUTs — está bien para 16 entradas, pero cualquier cosa más grande necesita el patrón con reloj para que se asigne BRAM.
3. **`addr` se usa solo como índice.** Sin lógica combinacional costosa entre `addr` y el acceso.

## ¿Por qué estos valores?

La ROM de ejemplo almacena `n²` para `n = 0..15`. Útil para un cuadrático por lookup — más rápido que un multiplicador completo cuando los operandos son pequeños. También puedes guardar:

- Una tabla de senoides (para DDS)
- Polinomios CRC
- Tablas de protocolo
- Fuentes de caracteres

## Qué hace el ejemplo

La simulación pasa `addr` por 5 valores: 0, 1, 4, 8, 15. Tras el flanco, `data` muestra el valor correspondiente, un ciclo después (latencia de la ROM síncrona — pides la dirección, recibes el dato al **siguiente** reloj).

## Tu turno

- **Parametrízalo.** Reemplaza el tamaño fijo de 16 entradas con `generic (DEPTH : integer := 16; WIDTH : integer := 8)`. El resto debe seguir funcionando.
- **Lee el contenido de un archivo.** Los proyectos reales cargan la ROM vía `init_from_file()` o un paquete separado de constantes. Intenta que el VHDL lea un archivo de hex (`textio` + `hread`) en elaboración.
- **Añade un segundo puerto de lectura.** Para cosas como memoria de instrucciones/datos en un CPU monociclo. Verás este patrón en el proyecto de CPU más adelante.
