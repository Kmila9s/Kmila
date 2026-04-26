# ¿Qué es un SoC?

Un **System-on-Chip** (SoC) es exactamente lo que dice su nombre: un sistema entero — CPU, memoria, periféricos, interconexión — integrado en un único silicio. Tu teléfono es un SoC. Una Raspberry Pi es un SoC sobre una PCB.

## Los tres ingredientes

1. **Uno o más procesadores**: ARM Cortex-A, RISC-V, MicroBlaze en FPGA, etc.
2. **Periféricos**: UART, SPI, I²C, GPIO, controladores de DMA, temporizadores.
3. **Un interconector**: AMBA **AXI4** y **AXI4-Lite** son los estándares de facto en la industria FPGA/ASIC.

```
┌─────────┐   ┌──────────┐   ┌──────────┐
│ CPU     │◄─►│          │◄─►│ UART     │
└─────────┘   │  AXI     │   └──────────┘
              │  Inter-  │
┌─────────┐   │  connect │   ┌──────────┐
│ Memoria │◄─►│          │◄─►│ GPIO     │
└─────────┘   └──────────┘   └──────────┘
```

## AXI4 en un párrafo

AXI4 es un **protocolo maestro/esclavo** con cinco canales independientes: dirección de lectura, dirección de escritura, datos de lectura, datos de escritura y respuesta. Los cinco pueden operar en paralelo, lo que permite que una CPU emita lecturas mientras se completan escrituras anteriores.

**AXI4-Lite** es la versión simplificada: sin bursts, sin reordenamiento, ideal para registros de control de periféricos.

## Por qué un SoC y no chips separados

- **Ancho de banda interno** — comunicación on-die es órdenes de magnitud más rápida que off-die.
- **Consumo** — mover bits a través del encapsulado cuesta energía.
- **Espacio y coste** — una sola pieza reemplaza decenas.

## Relación con FPGAs

Los FPGAs modernos (Xilinx Zynq, Intel SoC FPGA) integran uno o varios ARM + lógica programable. Tu diseño VHDL se convierte en un periférico AXI más, hablando con la CPU como lo haría un UART.

*Más lecciones vendrán — este módulo está en desarrollo.*
