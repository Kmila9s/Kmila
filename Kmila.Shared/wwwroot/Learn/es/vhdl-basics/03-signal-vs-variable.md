# Signal vs. variable

Es la distinción peor entendida de todo VHDL. Los estudiantes leen "una variable es como una variable local en C" y salen corriendo en la dirección equivocada. Pongámosla en su lugar, porque entenderla bien es lo que separa el código que *simula* del código que *se convierte en el hardware que querías*.

## Dos clases de nombres en VHDL

VHDL tiene **señales** (declaradas a nivel de arquitectura con `signal`) y **variables** (declaradas dentro de un proceso con `variable`). Ambas guardan valores. Ambas se pueden asignar. **No** son intercambiables.

| | señal | variable |
|---|---|---|
| Se declara en | arquitectura (o paquete) | dentro de un proceso/subprograma |
| Operador de asignación | `<=` | `:=` |
| Toma efecto | al final del paso de simulación | de inmediato |
| Ámbito | compartido en toda la arquitectura | local a su proceso |
| Modela | un cable o un registro | un cálculo temporal |

## La regla de oro

- **Las asignaciones de señal se *agendan*; el valor aparece al final del paso de simulación actual.**
- **Las asignaciones de variable ocurren de inmediato, justo donde está la línea.**

Eso es todo. Cada tropiezo con señales frente a variables es una reformulación de esta regla.

## La demostración

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- agenda: pon s en 1 (aún no visible)
      s <= 2;           -- agenda: pon s en 2 (sobrescribe la primera)
      -- ¿cuánto vale s aquí?  Todavía 0. Ninguna asignación ha tomado efecto.
    end if;
  end process;
end architecture demo;
```

Cuando se completa el flanco de reloj, `s` queda en **2** — cuando una señal se agenda varias veces en un mismo paso del proceso, solo gana la *última* asignación. Dentro del proceso, después de ambas asignaciones, `s` sigue siendo `0`, porque el nuevo valor no se propaga hasta que el proceso termina.

Ahora con una variable:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- la variable vive dentro del proceso
  begin
    if rising_edge(clk) then
      v := 1;           -- toma efecto de inmediato
      v := 2;           -- toma efecto de inmediato
      -- v vale 2 aquí, ahora mismo, durante este paso
    end if;
  end process;
end architecture demo;
```

Fíjate en dos cosas:

- Las variables usan `:=`; las señales usan `<=`.
- Las variables son locales a un solo proceso — no puedes leer `v` desde otro proceso ni desde una asignación concurrente.

> Un matiz sobre el valor inicial: `variable v : integer := 0;` ejecuta su `:= 0` solo **una vez**, cuando arranca la simulación — no en cada paso del proceso. Una variable conserva su valor entre pasos salvo que la reasignes. Eso es lo que la hace capaz de *recordar*, y la razón por la que una variable leída antes de escribirse dentro de un proceso con reloj puede inferir un registro sin querer.

## Por qué esto importa para el hardware

Una señal **modela un cable o un flip-flop**. No puedes cambiar el valor de un cable dos veces en el mismo instante — sea lo que sea que maneje el cable durante un paso, se estabiliza en un valor al final de ese paso.

Una variable **modela un cálculo temporal** dentro de un proceso. El sintetizador típicamente la implementa como lógica combinacional que alimenta (o sale de) los registros — o la elimina por completo.

## Un ejemplo resuelto: contar bits en alto

Las variables brillan cuando necesitas un resultado intermedio que vas a actualizar dentro de un bucle:

```vhdl
process(clk)
  variable ones : integer range 0 to 8;
begin
  if rising_edge(clk) then
    ones := 0;                      -- reinicia el acumulador en cada paso
    for i in data'range loop
      if data(i) = '1' then
        ones := ones + 1;           -- actualización inmediata, se relee en la siguiente iteración
      end if;
    end loop;
    popcount <= ones;               -- publica el conteo final en una señal
  end if;
end process;
```

Intenta esto con una señal en vez de una variable y se rompe: `ones <= ones + 1` dentro del bucle agendaría ocho asignaciones que compiten, solo sobrevive la última, y siempre obtendrías `0` o `1`. El bucle necesita la semántica *inmediata* de una variable.

## Cuándo usar cuál

- **Señales** — por defecto. Cada puerto, cada registro, cada bus que vas a ver en el visor de formas de onda: señal.
- **Variables** — para contadores de bucle locales o resultados aritméticos intermedios dentro de un proceso, donde quieres semántica tipo C, de leer-lo-que-acabas-de-escribir.

## Una trampa común

Algunos estudiantes escriben:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- registro 1
    b <= a;    -- registro 2
  end if;
end process;
```

Les preocupa que no intercambie `a` y `b`. Como las asignaciones de señal se agendan, **sí** las intercambia: en el flanco, `a` toma el valor **viejo** de `b`, y `b` toma el valor **viejo** de `a`, simultáneamente. Esta es, de hecho, la forma idiomática de intercambiar dos registros en un ciclo.

Pero cambia a variables y la semántica cambia:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a ahora vale b
    b := a;    -- b toma el NUEVO a — así que b conserva su valor viejo y a cambia
  end if;
end process;
```

No hay intercambio. Ambos terminan iguales al viejo `b`. Código de apariencia idéntica, resultado opuesto — todo por la regla de oro.

## Qué recordar

- `señal`: el valor se agenda, aparece al final del paso; asignación `<=`; modela cables y registros; compartida en toda la arquitectura.
- `variable`: se actualiza de inmediato; asignación `:=`; local a un proceso; modela cálculo intermedio.
- Varias asignaciones de señal en un paso → gana la última. Varias asignaciones de variable → se ejecutan todas, en orden.
- En la duda — usa una señal.
