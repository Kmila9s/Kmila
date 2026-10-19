# FPGA — el chip completo

Una **FPGA** es un mar de estas piezas en un chip: una malla de **CLB** (celdas lógicas), columnas de **block RAM** y **DSP**, un **anillo de E/S**, un **árbol de reloj** y el ruteo que los une.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/29-fpga.html" title="Figura interactiva" loading="lazy"></iframe></div>

Los caminos encendidos son dos señales configuradas ruteándose por la malla. Todo lo de esta ruta vive aquí: compuertas y sumadores se vuelven LUT, los registros son los flip-flops, y el router los cablea por los canales.

Esa es toda la ruta en un chip: el **reloj** marca el ritmo, **compuertas** y **sumadores** se vuelven **LUT** dentro de **celdas lógicas**, los **flip-flops** guardan el estado, **memoria** y **DSP** van en columnas, y la **interconexión** lo cablea todo — reprogramable, porque todo son tablas de consulta e interruptores cargados desde un bitstream.
