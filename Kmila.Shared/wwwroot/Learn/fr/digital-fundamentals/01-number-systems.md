# Systèmes de numération : bits, hexadécimal, complément à deux

Les circuits numériques ne connaissent pas le « sept » — ils connaissent des rangées d'interrupteurs soit allumés soit éteints. Pour concevoir avec eux, tu dois être à l'aise pour passer entre la représentation que les humains aiment (décimal) et celle que le silicium comprend (binaire), avec l'hexadécimal comme raccourci utile au milieu.

## Binaire (base 2)

Un **bit** est une valeur unique on/off, écrite `0` ou `1`. Un **octet** (byte) fait huit bits.

Des piles de bits encodent des entiers positionnellement, tout comme le décimal — chaque position est une puissance de deux :

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

Écrire 32 bits en binaire te donne une soupe de 32 caractères que personne ne peut relire :

```
00000000 00000000 11001010 11111110
```

L'hexadécimal groupe les bits par quatre. Chaque chiffre hex couvre exactement un nibble (4 bits), donc 32 bits deviennent une chaîne bien rangée de 8 caractères :

```
0x0000CAFE
```

La correspondance est figée :

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

En VHDL, tu écriras des littéraux hex pour les vecteurs dont la longueur est un multiple de quatre :

```vhdl
signal addr : std_logic_vector(7 downto 0) := x"CA";  -- 8 bits: 11001010
```

## Complément à deux (entiers signés)

Le binaire tel que montré plus haut n'encode que des nombres **non négatifs**. Pour faire rentrer des nombres négatifs dans le même motif binaire, les concepteurs numériques utilisent le **complément à deux** :

1. Écris le nombre positif en binaire.
2. Inverse chaque bit.
3. Ajoute 1.

Pour représenter `-5` sur 8 bits : `+5 = 00000101`, inverse → `11111010`, ajoute 1 → `11111011`.

Pourquoi ce schéma ? Parce que l'addition fonctionne à l'identique, que les nombres soient signés ou non — pas besoin de deux additionneurs. `-5 + 7` en complément à deux 8 bits :

```
  11111011   (-5)
+ 00000111   ( 7)
-----------
 100000010
```

Le neuvième bit déborde par le haut, et les 8 bits du bas valent `00000010 = 2`. Ce qui est la bonne réponse.

## À retenir

- Les bits sont les atomes ; les octets sont des groupes de 8.
- Binaire et hexadécimal sont le **même nombre** avec des habits différents. L'hex est plus facile à lire.
- Le complément à deux encode les négatifs en inversant-puis-ajoutant-un — et c'est la raison pour laquelle `A + B` dans ton ALU fonctionne indépendamment du signe.
