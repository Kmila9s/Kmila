# Máquinas Mealy vs Moore

Una máquina de estados finitos (FSM) es lógica de control que recuerda qué está haciendo. Toda FSM tiene:

- Un **registro de estado** (un grupo de flip-flops).
- **Lógica de siguiente estado** (combinacional, decide qué estado será en el próximo flanco).
- **Lógica de salida** (combinacional, decide las salidas ahora mismo).

De dónde toma sus entradas la lógica de salida divide a las FSM en dos familias.

## Moore: salidas dependen solo del estado

```
 entrada → [ siguiente ] → [ reg. estado ] → [ salidas ]
                                         ↑
                                       (eso es todo)
```

Dado un estado, la salida está fija. Si estás en `S_GREEN`, `green = '1'`. Siempre. Sin importar cómo se vea la entrada.

**Pros**
- Las salidas solo cambian en flancos → sin glitches.
- Fáciles de razonar, depurar y verificar.

**Contras**
- Al menos un ciclo de latencia entre entrada y salida (el estado tiene que transicionar primero).
- A veces necesitas más estados para codificar el mismo comportamiento que una Mealy.

## Mealy: salidas dependen de estado + entrada

```
 entrada ──→ [ siguiente ] ──→ [ reg. estado ]
    │                               │
    └───→ [ lógica salida ] ←───────┘
                  │
               salidas
```

Las salidas son función del estado **y** la entrada actuales. Pueden cambiar entre flancos.

**Pros**
- Más rápida — las salidas reaccionan en el mismo ciclo, no en el siguiente.
- Suele necesitar menos estados.

**Contras**
- Las salidas pueden glitchar (siguen la entrada combinacional).
- Más difícil depurar cuando las salidas no se alinean con los flancos.

## ¿Cuál deberías usar?

**Por defecto: Moore.** La ausencia de glitches y la alineación con flancos hacen los diseños predecibles. Para control dentro de un CPU, un controlador de memoria o algo crítico, Moore es más seguro.

**Usa Mealy cuando** necesites reaccionar en el mismo ciclo, o cuando los estados extra que te pediría una Moore hagan el diseño inflado. UART y motores de protocolo suelen terminar Mealy por tiempo de reacción.

## Cómo las sostiene Kmila

Ambos estilos caen en el mismo **patrón de dos procesos** que verás en el ejemplo del semáforo:

```vhdl
-- Proceso 1: registro de estado (siempre Moore-seguro)
process(clk, reset)
begin
  if reset = '1' then
    state <= S_IDLE;
  elsif rising_edge(clk) then
    state <= next_state;
  end if;
end process;

-- Proceso 2: lógica combinacional de siguiente estado + salidas
process(state, input)
begin
  ...
end process;
```

Para Moore las salidas solo miran a `state`. Para Mealy miran a `state` e `input`.

## Qué recordar

- Moore: salida = función del estado. Sin glitches.
- Mealy: salida = función del estado + entrada. Más rápida.
- Empieza con Moore; pásate a Mealy cuando tengas razón específica.
