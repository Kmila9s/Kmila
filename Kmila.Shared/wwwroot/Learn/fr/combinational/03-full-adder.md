# Exemple : additionneur à retenue propagée 4 bits

Additionner deux nombres binaires, c'est le « hello world » de l'arithmétique numérique. Cet exemple construit un additionneur 4 bits avec retenue entrante et retenue sortante — exactement la cellule que tu empilerais dans une ALU plus grande ou dans la logique d'incrémentation d'adresse d'un CPU.

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

- `unsigned(a)` et `unsigned(b)` convertissent les vecteurs en types numériques pour l'opérateur `+`.
- `resize(…, 5)` étend à 5 bits pour que la somme puisse déborder sur le bit de poids fort.
- `("0000" & cin)` construit un masque 5 bits contenant uniquement `cin` sur le LSB.
- Après l'addition, `t(3 downto 0)` est la somme et `t(4)` est la retenue sortante.

## Pourquoi 5 bits ?

Additionner deux nombres 4 bits avec une retenue entrante peut produire un résultat aussi grand que `1111 + 1111 + 1 = 11111` — cinq bits. Fais le calcul dans un conteneur 4 bits et tu perdrais silencieusement le bit de poids fort. Resize est l'idiome sûr.

## Ce que fait l'exemple

La configuration de simulation pilote `a` et `b` à travers quatre patterns (`3+5 = 8`, puis une addition avec retenue `10 + 6 = 16`, puis `10 + 15 = 25`…). La forme d'onde montre `sum` et `cout` changer de manière combinatoire — pas besoin d'horloge puisque ce design n'a pas de registres.

## À toi

- **Rends-le paramétrique.** Ajoute un `generic (N : integer := 4)` à l'entité et utilise `N` partout où le 4 apparaît. Tu as maintenant un `adder_N` que tu peux instancier à n'importe quelle largeur.
- **Construis un soustracteur.** Inverse `b` et ajoute un `+1` supplémentaire — c'est la soustraction en complément à deux. Aucun matériel supplémentaire hormis un NOT par bit.
- **Chaîne-en deux.** Mets-en un pour le nibble bas et un pour le haut, et tu obtiens un additionneur 8 bits avec propagation de retenue entre les deux.
