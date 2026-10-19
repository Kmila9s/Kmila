# Procesos y listas de sensibilidad

Todo en VHDL fuera de un proceso es concurrente — cables y compuertas. En el momento en que quieres que un circuito **recuerde** algo de un ciclo de reloj al siguiente, recurres a un proceso. Los procesos son donde viven las "sentencias secuenciales" de la unidad de HDL, y son el puente de la lógica combinacional (Unidad III) a los elementos de memoria (Unidad IV).

## Qué es un proceso

Un `process` es un bloque de código secuencial dentro de una arquitectura VHDL. Sus líneas se leen de arriba abajo, igual que en software. Pero el proceso en sí solo "se ejecuta" cuando algo en su **lista de sensibilidad** cambia — la lista es la condición de despertar del proceso.

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

Este bloque significa: "cada vez que `clk` o `reset` cambien, reevalúa mi cuerpo". Entre esos eventos el proceso duerme y `q` mantiene su valor — que es exactamente el comportamiento de un registro.

## Dos formas que verás el 99 % del tiempo

### Proceso combinacional

Lista de sensibilidad = **cada señal leída** adentro. Sin reloj. La salida es una función pura de las entradas.

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

Se sintetiza como un mux 2 a 1. Sin almacenamiento — siempre que cada salida se asigne en cada camino (más de eso en el arreglo del latch).

### Proceso con reloj (secuencial)

Lista de sensibilidad = `(clk, reset)` para un reset asíncrono, o solo `(clk)` para un reset síncrono. Las asignaciones de datos ocurren únicamente dentro de `rising_edge(clk)` (o `falling_edge`). La salida es **registrada** — solo cambia en los flancos de reloj.

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

Este es el patrón clásico del flip-flop D, y es la plantilla de básicamente todo registro que vayas a escribir.

## `rising_edge` vs. el viejo idiom `clk'event`

Verás dos maneras de detectar un flanco de reloj:

```vhdl
elsif rising_edge(clk) then        -- preferida
elsif clk'event and clk = '1' then -- más antigua, equivalente para '0'/'1'
```

`rising_edge(clk)` (de `std_logic_1164`) es la forma moderna y más clara. Además ignora correctamente los flancos que involucran metavalores (como `'X'` → `'1'`), mientras que el crudo `clk'event and clk='1'` puede dispararse mal con ellos. Usa `rising_edge`.

## La trampa de la lista de sensibilidad

Si olvidas listar una señal que un cuerpo *combinacional* lee, la simulación y la síntesis no concuerdan:

- **Simulación**: el proceso no se reevalúa cuando esa señal cambia. La forma de onda se ve mal (las salidas parecen "atoradas").
- **Síntesis**: la herramienta lee el cuerpo, ve cada lectura y construye el hardware correcto de todos modos — ignorando tu lista.

Resultado: tu testbench y tu placa se comportan distinto, que es el peor tipo de bug. (Los procesos con reloj son inmunes a esta trampa en particular porque solo actúan en el flanco de reloj, pero una lista incompleta en un proceso *combinacional* es una causa clásica de fallas fantasma.) VHDL-2008 lo arregla de tajo con `process(all)`:

```vhdl
process(all)  -- la herramienta incluye cada señal leída en el cuerpo
begin
  ...
end process;
```

El parser de Kmila acepta `process(all)`, y es lo que usan la mayoría de los equipos para procesos combinacionales en diseños nuevos.

## Una regla práctica

- Sensibilidad = solo `(clk)` o `(clk, reset)` → estás describiendo **registros** (flip-flops).
- Sensibilidad = cada entrada que lees (o `all`) → estás describiendo lógica **combinacional**.
- Sensibilidad mezcla un flanco de reloj con señales de datos extra que luego usas *fuera* del flanco → casi seguro tienes un bug.

## Una más: no manejes la misma señal desde dos procesos

Una señal manejada dentro de un proceso tiene a ese proceso como su driver. Si un *segundo* proceso (o una sentencia concurrente) también la maneja, has creado múltiples drivers — resueltos a `'X'` para `std_logic`, o un error de compilación para tipos no resueltos. Dale a cada señal exactamente un driver.

## Qué recordar

- Un proceso es la unidad de código secuencial dentro de un archivo por lo demás concurrente.
- Los procesos con reloj (`rising_edge(clk)`) se sintetizan como flip-flops — así obtienes memoria.
- Prefiere `rising_edge(clk)` sobre `clk'event and clk='1'`.
- Lista cada señal que un proceso combinacional lee, o usa `process(all)`; una lista incompleta hace que sim y síntesis no concuerden.
- Una señal, un driver.
