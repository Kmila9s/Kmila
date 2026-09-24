# Arréglalo: el latch accidental

Es uno de los dos o tres errores que todo estudiante de VHDL comete al menos una vez. El código parece correcto sobre papel, compila sin errores, simula con una salida plausible — y al sintetizarlo, la herramienta te avisa que infirió un **latch** que nunca pediste.

## El inicio

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- Falta: ¿qué debe ser y cuando enable = '0'?
  end process;
end architecture rtl;
```

## Qué sale mal

En un proceso puramente combinacional, cada salida debe quedar asignada **en cada camino** por el proceso. Este solo asigna `y` cuando `enable = '1'`. Cuando `enable = '0'`, el proceso no dice nada sobre `y`.

La solución por defecto de VHDL a "no me dijiste qué hacer con esta señal" es **recordar el valor anterior**. Eso es un latch — un elemento de memoria transparente. La síntesis inferirá uno, tu diseño tendrá una pesadilla de timing y la placa glitchará en transiciones.

## Tu misión

Abre el ejemplo y haz el diseño **puramente combinacional** — sin latch, sin flip-flop, solo compuertas. Dos arreglos idiomáticos:

**Arreglo 1: asignación por defecto arriba**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- por defecto
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Una señal puede asignarse varias veces en un proceso — solo la última toma efecto. El "por defecto" arriba dice "a menos que algo me sobrescriba, soy `'0'`".

**Arreglo 2: completar el `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';
  end if;
end process;
```

Ambos son correctos. Ambos producen el mismo hardware. Elige el que se lea mejor.

## Verifica el arreglo

Después, corre la simulación. En la forma de onda:

- Para `15–25 µs` (cuando `enable = '1'` y `a=b='1'`): `y = '1'`.
- En cualquier otro momento: `y = '0'`.

Antes del arreglo, `y` se quedaría en `'1'` al bajar `enable` a `'0'` — eso es el latch reteniendo su valor previo, justo lo que intentas evitar.

## La regla

En un proceso combinacional, **cada salida debe quedar asignada en cada camino**. Si no puedes cumplirlo, o agregas un valor por defecto, o en realidad querías un flip-flop — en cuyo caso muévete a un proceso con reloj y hazte cargo de esa decisión.
