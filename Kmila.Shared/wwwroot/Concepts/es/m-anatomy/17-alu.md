# ALU — unidad aritmético-lógica

La **ALU** es la calculadora en el corazón de una CPU: toma dos operandos A y B y, según una **selección de operación**, produce A+B, A−B, A AND B, A OR B, y más.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/17-alu.html" title="ALU interactiva con tambor de selección de operación" loading="lazy"></iframe></div>

El tambor de selección del módulo elige la operación; los émbolos fijan **A** y **B**. La lista resalta la operación actual y el lector muestra **A op B = R**. Es puramente combinacional — sin reloj.

En VHDL:

```vhdl
with op select
  r <= a + b     when "00",   -- ADD
       a - b     when "01",   -- SUB
       a and b   when "10",   -- AND
       a or b    when others; -- OR
```

Una ALU es **lógica combinacional** (sumadores, compuertas y MUX que eligen el resultado) sin memoria propia — los registros a su alrededor guardan operandos y resultado. La selección de operación es solo un MUX sobre los resultados candidatos.
