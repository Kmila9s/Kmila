# Cómo un CPU ejecuta una instrucción

Mira dentro de cualquier CPU — desde un microcontrolador de 8 bits hasta un shader core de GPU moderna — y encontrarás las mismas cinco etapas haciendo los mismos cinco trabajos:

```mermaid
flowchart LR
    IF["IF<br/>fetch"] --> ID["ID<br/>decode"] --> EX["EX<br/>execute"] --> MEM["MEM<br/>memoria"] --> WB["WB<br/>writeback"]
```

## Las cinco etapas clásicas

**IF (Fetch de instrucción).** Lee la siguiente instrucción de la memoria de programa en la dirección que marca el Program Counter (PC). Incrementa PC para que el próximo ciclo apunte a la siguiente.

**ID (Decodificación).** Abre la palabra de la instrucción. La parte en **opcode** (¿qué operación?) y **campos de operando** (¿qué registros? ¿qué inmediato?).

**EX (Ejecución).** Mete los operandos en la ALU. Para `ADD R0, R1`, EX lee R0 y R1 del banco de registros, le dice a la ALU "suma" y captura el resultado. Para `JUMP addr`, EX calcula el nuevo PC.

**MEM (Memoria).** Si la instrucción es load o store, aquí se habla con la memoria de datos. Para instrucciones registro-registro, es un paso transparente — pero la etapa siempre está para que toda instrucción tenga la misma profundidad de pipeline.

**WB (Writeback).** El resultado aterriza en su destino — un registro para aritmética, el PC para un jump, una celda de memoria para un store.

## Monociclo vs multiciclo vs segmentado

Los mismos cinco trabajos pueden ordenarse de tres formas en silicio:

**Monociclo.** Todas las etapas se ejecutan combinacionalmente dentro de un mismo período de reloj. Pros: simple. Contras: el reloj tiene que ser lo bastante lento para que la instrucción más larga (usualmente un load) propague por *todo* en un solo tick. Todos los ejemplos didácticos sencillos de MIPS son monociclo.

**Multiciclo.** Una etapa por ciclo. La instrucción se guarda en un registro de estado mientras pasa por las etapas. Pros: reloj más rápido que el monociclo (cada etapa con su propio camino crítico corto). Contras: cinco ciclos por instrucción.

**Segmentado (pipelined).** Cinco instrucciones en vuelo al mismo tiempo, cada una en una etapa. Pros: una instrucción nueva retira por ciclo, así que la tasa (throughput) ≈ monociclo pero a un reloj mucho más rápido. Contras: riesgos (hazards) — de datos, de control, estructurales — a los que se dedica el resto de un curso de Arquitectura de Computadoras. Los cores RISC-V, ARM Cortex-M, AMD Zen — todos segmentados.

## RISC, CISC, y por qué las etapas quedan tan limpias

Esas cinco etapas tan ordenadas no son casualidad — son lo que obtienes cuando el conjunto de instrucciones se diseña para ser regular. Esa es la filosofía **RISC** (Reduced Instruction Set Computer) sobre la que está armado el curso de ESCOM:

- **Instrucciones de ancho fijo** (toda instrucción tiene el mismo número de bits), así que el fetch y la decodificación son triviales.
- **Arquitectura load/store** — solo instrucciones dedicadas de load y store tocan la memoria; la aritmética trabaja registro-registro. Por eso MEM es una etapa separada y limpia.
- **Muchos registros de propósito general**, para que los compiladores mantengan los valores en el banco de registros en vez de volcarlos a memoria.

**CISC** (Complex Instruction Set Computer, p. ej. x86) apuesta por lo contrario: instrucciones de longitud variable, operaciones que leen memoria *y* calculan en una sola instrucción, y cientos de modos de direccionamiento. Potente por instrucción, pero mucho más difícil de segmentar limpiamente. Los chips x86 modernos en realidad descomponen cada instrucción compleja en micro-ops tipo RISC internamente.

Una división más vale la pena nombrar: las máquinas **Von Neumann** guardan programa y datos en una sola memoria (un solo bus, simple, pero código y datos compiten por él); las máquinas **Harvard** usan memorias de instrucciones y de datos separadas, que es justo lo que le permite a un core monociclo o segmentado hacer fetch de una instrucción *y* acceder a datos en el mismo tick. El CPU que estás por construir es efectivamente Harvard — una ROM para el código, un camino separado para los datos.

## Formatos de instrucción

