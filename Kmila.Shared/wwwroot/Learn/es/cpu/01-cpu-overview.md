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

**Monociclo.** Todas las etapas se ejecutan combinacionalmente dentro de un mismo período de reloj. Pros: simple. Contras: el reloj tiene que ser lento para que la instrucción más larga (usualmente un load) propague por *todo* en un tick. Los ejemplos didácticos de MIPS son monociclo.

**Multiciclo.** Una etapa por ciclo. La instrucción se guarda en un registro de estado mientras pasa por las etapas. Pros: reloj más rápido (cada etapa con su propio camino crítico corto). Contras: cinco ciclos por instrucción.

**Segmentado (pipelined).** Cinco instrucciones en vuelo al mismo tiempo, cada una en una etapa. Pros: una instrucción retira por ciclo, así que la tasa ≈ monociclo a reloj mucho mayor. Contras: hazards (de datos, de control, estructurales) — tema del resto de un curso de arquitectura. RISC-V, ARM Cortex-M, AMD Zen — todos segmentados.

## El proyecto de CPU de este plan

Tu primer CPU será **monociclo** para mantenerlo tratable. Todo pasa combinacionalmente dentro de un período de reloj:

- PC direcciona una ROM de 16 bytes.
- El byte leído es a la vez el nibble de opcode y el inmediato.
- Decode + execute + writeback pasan antes del siguiente flanco, que captura el nuevo PC y el nuevo R0.

## La ISA que implementaremos

Cuatro instrucciones, opcode de 4 bits + inmediato de 4 bits:

| Opcode | Mnemónico     | Semántica                   |
|--------|---------------|-----------------------------|
| `0000` | `LOAD R0, #i` | `R0 <= zero_extend(i)`      |
| `0001` | `ADD  R0, #i` | `R0 <= R0 + zero_extend(i)` |
| `0010` | `JUMP addr`   | `PC <= addr`                |
| `1111` | `HALT`        | congela PC y R0             |

Un programa que puedes simular a mano:

```
0: 03   LOAD R0, #3       -- R0 = 3
1: 14   ADD  R0, #4       -- R0 = 7
2: 12   ADD  R0, #2       -- R0 = 9
3: F0   HALT
```

## Qué te da

Cuando el andamiaje corra, tienes una **máquina programable**. Cambia los bytes de la ROM — cambia el programa que ejecuta el CPU. Ese es el truco fundamental: el mismo hardware, distinto contenido en memoria, distinto comportamiento.

De ahí crece:

- Más registros (R0..R3).
- Jumps condicionales (`JZ`, `JNZ`) para ramificar.
- Load/store desde un módulo de RAM (reutiliza el del módulo de memorias).
- Segmentación del conjunto.

El camino de esta máquina de 4 instrucciones a un core RV32I RISC-V completo es largo, pero cada paso es más de lo mismo que vas a construir.
