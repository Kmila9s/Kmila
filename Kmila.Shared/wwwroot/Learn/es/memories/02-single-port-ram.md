# Ejemplo: RAM de un puerto con lectura después de escritura

La memoria de lectura/escritura es la otra mitad de "inferir BRAM". Funciona el mismo patrón `type…array…of`, pero ahora el arreglo es una **signal** y escribes en unos flancos y lees en otros.

## La plantilla

```vhdl
type mem_t is array(0 to 31) of std_logic_vector(7 downto 0);
signal mem : mem_t := (others => (others => '0'));
...
process(clk)
begin
  if rising_edge(clk) then
    if we = '1' then
      mem(to_integer(unsigned(addr))) <= din;
      dout <= din;                             -- write-first
    else
      dout <= mem(to_integer(unsigned(addr)));
    end if;
  end if;
end process;
```

## Las tres políticas de lectura

Toda RAM síncrona tiene que decidir qué hace `dout` **durante una escritura**. Hay tres convenciones:

- **Write-first (arriba):** en una escritura, `dout` muestra el dato recién escrito. Semántica más limpia — leer y escribir a la misma dirección en el mismo ciclo te da el valor fresco.
- **Read-first:** en escritura, `dout` muestra el contenido **antiguo**. Algunos registros de CPU lo usan.
- **No-change:** en escritura, `dout` no cambia. Diseños de bajo consumo.

Las tres mapean a block RAM en cualquier FPGA moderno, pero el sintetizador elige el modo según la forma exacta del proceso. La plantilla write-first es la más portable — en la duda, escríbela así.

## Qué hace el ejemplo

Los primeros 30 µs la simulación escribe tres direcciones (`0`, `1`, `2`) con tres patrones distintos. Luego `we` baja, y `addr` vuelve a 0 en el µs 40. La pestaña de formas de onda muestra `dout` siguiendo a `din` durante las escrituras, y luego devolviendo el dato guardado al leer.

## Tu turno

- **Conviértela en dual-port real.** Dos conjuntos completos de `addr`/`din`/`dout`/`we`, ambos manejados por `clk`. Es el patrón que usan los FIFOs y DPRAM dual-clock de todo fabricante.
- **Agrega un registro de salida.** Block RAM en Xilinx soporta un segundo registro de pipeline tras la memoria. Añade `dout_reg <= dout;` en un segundo bloque con reloj y mira qué cambia (pista: un ciclo más de latencia pero mucho mejor cierre de timing).
- **Cambia el contenido inicial.** Preinicializa entradas cambiando el `signal mem`. No todos los sintetizadores respetan inicializadores de BRAM en VHDL portable — revisa tu target antes de confiar en ello en producción.
