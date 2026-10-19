# Prerrequisitos de matemática discreta para el diseño digital

No todos los estudiantes llegan con la misma base matemática. Esta lección es un repaso de 25 minutos de las ideas de matemática discreta que aparecen a cada rato en el diseño digital, para que cuando una lección más adelante diga "piénsalo como un conjunto" o "el espacio de estados es el producto cartesiano de…", nada te deje clavado. Todo lo que vemos aquí se conecta directamente con las cuatro unidades de la asignatura de *Matemáticas Discretas* de la ESCOM —Lógica, Teoría de conjuntos, Enteros y Estructuras discretas— y esta vez cada una la leemos como hardware.

## La lógica es diseño digital con otros símbolos

La lógica proposicional (la Unidad I de la asignatura) es la misma álgebra que obedecen tus compuertas, solo que escrita con otros signos. Una **proposición** es un enunciado que es verdadero o falso —exactamente los dos valores que puede llevar un cable.

| Matemáticas | Se lee como   | VHDL            |
|-------------|---------------|-----------------|
| ∧ (and)     | "P y Q"       | `and`           |
| ∨ (or)      | "P o Q"       | `or`            |
| ¬ (not)     | "no P"        | `not`           |
| ⊕ (xor)     | "P xor Q"     | `xor`           |
| P ⇒ Q       | "si P entonces Q" | `(not P) or Q` |

Esa última fila vale la pena memorizarla. El condicional **P ⇒ Q es lógicamente equivalente a ¬P ∨ Q** —no existe una compuerta `implies` porque nunca la necesitas. Otras dos equivalencias que vas a usar todo el tiempo son las **leyes de De Morgan**:

- ¬(P ∧ Q) ≡ ¬P ∨ ¬Q  →  `not (a and b) = (not a) or (not b)`
- ¬(P ∨ Q) ≡ ¬P ∧ ¬Q  →  `not (a or b) = (not a) and (not b)`

De Morgan es la razón por la que puedes empujar una burbuja (una inversión) a través de una compuerta y cambiar un AND por un OR. Las herramientas de síntesis hacen esto miles de veces para acomodar tu lógica en las celdas que la librería realmente ofrece.

**Aguas:** la recíproca (Q ⇒ P), la inversa (¬P ⇒ ¬Q) y la contrapositiva (¬Q ⇒ ¬P) *no* son intercambiables. Solo la **contrapositiva es equivalente** a la original —una distinción que la asignatura machaca en la Unidad I, y la razón por la que un "if-else" descuidado puede describir el circuito equivocado.

## Álgebra booleana: la lógica que puedes soldar

La Unidad IV la llama *álgebras y funciones booleanas*, y es el núcleo formal del diseño combinacional. Una **función booleana** de n variables es una aplicación de {0,1}ⁿ en {0,1}. Hay exactamente `2^(2^n)` de ellas para n entradas —16 funciones distintas de 2 entradas, que es la razón por la que las seis compuertas con nombre más las constantes cubren absolutamente todo.

Toda función de este tipo tiene dos formas canónicas:

- **Suma de minitérminos (SOP):** haz un OR entre un término producto por cada fila de entrada que dé un `1`.
- **Producto de maxtérminos (POS):** haz un AND entre un término suma por cada fila que dé un `0`.

Ejemplo resuelto —la función mayoría (salida `1` cuando dos o tres entradas son `1`):

```
f(a,b,c) = (a and b) or (a and c) or (b and c)
```

Esos tres productos son los minitérminos. Luego puedes reducir la expresión con los axiomas de Boole —identidad, complemento (`a or not a = 1`), distributividad, absorción (`a or (a and b) = a`)— o con un **mapa de Karnaugh**, el minimizador visual de la Unidad IV. Menos literales significa menos transistores.

```vhdl
y <= (a and b) or (a and c) or (b and c);  -- tres compuertas, evaluadas como cables
```

**Aguas:** minimizar para *área* no es lo mismo que minimizar para *velocidad*, y un sintetizador puede deshacer tu álgebra afinada a mano para equilibrar ambas. Escribe lógica clara; deja que la herramienta optimice.

## Conjuntos: el vocabulario de señales, puertos y estados

Un **conjunto** (Unidad II) es una colección no ordenada de elementos distintos: `{a, b, c}`. En el diseño digital te los encuentras por todos lados:

