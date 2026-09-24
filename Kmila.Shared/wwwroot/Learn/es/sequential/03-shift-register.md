# Ejemplo: registro de desplazamiento de 8 bits

Un registro de desplazamiento es una fila de flip-flops en cadena. Cada flanco de reloj desplaza los bits almacenados una posición y entra un bit nuevo por un extremo. Son el corazón de UARTs, controladores SPI, motores CRC y generadores pseudoaleatorios tipo LFSR.

## Qué hace este ejemplo

`shift8` es un **entrada serial, salida paralela**:

- `din` es el bit nuevo que llega en cada flanco de subida.
- `r` es el registro interno de 8 bits.
- `q` expone `r` al exterior como un byte paralelo.

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

`&` es el operador de **concatenación**. `r(6 downto 0)` son los 7 bits bajos del registro actual. Pegamos `din` al extremo derecho, formamos un nuevo valor de 8 bits y lo asignamos de vuelta a `r`.

Como esto es una asignación de signal dentro de un proceso con reloj, el desplazamiento ocurre en cada flanco.

## Recorrido por la forma de onda

La configuración mueve `din` por un patrón: `1, 0, 1, 1, 0, 1, 0, 1`. Observa `q` con el tiempo:

- Después del primer tick: `q = 00000001`
- Del segundo: `q = 00000010`
- Del tercero: `q = 00000101`
- …y así. El bit nuevo cae en el LSB; los viejos marchan hacia el MSB.

Si alejas el zoom verás que el bit de arriba (bit 7) toma el primer valor de `din` exactamente 8 ciclos después de haber llegado. Esa es la latencia de ida y vuelta del registro.

## Tu turno

- **Desplaza a la derecha.** Intercambia la concatenación: `r <= din & r(7 downto 1);`.
- **Carga paralela.** Agrega una entrada `load` y un bus `din_parallel`. Cuando `load = '1'` en el flanco, el registro toma todo el bus en lugar de desplazar.
- **Salida serial.** Agrega `dout <= r(7);`. Ahora es entrada serial / salida serial (SISO) — el primer bit que enviaste en `din` aparece en `dout` ocho ciclos después. Así funciona el retraso bit a bit en la RX de UART.
