# Celda lógica (slice)

Una **celda lógica** (o *slice*) es el ladrillo que se repite en la FPGA: una **LUT** + un **flip-flop** + un **MUX** de salida (más una cadena de acarreo). Da una salida combinacional o registrada.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/27-slice.html" title="Figura interactiva" loading="lazy"></iframe></div>

Las entradas alimentan la LUT; su resultado sale directo (**combinacional**) o pasa por el flip-flop (**registrado**); el MUX elige. El reloj mueve el flip-flop; una cadena de acarreo cruza el tile.

Junta `y <= a and b` y un registro con `rising_edge(clk)` y caben en un slice — la LUT para la lógica, el flip-flop para el estado. Los slices modernos tienen varias LUT y flip-flops.
