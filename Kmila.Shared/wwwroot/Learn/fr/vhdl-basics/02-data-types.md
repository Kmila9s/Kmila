# Les types de données : `std_logic`, vecteurs, `unsigned`, `integer`

VHDL est un langage **fortement typé**. Quand vous voyez un port déclaré `in std_logic`, le compilateur ne vous laissera pas le câbler à quelque chose d'un type différent sans conversion explicite. Choisir le bon type pour chaque signal, c'est déjà la moitié du travail quand on veut écrire du VHDL propre.

## `std_logic` — le fil universel

Un seul bit de la logique IEEE 1164 à **9 valeurs**. Oui, neuf :

| Valeur | Signification |
|-------|---------|
| `'0'` | niveau bas fort |
| `'1'` | niveau haut fort |
| `'Z'` | haute impédance (tristate désactivé) |
| `'L'` | niveau bas faible (pull-down) |
| `'H'` | niveau haut faible (pull-up) |
| `'X'` | inconnu fort — p. ex. deux drivers qui se disputent avec des valeurs différentes |
| `'U'` | non initialisé — votre signal n'a jamais reçu d'affectation |
| `'W'` | inconnu faible |
| `'-'` | peu importe (utilisé comme motif pour `std_match` / indice de synthèse) |

Pourquoi neuf au lieu de deux ? Parce qu'un vrai fil n'est pas juste « 0 ou 1 ». Il peut être flottant (`'Z'`), tiré faiblement (`'L'`/`'H'`), ou en véritable conflit (`'X'`). C'est ce qui permet à `std_logic` d'être un type **résolu** : quand plusieurs drivers partagent un même fil, une fonction de résolution décide du résultat. Une valeur forte l'emporte sur une valeur faible ; deux valeurs fortes qui se contredisent produisent `'X'` ; et un fil non piloté reste à `'Z'`.

En synthèse, seules `'0'`, `'1'` et `'Z'` se traduisent en matériel. Les autres sont des diagnostics de simulation — si vous voyez `'U'` dans un chronogramme, c'est que vous avez un signal lu avant d'être écrit ; une bande de `'X'` signifie généralement un bus à deux drivers actifs.

> Il existe aussi un cousin non résolu, `std_ulogic` (le `u` signifie « unresolved »). `std_logic` est littéralement `std_ulogic` plus une fonction de résolution. Utilisez `std_logic` par défaut — c'est justement la résolution que vous voulez pour les bus.

## `std_logic_vector` — un bus

Un tableau de `std_logic` à largeur fixe :

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, indices 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, indices 0..15
```

`downto` est de loin la direction la plus courante — elle correspond à la façon dont nous écrivons les nombres (MSB à gauche, donc `data(7)` est le bit de poids fort). Choisissez une direction par projet et tenez-vous-y ; mélanger `to` et `downto` est une source classique de bus silencieusement inversés.

Les littéraux :

```vhdl
data <= "10101010";      -- littéral binaire, un caractère par bit
addr <= x"CAFE";         -- littéral hexadécimal (la largeur doit être un multiple de 4 bits)
data <= (others => '0'); -- agrégat : tout à zéro, quelle que soit la largeur
data <= (0 => '1', others => '0'); -- seul le bit 0 (le LSB) est à '1'
```

La forme `(others => '0')` mérite votre affection : elle s'adapte automatiquement si vous changez la largeur plus tard, donc elle ne devient jamais obsolète.

## `unsigned` / `signed` — des vecteurs sur lesquels on peut calculer

`std_logic_vector`, c'est « juste un sac de bits ». Il n'a aucune signification numérique, donc `slv1 + slv2` provoque une erreur de compilation avec `numeric_std`. Pour additionner ou soustraire, il vous faut `unsigned` ou `signed` :

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- fonctionne : addition unsigned de même largeur
```

Les deux types ne diffèrent que par la façon dont le bit de poids fort est interprété : `unsigned` traite tous les bits comme une magnitude ; `signed` traite le MSB comme un signe en complément à deux.

Passer de l'un à l'autre — voici les quatre conversions que vous utiliserez en permanence :

```vhdl
-- réinterprète les mêmes bits comme unsigned (pour le calcul)
u   <= unsigned(slv);

-- réinterprète un unsigned comme de simples bits (pour le monde extérieur / un port)
slv <= std_logic_vector(u);

-- unsigned -> integer, pour indexer ou comme constante
idx <= to_integer(u);

-- integer -> unsigned ; vous DEVEZ donner la largeur
u   <= to_unsigned(42, 8);
```

`unsigned(slv)` et `std_logic_vector(u)` sont des **conversions de type (casts)** — elles changent l'étiquette posée sur les bits, pas les bits eux-mêmes. `to_integer` et `to_unsigned` sont des **fonctions** qui calculent réellement. Une erreur fréquente de débutant est d'appeler `to_unsigned` sur un vecteur (elle prend un entier) ou de convertir un entier avec `unsigned(…)` (les casts ne marchent qu'entre les types tableaux).

Une dernière qui vous évite des bugs de débordement — `resize`, qui complète ou tronque vers une nouvelle largeur. Elle **étend avec des zéros** pour `unsigned` et **étend le signe** pour `signed`, donc elle préserve la valeur avant un calcul :

```vhdl
wide <= resize(a, 9) + resize(b, 9);  -- résultat sur 9 bits, la retenue est conservée
```

Règle empirique : utilisez `std_logic_vector` dans les **ports** (le public le plus large), et convertissez en `unsigned` / `signed` à l'intérieur des architectures où vous faites réellement de l'arithmétique.

## `integer` — pour indexer et compter

Utilisez `integer` (ou un sous-type contraint) pour les compteurs de boucle, les constantes, et tout ce qui ne traverse pas une frontière de port :

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

Un `integer` non borné fait au moins 32 bits en simulation (plage −2³¹+1 à 2³¹−1). Pour le matériel, vous voulez presque toujours une plage contrainte afin que le synthétiseur dimensionne le registre :

```vhdl
signal counter : integer range 0 to 255;  -- se synthétise en exactement 8 bits
```

Un compteur `integer` non contraint vous coûtera 32 bascules dont vous ne vouliez pas.

## Les énumérations — pour les états d'une FSM

Déclarez un type lisible pour les machines à états :

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

Vous écrivez alors `state <= S_ACTIVE` au lieu de `state <= "010"`. Le synthétiseur choisit l'encodage binaire (ou one-hot) pour vous, et vous pouvez le lui suggérer par un attribut si ça vous importe. Votre code se lit comme une spécification, et le chronogramme affiche les *noms* des états, pas des bits bruts.

## À retenir

- Un seul fil → `std_logic`. Un bus → `std_logic_vector`. Les deux viennent de `STD_LOGIC_1164`.
- Besoin de calculer ? Utilisez `unsigned` (ou `signed`) et `IEEE.NUMERIC_STD`. `std_logic_vector` n'a aucune signification numérique.
- `unsigned(x)` / `std_logic_vector(x)` sont des casts (ils ré-étiquettent les bits) ; `to_integer` / `to_unsigned` sont des fonctions (et `to_unsigned` a besoin d'une largeur).
- Déclarer vos **ports** en `std_logic_vector` rend votre entity la plus portable possible ; convertissez à l'intérieur.
- Contraignez vos plages d'`integer`, et utilisez des énumérations pour les états de FSM.
