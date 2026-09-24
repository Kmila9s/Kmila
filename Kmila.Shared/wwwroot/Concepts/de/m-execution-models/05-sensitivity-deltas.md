# Sensitivitätslisten & Delta-Zyklen

Du hast gesehen, dass sich Signale erst aktualisieren, „wenn der
Delta-Zyklus endet". Jetzt lernen wir die klassische Falle kennen,
die aus dieser Regel entsteht.

Eine **Sensitivitätsliste** ist das `( … )` nach `process`. Sie sagt
dem Simulator: *weck diesen `process` nur auf, wenn sich eines dieser
Signale ändert*.

Ein **Delta-Zyklus** ist die kleinste Einheit von Simulationszeit.
Innerhalb eines Delta-Zyklus **planen** Signalzuweisungen nur neue
Werte ein — die Werte selbst erscheinen erst, wenn der Delta endet.
Solange gibt dir das Lesen des Signals seinen **alten** Wert.

Beide Regeln zusammen ergeben die Falle unten. Der Player führt einen
winzigen `process` mit zwei Signalzuweisungen aus:

```vhdl
process(a, b)
begin
  out1 <= a;
  out2 <= b and out1;   -- ← was liest das hier?
end process;
```

Sieht harmlos aus. Der Studi denkt meistens: „Zeile 3 setzt `out1 = a`,
dann liest Zeile 4 `out1` und verundet es mit `b`." Aber Zeile 4 liest
das **alte** `out1` — die Transaktion aus Zeile 3 hat noch nicht
committet.

Schau den Player an und du siehst, wie `out2` als `U` rauskommt,
obwohl `a` und `b` beide `1` sind. Beim ersten Mal überraschend.

## Die zwei Wege, das zu fixen

### Variante A — Variable nehmen

Variablen aktualisieren sofort, also gibt das Lesen von `tmp` in der
nächsten Zeile den gerade zugewiesenen Wert:

```vhdl
process(a, b)
  variable tmp : std_logic;
begin
  tmp  := a;
  out1 <= tmp;
  out2 <= b and tmp;   -- liest das NEUE tmp — klappt.
end process;
```

### Variante B — auf den zweiten Delta warten

Nimm `out1` in die Sensitivitätsliste auf. Nachdem der erste Delta
`out1` committet, wacht der `process` ein zweites Mal auf und läuft
neu:

```vhdl
process(a, b, out1)
begin
  out1 <= a;
  out2 <= b and out1;   -- zweiter Wecker liest das nun committete out1
end process;
```

Beide Varianten funktionieren; Variante A wird meistens bevorzugt,
weil sie einen Delta schneller ist und der Datenfluss explizit steht.

> **Probier's:** geh Schritt für Schritt durch den Player und schau in
> die Signaltabelle. Obwohl `a=1` und `b=1` sind, endet `out2` als `U`
> — nicht `1`. Genau diese Delta-Zyklus-Regel beißt Anfänger jedes
> Semester.
