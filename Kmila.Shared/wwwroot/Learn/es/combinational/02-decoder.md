# Ejemplo: decodificador 3 a 8

Un decodificador toma un código binario pequeño y lo convierte en una salida **one-hot**: exactamente una línea de salida está en alto, y su posición coincide con el código. Es uno de los circuitos con nombre propio de la unidad combinacional ("codificador y decodificador"), y es la pareja natural del codificador, que hace lo inverso.

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

Los decodificadores viven dentro de todo sistema de memoria — las líneas de dirección manejan un decodificador, y la salida "fila coincidente" habilita exactamente una línea de palabra (word line). Cada vez que oyes "decodificador de direcciones", esto es lo que hay debajo. La misma estructura también implementa un conjunto completo de minterms: un decodificador de n entradas emite los 2ⁿ minterms de sus entradas, así que cualquier función booleana se puede construir haciendo OR de las salidas correctas del decodificador.

## El VHDL

Un decodificador es el caso de libro de texto para `with/select`:

```vhdl
with sel select
  y <= "00000001" when "000",
       "00000010" when "001",
       "00000100" when "010",
       "00001000" when "011",
       "00010000" when "100",
       "00100000" when "101",
       "01000000" when "110",
       "10000000" when others;
```

Nota el `when others` al final. `sel` es un `std_logic_vector(2 downto 0)` — 3 bits — así que hay 2³ = 8 valores *pretendidos*, pero como cada bit es un `std_logic` de nueve valores el compilador ve muchos más patrones posibles y necesita `others` para ser exhaustivo. Aquí `others` se funde con el caso `"111"` (se sintetiza a la misma salida).

## La alternativa escalable

Escribir ocho renglones está bien para un decodificador 3 a 8, pero no escala. La misma función en una sola línea, usando un desplazamiento, es:

```vhdl
y <= std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Léelo de adentro hacia afuera: toma `sel` como número, desplaza un solo `'1'` a la izquierda esa cantidad de posiciones dentro de un campo de 8 bits, y castea de vuelta a `std_logic_vector`. Ambas versiones se sintetizan idéntico — elige la que te parezca más legible. (La tabla explícita es más fácil de verificar contra la tabla de verdad para un principiante; la forma con desplazamiento es la que ampliarías a 32 o 64 salidas.)

## Qué hace el ejemplo

El ejemplo hace pasar `sel` por los ocho valores de 3 bits, 10 µs por valor. En la pestaña de formas de onda verás los 8 bits de `y` convertirse en un `'1'` caminante — una "luz corrediza" digital.

## Agrega un enable (y conoce el demux)

Los decodificadores reales tienen un **enable**. Cuando están deshabilitados, todas las salidas están en bajo:

```vhdl
y <= (others => '0') when enable = '0' else
     std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Aquí hay una conexión bonita: un decodificador-con-enable, donde el enable es una línea de *datos* en vez de una constante, **es un demultiplexor** — el valor del enable se enruta a la única salida que escoge la dirección, y el resto se queda en `'0'`. El decodificador y el demux son el mismo hardware con etiquetas distintas.

## Tu turno

Cuando corra:

- Amplíalo a un decodificador **4 a 16**. Hazlo a mano (dieciséis renglones) y luego con la forma de desplazamiento — nota lo poco que tiene que cambiar la forma de desplazamiento (solo los dos `16`).
- Agrega el `enable` de arriba y confirma en la forma de onda que toda salida cae a `'0'` mientras `enable = '0'`.
- Construye el inverso: un **codificador con prioridad 8 a 3** que emita el índice de la entrada más alta que esté en alto. Esa es la otra mitad de esta sección del curso.

## Qué recordar

- Un decodificador mapea un código de n bits a una salida one-hot de 2ⁿ; sus salidas son los minterms de las entradas.
- `with/select` con un `when others` obligatorio es la forma clara, con forma de tabla de verdad, de escribirlo.
- El idiom `shift_left(to_unsigned(1, N), …)` es la versión que escala a cualquier ancho.
- Un decodificador con un enable manejado por datos es un demultiplexor; su imagen en espejo es el codificador.
