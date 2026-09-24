# Tipos de datos: `std_logic`, vectores, `unsigned`, `integer`

VHDL es un lenguaje tipado. Si ves un puerto declarado como `in std_logic`, el compilador no te deja conectarlo a algo de otro tipo. Elegir el tipo correcto para cada señal es la mitad de escribir VHDL limpio.

## `std_logic` — el cable universal

Un bit de la lógica IEEE de **9 valores**. Sí, nueve:

| Valor | Significado |
|-------|-------------|
| `'0'` | bajo fuerte |
| `'1'` | alto fuerte |
| `'Z'` | alta impedancia (tri-estado apagado) |
| `'L'` | bajo débil (pull-down) |
| `'H'` | alto débil (pull-up) |
| `'X'` | desconocido / múltiples drivers en conflicto |
| `'U'` | no inicializado — la señal se leyó antes de asignarse |
| `'W'` | débil desconocido |
| `'-'` | don't care (usado en `case`) |

Al sintetizar solo `'0'`, `'1'` y `'Z'` llegan al hardware. Los demás son señales de diagnóstico del simulador — si ves `'U'` en una forma de onda, hay una señal leída antes de ser escrita.

## `std_logic_vector` — un bus

Un arreglo de `std_logic` de ancho fijo:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, índices 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, índices 0..15
```

`downto` es por mucho la dirección más común — coincide con cómo escribimos números (MSB a la izquierda).

Literales:

```vhdl
data <= "10101010";     -- literal binario, un caracter por bit
addr <= x"CAFE";        -- literal hex (debe ser múltiplo de 4 bits)
data <= (others => '0');-- atajo para todo en cero
data <= (0 => '1', others => '0'); -- solo el LSB en alto
```

## `unsigned` / `signed` — vectores con matemática

`std_logic_vector` es "solo una bolsa de bits". No tiene significado numérico. Para sumar o restar necesitas `unsigned` o `signed`:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- sirve
```

Cómo convertir:

```vhdl
-- vector → unsigned para hacer cuentas
u <= unsigned(slv);

-- unsigned → vector para hablar con el exterior
slv <= std_logic_vector(u);

-- unsigned → integer para indexar o constantes
idx <= to_integer(u);

-- integer → unsigned (indica el ancho)
u <= to_unsigned(42, 8);
```

Regla: usa `std_logic_vector` en **puertos** (máxima compatibilidad), y convierte a `unsigned` / `signed` dentro de las arquitecturas donde realmente haces cuentas.

## `integer` — para indexar y contar

Usa `integer` (o un subtipo restringido) para contadores de ciclos, constantes y cualquier cosa que no cruce un puerto:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Un `integer` sin restringir es usualmente de 32 bits en simulación. Para hardware, querrás un rango acotado:

```vhdl
signal counter : integer range 0 to 255;
```

El sintetizador entonces te da exactamente 8 bits.

## Enumeraciones — para estados de FSM

Declara un tipo legible para máquinas de estados:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

Luego escribes `state <= S_ACTIVE` en lugar de `state <= "010"`. El sintetizador elige la codificación binaria (y puedes sugerirla con un atributo si te importa).

## Qué recordar

- Un cable → `std_logic`. Un bus → `std_logic_vector`.
- ¿Necesitas matemática? Usa `unsigned` (o `signed`) e `IEEE.NUMERIC_STD`.
- Declarar **puertos** en `std_logic_vector` hace tu entidad lo más portable posible.
- Para estados de FSM, usa enumeraciones — el código se lee como una especificación.
