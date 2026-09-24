# Sistemas numéricos: bits, hex, complemento a dos

Los circuitos digitales no conocen el "siete" — conocen filas de interruptores que están encendidos o apagados. Para diseñar con ellos debes moverte con soltura entre la representación que prefieren los humanos (decimal) y la que entiende el silicio (binario), con el hexadecimal como atajo útil en el medio.

## Binario (base 2)

Un **bit** es un valor encendido/apagado, que escribimos `0` o `1`. Un **byte** son ocho bits.

Los bits se apilan y codifican enteros posicionalmente, igual que en decimal — cada posición es una potencia de dos:

```
  1 0 1 1 0
  │ │ │ │ └── 2⁰ = 1
  │ │ │ └──── 2¹ = 2
  │ │ └────── 2² = 0
  │ └──────── 2³ = 8
  └────────── 2⁴ = 16

  16 + 0 + 0 + 2 + 0 = 18
```

Entonces `10110₂ = 18₁₀`.

## Hexadecimal (base 16)

Escribir 32 bits en binario produce una sopa de 32 caracteres imposible de revisar:

```
00000000 00000000 11001010 11111110
```

Hex agrupa los bits de cuatro en cuatro. Cada dígito hex cubre exactamente un nibble (4 bits), así que 32 bits se convierten en una cadena de 8 caracteres:

```
0x0000CAFE
```

La correspondencia es fija:

| bin  | hex | bin  | hex |
|------|-----|------|-----|
| 0000 | 0   | 1000 | 8   |
| 0001 | 1   | 1001 | 9   |
| 0010 | 2   | 1010 | A   |
| 0011 | 3   | 1011 | B   |
| 0100 | 4   | 1100 | C   |
| 0101 | 5   | 1101 | D   |
| 0110 | 6   | 1110 | E   |
| 0111 | 7   | 1111 | F   |

En VHDL escribirás literales hex para vectores cuyo tamaño sea múltiplo de cuatro:

```vhdl
signal addr : std_logic_vector(7 downto 0) := x"CA";  -- 8 bits: 11001010
```

## Complemento a dos (enteros con signo)

El binario tal como lo mostramos solo codifica números **no negativos**. Para meter también los negativos en el mismo patrón de bits, los diseñadores digitales usan **complemento a dos**:

1. Escribe el número positivo en binario.
2. Invierte cada bit.
3. Suma 1.

Para representar `-5` en 8 bits: `+5 = 00000101`, invertir → `11111010`, sumar 1 → `11111011`.

¿Por qué este esquema? Porque la suma funciona igual para números con o sin signo — no necesitas dos sumadores. `-5 + 7` en complemento a dos de 8 bits:

```
  11111011   (-5)
+ 00000111   ( 7)
-----------
 100000010
```

El noveno bit se desborda, y los 8 de abajo son `00000010 = 2`. Que es la respuesta correcta.

## Qué recordar

- Los bits son los átomos; los bytes, grupos de 8.
- Binario y hex son **el mismo número** con otra piel. Hex es más fácil de leer.
- El complemento a dos codifica negativos invirtiendo y sumando uno — por eso `A + B` en tu ALU funciona sin importar el signo.
