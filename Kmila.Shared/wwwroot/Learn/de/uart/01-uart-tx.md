# Projekt: UART-Sender

Ein UART (Universal Asynchronous Receiver / Transmitter) ist das erste echte Protokoll, das die meisten Embedded-Entwickler bauen. Punkt-zu-Punkt, vollduplex, braucht nur drei Drähte (TX, RX, GND). Jeder FTDI-USB-Serial-Adapter, jedes `Serial.print` in Arduino, jedes alte Terminal ist im Grunde ein UART.

Dieses Projekt baut die **Sende**-Seite. Die Empfangsseite ist ein hervorragendes Folgeprojekt, sobald du das hier durch hast.

## Rahmenformat (keine Parität, 8 Datenbits, 1 Stopp)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    Leitung normal high ('1'); Startbit ist die fallende Flanke.
```

Jedes Bit wird auf `tx` für genau `clk_freq / baud` Taktzyklen gehalten. Bei 50 MHz Takt und 115 200 Baud sind das ≈ 434 Zyklen pro Bit.

## Dein Job

1. Bau einen Taktzähler, der alle ~434 Zyklen einen Ein-Zyklus-`baud_tick` erzeugt.
2. Bei `send = '1'` lade `data` in ein 10-Bit-Schieberegister: `{ stop_bit, data[7..0], start_bit } = 1 & data & 0`. Treibe `busy <= '1'`.
3. Schiebe das Register bei jedem `baud_tick` nach rechts (so geht das Startbit zuerst raus, dann LSB bis MSB der Daten, dann das Stoppbit).
4. Nach dem 10. Tick fällt `busy`, und `tx` liegt wieder auf High (Idle).

## Gerüst

Entity und Idle-Zustand sind gegeben; `tx` ist standardmäßig high, `busy` ist standardmäßig low. Alles zwischen diesen Defaults liegt in deiner Verantwortung.

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
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — rechts schieben, oben mit Idle padden
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - wenn `bit_count = 10`:
    - `sending <= '0';`

Mach `BAUD_PERIOD` zu einem Generic, damit du es für die Simulation kleiner setzen kannst (z. B. 5 Zyklen/Bit) und derselbe Code in Hardware bei voller Geschwindigkeit läuft.

## Was zu testen ist

Treibe `send = '1'` mit `data = "01001000"` (ASCII `'H'`). Nach Frame-Ende sollte die `tx`-Wellenform exakt 10 Bitperioden Aktivität zeigen:

1. Start (low)
2. Bit 0 der Daten (LSB: `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Stopp (high)

Danach wieder idle high. Zoome in den Wellenform-Tab und prüfe das Muster Bit für Bit.

## Weitergehen

- Füge eine **FIFO** hinzu, damit du mehrere Bytes einreihen kannst; `send` wird zu „push“.
- Bau die **RX**-Seite: jedes ankommende Bit bei 1,5 × Bitperiode samplen, danach bei 1 × .
- Parametriere den Frame: Parität, 7-Bit vs. 8-Bit Daten, 1 vs. 2 Stoppbits.
