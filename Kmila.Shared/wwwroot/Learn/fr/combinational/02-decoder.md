# Exemple : décodeur 3 vers 8

Un décodeur prend un petit code binaire et le transforme en une sortie **one-hot** : exactement une ligne de sortie est haute, et sa position correspond au code. C'est l'un des circuits nommés de l'unité combinatoire (« encodeur et décodeur »), et c'est le partenaire naturel de l'encodeur, qui fait l'inverse.

```
  sel  │  y
  000  │  00000001
  001  │  00000010
  010  │  00000100
  011  │  00001000
  100  │  00010000
  101  │  00100000
  110  │  01000000
  111  │  10000000
```

Les décodeurs vivent au cœur de tout système mémoire — les lignes d'adresse pilotent un décodeur, et la sortie « ligne qui correspond » active exactement une word line. Chaque fois que vous entendez « décodeur d'adresse », c'est ça qu'il y a dessous. La même structure implémente aussi un jeu complet de minterms : un décodeur à n entrées sort les 2ⁿ minterms de ses entrées, donc n'importe quelle fonction booléenne peut se construire en faisant un OU sur les bonnes sorties du décodeur.

## Le VHDL

Un décodeur est un cas d'école pour `with/select` :

```vhdl
with sel select
  y <= "00000001" when "000",
       "00000010" when "001",
       "00000100" when "010",
       "00001000" when "011",
       "00010000" when "100",
       "00100000" when "101",
       "01000000" when "110",
       "10000000" when others;
```

Notez le `when others` à la fin. `sel` est un `std_logic_vector(2 downto 0)` — 3 bits — donc il y a 2³ = 8 valeurs *voulues*, mais comme chaque bit est un `std_logic` à neuf valeurs, le compilateur voit bien plus de motifs possibles et a besoin de `others` pour être exhaustif. Ici `others` se replie sur le cas `"111"` (il se synthétise vers la même sortie).

## L'alternative qui passe à l'échelle

Écrire huit lignes à la main convient pour un décodeur 3 vers 8, mais ça ne passe pas à l'échelle. La même fonction en une ligne, à l'aide d'un décalage, c'est :

```vhdl
y <= std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Lisez-la de l'intérieur vers l'extérieur : prenez `sel` comme un nombre, décalez un unique `'1'` vers la gauche de ce nombre de positions à l'intérieur d'un champ de 8 bits, puis reconvertissez en `std_logic_vector`. Les deux versions se synthétisent à l'identique — choisissez celle que vous trouvez la plus lisible. (La table explicite est plus facile à vérifier pour un débutant face à la table de vérité ; la forme à décalage est celle que vous élargiriez à 32 ou 64 sorties.)

## Ce que fait l'exemple

L'exemple parcourt `sel` sur les huit valeurs de 3 bits, 10 µs par valeur. Sur l'onglet Formes d'onde, vous verrez les 8 bits de `y` devenir un `'1'` qui se déplace — un « chenillard » numérique.

## Ajoutez un enable (et rencontrez le démux)

Les vrais décodeurs ont un **enable**. Quand il est désactivé, toutes les sorties sont basses :

```vhdl
y <= (others => '0') when enable = '0' else
     std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Voici un joli rapprochement : un décodeur-avec-enable, où l'enable est une ligne de *données* plutôt qu'une constante, **est un démultiplexeur** — la valeur de l'enable est aiguillée vers la seule sortie que l'adresse désigne, et le reste reste à `'0'`. Le décodeur et le démux sont le même matériel portant des étiquettes différentes.

## À vous de jouer

Une fois que ça tourne :

- Élargissez-le en un décodeur **4 vers 16**. Essayez à la main (seize lignes), puis avec la forme à décalage — remarquez à quel point la forme à décalage change peu (juste les deux `16`).
- Ajoutez l'`enable` ci-dessus et confirmez sur le chronogramme que chaque sortie tombe à `'0'` tant que `enable = '0'`.
- Construisez l'inverse : un **encodeur prioritaire 8 vers 3** qui sort l'indice de l'entrée active de plus haut rang. C'est l'autre moitié de cette section du cours.

## À retenir

- Un décodeur fait correspondre un code de n bits à une sortie one-hot de 2ⁿ ; ses sorties sont les minterms des entrées.
- `with/select` avec un `when others` obligatoire est la façon claire, en forme de table de vérité, de l'écrire.
- L'idiome `shift_left(to_unsigned(1, N), …)` est la version qui passe à n'importe quelle largeur.
- Un décodeur avec un enable piloté par des données est un démultiplexeur ; son image miroir est l'encodeur.
