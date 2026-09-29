# Exemple : décodeur 3 vers 8

Un décodeur prend un petit code binaire et le transforme en sortie **one-hot** : exactement une sortie est haute, et sa position correspond au code.

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

Les décodeurs vivent dans tout système mémoire — les lignes d'adresse pilotent un décodeur, et la sortie « match de ligne » active exactement une ligne. Quand vous entendez « décodeur d'adresse », c'est ça qui est en dessous.

## Le VHDL

Un décodeur est le cas d'école pour `with/select` :

```vhdl
with sel select
  y <=
    "00000001" when "000",
    "00000010" when "001",
    "00000100" when "010",
    "00001000" when "011",
    "00010000" when "100",
    "00100000" when "101",
    "01000000" when "110",
    "10000000" when others;
```

Notez le `when others` à la fin. `sel` est un `std_logic_vector(2 downto 0)` — 3 bits — donc il y a 2³ = 8 valeurs binaires, mais comme `std_logic` a neuf états, le compilateur a besoin de la clause `others` pour couvrir toute combinaison (elle se synthétise comme `"111"`).

## Ce que fait l'exemple

L'exemple parcourt `sel` sur les huit valeurs 3 bits, 10 µs par valeur. Sur l'onglet Formes d'onde vous verrez les 8 bits de `y` devenir un `'1'` qui se déplace — un « chenillard » numérique.

## À vous de jouer

Une fois que ça marche :

- Élargissez le décodeur en version 4 vers 16. À la main, ou réécrivez le corps en `y <= std_logic_vector(shift_left(to_unsigned(1, 16), to_integer(unsigned(sel))));`. Les deux se synthétisent à l'identique — choisissez ce que vous trouvez le plus lisible.
- Ajoutez une entrée `enable`. Quand `enable = '0'`, toutes les sorties sont basses quel que soit `sel`.
