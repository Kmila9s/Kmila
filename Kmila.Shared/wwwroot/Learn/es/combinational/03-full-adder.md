# Ejemplo: sumador por acarreo serie de 4 bits

Sumar dos números binarios es el "hola mundo" de la aritmética digital. Este ejemplo construye un sumador de 4 bits con acarreo de entrada y acarreo de salida — justo la celda que teselarías dentro de una ALU más grande o dentro de la lógica de incremento de direcciones de un CPU. Es el primer circuito de la unidad combinacional ("circuito sumador/restador") por una razón: una vez que puedes sumar, la resta, la comparación y el conteo vienen solos.

## Empieza desde un bit: el sumador completo

Antes de la versión de 4 bits, conoce el **sumador completo** de un solo bit del que está hecho. Para el bit `i`, con acarreo de entrada `cin`:

```
sum  = a xor b xor cin
cout = (a and b) or (cin and (a xor b))
```

La suma es el XOR de las tres entradas (paridad impar); el acarreo es "dos o más de las tres entradas son 1". Encadena cuatro de estos — el `cout` de cada uno alimentando el `cin` del siguiente — y tienes un sumador **por acarreo serie** (ripple-carry). El nombre es literal: el acarreo tiene que propagarse desde el LSB hasta el MSB antes de que el bit de suma más alto sea definitivo, por eso esta estructura es simple pero no la más rápida.

## Interfaz

```vhdl
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## El cuerpo

En vez de cablear cuatro sumadores completos a mano, deja que `numeric_std` haga la aritmética y solo captura el acarreo:

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

Qué está pasando:

- `unsigned(a)` y `unsigned(b)` reinterpretan los vectores como tipos numéricos para el operador `+`.
- `resize(…, 5)` extiende con ceros a 5 bits para que la suma tenga espacio para desbordarse hacia el bit superior.
- `("0000" & cin)` construye un valor de 5 bits con solo `cin` en el LSB, para que se sume limpiamente.
- Tras la suma, `t(3 downto 0)` es la suma y `t(4)` es el acarreo de salida.

> Los dos estilos — cuatro sumadores completos explícitos vs. un `+` sobre `unsigned` redimensionados — se sintetizan al *mismo* hardware por acarreo serie (una herramienta moderna incluso puede promoverlo a una estructura de acarreo más rápida). La versión con `+` es la que realmente escribirás; la versión a nivel de compuertas es la que deberías poder *explicar*.

## ¿Por qué 5 bits?

Sumar dos números de 4 bits con un acarreo de entrada puede producir un resultado tan grande como `1111 + 1111 + 1 = 11111` — cinco bits. Haz la cuenta en un contenedor de 4 bits y perderías el bit superior silenciosamente. Redimensionar a un bit extra es el idiom seguro, y el bit 4 *es* tu acarreo de salida.

## Hacer un restador

El curso empareja el sumador con un restador, y en complemento a dos son casi el mismo circuito. Para calcular `a − b`, suma el complemento bit a bit de `b` y fuerza el acarreo de entrada a 1:

```vhdl
t <= resize(unsigned(a), 5) + resize(unsigned(not b), 5) + "00001";
```

Eso es `a + (¬b) + 1 = a − b`. Un sumador/restador real agrega un XOR por bit (controlado por una línea `add_sub`) y alimenta esa misma línea al `cin` — un solo bit de control cambia toda la unidad entre las dos operaciones.

## Qué hace el ejemplo

La configuración de simulación mueve `a` y `b` por varios patrones (`3+5 = 8`, luego un `10 + 6 = 16` que genera acarreo, luego `10 + 15 = 25`…). La forma de onda muestra `sum` y `cout` cambiando **combinacionalmente** — no hace falta reloj porque este diseño no tiene registros.

## Tu turno

- **Hazlo paramétrico.** Agrega un `generic (N : integer := 4)` a la entidad y usa `N` donde aparezca el `4`. Ahora tienes un `adder_N` que puedes instanciar a cualquier ancho.
- **Construye el sumador/restador** de arriba con una entrada de control `add_sub`.
- **Conecta dos de ellos.** Uno para el nibble bajo, otro para el alto, con el primer `cout` alimentando el segundo `cin`, y tienes un sumador por acarreo serie de 8 bits — y una idea de por qué el retraso del acarreo crece con el ancho.

## Qué recordar

- Un sumador completo es `sum = a xor b xor cin`, `cout = majority(a, b, cin)`; encadénalos para sumar números anchos.
- Dale siempre a la aritmética un bit extra (`resize`) para que no se pierda el acarreo.
- La resta es la suma del complemento a dos: `a − b = a + not b + 1`.
- El acarreo serie es simple, pero su retraso crece con el número de bits.
