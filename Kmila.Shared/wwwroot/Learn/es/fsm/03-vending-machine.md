# Proyecto: máquina expendedora por monedas

Este es tu primer proyecto en forma. La especificación está fija, el código inicial te da la plomería, y tu trabajo es escribir la lógica que lo completa.

## Qué vas a construir

Una FSM que lleva la cuenta del dinero que un cliente ha metido y expende un producto (un solo pulso de salida, `dispense`) cuando el total llega a 25¢. Las entradas son:

- `nickel` — un pulso de 1 ciclo que significa "acaba de caer una moneda de 5¢"
- `dime`   — un pulso de 1 ciclo que significa "acaba de caer una moneda de 10¢"
- `clk`, `reset`

Y las salidas:

- `dispense` — se pone en alto por exactamente un ciclo de reloj cuando llegas a ≥ 25¢
- `change`   — se pone en alto por un ciclo junto con `dispense` si el total fue **mayor** que 25¢ (así la máquina necesita devolver dinero)

## Dos maneras de modelar esto

Una máquina expendedora es una FSM clásica, y hay dos maneras honestas de describirla:

- **Estados explícitos.** Enumera cada total alcanzable — `S0, S5, S10, S15, S20` — y dibuja una transición por cada moneda. Es el enfoque de libro de texto, y es lo que harías a mano para un conjunto chico de monedas. Deja obvio el diagrama de estados, pero explota si agregas más monedas o un precio más alto.
- **Estilo acumulador (ruta de datos).** Guarda el total en un registro pequeño y compáralo contra el precio. Menos líneas, escala a cualquier precio, y es la forma que usa el código inicial. Puedes pensar en el registro `total` como el estado y en el comparador como la lógica de siguiente estado.

De cualquier modo, `dispense` depende de la moneda que llega en *este* ciclo, así que se comporta como una salida **Mealy** — reacciona en el mismo ciclo en que la condición de venta se vuelve verdadera, en lugar de esperar un ciclo.

## Qué te da el código inicial

La entidad y el acumulador `total` ya están cableados. Cada pulso de `nickel` suma 5; cada pulso de `dime` suma 10. Pero el inicio siempre maneja `dispense = '0'` y `change = '0'`. **Eso es lo que necesitas cambiar.**

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    total <= (others => '0');
  elsif rising_edge(clk) then
    if    nickel = '1' then total <= total + 5;
    elsif dime   = '1' then total <= total + 10;
    end if;
  end if;
end process;

dispense <= '0';  -- TODO
change   <= '0';  -- TODO
```

## Enfoque sugerido

1. **Detecta la condición de venta.** En cada flanco de subida del reloj, revisa si `total >= 25` después de sumar la moneda actual. (Cuidado: necesitas calcular el *nuevo* total, no el del ciclo anterior.)
2. **Pulsa `dispense` por exactamente un ciclo.** El truco: cuando detectes la venta, activa el pulso y regresa `total` a cero. Como `total` se limpia en el mismo flanco, la condición es falsa en el siguiente ciclo, así que el pulso queda naturalmente de un ciclo de ancho.
3. **Calcula `change`.** Si el total en el momento de la venta fue > 25, pulsa `change` también.

Calcula el "próximo total" como una `variable` dentro del proceso para que la decisión use el valor actualizado, no el registro del ciclo anterior:

```vhdl
process(clk, reset)
  variable next_total : unsigned(5 downto 0);
begin
  if reset = '1' then
    total    <= (others => '0');
    dispense <= '0';
    change   <= '0';
  elsif rising_edge(clk) then
    next_total := total;
    if    nickel = '1' then next_total := total + 5;
    elsif dime   = '1' then next_total := total + 10;
    end if;

    if next_total >= 25 then
      dispense <= '1';
      if next_total > 25 then change <= '1'; else change <= '0'; end if;
      total    <= (others => '0');
    else
      dispense <= '0';
      change   <= '0';
      total    <= next_total;
    end if;
  end if;
end process;
```

Dos cosas por notar. Una `variable` se asigna con `:=` y se actualiza **de inmediato** dentro del proceso, que es exactamente por lo que `next_total` ya incluye la moneda de este ciclo — una señal (`<=`) no se actualizaría hasta que el proceso se suspenda. Y como `dispense`, `change` y `total` se manejan todos dentro del proceso con reloj, quedan **registrados** — los pulsos se alinean con los flancos de reloj y no glitchan.

## Cuídate de

- **El ancho.** `total + 10` puede llegar a 30 antes de que dispare el reset, así que dimensiona el registro para al menos 6 bits — `unsigned(5 downto 0)` guarda 0–63 con espacio de sobra. Un vector demasiado angosto se desborda en silencio.
- **Ambas monedas a la vez.** El `if … elsif` hace que un `nickel` y un `dime` simultáneos solo cuenten el nickel. Decide si eso es aceptable o un error para tu especificación.
- **Olvidar limpiar `total`.** Si pulsas `dispense` pero no pones el acumulador en cero, la máquina "vende" otra vez cada ciclo hasta que más monedas cambien el total. Limpiar en el flanco de la venta es lo que hace que el pulso sea de un ciclo de ancho.

## Criterios de aceptación

| Secuencia de monedas   | Comportamiento esperado       |
|------------------------|-------------------------------|
| 5 nickels              | `dispense=1, change=0`        |
| 2 dimes + 1 nickel     | `dispense=1, change=0`        |
| 3 dimes (= 30¢)        | `dispense=1, change=1`        |
| 2 nickels + 1 dime     | sin dispense (solo 20¢)       |

## Yendo más lejos

- Agrega un botón de **devolución** que ponga `total` en cero sin expender.
- Soporta una moneda de **cuarto** (25¢) — debe expender de inmediato.
- Lleva la cuenta de productos vendidos en un contador separado de 8 bits visible como salida. Es el tipo de retroalimentación que las expendedoras reales mandan hacia arriba.

## Relación con la asignatura

Esto es la *aplicaciones con autómatas y lenguaje de descripción de hardware* de la Unidad II: una máquina secuencial determinista especificada a partir de un comportamiento, y luego descrita en HDL. La forma de acumulador-más-comparador también es una introducción suave a la Unidad V, donde dividirás un diseño explícitamente en una **ruta de datos** (el registro `total` y el sumador) y un autómata de **control** que la maneja.

## Qué recordar

- Una máquina expendedora es una FSM; modélala con estados explícitos para dar claridad, o con un registro acumulador para escalar.
- Usa una `variable` (`:=`) para el "próximo total" de modo que la revisión de venta incluya la moneda que acaba de llegar.
- Haz un pulso de un ciclo limpiando el acumulador en el flanco de la venta — las salidas registradas mantienen el pulso limpio.
- Dimensiona los vectores para el valor más grande que puedas alcanzar, y decide de antemano cómo se comportan las entradas simultáneas.
