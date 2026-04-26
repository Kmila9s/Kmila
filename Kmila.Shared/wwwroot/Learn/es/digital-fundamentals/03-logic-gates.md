# Las siete compuertas lógicas

Todo circuito digital, hasta una GPU moderna, es una combinación de siete celdas elementales. Si conoces cómo se comporta cada una y cómo se escribe en VHDL, ya empezaste a hacer diseño de hardware.

## Las seis compuertas de dos entradas

| Compuerta | Símbolo en VHDL | Se lee | La salida es `1` cuando… |
|-----------|------------------|--------|---------------------------|
| AND   | `a and b`   | "A y B"           | ambas entradas son 1 |
| OR    | `a or b`    | "A o B"           | al menos una entrada es 1 |
| XOR   | `a xor b`   | "A xor B"         | las entradas son diferentes |
| NAND  | `a nand b`  | "no (A y B)"      | al menos una entrada es 0 |
| NOR   | `a nor b`   | "no (A o B)"      | ambas entradas son 0 |
| XNOR  | `a xnor b`  | "A = B"           | las entradas son iguales |

Y la NOT de una sola entrada:

```vhdl
y <= not a;
```

## Por qué NAND y NOR son especiales

Resultado clásico de la lógica digital: **solo con NAND** (o solo con NOR) puedes construir cualquier otra compuerta. Son "funcionalmente completas". Por eso las librerías CMOS traen muchísimas celdas NAND2 y NOR2 — se sintetiza todo a partir de ellas.

- NOT: `not a = a nand a`
- AND: `a and b = not (a nand b)`
- OR:  `a or b = (not a) nand (not b)`

## XOR es la "matemática"

XOR se dibuja con un círculo-más (⊕). Calcula la "paridad" — su salida es `1` cuando un número impar de entradas es `1`. Por eso está en el corazón de:

- Sumadores: `sum = a xor b xor carry_in`
- Detectores de paridad y códigos de detección de errores
- Flip-flops tipo T: `q_next = q xor toggle_input`

## Ver las seis en un solo archivo

Si abres el laboratorio **explorador de tablas de verdad** de este módulo, encontrarás una entidad VHDL que calcula las seis compuertas de dos entradas de golpe, con `a` y `b` cableadas a la pestaña Puertos para estímulos. El visor de formas de onda muestra cómo reacciona cada salida en cada combinación.

```vhdl
architecture rtl of gates is
begin
  o_and  <= a and b;
  o_or   <= a or  b;
  o_xor  <= a xor b;
  o_nand <= a nand b;
  o_nor  <= a nor  b;
  o_xnor <= a xnor b;
end architecture rtl;
```

Fíjate que cada una de las seis líneas es **un cable**, no una sentencia que se ejecuta. Seis compuertas, seis piezas de hardware — se evalúan a la vez.

## Qué recordar

- Seis compuertas de 2 entradas + NOT es el conjunto completo.
- NAND y NOR son "universales" — puedes construir cualquier cosa con ellas.
- En VHDL, las compuertas son asignaciones concurrentes de una línea. No se "llaman"; existen.
