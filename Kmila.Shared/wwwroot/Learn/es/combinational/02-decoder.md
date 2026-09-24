# Ejemplo: decodificador 3 a 8

Un decodificador toma un código binario pequeño y lo convierte en una salida **one-hot**: exactamente una salida está en alto, y su posición coincide con el código.

```
  sel  │  y
  000  │  00000001
  001  │  00000010
  010  │  00000100
  011  │  00001000
  100  │  00010000
  101  │  00100000
  110  │  01000000
  111  │  10000000
```

Los decodificadores viven dentro de todo sistema de memoria — las líneas de dirección manejan un decodificador, y la salida "fila seleccionada" habilita exactamente una fila. Cada vez que oyes "decodificador de direcciones", esto es lo que hay debajo.

## El VHDL

Un decodificador es el caso de libro de texto para `with/select`:

```vhdl
with sel select
  y <=
    "00000001" when "000",
    "00000010" when "001",
    "00000100" when "010",
    "00001000" when "011",
    "00010000" when "100",
    "00100000" when "101",
    "01000000" when "110",
    "10000000" when others;
```

Nota el `when others` al final. `sel` es un `std_logic_vector(2 downto 0)` — 3 bits — así que hay 2³ = 8 combinaciones binarias, pero como `std_logic` tiene nueve estados, el compilador necesita la cláusula `others` para cubrir todo (se sintetiza igual que `"111"`).

## Qué hace el ejemplo

La configuración hace pasar `sel` por los ocho valores de 3 bits, 10 µs cada uno. En la pestaña de formas de onda verás los 8 bits de `y` convertirse en un `'1'` caminante — una "luz corrediza" digital.

## Tu turno

Cuando corra:

- Amplíalo a un decodificador 4 a 16. Puedes hacerlo a mano o reescribir el cuerpo como `y <= std_logic_vector(shift_left(to_unsigned(1, 16), to_integer(unsigned(sel))));`. Ambos se sintetizan igual — elige el que te parezca más legible.
- Agrega una entrada `enable`. Cuando `enable = '0'`, todas las salidas van a cero sin importar `sel`.
