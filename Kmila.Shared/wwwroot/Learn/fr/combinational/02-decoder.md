# Exemple : décodeur 3 vers 8

Un décodeur prend un petit code binaire et le transforme en sortie **one-hot** : exactement une sortie est à 1, et sa position correspond au code.

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

On trouve des décodeurs dans tous les systèmes de mémoire — les lignes d'adresse pilotent un décodeur, et la sortie « row match » active exactement une ligne. Chaque fois que tu entends « décodeur d'adresse », c'est ce qu'il y a dessous.

## Le VHDL

Un décodeur est un cas d'école pour `with/select` :

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

Remarque le `when others` à la fin. `sel` est un `std_logic_vector(2 downto 0)` — 3 bits — il y a donc 2³ = 8 valeurs binaires, mais comme `std_logic` a neuf états, le compilateur a besoin de la clause `others` pour couvrir toutes les combinaisons (elle se synthétisera comme `"111"`).

## Ce que fait l'exemple

L'exemple fait cycler `sel` à travers les huit valeurs 3 bits, 10 µs par valeur. Dans l'onglet Waveforms, tu verras les 8 bits de `y` former un `'1'` marchant — un « chenillard » numérique.

## À toi

Une fois que ça tourne :

- Élargis le décodeur à une version 4 vers 16. Tu peux le faire à la main, ou réécrire le corps en `y <= std_logic_vector(shift_left(to_unsigned(1, 16), to_integer(unsigned(sel))));`. Les deux se synthétisent à l'identique — prends celle que tu trouves la plus lisible.
- Ajoute une entrée `enable`. Quand `enable = '0'`, toutes les sorties sont basses, quel que soit `sel`.