- Los **valores que puede tomar una señal.** `std_logic` es un conjunto de 9 elementos —`{U, X, 0, 1, Z, W, L, H, -}`— que abarca no inicializado, desconocido, los dos niveles lógicos fuertes, alta impedancia, niveles débiles y don't-care.
- Los **puertos** que declara una entity, y los **estados** que puede ocupar una máquina de estados finitos.

Las operaciones que se repiten son **unión** (∪), **intersección** (∩), **diferencia** (∖) y **complemento**. Dos herramientas de conteo de la Unidad II se ganan su lugar:

- **Producto cartesiano** A × B: el conjunto de todos los pares ordenados. Una señal de 2 bits vive en `{0,1} × {0,1}`, así que tiene 4 valores; un vector de n bits vive en `{0,1}ⁿ`, lo que da **2ⁿ**. Esta es literalmente la razón por la que una dirección de 4 bits alcanza 16 localidades.
- **Conjunto potencia** 𝒫(A): el conjunto de todos los subconjuntos de A, con cardinalidad **2^|A|**. La tabla de verdad de una función de n entradas es una enumeración de 𝒫 sobre las entradas —las mismas 2ⁿ filas.

Para dimensionar conjuntos que se traslapan, el **principio de inclusión–exclusión** da |A ∪ B| = |A| + |B| − |A ∩ B|.

## Funciones y relaciones

Todo circuito combinacional **es una función**: dadas las entradas, hay exactamente una salida correcta. `y <= a and b` realiza `{(0,0)→0, (0,1)→0, (1,0)→0, (1,1)→1}`. Dos propiedades (Unidad IV, *propiedades de las funciones*) que vas a invocar sin nombrarlas:

- **Total** —toda entrada tiene una salida. En un `process` combinacional debes asignar la salida en *todos* los caminos, o la herramienta infiere un latch indeseado para "recordar" el caso faltante.
- **Determinista** —la misma entrada siempre da la misma salida, sin condiciones de carrera.

La lógica secuencial amplía esto a **relaciones** (Unidad IV): una relación de estado siguiente puede mandar un estado a varios, que es exactamente una FSM no determinista —normalmente un bug, de vez en cuando una especificación que vas afinando hasta convertirla en una máquina determinista.

## Enteros, bases e inducción

La Unidad III es teoría de números, y su sección de *sistemas de numeración* es puro cimiento del diseño digital: el algoritmo de cambio de base, el vínculo estrecho entre binario y hexadecimal (un dígito hex = un nibble de 4 bits), y la aritmética en base *b*. Si la conversión de bases o el complemento a dos se sienten tambaleantes, la lección de **Sistemas numéricos** del módulo de Fundamentos digitales los recorre con detalle.

La teoría de números también te entrega herramientas que vas a usar por su nombre:

- **Divisibilidad y aritmética modular.** Un contador que se reinicia en K está haciendo aritmética *mód K*; los divisores de reloj y el wrap-around de direcciones son congruencias (Unidad IV, *enteros módulo n*).
- **Inducción matemática.** Para demostrar que un sumador con acarreo en cadena de N bits es correcto, muestra que funciona el caso de 1 bit, y luego que "si funciona la versión de N bits, también la de N+1". Esa es la forma de todo ciclo `for … generate` que arma una ruta de datos ancha a partir de una rebanada repetida.

## Contar bits

Dos fórmulas cierran el círculo entre las cuatro unidades:

- Un puerto de ancho N guarda **2^N** valores distintos (producto cartesiano / conjunto potencia).
- Una máquina con K estados cabe en **⌈log₂ K⌉** flip-flops con codificación binaria, o **K** flip-flops con one-hot.

Por eso una FSM de semáforo cabe en 2 flip-flops, y por eso un contador de 32 bits ve unos 4 mil millones de flancos antes de reiniciarse.

## Qué recordar

- La lógica proposicional *es* álgebra booleana —los mismos operadores, y P ⇒ Q no es más que `(not P) or Q`. De Morgan te deja mover inversiones a través de las compuertas.
- Una función booleana tiene las formas canónicas de suma de minitérminos / producto de maxtérminos; los mapas de Karnaugh y los axiomas las reducen.
- Los conjuntos nombran los valores, puertos y estados de un circuito; **2ⁿ** (producto cartesiano / conjunto potencia) y **⌈log₂ K⌉** son las fórmulas de ancho que vas a reutilizar para siempre.
- La lógica combinacional es una función *total y determinista* —asigna cada salida en cada camino o te sale un latch accidental.
