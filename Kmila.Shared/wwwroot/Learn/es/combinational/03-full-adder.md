# Ejemplo: sumador por acarreo serie de 4 bits

Sumar dos números binarios es el "hola mundo" de la aritmética digital. Este ejemplo construye un sumador de 4 bits con acarreo de entrada y acarreo de salida — justo la celda que se teselaría dentro de una ALU más grande o dentro del incremento de direcciones de un CPU.

## Interfaz

```
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## El cuerpo

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

Qué está pasando:

- `unsigned(a)` y `unsigned(b)` convierten los vectores a tipos numéricos para usar `+`.
- `resize(…, 5)` expande a 5 bits para que la suma pueda desbordarse al bit superior.
- `("0000" & cin)` construye una máscara de 5 bits con `cin` en el LSB.
- Tras sumar, `t(3 downto 0)` es la suma y `t(4)` es el acarreo de salida.

## ¿Por qué 5 bits?

Sumar dos números de 4 bits más un acarreo de entrada puede producir un resultado tan grande como `1111 + 1111 + 1 = 11111` — cinco bits. Si haces la cuenta en 4 bits pierdes el bit superior silenciosamente. `resize` es el idiom seguro.

## Qué hace el ejemplo

La configuración de simulación mueve `a` y `b` por cuatro patrones (`3+5=8`, luego `10+6=16` que genera acarreo, luego `10+15=25`…). La forma de onda muestra `sum` y `cout` cambiando combinacionalmente — no hace falta reloj, este diseño no tiene registros.

## Tu turno

- **Hazlo paramétrico.** Agrega `generic (N : integer := 4)` a la entidad y usa `N` donde aparezca el 4. Ya tienes un `adder_N` para instanciar a cualquier ancho.
- **Constrúyelo como restador.** Invierte `b` y suma `+1` más — eso es resta en complemento a dos. Ni un gate extra aparte de un NOT por bit.
- **Conéctalos.** Pon uno para el nibble bajo y otro para el alto, y tienes un sumador de 8 bits con acarreo serie.
