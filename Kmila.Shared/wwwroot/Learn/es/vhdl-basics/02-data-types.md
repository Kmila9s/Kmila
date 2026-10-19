# Tipos de datos: `std_logic`, vectores, `unsigned`, `integer`

VHDL es un lenguaje de **tipado fuerte**. Cuando ves un puerto declarado `in std_logic`, el compilador no te deja conectarlo a algo de otro tipo sin una conversión explícita. Elegir el tipo correcto para cada señal es la mitad de escribir VHDL limpio.

## `std_logic` — el cable universal

Un solo bit de la lógica IEEE 1164 de **9 valores**. Sí, nueve:

| Valor | Significado |
|-------|-------------|
| `'0'` | nivel bajo fuerte |
| `'1'` | nivel alto fuerte |
| `'Z'` | alta impedancia (tristate apagado) |
| `'L'` | bajo débil (pull-down) |
| `'H'` | alto débil (pull-up) |
| `'X'` | desconocido fuerte — p. ej. dos drivers peleando con valores distintos |
| `'U'` | sin inicializar — tu señal nunca recibió asignación |
| `'W'` | desconocido débil |
| `'-'` | no importa (se usa como patrón de `std_match` / pista de síntesis) |

¿Por qué nueve en vez de dos? Porque un cable real no es solo "0 o 1". Puede estar flotando (`'Z'`), jalado débilmente (`'L'`/`'H'`) o en conflicto genuino (`'X'`). Esto es lo que permite que `std_logic` sea un tipo **resuelto**: cuando varios drivers comparten un cable, una función de resolución decide el resultado. Un valor fuerte le gana a uno débil; dos valores fuertes que no coinciden producen `'X'`; y un cable sin driver se queda en `'Z'`.

En síntesis solo `'0'`, `'1'` y `'Z'` llegan al hardware. Los demás son diagnósticos de simulación — si ves `'U'` en una forma de onda, tienes una señal que se lee antes de escribirse; una franja de `'X'` suele significar un bus con dos drivers activos.

> También existe un primo no resuelto, `std_ulogic` (la `u` es de "unresolved", sin resolver). `std_logic` es, literalmente, `std_ulogic` más una función de resolución. Usa `std_logic` por defecto — la resolución es justo lo que quieres para buses.

## `std_logic_vector` — un bus

Un arreglo de ancho fijo de `std_logic`:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, índices 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, índices 0..15
```

`downto` es por mucho la dirección más común — coincide con la forma en que escribimos los números (el MSB a la izquierda, así que `data(7)` es el bit más significativo). Elige una dirección por proyecto y mantenla; mezclar `to` y `downto` es una fuente clásica de buses invertidos en silencio.

Literales:

```vhdl
data <= "10101010";      -- literal binario, un carácter por bit
addr <= x"CAFE";         -- literal hexadecimal (el ancho debe ser múltiplo de 4 bits)
data <= (others => '0'); -- agregado: todo en ceros, cualquier ancho
data <= (0 => '1', others => '0'); -- solo el bit 0 (el LSB) en alto
```

La forma `(others => '0')` vale la pena amar: se adapta sola si después cambias el ancho, así que nunca queda obsoleta.

## `unsigned` / `signed` — vectores con los que sí puedes hacer matemáticas

`std_logic_vector` es "solo una bolsa de bits". No tiene significado numérico, así que `slv1 + slv2` es un error de compilación con `numeric_std`. Para sumar o restar necesitas `unsigned` o `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- funciona: suma unsigned del mismo ancho
```

Los dos tipos solo difieren en cómo se lee el bit superior: `unsigned` trata todos los bits como magnitud; `signed` trata el MSB como un signo en complemento a dos.

Moverte entre ellos — estas son las cuatro conversiones que usarás constantemente:

```vhdl
-- reinterpreta los mismos bits como unsigned (para hacer matemáticas)
u   <= unsigned(slv);

-- reinterpreta un unsigned como bits planos (para el exterior / un puerto)
slv <= std_logic_vector(u);

-- unsigned -> integer, para indexar o constantes
idx <= to_integer(u);

-- integer -> unsigned; DEBES indicar el ancho
u   <= to_unsigned(42, 8);
```

`unsigned(slv)` y `std_logic_vector(u)` son **casts de tipo** — cambian la etiqueta de los bits, no los bits. `to_integer` y `to_unsigned` son **funciones** que de verdad calculan. Un error común de principiante es llamar `to_unsigned` sobre un vector (recibe un entero) o castear un entero con `unsigned(…)` (los casts solo funcionan entre los tipos de arreglo).

Una más que te salva de bugs de desbordamiento — `resize`, que rellena o trunca a un ancho nuevo. **Extiende con ceros** a `unsigned` y **extiende el signo** de `signed`, así que preserva el valor antes de la aritmética:

```vhdl
wide <= resize(a, 9) + resize(b, 9);  -- un resultado de 9 bits conserva el acarreo
```

Regla de dedo: usa `std_logic_vector` en los **puertos** (la audiencia más amplia), y convierte a `unsigned` / `signed` dentro de las arquitecturas donde de verdad haces aritmética.

## `integer` — para indexar y contar

Usa `integer` (o un subtipo acotado) para contadores de ciclo, constantes y cualquier cosa que no cruce la frontera de un puerto:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Un `integer` sin acotar es de al menos 32 bits en simulación (rango de −2³¹+1 a 2³¹−1). Para hardware casi siempre quieres un rango acotado para que el sintetizador dimensione el registro:

```vhdl
signal counter : integer range 0 to 255;  -- sintetiza a exactamente 8 bits
```

Un contador `integer` sin acotar te costará 32 flip-flops que no querías.

## Enumeraciones — para estados de FSM

Declara un tipo legible para máquinas de estados:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

Luego escribes `state <= S_ACTIVE` en vez de `state <= "010"`. El sintetizador elige por ti la codificación binaria (o one-hot), y puedes sugerírsela con un atributo si te importa. Tu código se lee como una especificación, y la forma de onda muestra los *nombres* de estado, no bits crudos.

## Qué recordar

- Un solo cable → `std_logic`. Un bus → `std_logic_vector`. Ambos vienen de `STD_LOGIC_1164`.
- ¿Necesitas hacer matemáticas? Usa `unsigned` (o `signed`) y `IEEE.NUMERIC_STD`. `std_logic_vector` no tiene significado numérico.
- `unsigned(x)` / `std_logic_vector(x)` son casts (reetiquetan bits); `to_integer` / `to_unsigned` son funciones (y `to_unsigned` necesita un ancho).
- Declarar los **puertos** en `std_logic_vector` hace tu entidad lo más portable posible; convierte adentro.
- Acota los rangos de tus `integer`, y usa enumeraciones para los estados de una FSM.
