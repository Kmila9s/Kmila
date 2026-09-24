# Asignaciones concurrentes: `when/else` y `with/select`

La lógica combinacional en VHDL se escribe con sentencias concurrentes — cada línea fuera de un proceso describe hardware cableado permanentemente. Hay dos formas de hacer el mismo trabajo: **condicional** y **selectiva**.

## `when/else` — condicional

Bien cuando tienes una lista priorizada de condiciones o cuando cada rama revisa una señal distinta:

```vhdl
y <= '0' when reset = '1' else
     a   when mode  = "read" else
     b   when mode  = "write" else
     'Z';
```

Gana la primera condición que se cumpla. El orden importa — se sintetiza como un **multiplexor con prioridad**: la condición de arriba tiene el camino más directo a `y`.

## `with/select` — selectiva

Bien cuando estás ramificando sobre una sola expresión (igual que un `switch` de software):

```vhdl
with mode select
  y <=
    a   when "00",
    b   when "01",
    c   when "10",
    'Z' when others;
```

Todos los casos se comparan contra la misma señal (`mode`), y `when others` es obligatorio para cubrir todos los patrones posibles. Este estilo se sintetiza como un **multiplexor balanceado** — ninguna rama es privilegiada.

## ¿Cuál elegir?

- **Misma señal, muchos patrones:** `with/select` se lee como una tabla de verdad. Úsalo.
- **Señales distintas, lógica con prioridad:** `when/else`. Natural.
- **Salida de síntesis:** ambos producen más o menos las mismas compuertas en un FPGA moderno — elegir la forma más clara casi siempre es correcto.

## Una pequeña trampa

Ambas formas requieren que `y` termine asignada en **todos** los escenarios. Si olvidas el último `else` de un `when/else`, o saltas el `when others` en un `with/select`, el compilador te avisa — y el sintetizador puede inferir un latch para "recordar" el valor anterior, lo cual casi nunca es lo que quieres. Es la misma trampa del latch que verás en el ejercicio de arreglar del módulo secuencial.

## Dónde aparece después

Usarás `with/select` en el ejemplo del decodificador 3 a 8 justo después de esta lección, y `when/else` por todo el FSM del semáforo más adelante. Ambas son pan de todos los días en VHDL.
