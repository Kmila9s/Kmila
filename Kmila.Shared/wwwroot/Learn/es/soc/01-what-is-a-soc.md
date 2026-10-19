# ¿Qué es un Sistema en Chip?

Tu CPU de 8 bits del módulo anterior funciona por su cuenta. Calcula, pero no habla con el mundo. Un **Sistema en Chip (SoC)** es el siguiente escalón: una CPU más un puñado de periféricos más el tejido que les permite comunicarse entre sí, todo en el mismo dado de silicio.

Esa es justo la premisa del curso de *Sistemas en Chip*: en lugar de cablear un procesador a chips de memoria por separado y chips de E/S por separado sobre una placa, integras el procesador, su memoria y sus periféricos en un solo dispositivo y programas el resultado.

## Tres piezas obligatorias

Todo SoC, desde la CPU de Kmila extendida hasta un Apple de la serie A de 5 millones de dólares, se reduce a:

1. **Uno o más elementos de procesamiento**: CPUs, DSPs, GPUs, aceleradores.
2. **Memoria**: SRAM en el chip, block RAM, a veces DDR fuera del chip.
3. **Una interconexión**: los cables y el arbitraje que dejan que (1) hable con (2) y con los periféricos.

La interconexión es la parte que deja de ser "nada más RTL" y empieza a sentirse arquitectónica. Define quién puede hablar y cuándo, cuántas transacciones pueden estar en curso y qué tan rápido puede completarse cualquier petición.

## El elemento de procesamiento: RISC y Harvard

La mayoría de los núcleos de SoC con los que te vas a topar —PIC, AVR, ARM Cortex-M, RISC-V— son **RISC** (Reduced Instruction Set Computer, computadora con conjunto reducido de instrucciones). RISC significa un conjunto pequeño de instrucciones simples y de ancho fijo que hacen una sola cosa cada una, así que el hardware para decodificarlas y ejecutarlas se mantiene chico y rápido. El costo es que las operaciones complejas requieren varias instrucciones en lugar de una.

Esos núcleos suelen ser de arquitectura **Harvard**: la memoria de programa y la memoria de datos viven en buses *separados*. Como los dos buses son independientes, el núcleo puede traer la siguiente instrucción al mismo tiempo que lee o escribe un operando de datos: el doble de ancho de banda de memoria que con un solo bus compartido. Un bonito efecto secundario: la palabra de instrucción no tiene por qué coincidir con el ancho de la palabra de datos. Un núcleo PIC de gama media, por ejemplo, maneja datos de 8 bits pero instrucciones de 14 bits, de modo que cada instrucción cabe en una sola palabra.

(Una máquina **von Neumann** pura comparte un único bus para código y datos: más simple, pero los accesos a código y a datos compiten por él. El 8051 es un compromiso "Harvard modificado".)

## Segmentación (pipelining)

Un núcleo traslapa trabajo para terminar más instrucciones por segundo. La forma más simple es una **segmentación** (pipeline) de dos etapas: mientras se ejecuta la instrucción *N*, el núcleo ya está trayendo la instrucción *N+1*.

```
  ciclo:   1        2        3        4
  fetch:  I1       I2       I3       I4
  exec:            I1       I2       I3
```

El problema son los **saltos**. Cuando un `GOTO`, un `CALL` o un `RETURN` cambia el contador de programa, la instrucción que ya se había traído detrás de él es la incorrecta y hay que descartarla: la segmentación se *vacía* (flush) y se vuelve a llenar, lo que cuesta un ciclo. Por eso los bucles apretados y el código lleno de saltos corren un poco más lento que el código en línea recta.

## Memoria de programa, memoria de datos y el modelo de programación

La asignatura de la ESCOM divide el mapa de memoria en dos roles:

- La **memoria de programa** guarda tus instrucciones (a menudo flash/ROM). Es no volátil, así que el dispositivo arranca con el mismo programa en cada encendido.
- La **memoria de datos** guarda variables, la pila y los registros de E/S (SRAM). Es volátil, se borra al reiniciar.

Por encima de todo esto está el **modelo de programación**: los registros y el mapa de direcciones que el programador realmente ve. Define los registros de propósito general, los registros de función especial (banderas de estado, el contador de programa, el apuntador de pila) y —algo crucial para un SoC— los **registros de periférico mapeados en memoria**. En un diseño mapeado en memoria, un periférico se ve como unas cuantas direcciones: escribes un byte en una dirección y el UART lo transmite; lees otra dirección y obtienes el resultado del ADC. La CPU nunca aprende una instrucción nueva para cada periférico; simplemente hace cargas y almacenamientos.

## Protocolos de interconexión comunes

La industria se ha decantado por un puñado de estándares para ese tejido de carga/almacenamiento:

| Protocolo        | Fabricante | Dónde lo verás                                   |
|------------------|------------|--------------------------------------------------|
| AXI4 / AXI4-Lite | ARM        | Núcleos RISC-V, frontera PS/PL de Xilinx, casi todo IP nuevo |
| AHB / APB        | ARM        | Núcleos ARM antiguos, periféricos de baja velocidad |
| Avalon MM / ST   | Intel      | NIOS-II, SoCs generados con Qsys                 |
| Wishbone         | OpenCores  | OpenRISC, muchos núcleos IP de código abierto    |

Todos se reducen a los mismos carriles: un **canal de dirección**, un **canal de datos de escritura** y un **canal de datos de lectura**. Las variaciones tienen que ver con el protocolo de handshake, el soporte para completar transacciones fuera de orden y la semántica de las ráfagas (bursts).

## Qué es en realidad AXI4-Lite

El miembro más simple de la familia AXI. Quítale las ráfagas, el fuera de orden y la calidad de servicio (QoS) y te quedan cinco canales independientes, cada uno con un handshake `valid`/`ready`:

```
  AW  (dirección de escritura)
  W   (datos de escritura)
  B   (respuesta de escritura)
  AR  (dirección de lectura)
  R   (datos de lectura)
```

Cada transferencia es un solo pulso (beat). Cada canal se detiene cuando el otro lado no está `ready`. Eso es todo, y justo por eso tiene el tamaño perfecto para construirlo a mano.

## El plan del plan de estudios de Kmila

Las lecciones posteriores construyen una interfaz de registros AXI4-Lite desde cero y luego la conectan entre tu CPU de 8 bits y un UART mapeado en memoria, para que la CPU pueda "imprimir" escribiendo en una dirección. Eso te da el modelo mental de lo que ocurre dentro de cualquier SoC moderno, a un tamaño que de verdad te cabe en la cabeza.

## Qué recordar

- Un SoC = elemento(s) de procesamiento + memoria + una interconexión, todo en un solo dado de silicio.
- La mayoría de los núcleos de SoC son **RISC** con una división **Harvard**, así que código y datos se mueven por buses separados al mismo tiempo.
- Una **segmentación** corta traslapa el fetch y la ejecución; los **saltos la vacían**.
- La memoria de programa es código no volátil; la memoria de datos es RAM volátil; el **modelo de programación** es el mapa de registros y direcciones que ve el programador.
- Los periféricos están **mapeados en memoria**: la CPU los alcanza con cargas y almacenamientos ordinarios a través de una interconexión estándar como AXI4-Lite.
