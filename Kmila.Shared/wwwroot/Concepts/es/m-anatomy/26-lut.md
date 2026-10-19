# Tabla de consulta (LUT)

Una **tabla de consulta (LUT)** es cómo una FPGA se convierte en cualquier compuerta: una memoria diminuta con una tabla de verdad. Las entradas forman una **dirección**; la salida es el bit guardado en esa dirección.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/26-lut.html" title="Figura interactiva" loading="lazy"></iframe></div>

Las entradas direccionan las 4 celdas de configuración (la tabla de verdad cargada); la salida es el bit seleccionado. Observa cómo la configuración se **reprograma** — la misma LUT se vuelve AND, luego OR, luego XOR. Eso es lógica reconfigurable.

En VHDL:

```vhdl
y <= a and b;   -- the synthesizer loads this
                -- truth table into ONE LUT
```

Todo tu diseño se mapea a LUT: el sintetizador convierte cada expresión booleana en tablas de verdad y las carga al arrancar desde el bitstream de configuración. Una LUT de N entradas puede ser **cualquier** función de N entradas.
