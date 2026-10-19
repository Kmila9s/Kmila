# Proyecto: ALU de 8 bits con banderas

Una **unidad aritmético-lógica (ALU)** es el corazón que late en cualquier CPU. Le das dos operandos y un opcode; te regresa un resultado y un puñado de banderas que describen qué pasó (¿hubo desbordamiento? ¿resultó cero? etc.).

## Qué vas a construir

Una ALU de 8 bits con las siguientes operaciones:

| Opcode | Nombre  | Fórmula                      |
|--------|---------|------------------------------|
| `000`  | ADD     | `y = a + b`                  |
| `001`  | SUB     | `y = a - b`                  |
| `010`  | AND     | `y = a and b`                |
| `011`  | OR      | `y = a or  b`                |
| `100`  | XOR     | `y = a xor b`                |
| `101`  | SHL     | `y = a desplazado 1 a la izq.`|
| `110`  | SHR     | `y = a desplazado 1 a la der.`|
| `111`  | PASS_A  | `y = a` (pasa directo)       |

Más dos salidas de bandera:

- `zero` — en alto cuando `y` es todo ceros
- `carry` — el acarreo de salida de ADD/SUB; pon `'0'` para todo lo demás

En el curso de *Arquitectura de Computadoras* de ESCOM este es el tema 3.1, el primer bloque funcional del procesador RISC monociclo. Constrúyelo limpio ahora y lo podrás meter directo en la ruta de datos más adelante.

## El sumador que hay debajo

ADD y SUB parecen una sola línea de VHDL (`a + b`), pero el hardware que el sintetizador construye para ese `+` es donde vive la ingeniería interesante. El curso te pide comparar dos estructuras clásicas.

**Sumador de acarreo en cascada (ripple-carry).** Encadenas ocho sumadores completos, metiendo el acarreo de salida de cada etapa en el acarreo de entrada de la siguiente. Es diminuto y evidente, pero el acarreo tiene que *propagarse* desde el bit 0 hasta el bit 7 antes de que el bit de suma más alto sea válido — así que el retardo del peor caso crece linealmente con el ancho de la palabra. Para un sumador de 8 bits son ocho retardos de sumador completo apilados uno tras otro.

**Sumador de acarreo anticipado (carry-lookahead, CLA).** En lugar de esperar la propagación, calcula cada acarreo directo desde las entradas. Define dos señales por bit:

```
generate:  g(i) = a(i) and b(i)      -- este bit genera un acarreo por sí solo
propagate: p(i) = a(i) xor b(i)      -- este bit propaga un acarreo entrante
```

Entonces cada acarreo es `c(i+1) = g(i) or (p(i) and c(i))`, expandido para que los acarreos se produzcan en paralelo en lugar de uno tras otro. El retardo se vuelve casi constante con el ancho, a costa de más compuertas. Este es el "acarreo anticipado" que el temario contrasta contra el diseño en cascada del "acarreo en cascada".

Para este proyecto no tienes que construir ninguno de los dos a mano — escribir `a + b` deja que las herramientas de la FPGA elijan una cadena de acarreo optimizada (el tejido Artix-7 hasta tiene lógica dedicada de acarreo rápido). El punto es *entender* qué compromiso representa ese `+`: área contra velocidad.

## Dejar bien las banderas

Las banderas son donde los principiantes pierden puntos sin darse cuenta, porque **acarreo** y **desbordamiento** son dos cosas distintas:

- El **acarreo (C)** es el bit que se cae por arriba de una suma *sin signo*. Es el bit número 9 de `a + b`.
- El **desbordamiento (V)** avisa que un resultado *con signo* (complemento a dos) ya no cabe en 8 bits. La regla limpia: `V = acarreo_hacia_el_MSB xor acarreo_desde_el_MSB`. Dicho de otro modo, hubo desbordamiento si dos operandos del mismo signo produjeron un resultado de signo opuesto.

