# ¿Por qué segmentar un CPU?

Tu CPU monociclo de 8 bits del track anterior funciona — pero está limitado en una dimensión que importa enormemente en hardware real: la **frecuencia de reloj**.

## El techo del monociclo

En un diseño monociclo, el período de reloj tiene que ser lo bastante largo para que la instrucción con el camino crítico más largo propague de principio a fin: fetch → decode → lectura de registros → ALU → memoria → writeback, todo combinacionalmente. Si el camino más largo es de 20 ns, tu reloj máximo es de 50 MHz. Punto.

## Segmentar es dividir ese camino

Rompe el camino en N etapas separadas por registros:

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         registros entre etapas
```

El reloj ahora solo tiene que ser lo bastante largo para que la *etapa* más larga termine, no todo el camino. Cinco etapas, cada una de ~4 ns, dan un reloj de 250 MHz. La tasa (throughput) pasa de 1 instrucción / 20 ns a 1 instrucción / 4 ns — **5× de aceleración** gratis, conceptualmente.

Fíjate en lo que la segmentación *no* mejora: la latencia de una sola instrucción. Una instrucción sigue tardando cinco etapas en terminar — unos 20 ns, igual que antes (de hecho un pelín más, por el tiempo de setup del registro en cada frontera). Lo que cambia es que una instrucción *nueva* se completa en cada ciclo una vez que el pipe está lleno. No estás haciendo que un coche vaya más rápido; estás convirtiendo a un trabajador en una línea de ensamblaje.

## Los registros entre etapas

Los registros entre etapas no son papeleo — *son* el pipeline. Cada uno captura todo lo que las etapas de aguas abajo todavía necesitan: el resultado parcial, el número de registro destino, las señales de control que se decodificaron allá en ID. Sin ellos, la etapa N+1 vería cambiar a mitad del cálculo las entradas de la etapa N. Estos son los "registros inter etapa" que el temario de ESCOM (unidad IV) te pide implementar en HDL.

## Por qué nunca obtienes realmente 5×

El ideal es **CPI = 1** (un ciclo por instrucción) una vez que el pipe está lleno. Dos cosas lo erosionan:

1. **Llenado y vaciado.** La primera instrucción aún necesita las cinco etapas antes de que algo retire, así que un programa corto paga un costo de arranque. Para una corrida larga esto es insignificante; para un bucle apretado no lo es.
2. **Riesgos (hazards)** — el impuesto de verdad, y el tema del resto de este módulo.

## Riesgos (hazards) — el costo

Tres tipos de colisión, cada uno con su propio arreglo:

**Riesgo estructural** — dos etapas quieren el mismo recurso físico en el mismo ciclo. El caso clásico: IF quiere leer una instrucción mientras MEM quiere leer datos, pero solo hay un puerto de memoria. Arreglo: separar la memoria de instrucciones y la de datos (una división Harvard), o duplicar el puerto.

**Riesgo de datos** — la instrucción N+1 necesita un valor que la instrucción N aún no ha escrito en writeback:

```
  ADD R1, R2, R3   -- resultado listo al final de EX, escrito en WB
  SUB R4, R1, R5   -- necesita R1 en su propio EX, un ciclo después
```

Sin ayuda, SUB lee el R1 *viejo*. El arreglo es el **adelantamiento (forwarding)** (también llamado bypassing): rutear el resultado de la ALU directo desde la salida EX/MEM de N de vuelta a la entrada de la ALU de N+1, en lugar de esperar a que se arrastre por WB y vuelva a salir del banco de registros. El adelantamiento mata la mayoría de los stalls de datos — pero no todos. Un **riesgo load-use** es el terco: el valor de un load no está disponible hasta el final de MEM, una etapa *después* de EX, así que una instrucción que lo use de inmediato aún tiene que **detenerse un ciclo** (insertar una burbuja) por muy bueno que sea tu adelantamiento.

**Riesgo de control** — la instrucción N es un salto (branch), y el pipeline ya hizo fetch de N+1 (y tal vez N+2) antes de saber si el salto se toma o no. Arreglos:

- **Stall / flush** — congelar el fetch hasta que el salto se resuelva, o dejar que las instrucciones del camino equivocado arranquen y aplastarlas si la suposición falló. Resolver el salto antes (en ID en lugar de EX) reduce la penalización a una sola burbuja.
- **Predicción estática** — siempre suponer una dirección en tiempo de compilación (p. ej. "los saltos hacia atrás se toman" atrapa bien los bucles).
- **Predicción dinámica** — aprender en tiempo de ejecución con una tabla de **contadores saturantes de 2 bits** indexada por la dirección del salto. El contador necesita *dos* suposiciones equivocadas seguidas antes de cambiar su predicción, así que un bucle que se toma 99 veces y luego sale una vez no cambia de opinión de inmediato. Predice "tomado" cuando el contador está en sus dos estados altos, "no tomado" en sus dos estados bajos; empújalo hacia tomado en cada salto tomado y hacia no-tomado en caso contrario. Barato, y lo hace casi tan bien como esquemas mucho más elaborados.

Cada lección siguiente de este módulo implementa un arreglo a la vez sobre el CPU de 8 bits del plan, empezando por el adelantamiento — siguiendo la progresión de ESCOM de estructural → datos → bypassing/forwarding → riesgo de control.

## Qué recordar

- La segmentación cambia **latencia por instrucción** (sin cambio, incluso un poco mayor) por **tasa/throughput** (mucho mayor): CPI ideal → 1.
- La aceleración del reloj está acotada por la *etapa* más larga, no por todo el camino ni por la etapa más corta.
- Los registros entre etapas llevan datos *y* control hacia adelante; son los que hacen seguro el traslape.
- El adelantamiento borra la mayoría de los riesgos de datos, pero un par load-use aún cuesta un stall. Los saltos cuestan un flush a menos que los predigas — un contador saturante de 2 bits es el predictor caballito de batalla.
- Los riesgos cuestan ciclos; las mitigaciones cuestan compuertas. El diseño real de un CPU es este compromiso a escala.
