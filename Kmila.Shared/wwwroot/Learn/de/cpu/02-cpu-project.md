# Projekt: Single-Cycle-8-Bit-CPU

Das ist das Abschlussprojekt. Du wirst alles aus den vorherigen Modulen zusammenbauen — ein ROM, ein Register, eine ALU, eine FSM-artige Steuerung — zu einer winzigen Maschine, die tatsächlich Programme ausführt.

## Die Maschine

```
            ┌──────────┐
            │  PC (4b) │ ◀── reset = "0000"
            └────┬─────┘
                 │ addr
                 ▼
            ┌──────────┐
            │   ROM    │   (16 Bytes, zur Synthesezeit vorgeladen)
            │  16×8    │
            └────┬─────┘
                 │ instr (8 Bits)
                 ▼
  ┌──────────────┴──────────────┐
  │  opcode = instr(7..4)       │
  │  imm4   = instr(3..0)       │
  └──────┬───────────────┬──────┘
         │               │
         ▼               ▼
   [decode/exec]    [PC next logic]
         │               │
         ▼               ▼
     R0 (8b)          PC_next
```

## Was dir das Gerüst gibt

Eine vollständige, funktionierende CPU. Lies den Starter sorgfältig:

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    pc   <= (others => '0');
    r0   <= (others => '0');
    halt <= '0';
  elsif rising_edge(clk) then
    if halt = '0' then
      case instr(7 downto 4) is
        when "0000" => r0 <= resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0001" => r0 <= r0 + resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0010" => pc <= unsigned(instr(3 downto 0));
        when "1111" => halt <= '1';
        when others => pc <= pc + 1;
      end case;
    end if;
  end if;
end process;
```

Es implementiert bereits:

- LOAD (`0000`)
- ADD  (`0001`)
- JUMP (`0010`)
- HALT (`1111`)

Das vorgeladene ROM führt `R0 = 3; R0 += 4; R0 += 2; HALT` aus.

## Deine Aufgabe

Lass es zuerst unverändert laufen. Simuliere 200 µs mit dem mitgelieferten Clock- + Reset-Stimulus. Auf dem Waveforms-Tab solltest du sehen, wie `r0_out` die Werte `3 → 7 → 9` durchläuft, dann geht `halted` auf high und bleibt high.

Sobald die Basis funktioniert, **erweitere** die ISA:

### Aufgabe 1: SUB

Opcode `0011`. `R0 <= R0 - imm4`. Eine einzige Zeile Änderung.

### Aufgabe 2: Bedingter Sprung

Opcode `0100`: `JZ addr` — springe nach `addr`, wenn `R0 = 0`, sonst einfach `pc <= pc + 1`.

Schreibe ein kleines Programm, das von einem Wert herunterzählt und zurück zu 0 springt, wenn fertig. Benutze `JZ` als Schleifen-Ausgang.

### Aufgabe 3: Ein zweites Register

Füge `r1` als zweites 8-Bit-Register hinzu. Definiere `LOAD R1, #i` als Opcode `0101` und `ADD R0, R1` (ohne Immediate) als Opcode `0110`. Du musst den Decode-Case erweitern und einen zweiten Ausgabeport für die Sichtbarkeit hinzufügen.

### Aufgabe 4: Externer Datenspeicher

Verschalte das RAM-Modul aus dem Memories-Track in diese CPU. Füge die Opcodes `LOAD R0, [addr]` und `STORE R0, [addr]` hinzu. Vorsicht: Ein Single-Cycle-Design erfordert, dass der Lese-Port des Speichers seinen Ausgang im selben Zyklus gültig hat, in dem du die Adresse anlegst — verwende einen kombinatorischen Lesezugriff für das ROM und einen registrierten Lesezugriff für das Daten-RAM, und plane die Pipeline sorgfältig.

## Warum das tun?

Weil beim ersten Mal, wenn du zuschaust, wie deine CPU ein Programm durchläuft, das du geschrieben hast (in Bytes, die du in ein ROM geschrieben hast), etwas klickt. Jede Abstraktion obendrüber — Assembler, Compiler, Betriebssystem — steht auf genau diesem Mechanismus. Und jede einzelne davon, den ganzen Weg hoch bis zum Webbrowser, der das JavaScript ausführt, das du letzte Woche geschrieben hast, ist derselbe Trick, wiederholt in größerem Maßstab.

## Noch weiter

- Implementiere die klassische fünfstufige Pipeline auf dieser ISA. Füge Hazard-Erkennung und eine Forwarding-Einheit hinzu. Du hast jetzt eine Mikro-Version dessen, was RISC-V-Lehrbücher lehren.
- Ziel das ROM um: statt Konstanten lade es zur Simulationszeit aus einer externen Datei, dann baue eine Toolchain, die Text in diese Bytes assembliert.
- Pack den UART-TX aus dem früheren Projekt dazu und bringe die CPU dazu, `'HI'` in die Waveform zu "drucken". Herzlichen Glückwunsch — du hast ein SoC.