Un truco ordenado de VHDL para obtener ambos de un jalón es sumar en un espacio de 9 bits:

```vhdl
signal ext : unsigned(8 downto 0);
...
ext   <= ('0' & unsigned(a)) + ('0' & unsigned(b));  -- suma de 9 bits
result <= std_logic_vector(ext(7 downto 0));
cout   <= ext(8);                                     -- la bandera de acarreo
```

Para SUB, recuerda que la resta es `a + (not b) + 1` en complemento a dos, así que `15 - 15` internamente genera un acarreo de salida de `1` aunque el resultado sea cero — que es exactamente por qué muchas ISAs tratan el acarreo de la resta como un *préstamo (borrow)* (C = not borrow). Mantén tu convención consistente con lo que espere el testbench que la acompaña.

## Qué te da el andamiaje

El código inicial declara la entidad, las señales `result` y `cout`, y un armazón `process(op, a, b)` que ya maneja ADD y SUB. Tu trabajo es llenar las otras seis ramas del `case`.

## Pistas

- Para **AND / OR / XOR**, es una sola línea cada una — solo asigna el resultado bit a bit:

  ```vhdl
  when "010" => result <= a and b;
  ```

- Para **SHL** y **SHR**, rellena con `'0'`:

  ```vhdl
  when "101" => result <= a(6 downto 0) & '0';
  when "110" => result <= '0' & a(7 downto 1);
  ```

- Para **PASS_A**, solo copia `a` en `result`.

Acuérdate de poner `cout <= '0'` para las operaciones lógicas — ya lo haces una vez al inicio del process, lo que significa que cualquier rama que no lo sobrescriba hereda ese valor por defecto. Cómodo, pero quiere decir que tienes que ser explícito cuando una rama **sí** quiere un acarreo distinto de cero.

## Criterios de aceptación

Con los estímulos de la configuración que la acompaña:

| tiempo | op    | a          | b          | `y` esperada | `carry` | `zero` |
|--------|-------|------------|------------|--------------|---------|--------|
|  0     | `000` | `00001111` | `00000101` | `00010100`   | `0`     | `0`    |
| 20     | `001` | `00001111` | `00001111` | `00000000`   | `0`     | `1`    |
| 40     | `010` | `00001111` | `00001111` | `00001111`   | `0`     | `0`    |
| 60     | `101` | `10110000` | n/a        | `01100000`   | `0`     | `0`    |
| 80     | `111` | `10110000` | n/a        | `10110000`   | `0`     | `0`    |

## Para ir más lejos

- Agrega una bandera de **signo** (resultado negativo para la SUB en complemento a dos).
- Agrega un opcode de **rotación** — igual que SHL pero el bit de arriba vuelve a dar la vuelta al bit 0.
- Agrega un opcode de **multiplicación** que produzca un resultado de 16 bits en dos puertos de salida separados. La mayoría de las FPGAs pequeñas tienen bloques DSP dedicados para esto — vigila tu utilización cuando vuelvas a sintetizar.

Esta ALU es un bloque de construcción directo para el proyecto de CPU del nivel Experto — mantén este diseño limpio — ahí lo vas a conectar.

## Qué recordar

- Una ALU es operandos a la entrada, resultado y banderas a la salida. El opcode selecciona qué operación realiza ese único bloque.
- El `+` que escribes esconde una decisión real: acarreo en cascada (chico, lento, el retardo crece con el ancho) contra acarreo anticipado (más grande, rápido, retardo casi constante) usando los términos generate/propagate.
- **Acarreo ≠ desbordamiento.** El acarreo es el bit 9 sin signo; el desbordamiento es `acarreo_entrada(MSB) xor acarreo_salida(MSB)` y solo importa para valores con signo.
- Pon un valor por defecto sensato para cada bandera al inicio del process, y después sobrescribe solo donde una rama de verdad lo necesite — así evitas latches y casos olvidados.
