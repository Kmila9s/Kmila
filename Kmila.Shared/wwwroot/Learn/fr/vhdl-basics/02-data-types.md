# Types de données : `std_logic`, vecteurs, `unsigned`, `integer`

VHDL est un langage typé. Quand tu vois un port déclaré `in std_logic`, le compilateur ne te laissera pas le câbler à quelque chose d'un autre type. Choisir le bon type pour chaque signal, c'est la moitié du travail d'écrire du VHDL propre.

## `std_logic` — le fil universel

Un bit unique de la logique IEEE à **9 valeurs**. Oui, neuf :

| Valeur | Sens |
|--------|------|
| `'0'`  | drive bas fort |
| `'1'`  | drive haut fort |
| `'Z'`  | haute impédance (tristate off) |
| `'L'`  | bas faible (pull-down) |
| `'H'`  | haut faible (pull-up) |
| `'X'`  | inconnu / drivers multiples en désaccord |
| `'U'`  | non initialisé — ton signal n'a jamais été assigné |
| `'W'`  | inconnu faible |
| `'-'`  | don't care (utilisé dans les case) |

En synthèse, seuls `'0'`, `'1'` et `'Z'` se retrouvent dans le matériel. Les autres sont des diagnostics de simulation — si tu vois `'U'` dans un chronogramme, tu as un signal qui est lu avant d'être écrit.

## `std_logic_vector` — un bus

Un tableau à largeur fixe de `std_logic` :

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, indices 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, indices 0..15
```

`downto` est de loin la direction la plus courante — elle correspond à la façon dont on écrit les nombres (MSB à gauche).

Littéraux :

```vhdl
data <= "10101010";     -- littéral binaire, un caractère par bit
data <= x"AA";          -- littéral hex (multiple de 4 bits)
data <= (others => '0');-- raccourci pour tout-zéros
data <= (0 => '1', others => '0'); -- seulement le LSB à 1
```

## `unsigned` / `signed` — des vecteurs avec lesquels tu peux faire des maths

`std_logic_vector` est « juste un sac de bits ». Il n'a aucun sens numérique. Pour additionner ou soustraire, il te faut `unsigned` ou `signed` :

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- marche
```

Passer de l'un à l'autre :

```vhdl
-- vecteur → unsigned pour les maths
u <= unsigned(slv);

-- unsigned → vecteur pour le monde extérieur
slv <= std_logic_vector(u);

-- unsigned → integer pour indexation ou constantes
idx <= to_integer(u);

-- integer → unsigned (précise la largeur)
u <= to_unsigned(42, 8);
```

Règle empirique : utilise `std_logic_vector` dans les **ports** (le plus large public), et convertis en `unsigned` / `signed` à l'intérieur de l'architecture là où tu fais réellement de l'arithmétique.

## `integer` — pour l'indexation et le comptage

Utilise `integer` (ou un sous-type contraint) pour les compteurs de boucle, les constantes, et tout ce qui ne traverse pas une frontière de port :

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

L'`integer` non borné fait habituellement 32 bits en simulation. Pour le matériel, tu veux une plage contrainte :

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

Tu écris ensuite `state <= S_ACTIVE` plutôt que `state <= "010"`. Le synthétiseur choisit l'encodage binaire pour toi (et tu peux orienter avec un attribut si tu y tiens).

## À retenir

- Fil unique → `std_logic`. Bus → `std_logic_vector`.
- Besoin de faire des maths ? Utilise `unsigned` (ou `signed`) et `IEEE.NUMERIC_STD`.
- Déclarer les **ports** en `std_logic_vector` rend ton entity maximalement portable.
- Pour les états de FSM, utilise des énumérations — ton code se lit comme une spec.
