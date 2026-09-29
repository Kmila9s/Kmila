# Boolesche Algebra und die wichtigen Gesetze

Boolesche Algebra ist die Mathematik von Wahr/Falsch-Werten und der Operationen, die du darauf ausführen kannst. Jede digitale Schaltung — von einem einzelnen Gatter bis zu einer CPU mit Millionen Transistoren — ist letztlich ein boolescher Ausdruck.

Die gute Nachricht: du musst dir nur eine Handvoll Identitäten merken. Damit kannst du fast jeden Ausdruck per Hand vereinfachen.

## Die drei Grundoperationen

| Op | Symbol (Mathe) | Symbol (VHDL) | Liest sich als |
|----|----------------|---------------|----------------|
| AND | · oder Aneinanderreihung | `and` | „A und B“ |
| OR  | +                  | `or`  | „A oder B“ |
| NOT | Überstrich (`Ā`)   | `not` | „nicht A“  |

Wahrheitstabellen der binären Operatoren:

```
 A  B  │  A·B   A+B
──────┼────────────
 0  0  │   0     0
 0  1  │   0     1
 1  0  │   0     1
 1  1  │   1     1
```

## Die Identitäten, die du wirklich nutzen wirst

**Kommutativität** — Reihenfolge ist egal:

```
A · B = B · A
A + B = B + A
```

**Assoziativität** — Gruppierung ist egal:

```
(A · B) · C = A · (B · C)
(A + B) + C = A + (B + C)
```

**Distributivität** — diejenige im OR-Fall sieht komisch aus:

```
A · (B + C) = A·B + A·C
A + (B · C) = (A + B) · (A + C)   ← ja, wirklich
```

**Identität und Dominanz**:

```
A + 0 = A      A · 1 = A
A + 1 = 1      A · 0 = 0
```

**Komplement**:

```
A + Ā = 1      A · Ā = 0
```

**Die Gesetze von De Morgan** — die zwei nützlichsten Umformungen im Digitaldesign:

```
not(A · B) = Ā + B̄
not(A + B) = Ā · B̄
```

De Morgan lässt dich ein NAND in „OR der Komplemente“ verwandeln, oder ein NOR in „AND der Komplemente“, oder lange NOT-Striche kürzen. Jede FPGA-Toolchain nutzt sie ständig bei der Optimierung.

## Eine durchgerechnete Vereinfachung

Start mit:

```
Y = A·B + A·B̄·C + A·B̄·C̄
```

Klammere `A` aus:

```
Y = A · (B + B̄·C + B̄·C̄)
```

Klammere `B̄` innen aus:

```
Y = A · (B + B̄ · (C + C̄))
```

`C + C̄ = 1` (Komplement), also:

```
Y = A · (B + B̄ · 1)
  = A · (B + B̄)
  = A · 1
  = A
```

Vier Terme mit fünf AND-OR-Operationen kollabieren auf einen Draht. Das ist die Arbeit, die der Synthesizer automatisch für dich macht — aber sie zu verstehen macht dich beim Lesen fremder Netlists schärfer.

## Was du behalten solltest

- AND / OR / NOT ist alles, was du brauchst. NAND und NOR sind AND/OR mit einem NOT dran.
- De Morgan ist das Schweizer Taschenmesser — lerne, ihn in beide Richtungen zu erkennen.
- Wenn ein Teilausdruck per Komplement zu `1` wird, kollabiert das ganze Produkt.
