# Types de données : `std_logic`, vecteurs, `unsigned`, `integer`

VHDL est un langage typé. Quand tu vois un port déclaré `in std_logic`, le compilateur ne te laissera pas le relier à quelque chose d'un autre type. Choisir le bon type pour chaque signal, c'est la moitié du boulot d'écriture de VHDL propre.

## `std_logic` — le fil universel

Un seul bit de la logique IEEE à **9 valeurs**. Oui, neuf :

| Valeur | Signification |
|--------|---------------|
| `'0'`  | drive bas fort |
| `'1'`  | drive haut fort |
| `'Z'`  | haute impédance (tristate off) |
| `'L'`  | bas faible (pull-down) |
| `'H'`  | haut faible (pull-up) |
| `'X'`  | inconnu / drivers multiples en désaccord |
| `'U'`  | non initialisé — ton signal n'a jamais été assigné |
| `'W'`  | inconnu faible |
| `'-'`  | peu importe (utilisé dans les case statements) |

En synthèse, seuls `'0'`, `'1'` et `'Z'` passent au matériel. Les autres sont des diagnostics de simulation — si tu vois `'U'` dans une forme d'onde, tu as un signal lu avant d'avoir été écrit.

## `std_logic_vector` — un bus

Un tableau de `std_logic` de largeur fixe :

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, indices 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, indices 0..15
```

`downto` est de loin la direction la plus courante — elle correspond à la façon dont on écrit les nombres (MSB à gauche).

Littéraux :

```vhdl
data <= "10101010";     -- littéral binaire, un caractère par bit
addr <= x"CAFE";        -- littéral hex (longueur multiple de 4 bits)
data <= (others => '0');-- raccourci pour tout à zéro
data <= (0 => '1', others => '0'); -- seulement le LSB à haut
```

## `unsigned` / `signed` — des vecteurs sur lesquels on peut faire des maths

`std_logic_vector` n'est « qu'un sac de bits ». Il n'a pas de signification numérique. Pour additionner ou soustraire, tu as besoin de `unsigned` ou `signed` :

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- fonctionne
```

Passer de l'un à l'autre :

```vhdl
-- vecteur → unsigned pour les maths
u <= unsigned(slv);

-- unsigned → vecteur pour le monde extérieur
slv <= std_logic_vector(u);

-- unsigned → entier pour l'indexation ou les constantes
idx <= to_integer(u);

-- entier → unsigned (préciser la largeur)
u <= to_unsigned(42, 8);
```

Règle empirique : utilise `std_logic_vector` dans les **ports** (audience la plus large), et convertis en `unsigned` / `signed` à l'intérieur des architectures où tu fais réellement de l'arithmétique.

## `integer` — pour l'indexation et le comptage

Utilise `integer` (ou un sous-type contraint) pour les compteurs de boucle, les constantes, et tout ce qui ne traverse pas une frontière de port :

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Un `integer` non borné est généralement 32 bits en simulation. Pour le matériel, tu veux une plage contrainte :

```vhdl
signal counter : integer range 0 to 255;
```

Le synthétiseur te donne alors exactement 8 bits.

## Énumérations — pour les états de FSM

Déclare un type lisible pour les machines à états :

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

Tu écris alors `state <= S_ACTIVE` au lieu de `state <= "010"`. Le synthétiseur choisit l'encodage binaire pour toi (et tu peux l'orienter avec un attribut si ça compte).

## À retenir

- Fil unique → `std_logic`. Bus → `std_logic_vector`.
- Besoin de maths ? Utilise `unsigned` (ou `signed`) et `IEEE.NUMERIC_STD`.
- Déclarer les **ports** en `std_logic_vector` rend ton entité portable au maximum.
- Pour les états de FSM, utilise des énumérations — ton code se lit comme une spec.
