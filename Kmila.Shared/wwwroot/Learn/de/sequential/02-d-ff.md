# Beispiel: D-Flipflop mit asynchronem Reset

Das D-Flipflop (DFF) ist die häufigste Speicherzelle im Digitaldesign. Ein moderner FPGA hat Zehntausende bis Hunderttausende davon. Jedes Register, jeder Zähler und jede Pipeline-Stufe wird aus DFFs gebaut.

## Was es tut

An jeder steigenden Flanke von `clk` wird der Wert an `d` nach `q` kopiert. Zwischen den Flanken hält `q` seinen Wert, auch wenn sich `d` ändert.

Wenn `reset` hoch ist, wird `q` sofort auf `'0'` gezwungen — das ist der **asynchrone** Reset.

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

- **Sensitivitätsliste `(clk, reset)`** — der Prozess wacht bei einem der beiden auf. Das macht den Reset asynchron.
- **`if reset = '1'`** — Reset hat Priorität. Wenn der Reset-Pin hoch ist, kümmern wir uns nicht darum, was Takt oder `d` tun.
- **`elsif rising_edge(clk)`** — nur wenn der Takt von 0 auf 1 geht, wird `d` abgetastet.

## Waveform-Durchlauf

Das Beispiel treibt `reset = '1'` für die ersten 5 µs, dann lässt es los. `d` kippt bei 10, 30 und 50 µs. Wechsle nach Run zum Waveforms-Tab und beachte:

- Vor 5 µs, `q = '0'` (vom Reset gehalten).
- Nachdem Reset losgelassen wird, ändert sich `q` nur an steigenden Taktflanken — der Wert von `d` an der Flanke wird erfasst.
- Änderungen an `d` zwischen Taktflanken haben keine Wirkung.

## Sync vs. Async Reset

Es gibt zwei Denkschulen:

**Async Reset** (`if reset = '1'` außerhalb des Flanken-Blocks, wie oben):
- Vorteile: setzt zurück, auch wenn der Takt nicht läuft. Behandelt Power-On sauber.
- Nachteile: schwerer, das Timing bei sehr hohen Taktraten einzuhalten; Xilinx' Empfehlung für die 7er-Serie und später ist, es zu vermeiden.

**Sync Reset** (`if rising_edge(clk) then if reset = '1'…`):
- Vorteile: verhält sich wie jeder andere Eingang an der Taktflanke; passt gut zum Synthesizer.
- Nachteile: braucht einen laufenden Takt, um zurückzusetzen.

Zum Lernen ist der asynchrone Reset leichter nachzuvollziehen. Für Produktions-FPGA-Arbeit lies die Anleitung deines Herstellers.

## Du bist dran

- Füge einen **Enable**-Eingang hinzu. `q` soll nur an der steigenden Flanke aktualisiert werden, wenn `enable = '1'`.
- Mache es so, dass es einen **4-Bit-Bus registriert** statt ein einzelnes Bit. Ändere `d` und `q` auf `std_logic_vector(3 downto 0)` und passe den Reset-Wert auf `(others => '0')` an.
- Füge einen `q_not`-Ausgang hinzu, der immer das Inverse von `q` ist. Bonus: Verschalte `q_not` zurück nach `d` und beobachte, wie es zu einem Teile-durch-zwei-Takt wird.
