# Algèbre de Boole et les lois qui comptent

L'algèbre de Boole est la mathématique des valeurs vrai/faux et des opérations qu'on peut faire dessus. Tout circuit numérique — d'une seule porte à un CPU à un million de transistors — est en dernière instance une expression booléenne.

La bonne nouvelle : vous n'avez qu'à mémoriser une poignée d'identités. Avec ça vous pouvez simplifier presque n'importe quelle expression à la main.

## Les trois opérations de base

| Op  | Symbole (math) | Symbole (VHDL) | Se lit |
|-----|----------------|----------------|--------|
| AND | · ou juxtaposition | `and` | « A et B » |
| OR  | +                  | `or`  | « A ou B » |
| NOT | barre (`Ā`)        | `not` | « non A »  |

Tables de vérité des opérateurs binaires :

```
 A  B  │  A·B   A+B
──────┼────────────
 0  0  │   0     0
 0  1  │   0     1
 1  0  │   0     1
 1  1  │   1     1
```

## Les identités que vous utiliserez vraiment

**Commutativité** — l'ordre est sans importance :

```
A · B = B · A
A + B = B + A
```

**Associativité** — le groupement est sans importance :

```
(A · B) · C = A · (B · C)
(A + B) + C = A + (B + C)
```

**Distributivité** — celle du cas OR a l'air étrange :

```
A · (B + C) = A·B + A·C
A + (B · C) = (A + B) · (A + C)   ← oui, vraiment
```

**Identité et dominance** :

```
A + 0 = A      A · 1 = A
A + 1 = 1      A · 0 = 0
```

**Complément** :

```
A + Ā = 1      A · Ā = 0
```

**Lois de De Morgan** — les deux réécritures les plus utiles en design numérique :

```
non(A · B) = Ā + B̄
non(A + B) = Ā · B̄
```

De Morgan vous laisse transformer un NAND en « OR des compléments », ou un NOR en « AND des compléments », ou annuler de longues barres NOT. Toute toolchain FPGA les utilise constamment lors de l'optimisation.

## Une simplification déroulée

On part de :

```
Y = A·B + A·B̄·C + A·B̄·C̄
```

Factorisez `A` :

```
Y = A · (B + B̄·C + B̄·C̄)
```

Factorisez `B̄` à l'intérieur :

```
Y = A · (B + B̄ · (C + C̄))
```

`C + C̄ = 1` (complément), donc :

```
Y = A · (B + B̄ · 1)
  = A · (B + B̄)
  = A · 1
  = A
```

Quatre termes avec cinq opérations AND-OR s'effondrent en un fil. C'est le genre de travail que le synthétiseur fait pour vous automatiquement — mais le comprendre vous rend plus aiguisé pour lire la netlist d'un collègue.

## À retenir

- AND / OR / NOT, c'est tout ce qu'il vous faut. NAND et NOR, c'est AND/OR avec un NOT collé dessus.
- De Morgan est le couteau suisse — apprenez à le reconnaître dans les deux sens.
- Quand une sous-expression vaut `1` par complément, le produit entier se simplifie.
