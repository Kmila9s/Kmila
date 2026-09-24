# Proyecto: ALU de 8 bits con banderas

Una **ALU (Arithmetic Logic Unit)** es el corazón de cualquier CPU. Le das dos operandos y un opcode; te devuelve un resultado y un puñado de banderas que describen qué pasó (¿hubo desbordamiento? ¿resultó cero?).

## Qué vas a construir

Una ALU de 8 bits con las siguientes operaciones:

| Opcode | Nombre  | Fórmula                     |
|--------|---------|-----------------------------|
| `000`  | ADD     | `y = a + b`                 |
| `001`  | SUB     | `y = a - b`                 |
| `010`  | AND     | `y = a and b`               |
| `011`  | OR      | `y = a or  b`               |
| `100`  | XOR     | `y = a xor b`               |
| `101`  | SHL     | `y = a desplazado 1 izq.`   |
| `110`  | SHR     | `y = a desplazado 1 der.`   |
| `111`  | PASS_A  | `y = a` (pasa directo)      |

Más dos banderas:

- `zero` — alto cuando `y` es todo ceros
- `carry` — el acarreo de ADD/SUB; maneja `'0'` para los lógicos

## Qué te da el andamiaje

El código inicial declara la entidad, las signals `result` y `cout`, y un `process(op, a, b)` que ya maneja ADD y SUB. Tu trabajo es llenar las otras seis ramas del `case`.

## Pistas

- Para **AND / OR / XOR**, es una línea cada una:

  ```vhdl
  when "010" => result <= a and b;
  ```

- Para **SHL** y **SHR**, rellena con `'0'`:

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Para **PASS_A**, copia `a` a `result`.

Recuerda manejar `cout <= '0'` en las operaciones lógicas — ya lo haces una vez al inicio del proceso, lo que significa que cualquier rama que no lo sobrescriba hereda ese default. Bien, pero implica ser explícito cuando una rama **sí** quiera un acarreo distinto.

## Criterios de aceptación

Con los estímulos de la configuración:

| tiempo | op    | a          | b          | `y` esperada | `carry` | `zero` |
|--------|-------|------------|------------|--------------|---------|--------|
|  0     | `000` | `00001111` | `00000101` | `00010100`   | `0`     | `0`    |
| 20     | `001` | `00001111` | `00001111` | `00000000`   | `0`     | `1`    |
| 40     | `010` | `00001111` | `00001111` | `00001111`   | `0`     | `0`    |
| 60     | `101` | `10110000` | n/a        | `01100000`   | `0`     | `0`    |
| 80     | `111` | `10110000` | n/a        | `10110000`   | `0`     | `0`    |

## Más lejos

- Agrega una bandera **signed** (resultado negativo en SUB con complemento a dos).
- Agrega un **rotate** — igual que SHL pero el bit superior envuelve al bit 0.
- Agrega un opcode **multiply** con resultado de 16 bits en dos puertos. Los FPGA pequeños tienen bloques DSP dedicados para esto — vigila la utilización al resintetizar.

Esta ALU es un bloque directo para el proyecto de CPU de nivel experto — mantén este diseño limpio, lo cablearás allí.
