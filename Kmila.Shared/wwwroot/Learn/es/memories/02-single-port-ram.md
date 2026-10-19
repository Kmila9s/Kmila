# Ejemplo: RAM de un puerto con lectura write-first

La memoria de lectura/escritura es la otra mitad de "inferir BRAM". Funciona el mismo patrón `type…array…of`, pero ahora el arreglo es una **signal**, y escribes en algunos flancos de reloj y lees en los otros.

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
      dout <= din;                             -- lectura write-first
    else
      dout <= mem(to_integer(unsigned(addr)));
    end if;
  end if;
end process;
```

¿Por qué una **signal** y no una `variable` o una constante? El arreglo tiene que ser un estado compartido que persiste a través de los flancos de reloj y se *reescribe* en tiempo de ejecución — eso es exactamente lo que es una señal. Una constante (como en el ejemplo de la ROM) no se puede escribir; una variable de proceso no sería visible fuera del proceso. Y como cada acceso está dentro de `rising_edge(clk)`, tanto la escritura como la lectura son **síncronas** — que es lo que permite que la herramienta mapee todo a una block RAM.

## Las tres políticas de lectura

Toda RAM síncrona tiene que decidir qué hace `dout` **durante una escritura** a la dirección que se está leyendo. Este comportamiento de "lectura-durante-escritura" tiene tres convenciones estándar, y la forma de tu proceso elige cuál obtienes:

- **Write-first** (también llamada read-through, mostrada arriba): en una escritura, `dout` muestra el dato recién escrito. La semántica más limpia — una lectura y una escritura a la misma dirección en el mismo ciclo te dan el valor fresco.
- **Read-first:** en una escritura, `dout` muestra el contenido **antiguo** de la celda. Los bancos de registros de algunos CPU usan esto.
- **No-change:** en una escritura, `dout` no se actualiza en absoluto — mantiene su último valor. Común en diseños de bajo consumo, ya que el registro de salida no se conmuta en las escrituras.

La diferencia es puramente el *orden* de las dos asignaciones en el proceso:

```vhdl
-- READ-FIRST: captura el dato antiguo, luego sobrescribe
if we = '1' then
  dout <= mem(to_integer(unsigned(addr)));   -- sale el valor antiguo
  mem(to_integer(unsigned(addr))) <= din;    -- luego escribe
end if;

-- NO-CHANGE: solo actualiza dout cuando NO se escribe
if we = '1' then
  mem(to_integer(unsigned(addr))) <= din;    -- dout queda intacto
else
  dout <= mem(to_integer(unsigned(addr)));
end if;
```

Las tres se mapean a block RAM en cualquier FPGA moderna, y cada puerto de una RAM de doble puerto verdadera puede incluso usar un modo distinto. Pero el sintetizador elige el modo a partir de la forma *exacta* de tu proceso, así que haz coincidir la plantilla con el comportamiento que de verdad quieres. La forma write-first es la más ampliamente portable — si tienes dudas, escríbela.

## Qué hace el ejemplo

Durante los primeros 30 µs la simulación escribe tres direcciones (`0`, `1`, `2`) con tres patrones de byte distintos. Luego `we` baja, y `addr` se regresa a 0 a los 40 µs. La pestaña de Formas de onda muestra `dout` siguiendo a `din` durante las escrituras, y luego produciendo de vuelta el valor almacenado cuando lees — un ciclo después de presentar la dirección, porque la lectura está registrada.

## Cuídate de

- **El ancho de la dirección vs. la profundidad.** Igual que en la ROM, `to_integer(unsigned(addr))` debe mantenerse dentro de `0 .. 31`. Dimensiona `addr` a `log2(DEPTH)` bits para que no pueda sobre-indexar.
- **No leas de forma combinacional.** Una lectura combinacional (`dout <= mem(...)` fuera del proceso con reloj) suele bloquear la inferencia de BRAM y forzar el arreglo a flip-flops/LUTs — está bien para una docena de bytes, desastroso para kilobytes.
- **Los inicializadores no están garantizados.** El valor inicial `signal mem := (others => ...)` funciona en simulación y en muchas FPGA (donde la BRAM tiene un estado de arranque), pero no en todos los targets y nunca en SRAM/DRAM externa real, que arranca indefinida. No confíes en ello en producción sin revisar tu dispositivo.

## Te toca

- **Súbela a una RAM de doble puerto verdadera.** Dos conjuntos completos de `addr`/`din`/`dout`/`we`, ambos manejados por `clk`. Es el patrón que usan por debajo los FIFO y las DPRAM de doble reloj de todo fabricante.
- **Agrega un registro de salida.** La block RAM soporta un segundo registro de pipeline después del arreglo de memoria. Agrega `dout_reg <= dout;` en un segundo bloque con reloj y observa qué cambia en la forma de onda (pista: un ciclo más de latencia, pero un cierre de timing mucho mejor).
- **Construye un FIFO encima.** Envuelve la RAM con un apuntador de escritura, un apuntador de lectura y banderas `full`/`empty`. Esa es la forma estándar en que se construye un FIFO síncrono, y la unidad lo pide por su nombre.

## Relación con la asignatura

Esto es el núcleo de la Unidad IV, *Memorias*: RAM / SRAM / DRAM y el diseño de memorias RAM, ROM y **FIFO** en HDL mediante el manejo de arreglos. Write-first/read-first/no-change es la cara en HDL del comportamiento de lectura-durante-escritura que exhiben las celdas de SRAM reales; el ejercicio del FIFO de arriba es exactamente los *tipos especiales de memorias* que la unidad enlista.

## Qué recordar

- Memoria de lectura/escritura = el mismo patrón de arreglo que la ROM, pero una **signal** que escribes en tiempo de ejecución.
- Mantén tanto la lectura como la escritura dentro de `rising_edge(clk)` para que la herramienta infiera block RAM.
- Write-first / read-first / no-change se eligen según el orden de las sentencias en el proceso — elige la que tu diseño necesita; write-first es el valor por defecto seguro.
- Vigila el ancho de la dirección, evita las lecturas combinacionales y no confíes en los inicializadores de BRAM en todos los targets.
