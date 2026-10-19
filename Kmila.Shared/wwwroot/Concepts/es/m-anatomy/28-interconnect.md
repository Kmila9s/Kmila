# Interconexión y E/S

La **interconexión** es el cableado programable entre celdas: **canales de ruteo**, **cajas de conmutación** en los cruces que los conectan, y **bloques de E/S (IOB)** en el borde que llegan a los pines.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/28-interconnect.html" title="Figura interactiva" loading="lazy"></iframe></div>

Una señal se rutea desde un bloque de E/S, por los canales y las cajas de conmutación, hasta un CLB — el camino encendido. Programar las cajas es lo que conecta tu diseño.

La mayor parte del área y del retardo de una FPGA es en realidad **ruteo**, no lógica. El place-and-route decide qué celdas usar y cómo cablearlas — el paso lento e ingenioso tras la síntesis.
