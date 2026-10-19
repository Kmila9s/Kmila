# Beispiel: 3-zu-8-Decoder

Ein Decoder nimmt einen kleinen Binärcode und verwandelt ihn in einen **One-Hot**-Ausgang: genau eine Ausgangsleitung ist high, und ihre Position entspricht dem Code. Er ist eine der benannten Schaltungen in der Kombinatorik-Einheit („codificador y decodificador“) und der natürliche Partner des Encoders, der das Umgekehrte tut.

```
  sel  │  y
  000  │  00000001
  001  │  00000010
  010  │  00000100
  011  │  00001000
  100  │  00010000
  101  │  00100000
  110  │  01000000
  111  │  10000000
```

Decoder leben in jedem Speichersystem — die Adressleitungen treiben einen Decoder, und der „Row-Match“-Ausgang aktiviert genau eine Wortleitung. Wann immer du „Adressdecoder“ hörst, das ist es darunter. Dieselbe Struktur implementiert auch einen vollständigen Satz Minterme: ein n-Eingang-Decoder gibt alle 2ⁿ Minterme seiner Eingänge aus, sodass sich jede boolesche Funktion durch ODER-Verknüpfung der richtigen Decoder-Ausgänge bauen lässt.

## Das VHDL

Ein Decoder ist der Lehrbuchfall für `with/select`:

```vhdl
with sel select
  y <= "00000001" when "000",
       "00000010" when "001",
       "00000100" when "010",
       "00001000" when "011",
       "00010000" when "100",
       "00100000" when "101",
       "01000000" when "110",
       "10000000" when others;
```

Beachte `when others` am Ende. `sel` ist ein `std_logic_vector(2 downto 0)` — 3 Bit — also gibt es 2³ = 8 *gemeinte* Werte, aber weil jedes Bit ein neunwertiges `std_logic` ist, sieht der Compiler weit mehr mögliche Muster und braucht `others`, um vollständig zu sein. Hier fällt `others` mit dem `"111"`-Fall zusammen (es synthetisiert zum gleichen Ausgang).

## Die skalierbare Alternative

Acht Zeilen auszuschreiben ist für einen 3-zu-8-Decoder in Ordnung, aber es skaliert nicht. Dieselbe Funktion in einer Zeile, mit einem Shift, ist:

```vhdl
y <= std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Lies es von innen nach außen: nimm `sel` als Zahl, schiebe eine einzelne `'1'` um so viele Positionen nach links innerhalb eines 8-Bit-Felds, und caste zurück zu `std_logic_vector`. Beide Versionen synthetisieren identisch — nimm die, die du lesbarer findest. (Die explizite Tabelle lässt sich für Anfänger leichter gegen die Wahrheitstabelle prüfen; die Shift-Form ist die, die du auf 32 oder 64 Ausgänge verbreitern würdest.)

## Was das Beispiel tut

Das Beispiel zyklt `sel` durch alle acht 3-Bit-Werte, 10 µs pro Wert. Auf dem Wellenform-Tab siehst du die 8 Bits von `y` zu einer wandernden `'1'` werden — ein digitaler „Licht-Chaser“.

## Füge ein Enable hinzu (und lerne den Demux kennen)

Echte Decoder haben ein **Enable**. Wenn deaktiviert, sind alle Ausgänge low:

```vhdl
y <= (others => '0') when enable = '0' else
     std_logic_vector(shift_left(to_unsigned(1, 8), to_integer(unsigned(sel))));
```

Hier eine schöne Verbindung: Ein Decoder-mit-Enable, bei dem das Enable eine *Daten*leitung statt einer Konstante ist, **ist ein Demultiplexer** — der Enable-Wert wird auf den einen Ausgang geroutet, den die Adresse wählt, und der Rest bleibt bei `'0'`. Decoder und Demux sind dieselbe Hardware mit verschiedenen Etiketten.

## Du bist dran

Sobald es läuft:

- Verbreitere ihn zu einem **4-zu-16**-Decoder. Versuch es per Hand (sechzehn Zeilen) und dann mit der Shift-Form — beachte, wie wenig sich die Shift-Form ändern muss (nur die zwei `16`er).
- Füge das `enable` von oben hinzu und bestätige auf dem Waveform, dass jeder Ausgang auf `'0'` fällt, solange `enable = '0'`.
- Bau die Umkehrung: einen **8-zu-3-Prioritätsencoder**, der den Index des höchsten gesetzten Eingangs ausgibt. Das ist die andere Hälfte dieses Kursabschnitts.

## Was du behalten solltest

- Ein Decoder bildet einen n-Bit-Code auf einen One-Hot-2ⁿ-Ausgang ab; seine Ausgänge sind die Minterme der Eingänge.
- `with/select` mit einem verpflichtenden `when others` ist die klare, wahrheitstabellen-förmige Art, ihn zu schreiben.
- Das Idiom `shift_left(to_unsigned(1, N), …)` ist die Version, die auf jede Breite skaliert.
- Ein Decoder mit einem datengetriebenen Enable ist ein Demultiplexer; sein Spiegelbild ist der Encoder.
