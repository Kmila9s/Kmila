# Die sieben Logikgatter

Jede digitale Schaltung, bis hinunter zur modernen GPU, ist eine Kombination von sieben elementaren Zellen. Wenn du weißt, wie sich jede verhält und wie du sie in VHDL schreibst, hast du bereits angefangen, Hardware zu entwerfen.

## Die sechs zwei-Eingang-Gatter

| Gatter | VHDL-Symbol  | Liest sich als | Ausgang ist `1`, wenn… |
|--------|--------------|----------------|------------------------|
| AND    | `a and b`    | „A und B"      | beide Eingänge 1 sind  |
| OR     | `a or b`     | „A oder B"     | mindestens ein Eingang 1 ist |
| XOR    | `a xor b`    | „A xor B"      | Eingänge unterschiedlich sind |
| NAND   | `a nand b`   | „nicht (A und B)" | mindestens ein Eingang 0 ist |
| NOR    | `a nor b`    | „nicht (A oder B)" | beide Eingänge 0 sind |
| XNOR   | `a xnor b`   | „A = B"        | Eingänge gleich sind   |

Und das ein-Eingang-NOT:

```vhdl
y <= not a;
```

## Warum NAND und NOR besonders sind

Ein klassisches Resultat der digitalen Logik: **NAND allein** (oder NOR allein) reicht, um jedes andere Gatter zu bauen. Beide nennt man „funktional vollständig". Deshalb liefern CMOS-Prozessbibliotheken jede Menge NAND2- und NOR2-Zellen — du kannst alles daraus synthetisieren.

- NOT: `not a = a nand a`
- AND: `a and b = not (a nand b)`
- OR:  `a or b = (not a) nand (not b)`

## XOR ist das mathematische

XOR wird oft mit einem Kreis-Plus (⊕) gezeichnet. Es ist das Gatter, das „Parität" berechnet — sein Ausgang ist `1`, wenn eine ungerade Zahl von Eingängen `1` ist. Damit ist es der Kern von:

- Addierern: `sum = a xor b xor carry_in`
- Paritätsprüfern und Fehlererkennungscodes
- Toggle-Flip-Flops: `q_next = q xor toggle_input`

## Alle sechs in einer Datei sehen

Wenn du das **Wahrheitstabellen-Explorer**-Lab dieses Moduls öffnest, findest du eine einzelne VHDL-Entity, die alle sechs 2-Eingangs-Gatter gleichzeitig berechnet, mit den Eingängen `a` und `b` ans Ports-Tab als Stimulus verdrahtet. Der Wellenform-Viewer zeigt dann die Reaktion jedes Ausgangs auf jede Kombination.

```vhdl
architecture rtl of gates is
begin
  o_and  <= a and b;
  o_or   <= a or  b;
  o_xor  <= a xor b;
  o_nand <= a nand b;
  o_nor  <= a nor  b;
  o_xnor <= a xnor b;
end architecture rtl;
```

Beachte: jede dieser sechs Zeilen ist **ein Draht**, keine Anweisung, die ausgeführt wird. Sechs Gatter, sechs Stücke Hardware — sie werden alle gleichzeitig ausgewertet.

## Was du behalten solltest

- Sechs Zwei-Eingang-Gatter + NOT ist das ganze Set.
- NAND und NOR sind „universell" — aus ihnen baust du alles.
- In VHDL sind Gatter Ein-Zeiler-Nebenläufige-Zuweisungen. Sie werden nicht „aufgerufen"; sie existieren.
