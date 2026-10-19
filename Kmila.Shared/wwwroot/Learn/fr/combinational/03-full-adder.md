# Exemple : additionneur 4 bits à propagation de retenue

Additionner deux nombres binaires, c'est le « hello world » de l'arithmétique numérique. Cet exemple construit un additionneur 4 bits avec retenue entrante et sortante — exactement la cellule que vous carrèleriez à l'intérieur d'une ALU plus grande ou dans la logique d'incrémentation d'adresse d'un CPU. C'est le premier circuit de l'unité combinatoire (« circuit additionneur/soustracteur ») pour une bonne raison : une fois que vous savez additionner, la soustraction, la comparaison et le comptage suivent tous.

## Partez d'un seul bit : l'additionneur complet

Avant la version 4 bits, connaissez l'**additionneur complet** (full adder) à un seul bit dont elle est faite. Pour le bit `i`, avec retenue entrante `cin` :

```
sum  = a xor b xor cin
cout = (a and b) or (cin and (a xor b))
```

La somme est le XOR des trois entrées (parité impaire) ; la retenue vaut « au moins deux des trois entrées sont à 1 ». Enchaînez-en quatre — le `cout` de chacun alimentant le `cin` du suivant — et vous avez un additionneur **à propagation de retenue** (ripple-carry). Le nom est littéral : la retenue doit se propager du LSB jusqu'au MSB avant que le bit de somme de poids fort ne soit définitif, ce qui explique pourquoi cette structure est simple mais pas la plus rapide.

## Interface

```vhdl
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## Le corps

Plutôt que de câbler quatre additionneurs complets à la main, laissez `numeric_std` faire l'arithmétique et contentez-vous de récupérer la retenue :

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

Ce qui se passe :

- `unsigned(a)` et `unsigned(b)` réinterprètent les vecteurs comme des types numériques pour l'opérateur `+`.
- `resize(…, 5)` étend avec des zéros à 5 bits pour que la somme ait la place de déborder sur le bit de poids fort.
- `("0000" & cin)` construit une valeur 5 bits avec juste `cin` en LSB, pour qu'elle s'additionne proprement.
- Après l'addition, `t(3 downto 0)` est la somme et `t(4)` la retenue sortante.

> Les deux styles — quatre additionneurs complets explicites contre un seul `+` sur des `unsigned` redimensionnés — se synthétisent vers le *même* matériel à propagation de retenue (un outil moderne peut même le promouvoir en une structure de retenue plus rapide). La version avec `+` est celle que vous écrirez réellement ; la version au niveau porte est celle que vous devriez savoir *expliquer*.

## Pourquoi 5 bits ?

Additionner deux nombres de 4 bits avec une retenue entrante peut produire un résultat aussi grand que `1111 + 1111 + 1 = 11111` — cinq bits. Faites le calcul dans un conteneur de 4 bits et vous perdriez le bit du haut silencieusement. Redimensionner à un bit de plus est l'idiome sûr, et le bit 4 *est* votre retenue sortante.

## Fabriquer un soustracteur

Le cours associe l'additionneur à un soustracteur, et en complément à deux ce sont presque le même circuit. Pour calculer `a − b`, additionnez le complément bit à bit de `b` et forcez la retenue entrante à 1 :

```vhdl
t <= resize(unsigned(a), 5) + resize(unsigned(not b), 5) + "00001";
```

C'est `a + (¬b) + 1 = a − b`. Un vrai additionneur/soustracteur ajoute un XOR par bit (commandé par une ligne `add_sub`) et injecte cette même ligne dans `cin` — un seul bit de commande fait basculer toute l'unité entre les deux opérations.

## Ce que fait l'exemple

La config de sim pilote `a` et `b` à travers plusieurs motifs (`3+5 = 8`, puis un `10 + 6 = 16` qui génère une retenue, puis `10 + 15 = 25`…). Le chronogramme montre `sum` et `cout` qui changent de façon **combinatoire** — aucune horloge n'est nécessaire parce que ce design n'a pas de registres.

## À vous de jouer

- **Rendez-le paramétrique.** Ajoutez un `generic (N : integer := 4)` à l'entity et utilisez `N` partout où apparaît le `4`. Vous avez maintenant un `adder_N` que vous pouvez instancier à n'importe quelle largeur.
- **Construisez l'additionneur/soustracteur** ci-dessus avec une entrée de commande `add_sub`.
- **Câblez-en deux ensemble.** Un pour le quartet bas, un pour le quartet haut, le premier `cout` alimentant le second `cin`, et vous avez un additionneur 8 bits à propagation de retenue — et une intuition de pourquoi le délai de retenue grandit avec la largeur.

## À retenir

- Un additionneur complet, c'est `sum = a xor b xor cin`, `cout = majorité(a, b, cin)` ; enchaînez-les pour additionner des nombres larges.
- Donnez toujours un bit de plus à l'arithmétique (`resize`) pour que la retenue ne soit pas perdue.
- La soustraction, c'est l'addition du complément à deux : `a − b = a + not b + 1`.
- La propagation de retenue est simple, mais son délai grandit avec le nombre de bits.