La decodificación solo es fácil porque los campos de cada instrucción están en posiciones fijas. El RISC clásico (estilo MIPS, el modelo que usa ESCOM) tiene tres formatos:

- **Tipo R** — operaciones ALU registro-registro: `opcode | rs | rt | rd | shamt | funct`. Dos registros fuente, uno destino, un monto de desplazamiento y un selector de función.
- **Tipo I** — inmediato / load / store / branch: `opcode | rs | rt | immediate`. El inmediato de 16 bits es una constante o un desplazamiento de dirección.
- **Tipo J** — saltos: `opcode | address`. Un campo de dirección grande para alcanzar destinos lejanos.

La ISA de juguete de abajo es una prima de dos campos de esta idea: un opcode de 4 bits y un inmediato de 4 bits. El mismo principio, más chico.

## El proyecto de CPU de este plan

Tu primer CPU será **monociclo** para mantenerlo tratable. Todo pasa combinacionalmente dentro de un período de reloj:

- PC direcciona una ROM de 16 bytes.
- El byte leído es a la vez el nibble de opcode y el nibble de inmediato.
- Decode + execute + writeback pasan todos antes del siguiente flanco de subida del reloj, que captura el nuevo PC y el nuevo R0.

## La ISA que implementaremos

Cuatro instrucciones, opcode de 4 bits + inmediato de 4 bits:

| Opcode | Mnemónico     | Semántica                   |
|--------|---------------|-----------------------------|
| `0000` | `LOAD R0, #i` | `R0 <= zero_extend(i)`      |
| `0001` | `ADD  R0, #i` | `R0 <= R0 + zero_extend(i)` |
| `0010` | `JUMP addr`   | `PC <= addr`                |
| `1111` | `HALT`        | congela PC y R0             |

Un programa que ya puedes simular a mano:

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## Qué te da

Cuando el andamiaje corra, tienes una **máquina programable**. Cambia los bytes de la ROM — cambias el programa que ejecuta el CPU. Ese es el truco fundamental: el mismo hardware, distinto contenido en la memoria, distinto comportamiento.

De ahí crece:

- Más registros (R0..R3).
- Jumps condicionales (`JZ`, `JNZ`) para que tus programas puedan ramificar.
- Load/store desde un módulo de RAM (reutiliza el del módulo de memorias).
- Segmentar todo el conjunto.

El camino de esta máquina de 4 instrucciones a un core RV32I RISC-V completo es largo, pero cada paso es más de lo mismo que vas a construir.

## Quién dirige todo esto: la unidad de control

La ruta de datos (PC, ROM, banco de registros, ALU, memoria) es la plomería; la **unidad de control** es la que abre las válvulas correctas en cada ciclo. Lee el opcode y maneja las señales de control: qué operación de la ALU, si el banco de registros escribe en este ciclo, si la memoria lee o escribe, de dónde sale el próximo PC. Hay dos formas de construirla:

- **Control cableado (hardwired)** — el opcode alimenta lógica combinacional (o una pequeña máquina de estados) que produce las señales directamente. Rápido, y la opción natural para un RISC limpio. Esto es lo que hace tu andamiaje con su `case` sobre el opcode.
- **Control microprogramado (microcoded)** — cada instrucción es un pequeño programa de *microinstrucciones* guardado en una memoria de microcódigo, que va soltando las señales de control. Flexible, y la forma en que históricamente las máquinas CISC domaron su complejidad, a costa de velocidad. El temario de ESCOM nombra ambas (microinstrucciones, memorias de microcódigo) para que las puedas distinguir.

Junto a ella está el **registro de estado** — las banderas que produjo tu ALU (cero, acarreo, desbordamiento, negativo). Los saltos condicionales no son más que la unidad de control probando una de esas banderas para decidir el próximo PC.

## Qué recordar

- Todo CPU, grande o chico, corre el mismo ciclo: fetch, decode, execute, memoria, writeback.
- Esas etapas pueden ser monociclo (simple, reloj lento), multiciclo (reloj más rápido, varios ciclos cada una) o segmentadas (traslapadas, la mayor tasa).
- RISC mantiene instrucciones de ancho fijo y load/store para que la decodificación y la segmentación queden limpias; CISC lo cambia por potencia por instrucción.
- La ruta de datos mueve los datos; la unidad de control — cableada o microprogramada — decide qué se mueve a dónde, usando el opcode y las banderas de estado.
