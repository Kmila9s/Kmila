# Signal vs. variable

Es la distinción peor entendida de VHDL. Los estudiantes leen "una variable es como una variable local en C" y salen corriendo en la dirección equivocada. Aclarémoslo.

## Dos clases de nombres en VHDL

VHDL tiene **signals** (declaradas a nivel de arquitectura con `signal`) y **variables** (declaradas dentro de un proceso con `variable`). Ambas guardan valores. Ambas se asignan. **No son intercambiables.**

## La regla de oro

- **Las asignaciones de signal toman efecto al final del tick de simulación actual.**
- **Las asignaciones de variable toman efecto inmediatamente, dentro del proceso.**

Eso es todo. Cada "gotcha" de signals vs variables es una versión distinta de esa regla.

## La demostración

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- petición: pon s en 1 (aún no visible)
      s <= 2;           -- petición: pon s en 2 (sobrescribe)
      -- ¿cuánto vale s aquí?  Todavía 0.
    end if;
  end process;
end architecture demo;
```

Cuando termina el flanco de reloj, `s` queda en **2** (solo la última asignación "gana" cuando varias ocurren en el mismo tick). Dentro del proceso, después de las dos asignaciones, `s` sigue siendo `0` — porque el nuevo valor no se propaga hasta que el proceso termina.

Ahora con una variable:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- la variable vive dentro del proceso
  begin
    if rising_edge(clk) then
      v := 1;           -- toma efecto inmediatamente
      v := 2;           -- toma efecto inmediatamente
      -- v vale 2 aquí, ahora, durante este tick
    end if;
  end process;
end architecture demo;
```

Dos detalles:

- Las variables usan `:=`; las signals usan `<=`.
- Las variables son locales a un solo proceso.

## Por qué importa para el hardware

Una signal **modela un cable o un flip-flop**. No puedes cambiar el valor de un cable dos veces en el mismo instante — sea lo que sea que maneje el cable durante un tick, se estabiliza en un valor al final del tick.

Una variable **modela un cálculo temporal** dentro de un proceso. El sintetizador puede implementarla como lógica combinacional, o eliminarla por completo.

## Cuándo usar cuál

- **Signals** — por defecto. Cada puerto, cada registro, cada bus que vas a ver en el visor: signal.
- **Variables** — para contadores de bucle locales o resultados aritméticos intermedios dentro de un proceso, donde quieres semántica tipo C dentro de ese proceso.

## Una trampa común

Muchos estudiantes escriben:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- registro 1
    b <= a;    -- registro 2 — "intercambia" a y b
  end if;
end process;
```

Esperan que esto intercambie `a` y `b`. Y como las signals se actualizan al final del tick, **sí lo hace**: en el flanco, `a` toma el valor **viejo** de `b`, y `b` toma el valor **viejo** de `a`. Es el idiomatic way de intercambiar dos registros en un ciclo.

Pero si cambias a variables la semántica cambia:

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

No hay intercambio. Ambos terminan con el viejo `b`.

## Qué recordar

- `signal`: se actualiza al final del tick. Asignación `<=`. Modela cables y registros.
- `variable`: se actualiza inmediatamente. Asignación `:=`. Local a un proceso. Modela cálculo intermedio.
- En la duda — usa una signal.
