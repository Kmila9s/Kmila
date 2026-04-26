# Álgebra booleana y las leyes que importan

El álgebra booleana es la matemática de los valores verdadero/falso y las operaciones que puedes hacer sobre ellos. Todo circuito digital — desde una compuerta hasta un CPU de millones de transistores — es, al final, una expresión booleana.

La buena noticia: solo necesitas memorizar unas pocas identidades. Con esas puedes simplificar a mano casi cualquier expresión.

## Las tres operaciones básicas

| Op  | Símbolo (mat.) | Símbolo (VHDL) | Se lee |
|-----|----------------|----------------|--------|
| AND | · o yuxtaposición | `and` | "A y B" |
| OR  | +                  | `or`  | "A o B" |
| NOT | barra superior (`Ā`) | `not` | "no A" |

Tablas de verdad para los operadores binarios:

```
 A  B  │  A·B   A+B
──────┼────────────
 0  0  │   0     0
 0  1  │   0     1
 1  0  │   0     1
 1  1  │   1     1
```

## Las identidades que realmente usarás

**Conmutatividad** — el orden no importa:

```
A · B = B · A
A + B = B + A
```

**Asociatividad** — los paréntesis no importan:

```
(A · B) · C = A · (B · C)
(A + B) + C = A + (B + C)
```

**Distributividad** — la que se ve rara en la versión con OR:

```
A · (B + C) = A·B + A·C
A + (B · C) = (A + B) · (A + C)   ← sí, es cierto
```

**Identidad y dominancia**:

```
A + 0 = A      A · 1 = A
A + 1 = 1      A · 0 = 0
```

**Complemento**:

```
A + Ā = 1      A · Ā = 0
```

**Leyes de De Morgan** — las dos reescrituras más útiles del diseño digital:

```
not(A · B) = Ā + B̄
not(A + B) = Ā · B̄
```

De Morgan te permite convertir un NAND en "OR de complementos", un NOR en "AND de complementos", o eliminar barras NOT largas. Todo toolchain de FPGA las usa constantemente al optimizar.

## Una simplificación paso a paso

Partimos de:

```
Y = A·B + A·B̄·C + A·B̄·C̄
```

Factorizamos `A`:

```
Y = A · (B + B̄·C + B̄·C̄)
```

Factorizamos `B̄` dentro:

```
Y = A · (B + B̄ · (C + C̄))
```

Como `C + C̄ = 1` (complemento):

```
Y = A · (B + B̄ · 1)
  = A · (B + B̄)
  = A · 1
  = A
```

Cuatro términos con cinco operaciones AND/OR se colapsan a un cable. Este es el trabajo que hace el sintetizador por ti — pero entenderlo te vuelve más hábil para leer un netlist.

## Qué recordar

- AND / OR / NOT es todo lo que necesitas. NAND y NOR son AND/OR con un NOT encima.
- De Morgan es la navaja suiza — reconócela en ambas direcciones.
- Cuando una subexpresión vale `1` por complemento, todo el producto se simplifica.
