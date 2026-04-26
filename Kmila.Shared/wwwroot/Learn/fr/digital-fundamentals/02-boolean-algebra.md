# Algèbre de Boole et les lois qui comptent

L'algèbre de Boole est la mathématique des valeurs vrai/faux et des opérations que tu peux effectuer dessus. Tout circuit numérique — d'une seule porte à un CPU d'un million de transistors — est en fin de compte une expression booléenne.

La bonne nouvelle : tu n'as qu'à retenir une poignée d'identités. Avec celles-ci, tu peux simplifier presque n'importe quelle expression à la main.

## Les trois opérations de base

| Op | Symbole (math)     | Symbole (VHDL) | Se lit |
|----|--------------------|----------------|--------|
| AND | · ou juxtaposition | `and`          | « A et B » |
| OR  | +                  | `or`           | « A ou B » |
| NOT | barre (`Ā`)        | `not`          | « non A »  |

Tables de vérité des opérateurs binaires :

```
 A  B  │  A·B   A+B
──────┼────────────
 0  0  │   0     0
 0  1  │   0     1
 1  0  │   0     1
 1  1  │   1     1
```

## Les identités que tu utiliseras vraiment

**Commutativité** — l'ordre n'a pas d'importance :

```
A · B = B · A
A + B = B + A
```

**Associativité** — le groupement n'a pas d'importance :

```
(A · B) · C = A · (B · C)
(A + B) + C = A + (B + C)
```

**Distributivité** — celle qui paraît bizarre dans le cas du OR :

```
A · (B + C) = A·B + A·C
A + (B · C) = (A + B) · (A + C)   ← oui, vraiment
```

**Identité et absorption** :

```
A + 0 = A      A · 1 = A
A + 1 = 1      A · 0 = 0
```

**Complément** :

```
A + Ā = 1      A · Ā = 0
```

**Lois de De Morgan** — les deux réécritures les plus utiles du design numérique :

```
not(A · B) = Ā + B̄
not(A + B) = Ā · B̄
```

De Morgan te laisse transformer un NAND en « OR de compléments », ou un NOR en « AND de compléments », ou annuler de longues barres NOT. Toute toolchain FPGA les utilise en permanence pendant l'optimisation.

## Une simplification détaillée

Commence par :

```
Y = A·B + A·B̄·C + A·B̄·C̄
```

Factorise `A` :

```
Y = A · (B + B̄·C + B̄·C̄)
```

Factorise `B̄` à l'intérieur :

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

Quatre termes avec cinq opérations AND-OR s'effondrent en un fil. C'est le genre de travail que le synthétiseur fait pour toi automatiquement — mais le comprendre te rend plus affûté pour lire la netlist de quelqu'un d'autre.

## À retenir

- AND / OR / NOT sont tout ce dont tu as besoin. NAND et NOR sont AND/OR avec un NOT collé dessus.
- De Morgan est le couteau suisse — apprends à le reconnaître dans les deux sens.
- Quand une sous-expression se simplifie à `1` via le complément, tout le produit se simplifie.
