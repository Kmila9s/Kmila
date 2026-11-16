# Listas de sensibilidad y ciclos delta

Ya has visto que las señales no se actualizan hasta que "termina el ciclo delta". Ahora conoceremos la trampa clásica que sale de esa regla.

Una **lista de sensibilidad** son los `( … )` después de `process`. Le dice al simulador: *solo despierta este proceso cuando alguna de estas señales cambie*.

Un **ciclo delta** es la unidad más pequeña de tiempo de simulación. Dentro de un ciclo delta, las asignaciones de señal solo **registran** nuevos valores — los valores en sí no aparecen hasta que termina el delta. Durante ese tiempo, leer la señal te da su valor **viejo**.

Junta esas dos reglas y obtienes la trampa de abajo. El reproductor corre un proceso minúsculo con dos asignaciones de señal:

```vhdl
process(a, b)
begin
  out1 <= a;
  out2 <= b and out1;   -- ← ¿qué lee esto?
end process;
```

Parece inofensivo. El estudiante suele pensar: "La línea 3 pone `out1 = a`, luego la línea 4 lee `out1` y lo combina con AND con `b`". Pero la línea 4 lee el **viejo** `out1` — la transacción de la línea 3 aún no se ha confirmado.

Mira el reproductor y verás que `out2` sale como `U` aunque `a` y `b` sean ambos `1`. Sorprendente la primera vez.

<!-- exec-fig2:start -->
## Los timbres

Un proceso es un cuarto cuyo trabajador **duerme** hasta que suena un timbre. La **lista de sensibilidad** decide qué señales tienen timbre.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-doorbell.html?mode=bug" title="Un proceso dormido que despiertan timbres conectados a su lista de sensibilidad" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- Con `process(a, b)`, `out1` **no tiene timbre** (su cable termina en un enchufe vacío ✕). Cuando corre la línea 4, el trabajador lee `out1` y ve su valor **viejo** U. Después `out1` pasa a 1, pero nadie despierta, así que `out2` se queda en U.
- Haz clic en el botón `process(a, b, out1)`: ahora suena el timbre de `out1`, el proceso corre una segunda vez y `out2` pasa a 1. Es la opción B de abajo.
<!-- exec-fig2:end -->

## Las dos formas de arreglarlo

### Opción A — usa una variable

Las variables se actualizan inmediatamente, así que leer `tmp` en la siguiente línea da el valor recién asignado:

```vhdl
process(a, b)
  variable tmp : std_logic;
begin
  tmp  := a;
  out1 <= tmp;
  out2 <= b and tmp;   -- lee el NUEVO tmp — funciona.
end process;
```

### Opción B — espera al segundo delta

Agrega `out1` a la lista de sensibilidad. Después de que el primer delta confirme `out1`, el proceso se despierta una segunda vez y vuelve a correr:

```vhdl
process(a, b, out1)
begin
  out1 <= a;
  out2 <= b and out1;   -- el segundo despertar lee el out1 ya confirmado
end process;
```

Ambas funcionan; la Opción A suele preferirse porque es un delta más rápida y el flujo de datos queda explícito.

> **Prueba esto:** avanza por el reproductor y mira la tabla de señales. Aunque `a=1` y `b=1`, `out2` termina en `U` — no `1`. Esa es la regla del ciclo delta atrapando principiantes cada semestre.
