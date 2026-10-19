# Projekt: UART-Sender

Ein UART (Universal Asynchronous Receiver / Transmitter) ist das erste echte Protokoll, das die meisten Embedded-Entwickler bauen. Punkt-zu-Punkt, vollduplex, und braucht nur drei Drähte (TX, RX, GND). Jeder FTDI-USB-Serial-Adapter, jedes `Serial.print` in Arduino, jedes alte Terminal ist im Grunde ein UART.

Dieses Projekt baut die **Sende**-Seite. Die Empfangsseite ist ein hervorragendes Folgeprojekt, sobald du das hier durch hast.

## Asynchron: es gibt keine Taktleitung

Das „A" in UART ist die ganze Herausforderung. Anders als SPI oder I2C teilen sich die beiden Enden **keine Taktleitung**. Der Empfänger muss das Timing aus den Daten selbst rekonstruieren. Das klappt nur, wenn sich beide Enden vorab auf die **Baudrate** (Bits pro Sekunde) und das **Rahmenformat** einigen. Hast du eins davon falsch, empfängst du Müll – das klassische Symptom eines Baud-Mismatches.

Im ESCOM-Kurs liegt das unter dem RS-232-Standard. Es lohnt sich, zwei Dinge zu trennen, die man oft vermengt: Der **UART** ist die digitale Logik, die Bytes serialisiert (das, was du hier baust, auf Logikpegeln 0 V / 3,3 V), während **RS-232** der elektrische Standard ist, der diese Pegel auf etwa ±12 V mit *invertierter* Polarität verschiebt, um ein Kabel zu treiben. Ein Leitungstreiber vom Typ MAX232 sitzt zwischen deinem UART-Pin und einem echten DB-9-Stecker. Dein VHDL hat es immer nur mit der sauberen Logikpegel-Seite zu tun.

## Rahmenformat (keine Parität, 8 Datenbits, 1 Stopp)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    Leitung normal high ('1'); Startbit ist die fallende Flanke.
```

Das ist „8N1": **8** Datenbits, **N**o Parität (keine Parität), **1** Stoppbit – 10 Bits auf der Leitung pro Byte. Die Regeln, die zählen:

- **Idle** ist high. Eine kaputte oder getrennte Leitung liest sich als Idle, nicht als Strom von Nullen.
- **Start**-Bit ist low. Seine fallende Flanke ist die einzige Timing-Referenz, die der Empfänger bekommt.
- **Daten** gehen **LSB zuerst** raus – Bit 0 vor Bit 7.
- **Stopp**-Bit ist high, was auch garantiert, dass für den nächsten Rahmen eine frische fallende Flanke verfügbar ist.

Jedes Bit wird auf `tx` für genau `clk_freq / baud` Taktzyklen gehalten. Bei einem 50-MHz-Takt und 115 200 Baud sind das 50 000 000 / 115 200 ≈ 434 Zyklen pro Bit.

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
    - `shift_reg <= '1' & shift_reg(9 downto 1);` -- rechts schieben, oben mit Idle auffüllen
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - wenn `bit_count = 10`:
    - `sending <= '0';`

Mach `BAUD_PERIOD` zu einem Generic, damit du es für die Simulation kleiner setzen kannst (z. B. 5 Zyklen/Bit) und derselbe Code trotzdem bei voller Geschwindigkeit in Hardware läuft.

## Wie viel Timing-Fehler du dir leisten kannst

Weil es keinen gemeinsamen Takt gibt, sampelt der Empfänger nahe der Mitte jedes Bits und driftet über den Rahmen hinweg ein wenig. Über 10 Bitperioden muss der aufsummierte Fehler unter einem halben Bit bleiben, also muss der Baud-Fehler jedes Endes unter etwa ±2–3 % bleiben. Ganzzahlige Teiler treffen selten exakt: Bei 50 MHz ergeben 434 Zyklen/Bit 115 207 Baud – ein Fehler von 0,006 %, völlig sicher. Ein ungünstiger Takt kann schlecht runden, weshalb viele echte UARTs einen gebrochenen Teiler ergänzen. Die Empfangsseite verdient sich Reserve durch **Oversampling** mit dem 16-Fachen der Baudrate: Finde die Startflanke, dann sample bei Zählerstand 8 (die Bit-Mitte) und danach alle 16 Ticks.

## Was zu testen ist

Treibe `send = '1'` mit `data = "01001000"` (ASCII `'H'`). Nach Abschluss des Rahmens sollte die `tx`-Wellenform exakt 10 Bitperioden Aktivität zeigen:

1. Start (low)
2. Bit 0 der Daten (das LSB: `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Stopp (high)

Danach wieder idle high. Zoome in den Wellenform-Tab und prüfe das Muster Bit für Bit.

## Häufige Fallstricke

- **Falsche Bitreihenfolge.** MSB zuerst zu senden ist der mit Abstand häufigste Bug. Das `'1' & data & '0'` des Gerüsts plus ein Rechts-Shift liefert LSB-first umsonst – „repariere" es nicht.
- **Off-by-One bei der Bitzahl.** 8N1 sind *10* Ticks, nicht 8. Start- oder Stoppbit zu vergessen schneidet den Rahmen ab.
- **Mitten im Rahmen neu triggern.** Beachte `send` nur, wenn `sending = '0'`, sonst verfälscht eine zweite Anforderung den gerade laufenden Rahmen. Dafür ist `busy` da.
- **Baud-Mismatch.** Wenn der Empfänger die richtige Anzahl Bytes zeigt, aber verwürfelte Werte, verdächtige den Teiler vor der Logik.

## Weitergehen

- Füge eine **FIFO** hinzu, damit du mehrere Bytes einreihen kannst; `send` wird zu „push".
- Bau die **RX**-Seite: 16× oversampeln, die Startflanke finden, in der Bit-Mitte sampeln.
- Parametriere den Rahmen: Parität, 7-Bit vs. 8-Bit Daten, 1 vs. 2 Stoppbits.

## Was du behalten solltest

- UART ist **asynchron**: keine Taktleitung, also müssen sich beide Enden vorab auf Baudrate und Rahmenformat einigen.
- 8N1 sind **10 Bits** auf der Leitung – Idle high, Start low, 8 Datenbits **LSB zuerst**, Stopp high.
- Ein Bit dauert `clk_freq / baud` Zyklen; ein Ein-Zyklus-`baud_tick` taktet das Schieberegister.
- Halte den Baud-Fehler pro Ende unter ~2 %; die RX-Seite kauft sich Reserve durch **Oversampling und Sampeln in der Bit-Mitte**.
- **UART** ist die Logik; **RS-232** ist die ±12-V-Elektrikschicht, die ein Leitungstreiber obendrauf setzt.
