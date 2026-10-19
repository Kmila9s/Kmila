# Beispiel: D-Flip-Flop mit asynchronem Reset

Das D-Flip-Flop (DFF) ist die häufigste Speicherzelle im Digitaldesign — der Star der „elementos de memoria tipo Flip-Flop“ aus Einheit IV. Ein moderner FPGA hat zig- bis hunderttausende davon. Jedes Register, jeder Zähler, jede Pipeline-Stufe ist aus DFFs gebaut.

## Flip-Flop vs. Latch (die eine Unterscheidung, die sitzen muss)

Beide speichern ein Bit, aber sie hören unterschiedlich zu:

- Ein **Latch** ist *pegel-sensitiv*: solange sein Enable high ist, folgt der Ausgang dem Eingang (er ist „transparent“); er hält nur, wenn das Enable low ist.
- Ein **Flip-Flop** ist *flanken-sensitiv*: es tastet den Eingang in einem einzigen Augenblick ab — der Taktflanke — und hält für den Rest des Zyklus.

Diese Lektion baut ein Flip-Flop. Die nächste Fix-it-Übung zeigt, was passiert, wenn du *versehentlich* ein Latch baust. Behalte den Unterschied im Kopf: flanken-getriggert = Flip-Flop, pegel-getriggert = Latch.

## Was es tut

Bei jeder steigenden Flanke von `clk` wird der Wert auf `d` nach `q` kopiert. Zwischen den Flanken hält `q` seinen Wert, auch wenn sich `d` ändert. Wenn `reset` high ist, wird `q` sofort auf `'0'` gezwungen — das ist der **asynchrone** Reset.

## Der Code

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

Zeile für Zeile:

- **Sensitivitätsliste `(clk, reset)`** — der Prozess wacht bei beiden auf. `reset` aufzulisten ist das, was den Reset asynchron macht: er wirkt in dem Moment, in dem `reset` steigt, nicht erst an der nächsten Taktflanke.
- **`if reset = '1'`** — Reset hat Priorität. Wenn der Reset-Pin high ist, kümmert uns nicht, was der Takt oder `d` tun.
- **`elsif rising_edge(clk)`** — nur wenn der Takt von 0 auf 1 geht, wird `d` in `q` abgetastet.

## Setup und Hold: warum Flanken Luft zum Atmen brauchen

Ein Flip-Flop kann `d` nur zuverlässig erfassen, wenn `d` für ein kurzes Fenster **vor** der Flanke (Setup-Zeit) und **nach** ihr (Hold-Zeit) stabil ist. Verletzt du dieses Fenster, kann der Ausgang *metastabil* werden — zwischen `'0'` und `'1'` schweben, bevor er sich unvorhersehbar einpegelt. Du modellierst diese Zeiten in diesem Beispiel nicht, aber sie sind der Grund, warum echte Designs asynchrone Eingänge durch zwei Flip-Flops (einen „Synchronizer“) registrieren, bevor sie sie nutzen. Gut, den Namen jetzt zu kennen; du triffst ihn wieder.

## Wellenform-Durchgang

Das Beispiel treibt `reset = '1'` für die ersten 5 µs, dann gibt es ihn frei. `d` togglet bei 10, 30 und 50 µs. Wechsle nach Run zum Wellenform-Tab und beachte:

- Vor 5 µs: `q = '0'` (vom Reset gehalten).
- Nachdem der Reset freigegeben ist, ändert sich `q` nur an steigenden Taktflanken — der Wert von `d` *an der Flanke* wird erfasst.
- Änderungen von `d` zwischen Taktflanken haben keine Wirkung auf `q`.

## Sync vs. Async Reset

Es gibt zwei Denkschulen, und die Wahl hat echte Konsequenzen:

**Asynchroner Reset** (`if reset = '1'` außerhalb des Flanken-Blocks, wie oben):
- Pro: setzt zurück, auch wenn der Takt nicht läuft; behandelt Power-On sauber.
- Contra: die *Freigabe* des Resets ist nicht auf den Takt ausgerichtet, sodass verschiedene Flip-Flops den Reset an verschiedenen Flanken verlassen können — ein Metastabilitätsrisiko — und es kann die maximale Taktfrequenz begrenzen.

**Synchroner Reset** (`if rising_edge(clk) then if reset = '1' then …`):
- Pro: verhält sich wie jeder andere synchrone Eingang; bildet sich direkt auf den eingebauten Reset des Flip-Flops ab und spielt gut mit dem Timing-Closure zusammen.
- Contra: braucht einen laufenden Takt, um zu wirken.

Zum *Lernen* ist der asynchrone Reset leichter zu durchdenken, weshalb dieses Beispiel ihn nutzt. Für *produktive FPGA-Arbeit* bevorzugen Herstellervorgaben (zum Beispiel AMD/Xilinx' Whitepaper „Get Smart About Reset“ und spätere Methodik-Guides) im Allgemeinen **synchrone** Resets, und manche frei laufende Logik braucht gar keinen Reset. Folge der Empfehlung deines Zielbausteins.

## Die anderen Flip-Flops

Der Kurs behandelt auch SR-, JK- und T-Flip-Flops. Sie sind alle ein D-Flip-Flop mit ein wenig kombinatorischer Logik vorn dran, die den nächsten Zustand berechnet:

- **T (Toggle):** `d = q xor t` → hält bei `t=0`, kippt bei `t=1`.
- **JK:** `d = (j and not q) or (not k and q)`.
- **SR:** Set/Reset, mit dem Fall `s=r=1` wegdefiniert.

Sobald du also ein sauberes D-Flip-Flop schreiben kannst, kannst du jedes von ihnen bauen.

## Du bist dran

- Füge einen **Enable**-Eingang hinzu: `q` soll nur an der steigenden Flanke aktualisieren, wenn `enable = '1'`.
- Lass es einen **4-Bit-Bus registrieren**: ändere `d` und `q` zu `std_logic_vector(3 downto 0)` und den Reset-Wert zu `(others => '0')`.
- Füge einen `q_not`-Ausgang hinzu, der immer `not q` ist. Bonus: verkable `q_not` zurück in `d` und beobachte, wie es zu einem **Teiler-durch-zwei**-Takt wird — ein T-Flip-Flop in Verkleidung.
- Wandle den Reset zu **synchron** um und vergleiche die Wellenformen bei der Reset-Freigabe.

## Was du behalten solltest

- Ein D-Flip-Flop tastet `d` an der Taktflanke ab und hält zwischen den Flanken — flanken-getriggert, anders als ein pegel-sensitives Latch.
- `process(clk, reset)` mit dem Reset *vor* `rising_edge` geprüft gibt einen asynchronen Reset; setz ihn *in* die Flanke für einen synchronen.
- Respektiere Setup/Hold; synchronisiere asynchrone Eingänge, um Metastabilität zu vermeiden.
- SR-, JK- und T-Flip-Flops sind nur ein D-Flip-Flop plus Nächster-Zustand-Logik.
