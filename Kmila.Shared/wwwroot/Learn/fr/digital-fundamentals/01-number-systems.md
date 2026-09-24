# Systèmes numériques : bits, hex, complément à deux

Les circuits numériques ne connaissent pas « sept » — ils connaissent des rangées d'interrupteurs allumés ou éteints. Pour concevoir avec eux, il faut être à l'aise pour naviguer entre la représentation que les humains aiment (décimal) et celle que le silicium comprend (binaire), avec l'hexadécimal comme raccourci utile au milieu.

## Binaire (base 2)

Un **bit** est une valeur on/off, écrite `0` ou `1`. Un **octet** vaut huit bits.

Des piles de bits encodent les entiers positionnellement, comme le décimal — chaque place est une puissance de deux :

```
  1 0 1 1 0
  │ │ │ │ └── 2⁰ = 1
  │ │ │ └──── 2¹ = 2
  │ │ └────── 2² = 0
  │ └──────── 2³ = 8
  └────────── 2⁴ = 16

  16 + 0 + 0 + 2 + 0 = 18
```

Donc `10110₂ = 18₁₀`.

## Hexadécimal (base 16)

Écrire 32 bits en binaire donne une soupe de 32 caractères que personne ne peut relire :

```
00000000 00000000 11001010 11111110
```

L'hex groupe les bits quatre par quatre. Chaque chiffre hex couvre exactement un quartet (4 bits), donc 32 bits deviennent une chaîne propre de 8 caractères :

```
0x0000CAFE
```

La correspondance est fixe :

| bin  | hex | bin  | hex |
|------|-----|------|-----|
| 0000 | 0   | 1000 | 8   |
| 0001 | 1   | 1001 | 9   |
| 0010 | 2   | 1010 | A   |
| 0011 | 3   | 1011 | B   |
| 0100 | 4   | 1100 | C   |
| 0101 | 5   | 1101 | D   |
| 0110 | 6   | 1110 | E   |
| 0111 | 7   | 1111 | F   |

En VHDL tu écriras des littéraux hex pour des vecteurs dont la longueur est multiple de quatre :

```vhdl
signal addr : std_logic_vector(7 downto 0) := x"CA";  -- 8 bits : 11001010
```

## Complément à deux (entiers signés)

Le binaire montré ci-dessus n'encode que des nombres **non négatifs**. Pour faire tenir les négatifs dans le même bit-pattern, les concepteurs numériques utilisent le **complément à deux** :

1. Écrire le nombre positif en binaire.
2. Inverser chaque bit.
3. Ajouter 1.

Pour représenter `-5` sur 8 bits : `+5 = 00000101`, invert → `11111010`, +1 → `11111011`.

Pourquoi ce schéma ? Parce que l'addition fonctionne à l'identique, signés ou non — tu n'as pas besoin de deux additionneurs. `-5 + 7` en complément à deux 8 bits :

```
  11111011   (-5)
+ 00000111   ( 7)
-----------
 100000010
```

Le neuvième bit déborde par le haut, et les 8 du bas valent `00000010 = 2`. Ce qui est la bonne réponse.

## À retenir

- Les bits sont les atomes ; les octets sont des groupes de 8.
- Binaire et hex sont **le même nombre** sous un autre vernis. L'hex est plus lisible.
- Le complément à deux encode les négatifs par invert-et-+1 — et c'est la raison pour laquelle `A + B` dans ton ALU fonctionne quel que soit le signe.
