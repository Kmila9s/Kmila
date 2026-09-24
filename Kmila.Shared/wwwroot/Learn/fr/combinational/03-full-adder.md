# Exemple : additionneur 4 bits à propagation de retenue

Ajouter deux nombres binaires est le « hello world » de l'arithmétique numérique. Cet exemple construit un additionneur 4 bits avec retenue entrante et sortante — exactement la cellule que tu carrelerais dans une ALU plus grosse ou dans la logique d'incrémentation d'adresse d'un CPU.

## Interface

```
port (
  a    : in  std_logic_vector(3 downto 0);
  b    : in  std_logic_vector(3 downto 0);
  cin  : in  std_logic;
  sum  : out std_logic_vector(3 downto 0);
  cout : out std_logic
);
```

## Le corps

```vhdl
signal t : unsigned(4 downto 0);
...
t    <= resize(unsigned(a), 5) + resize(unsigned(b), 5) + ("0000" & cin);
sum  <= std_logic_vector(t(3 downto 0));
cout <= t(4);
```

Ce qui se passe :

- `unsigned(a)` et `unsigned(b)` transforment les vecteurs en types numériques pour l'opérateur `+`.
- `resize(…, 5)` étend à 5 bits pour que la somme puisse déborder sur le bit de poids fort.
- `("0000" & cin)` construit un masque 5 bits avec juste `cin` en LSB.
- Après l'addition, `t(3 downto 0)` est la somme et `t(4)` la retenue sortante.

## Pourquoi 5 bits ?

Additionner deux nombres 4 bits avec une retenue entrante peut produire un résultat aussi grand que `1111 + 1111 + 1 = 11111` — cinq bits. Faire le calcul dans un conteneur 4 bits perdrait silencieusement le bit du haut. Resize est l'idiome sûr.

## Ce que fait l'exemple

La config de sim pilote `a` et `b` à travers quatre motifs (`3+5 = 8`, puis un `10 + 6 = 16` qui génère une retenue, puis `10 + 15 = 25` …). Le chronogramme montre `sum` et `cout` qui changent en combinatoire — pas d'horloge nécessaire puisque ce design n'a pas de registres.

## À toi de jouer

- **Rends-le paramétrique.** Ajoute un `generic (N : integer := 4)` à l'entity et utilise `N` partout où apparaît le 4. Tu as un `adder_N` instantiable à n'importe quelle largeur.
- **Construis un soustracteur.** Inverse `b` et ajoute un `+1` — c'est la soustraction en complément à deux. Pas de matériel en plus à part un NOT par bit.
- **Câble-en deux ensemble.** Un pour le quartet bas, un pour le quartet haut, et tu as un additionneur 8 bits à propagation entre eux.
