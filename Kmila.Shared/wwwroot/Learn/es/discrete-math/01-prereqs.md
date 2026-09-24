# Prerrequisitos de matemática discreta

Antes de entrar al diseño digital conviene tener a mano cinco herramientas de matemática discreta. No necesitas dominarlas: basta con reconocerlas cuando aparezcan.

## 1. Conjuntos

Un **conjunto** es una colección no ordenada y sin repeticiones. En hardware, el conjunto de estados de una FSM o el conjunto de direcciones válidas de una memoria son ejemplos cotidianos.

- Pertenencia: `x ∈ A`
- Unión, intersección, diferencia: `A ∪ B`, `A ∩ B`, `A \ B`
- Cardinalidad: `|A|` — cuántos elementos tiene

## 2. Funciones

Una **función** `f : A → B` asocia cada entrada de `A` con exactamente una salida de `B`. Una tabla de verdad *es* una función de `{0,1}ⁿ` en `{0,1}ᵐ`.

## 3. Demostraciones

No escribimos pruebas formales todos los días, pero reconocer una **inducción** o una **demostración por casos** ayuda cuando lees papers o documentación de protocolos.

## 4. Lógica proposicional

Los operadores `∧` (AND), `∨` (OR), `¬` (NOT), `⊕` (XOR) son la base tanto del razonamiento como de las puertas. Saber que `a ⊕ b = (a ∧ ¬b) ∨ (¬a ∧ b)` es la misma verdad que viste en álgebra booleana.

## 5. Conteo

Contar combinaciones es esencial para dimensionar buses y memorias:

- `n` bits → `2ⁿ` valores distintos
- Permutaciones de `n` elementos: `n!`

## Por qué importa aquí

Cada módulo posterior se apoya en al menos uno de estos cinco puntos. Si en algún momento una lección te pide "la función de transición de estados", ya sabes que se refiere al punto 2.

*Más lecciones vendrán — este módulo está en desarrollo.*
