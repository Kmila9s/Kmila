# Memoria ROM

La **memoria** guarda palabras que puedes volver a leer por **dirección**. Una ROM tiene contenido fijo; cada dirección selecciona una palabra, que aparece en la salida.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/25-memory.html" title="Figura interactiva" loading="lazy"></iframe></div>

Los émbolos de dirección eligen una celda; su palabra guardada enciende las lámparas de salida, y la tabla muestra cada dirección → dato.

En VHDL:

```vhdl
type rom_t is array(0 to 3) of unsigned(2 downto 0);
constant ROM : rom_t := ("101","011","110","001");
...
data <= ROM(to_integer(addr));  -- addressed read
```

Leer es puro direccionamiento — una ROM es literalmente un MUX N-a-1 por cada bit de salida. La RAM añade un camino de escritura; la block RAM (BRAM) es memoria dedicada dentro de la FPGA.
