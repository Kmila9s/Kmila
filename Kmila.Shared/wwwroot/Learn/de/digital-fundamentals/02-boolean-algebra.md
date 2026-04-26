# Boolesche Algebra und die wichtigen Gesetze

Die boolesche Algebra ist die Mathematik der Wahr/Falsch-Werte und der Operationen, die du darauf ausführen kannst. Jede digitale Schaltung — von einem einzelnen Gatter bis zu einer Millionen-Transistor-CPU — ist letztlich ein boolescher Ausdruck.

Die gute Nachricht: Du musst dir nur eine Handvoll Identitäten merken. Damit kannst du fast jeden Ausdruck von Hand vereinfachen.

## Die drei Grundoperationen

| Op | Symbol (Mathe) | Symbol (VHDL) | Liest sich als |
|----|----------------|---------------|----------------|
| AND | · oder Nebeneinander | `and` | "A und B" |
| OR  | +                    | `or`  | "A oder B" |
| NOT | Überstrich (`Ā`)     | `not` | "nicht A" |

Wahrheitstabellen für die Binäroperatoren:

```
 A  B  │  A·B   A+B
──────┼────────────
 0  0  │   0     0
 0  1  │   0     1
 1  0  │   0     1
 1  1  │   1     1
```

## Die Identitäten, die du wirklich brauchst

**Kommutativität** — die Reihenfolge spielt keine Rolle:

```
A · B = B · A
A + B = B + A
```

**Assoziativität** — die Gruppierung spielt keine Rolle:

```
(A · B) · C = A · (B · C)
(A + B) + C = A + (B + C)
```

**Distributivität** — die im OR-Fall seltsam aussieht:

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

**De Morgans Gesetze** — die zwei nützlichsten Umformungen im Digitaldesign:

```
not(A · B) = Ā + B̄
not(A + B) = Ā · B̄
```

Mit De Morgan kannst du ein NAND in "OR der Komplemente" verwandeln, ein NOR in "AND der Komplemente", oder lange NOT-Striche aufheben. Jede FPGA-Toolchain nutzt sie ständig während der Optimierung.

## Eine durchgearbeitete Vereinfachung

Starte mit:

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

Vier Terme mit fünf AND-OR-Operationen kollabieren zu einem Draht. Das ist die Art von Arbeit, die der Synthesizer automatisch für dich erledigt — aber sie zu verstehen macht dich schärfer darin, die Netzliste eines anderen zu lesen.

## Was zu merken ist

- AND / OR / NOT sind alles, was du brauchst. NAND und NOR sind AND/OR mit einem angehängten NOT.
- De Morgan ist das Schweizer Taschenmesser — lerne, ihn in beide Richtungen zu erkennen.
- Wenn ein Teilausdruck mit Komplement zu `1` auswertet, vereinfacht sich das gesamte Produkt.
