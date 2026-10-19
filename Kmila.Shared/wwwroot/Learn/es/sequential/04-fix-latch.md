# Arréglalo: el latch accidental

Este es uno de los dos o tres bugs que todo estudiante de VHDL escribe al menos una vez. El código parece correcto sobre el papel, compila sin errores, simula con una salida plausible — y cuando lo sintetizas, la herramienta te avisa que infirió un **latch** que nunca pediste.

La ironía es que la Unidad IV te enseña a construir latches (SR, D) *a propósito*. La habilidad aquí es reconocer cuándo has construido uno **por accidente**, dentro de lo que se suponía que era lógica puramente combinacional.

## El inicio

```vhdl
architecture rtl of latch_bug is
begin
  process(enable, a, b)
  begin
    if enable = '1' then
      y <= a and b;
    end if;
    -- Falta: ¿qué debe valer y cuando enable = '0'?
  end process;
end architecture rtl;
```

## Qué sale mal

En un proceso puramente combinacional, cada salida debe quedar asignada **en cada camino** por el proceso. Este solo asigna `y` cuando `enable = '1'`. Cuando `enable = '0'`, el proceso no dice nada sobre `y`.

La respuesta de VHDL a "no me dijiste qué hacer con esta señal" es: **conserva el valor anterior**. El único hardware que puede conservar un valor anterior es un elemento de memoria — así que la síntesis infiere un **latch transparente**. Eso es exactamente un latch D: mientras `enable = '1'` la salida sigue a `a and b`; cuando `enable` baja, el último valor queda congelado. Pediste una compuerta y obtuviste un latch con enable.

## Por qué un latch accidental es malo

No es que los latches sean malvados — es que uno *no intencional* arruina tu diseño:

- **El timing es difícil de cerrar.** El análisis estático de tiempos batalla con los latches transparentes; la mayoría de los flujos de FPGA asumen flip-flops disparados por flanco y tratan los latches inferidos como una advertencia por buenas razones.
- **Los glitches pasan de largo.** Mientras es transparente, cualquier glitch en `a` o `b` fluye directo a `y` y puede quedar enganchado.
- **Riesgo de discrepancia sim/síntesis.** El comportamiento depende de una cobertura de asignaciones sutil que es fácil de equivocar.

Así que cuando la herramienta dice "latch inferido para la señal `y`", trátalo como un error que corregir, no como una advertencia que ignorar.

## Tu misión

Abre el ejemplo y haz el diseño **puramente combinacional** — sin latch, sin flip-flop, solo compuertas. Dos arreglos idiomáticos:

**Arreglo 1: asignación por defecto arriba**

```vhdl
process(enable, a, b)
begin
  y <= '0';                       -- por defecto: cubre todos los caminos
  if enable = '1' then
    y <= a and b;
  end if;
end process;
```

Una señal puede asignarse varias veces en un mismo paso del proceso — solo toma efecto la última que se ejecuta. El valor por defecto de arriba significa "a menos que algo de abajo me sobrescriba, sé `'0'`". Este es el hábito que escala: primero pon los valores por defecto, luego sobrescribe.

**Arreglo 2: completar el `if`**

```vhdl
process(enable, a, b)
begin
  if enable = '1' then
    y <= a and b;
  else
    y <= '0';                     -- la rama que faltaba
  end if;
end process;
```

Ambos son correctos. Ambos producen hardware idéntico (una AND de 2 entradas gobernada por `enable`, salida `'0'` cuando está deshabilitada). Elige el que se lea mejor.

## La misma trampa en un `case`

Los latches no son solo un problema de `if`. Un `case` incompleto, o una rama que olvida una salida, también infiere un latch:

```vhdl
-- BUG: 'y' queda sin asignar para sel = "10" y "11"
case sel is
  when "00" => y <= a;
  when "01" => y <= b;
  when others => null;   -- null no asigna nada -> latch en y
end case;
```

La misma cura: dale a `y` un valor por defecto antes del `case`, o asígnala en el `when others` (y en cada rama). `process(all)` te ayuda a evitar el error de la *lista de sensibilidad*, pero **no** te salva del error de cobertura — todavía tienes que asignar cada salida en cada camino.

## Verifica el arreglo

Después de tu arreglo, ejecuta la simulación. En la forma de onda:

- Para `15–25 µs` (cuando `enable = '1'` y `a = b = '1'`): `y = '1'`.
- En cualquier otro momento: `y = '0'`.

Antes del arreglo, `y` se quedaría en `'1'` cuando `enable` bajara de vuelta a `'0'` — eso es el latch reteniendo su valor anterior, justo lo que intentas evitar.

## Qué recordar

- En un proceso combinacional, **cada salida debe quedar asignada en cada camino** — un `if`/`case` que se salta un camino infiere un latch transparente.
- Una asignación por defecto al inicio del proceso es la forma más fácil de garantizar la cobertura.
- Un latch *accidental* causa problemas de timing y de glitches; uno *intencional* (latch D/SR) es una decisión de diseño deliberada de la Unidad IV.
- `process(all)` arregla las listas de sensibilidad, no la cobertura de asignaciones — todavía tienes que asignar cada salida.
- Si de verdad quieres memoria, no dejes que se cuele — múdate a un proceso con reloj y hazte cargo de la decisión.
