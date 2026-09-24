# Procesos y listas de sensibilidad

Todo en VHDL fuera de un proceso es concurrente — cables y compuertas. En el momento en que quieres que un circuito **recuerde** algo de un ciclo de reloj al siguiente, necesitas un proceso.

## Qué es un proceso

Un `process` es un bloque de código secuencial dentro de una arquitectura VHDL. Sus líneas se leen de arriba abajo, igual que en software. Pero el proceso solo "se ejecuta" cuando algo en su **lista de sensibilidad** cambia.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Este bloque dice: "cada vez que `clk` o `reset` cambien, reevalúa mi cuerpo".

## Dos formas que verás el 99 % del tiempo

### Proceso combinacional

Lista de sensibilidad = cada entrada leída dentro. Sin reloj. La salida es función pura de las entradas.

```vhdl
process(a, b, sel)
begin
  if sel = '0' then
    y <= a;
  else
    y <= b;
  end if;
end process;
```

Se sintetiza como un MUX 2 a 1. Sin memoria.

### Proceso reloj-disparado (secuencial)

Lista de sensibilidad = `(clk, reset)`. Las asignaciones suceden solo dentro de `rising_edge(clk)` (o `falling_edge`, si tuviste mala suerte). La salida es **registrada** — cambia solo en los flancos de reloj.

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= (others => '0');
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Este es el patrón clásico del flip-flop D.

## La trampa de la lista de sensibilidad

Si olvidas listar una señal que el cuerpo lee, simulación y síntesis no concuerdan:

- **Simulación**: el proceso no se reevalúa cuando esa señal cambia. La forma de onda sale mal.
- **Síntesis**: la herramienta lee el cuerpo, ve cada lectura y construye el hardware correcto igual.

Resultado: tu testbench pasa, pero la placa no funciona — o al revés. VHDL-2008 te deja escribir `process(all)` para evitarlo:

```vhdl
process(all)  -- incluye cada señal leída en el cuerpo
begin
  ...
end process;
```

El parser de Kmila acepta `process(all)`, y es lo que usan la mayoría de los equipos en diseños nuevos.

## Regla práctica

- Sensibilidad = solo `(clk, reset)` → describes **registros** (flip-flops).
- Sensibilidad = cada entrada que lees → describes lógica **combinacional**.
- Sensibilidad mezcla señales con reloj y sin reloj → casi seguro tienes un bug.

## Qué recordar

- Un proceso es la unidad de código secuencial dentro de un archivo por lo demás concurrente.
- Procesos con reloj (`rising_edge(clk)`) se sintetizan como flip-flops. Así obtienes memoria.
- Lista cada señal que lees, o usa `process(all)` para que la herramienta lo haga por ti.
