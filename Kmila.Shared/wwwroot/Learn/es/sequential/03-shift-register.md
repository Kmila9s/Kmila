# Ejemplo: registro de desplazamiento de 8 bits

Un registro de desplazamiento es una fila de flip-flops encadenados. Cada flanco de reloj desplaza los bits almacenados una posición, y un bit nuevo entra por un extremo. Son el núcleo de los UARTs, los controladores SPI, los motores de CRC y los generadores de números aleatorios basados en LFSR — el premio práctico de los flip-flops de la lección anterior.

## Qué hace este ejemplo

`shift8` es un registro de desplazamiento **entrada serial, salida paralela** (SIPO):

- `din` es el bit nuevo que llega en cada flanco de subida del reloj.
- `r` es el registro interno de 8 bits.
- `q` expone `r` al exterior como un byte paralelo.

"Entrada serial" porque los bits entran uno a uno; "salida paralela" porque puedes leer los ocho de una vez. Eso es exactamente lo que hace un receptor UART: mete los bits uno por uno desde el cable, y luego entrega el byte armado al resto del sistema.

## El cuerpo

```vhdl
signal r : std_logic_vector(7 downto 0) := (others => '0');
...
process(clk, reset)
begin
  if reset = '1' then
    r <= (others => '0');
  elsif rising_edge(clk) then
    r <= r(6 downto 0) & din;
  end if;
end process;

q <= r;
```

La magia está en esta línea:

```vhdl
r <= r(6 downto 0) & din;
```

`&` es el operador de **concatenación**. `r(6 downto 0)` son los 7 bits bajos del registro actual. Pegamos `din` al extremo derecho, formando un nuevo valor de 8 bits, y lo asignamos de vuelta a `r`. Como esto es una asignación de señal dentro de un proceso con reloj, el desplazamiento ocurre en cada flanco de subida — y como las señales se actualizan al final del paso, el lado derecho usa el `r` *viejo*, que es justo lo que un desplazamiento necesita.

## No son más que ocho flip-flops en cadena

No dejes que el cuerpo de una sola línea te esconda el hardware. `r` son ocho flip-flops D. La asignación cablea la salida de cada flip-flop a la entrada del siguiente, con `din` alimentando al primero y el bit 7 cayéndose del extremo (descartado aquí). Esa cadena *es* el registro de desplazamiento — la concatenación es solo una forma compacta de describir el cableado.

## Recorrido por la forma de onda

La configuración de simulación mueve `din` por un patrón: `1, 0, 1, 1, 0, 1, 0, 1`. Observa `q` con el tiempo:

- Después del primer tick: `q = 00000001`
- Después del segundo: `q = 00000010`
- Después del tercero: `q = 00000101`
- …y así sucesivamente. El bit nuevo cae en el LSB; los bits viejos marchan hacia el MSB.

Si alejas el zoom verás que el primer valor de `din` llega al bit de arriba (bit 7) exactamente 8 ciclos de reloj después de haber llegado. Esa es la latencia de ida y vuelta de un registro de desplazamiento de 8 bits — y la razón por la que un enlace serial tiene un retraso fijo y predecible.

## Un tropiezo: desplazar en la dirección equivocada

Los principiantes a menudo invierten la dirección y pierden un bit. Si escribes `r <= r(7 downto 1) & din;` has conservado los *siete bits de arriba* y soltado el bit 0 — eso es un desplazamiento a la *derecha*, no a la izquierda. Decide por qué extremo entran los datos nuevos y por cuál se caen, y luego haz que la rebanada coincida. Las dos formas correctas son:

```vhdl
r <= r(6 downto 0) & din;   -- desplaza a la IZQUIERDA: din -> LSB, se descarta el MSB
r <= din & r(7 downto 1);   -- desplaza a la DERECHA: din -> MSB, se descarta el LSB
```

## Tu turno

- **Desplaza a la derecha en su lugar.** Usa la segunda forma de arriba y confirma que el bit caminante ahora se mueve en el otro sentido.
- **Carga paralela (PISO/universal).** Agrega una entrada `load` y un bus `din_par`. Cuando `load = '1'` en un flanco, `r <= din_par` (toma todo el bus de una vez); si no, desplaza. Ese es el corazón de un transmisor *entrada paralela, salida serial*.
- **Salida serial.** Agrega `dout <= r(7);`. Ahora es entrada serial, salida serial (SISO) — el primer bit que enviaste en `din` aparece en `dout` ocho ciclos después, el retraso del que depende la RX de un UART.
- **Haz un LFSR.** Realimenta `r(7) xor r(5) xor r(4) xor r(3)` hacia la entrada en vez de `din`. Ahora tienes un generador de secuencia pseudoaleatoria — la misma estructura usada para CRCs y patrones de prueba.

## Qué recordar

- Un registro de desplazamiento es una cadena de flip-flops D; cada flanco mueve cada bit un lugar y admite un bit nuevo por un extremo.
- `r <= r(6 downto 0) & din;` desplaza a la izquierda (bit nuevo en el LSB); intercambia la rebanada y el orden para desplazar a la derecha.
- Agregar una carga paralela lo convierte en un convertidor SIPO/PISO — el núcleo de los enlaces seriales como UART y SPI.
- Realimenta derivaciones (taps) seleccionadas a través de un XOR y se vuelve un LFSR para CRCs y secuencias pseudoaleatorias.
