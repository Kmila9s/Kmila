# Projekt: UART-Sender

Ein UART (Universal Asynchronous Receiver / Transmitter) ist das erste echte Protokoll, das die meisten Embedded-Designer bauen. Es ist Punkt-zu-Punkt, Vollduplex und braucht nur drei Drähte (TX, RX, GND). Jeder FTDI-USB-Serial-Adapter, jeder Arduino-`Serial.print`, jedes klassische Terminal ist darunter ein UART.

Dieses Projekt baut die **Sende**-Seite. Die Empfangsseite ist ein ausgezeichnetes nächstes Projekt, sobald du dieses fertig hast.

## Frameformat (keine Parität, 8-Bit-Daten, 1 Stop)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    Linie ist normalerweise hoch ('1'); Startbit ist eine fallende Flanke.
```

Jedes Bit wird auf `tx` für genau `clk_freq / baud` Taktzyklen gehalten. Für einen 50-MHz-Takt und 115 200 Baud sind das ≈ 434 Zyklen pro Bit.

## Deine Aufgabe

1. Baue einen Taktzähler, der etwa alle 434 Zyklen einen einzyklischen `baud_tick` erzeugt.
2. Bei `send = '1'` lade `data` in ein 10-Bit-Schieberegister: `{ stop_bit, data[7..0], start_bit } = 1 & data & 0`. Treibe `busy <= '1'`.
3. Bei jedem `baud_tick` schiebe das Register nach rechts (sodass zuerst das Startbit rausgeht, dann LSB bis MSB der Daten, dann das Stopbit).
4. Nach dem 10. Tick lasse `busy` fallen und halte `tx` im Idle-Zustand hoch.

## Gerüst

Die Entity und der Idle-Zustand sind vorgegeben; `tx` ist standardmäßig hoch, `busy` standardmäßig niedrig. Alles zwischen diesen Defaults liegt in deiner Verantwortung.

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## Vorgeschlagene Struktur

Zwei Zähler + ein Schieberegister:

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- Wenn `send = '1'` und `sending = '0'`:
  - lade `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- Solange `sending = '1'`:
  - `clk_count <= clk_count + 1;`
  - wenn `clk_count = BAUD_PERIOD - 1`:
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — nach rechts schieben, oben mit Idle auffüllen
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - wenn `bit_count = 10`:
    - `sending <= '0';`

Mach `BAUD_PERIOD` zu einem Generic, damit du es für die Simulation schrumpfen kannst (z. B. 5 Zyklen/Bit) und dieselbe Logik auf der Hardware bei voller Geschwindigkeit funktioniert.

## Was zu testen ist

Treibe `send = '1'` mit `data = "01001000"` (ASCII `'H'`). Nachdem das Frame abgeschlossen ist, sollte die `tx`-Waveform genau 10 Bit-Perioden Aktivität zeigen:

1. Start (niedrig)
2. Bit 0 der Daten (das ist das LSB: `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Stop (hoch)

Dann zurück zu Idle hoch. Zoome in den Waveforms-Tab und verifiziere das Muster Bit für Bit.

## Noch weiter

- Füge ein **FIFO** hinzu, damit du mehrere Bytes in die Warteschlange stellen kannst; `send` wird zu "push".
- Baue die **RX**-Seite: taste jedes eingehende Bit bei 1,5 × Bit-Periode ab, danach bei 1 ×.
- Parametriere das Frame: Parität, 7-Bit- vs. 8-Bit-Daten, 1 vs. 2 Stopbits.
