# NICHT-Gatter (Inverter)

Das **NICHT-Gatter** ist die einfachste Logik überhaupt: ein Eingang, ein Ausgang, und der Ausgang ist immer das **Gegenteil** des Eingangs — 0 hinein ergibt 1 hinaus, 1 ergibt 0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/02-not.html" title="Interaktives NICHT-Gatter — eine Wippe, die den Eingang invertiert" loading="lazy"></iframe></div>

**Bewege den Cursor**, um den Eingang **A** auf 0 oder 1 zu setzen. Die Figur ist eine Wippe mit gleichen Armen: drücke den Eingang (A) nach unten, und der Ausgang (Y) steigt — und umgekehrt, gleich und entgegengesetzt. Der kleine Kreis am Ausgang ist die Inversionsblase.

## Die Blase

Dieser **Kreis** am Ausgang ist die *Inversionsblase*, das Symbol des NICHT in jedem Schaltplan. Wo immer du ihn am Ausgang eines Gatters siehst (NAND, NOR, XNOR…), bedeutet er dasselbe: „und zusätzlich invertieren".

| A | Y |
|---|---|
| 0 | 1 |
| 1 | 0 |

In VHDL ist der Inverter der Operator `not`:

```vhdl
y <= not a;
```

Es ist rein **kombinatorisch**: der Ausgang hängt nur vom aktuellen Eingang ab — kein Takt, kein Speicher. Ändert sich der Eingang, kippt der Ausgang nach einer kurzen Laufzeitverzögerung.
