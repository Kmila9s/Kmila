# Proyecto: máquina expendedora por monedas

Este es tu primer proyecto completo. La especificación está fija, el andamiaje te da la plomería, y tu trabajo es escribir la lógica que lo completa.

## Qué vas a construir

Un FSM que rastrea el dinero que un cliente ha metido y expende un producto (un pulso de salida, `dispense`) cuando llega a 25¢. Las entradas son:

- `nickel` — un pulso de 1 ciclo que significa "acaba de caer una moneda de 5¢"
- `dime`   — un pulso de 1 ciclo que significa "acaba de caer una moneda de 10¢"
- `clk`, `reset`

Y las salidas:

- `dispense` — alto por exactamente un ciclo cuando llegas a ≥ 25¢
- `change`   — alto por un ciclo junto con `dispense` si el total pasó de 25¢ (hay que devolver cambio)

## Criterios de aceptación

| Secuencia de monedas   | Comportamiento esperado       |
|------------------------|-------------------------------|
| 5 nickels              | `dispense=1, change=0`        |
| 2 dimes + 1 nickel     | `dispense=1, change=0`        |
| 3 dimes (= 30¢)        | `dispense=1, change=1`        |
| 2 nickels + 1 dime     | sin dispense (solo 20¢)       |

## Qué te da el inicio

La entidad y el acumulador `total` están cableados. Cada pulso de `nickel` suma 5; cada pulso de `dime` suma 10. Pero siempre maneja `dispense = '0'` y `change = '0'`. **Eso es lo que tienes que cambiar.**

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

1. **Detecta la venta.** En cada flanco de subida, revisa si `total >= 25` tras sumar la moneda. (Ojo: necesitas el *nuevo* total, no el del ciclo anterior.)
2. **Pulsa `dispense` por exactamente un ciclo.** El truco: al detectar la venta, activa una bandera y resetea `total` a cero. La bandera vive un ciclo.
3. **Calcula `change`.** Si el total al momento de la venta era > 25, pulsa `change` también.

Pista: se vuelve más simple si calculas el "próximo total" como una señal local (o `variable` dentro del proceso):

```vhdl
variable next_total : unsigned(5 downto 0);
...
next_total := total;
if    nickel = '1' then next_total := total + 5;
elsif dime   = '1' then next_total := total + 10;
end if;

if next_total >= 25 then
  dispense <= '1';
  change   <= '1' when next_total > 25 else '0';
  total    <= (others => '0');
else
  dispense <= '0';
  change   <= '0';
  total    <= next_total;
end if;
```

## Más lejos

- Añade un botón de **devolución** que ponga `total` en cero sin expender.
- Acepta una moneda de **25¢** — debe expender de inmediato.
- Lleva la cuenta de productos vendidos en un contador separado de 8 bits como salida. Así es como las expendedoras reales reportan a los sistemas superiores.
