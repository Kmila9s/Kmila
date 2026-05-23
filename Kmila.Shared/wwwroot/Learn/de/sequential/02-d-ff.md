# Beispiel: D-Flip-Flop mit asynchronem Reset

Das D-Flip-Flop (DFF) ist die häufigste Speicherzelle im Digitaldesign. Ein moderner FPGA hat zig- bis hunderttausende davon. Jedes Register, jeder Zähler, jede Pipeline-Stufe ist aus DFFs gebaut.

## Was es tut

Bei jeder steigenden Flanke von `clk` wird der Wert auf `d` nach `q` kopiert. Zwischen den Flanken hält `q` seinen Wert, auch wenn sich `d` ändert.

Wenn `reset` high ist, wird `q` sofort auf `'0'` gezwungen — das ist der **asynchrone** Reset.

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

- **Sensitivitätsliste `(clk, reset)`** — der Prozess wacht bei beiden auf. Das macht den Reset asynchron.
- **`if reset = '1'`** — Reset hat Priorität. Wenn der Reset-Pin high ist, kümmert uns weder der Takt noch `d`.
- **`elsif rising_edge(clk)`** — nur wenn der Takt von 0 auf 1 wechselt, wird `d` abgetastet.

## Wellenform-Durchgang

Die Sim treibt `reset = '1'` für die ersten 5 µs, dann gibt sie ihn frei. `d` togglet bei 10, 30 und 50 µs. Wechsle nach Run zum Wellenform-Tab und beachte:

- Vor 5 µs: `q = '0'` (vom Reset gehalten).
- Nach dem Freigeben des Reset ändert sich `q` nur an steigenden Taktflanken — der Wert von `d` an der Flanke wird übernommen.
- Änderungen von `d` zwischen Taktflanken haben keine Wirkung.

## Sync vs. Async Reset

Zwei Denkschulen:

**Asynchroner Reset** (`if reset = '1'` außerhalb des Edge-Blocks, wie oben):
- Pro: setzt zurück, auch wenn der Takt nicht läuft. Behandelt Power-On sauber.
- Contra: schwerer, Timing bei sehr hohen Taktraten zu treffen; Xilinx empfiehlt für 7-Series und neuer, ihn zu vermeiden.

**Synchroner Reset** (`if rising_edge(clk) then if reset = '1'…`):
- Pro: verhält sich wie jeder andere Eingang an der Taktflanke; spielt schön mit dem Synthesizer.
- Contra: braucht laufenden Takt zum Reset.

Zum Lernen ist Async leichter zu verstehen. Für Produktion: lies die Vorgaben deines FPGA-Herstellers.

## Du bist dran

- Füge einen **Enable**-Eingang hinzu. `q` soll nur an der steigenden Flanke updaten, wenn `enable = '1'`.
- Lass es einen **4-Bit-Bus** registrieren statt eines einzelnen Bits. Ändere `d` und `q` zu `std_logic_vector(3 downto 0)` und passe den Reset-Wert auf `(others => '0')` an.
- Füge einen `q_not`-Ausgang hinzu, der immer das Inverse von `q` ist. Bonus: verkable `q_not` zurück in `d` und beobachte einen Teiler-durch-zwei-Takt.
