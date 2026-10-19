# Warum eine CPU pipelinen?

Deine Single-Cycle-8-Bit-CPU aus dem vorigen Track funktioniert – aber sie ist in einer Dimension begrenzt, die auf echter Hardware enorm zählt: der **Taktfrequenz**.

## Die Single-Cycle-Decke

In einem Single-Cycle-Entwurf muss die Taktperiode lang genug sein, damit der Befehl mit dem längsten kritischen Pfad komplett durchpropagiert: Fetch → Decode → Register-Read → ALU → Memory → Writeback, alles kombinatorisch. Wenn der längste Pfad 20 ns dauert, ist dein Maximaltakt 50 MHz. Punkt.

## Pipelining bedeutet, diesen Pfad zu teilen

Zerlege den Pfad in N Stufen, getrennt durch Register:

```
  [IF] → [ID] → [EX] → [MEM] → [WB]
    ^      ^      ^       ^       ^
    +------+------+-------+-------+
         Register zwischen den Stufen
```

Der Takt muss nun nur lang genug sein, damit die längste *Stufe* fertig wird, nicht der ganze Pfad. Fünf Stufen à ~4 ns ergeben einen 250-MHz-Takt. Der Durchsatz steigt von 1 Befehl / 20 ns auf 1 Befehl / 4 ns – **5-fache Beschleunigung**, konzeptuell umsonst.

Beachte, was Pipelining *nicht* verbessert: die Latenz eines einzelnen Befehls. Ein Befehl braucht immer noch fünf Stufen bis zum Abschluss – etwa 20 ns, genau wie vorher (sogar einen Tick mehr, wegen der Register-Setup-Zeit an jeder Grenze). Was sich ändert: Ein *neuer* Befehl wird jeden Zyklus fertig, sobald die Pipeline voll ist. Du machst nicht ein Auto schneller; du machst aus einem Arbeiter ein Fließband.

## Die Zwischenstufen-Register

Die Register zwischen den Stufen sind keine Buchhaltung – sie *sind* die Pipeline. Jedes latcht alles, was die nachgelagerten Stufen noch brauchen: das Teilergebnis, die Nummer des Zielregisters, die in ID decodierten Steuersignale. Ohne sie würde Stufe N+1 die Eingänge von Stufe N mitten in der Berechnung sich ändern sehen. Das sind die „registros inter etapa", die der ESCOM-Lehrplan (Einheit IV) dich in HDL implementieren lässt.

## Warum du nie wirklich 5× bekommst

Das Ideal ist **CPI = 1** (ein Zyklus pro Befehl), sobald die Pipeline voll ist. Zwei Dinge höhlen das aus:

1. **Füllen und Leeren.** Der erste Befehl braucht immer noch alle fünf Stufen, bevor irgendetwas fertig wird, also zahlt ein kurzes Programm einen Anlaufkosten. Für einen langen Lauf ist das vernachlässigbar; für eine enge Schleife nicht.
2. **Hazards** – die echte Steuer, und Thema des Rests dieses Moduls.

## Hazards – der Preis

Drei Arten von Kollision, jede mit ihrer eigenen Lösung:

**Struktureller Hazard** – zwei Stufen wollen im selben Zyklus dieselbe physische Ressource. Der klassische Fall: IF will einen Befehl lesen, während MEM Daten lesen will, aber es gibt nur einen Speicher-Port. Lösung: Befehls- und Datenspeicher trennen (eine Harvard-Teilung) oder den Port duplizieren.

**Daten-Hazard** – Befehl N+1 braucht einen Wert, den Befehl N noch nicht zurückgeschrieben hat:

```
  ADD R1, R2, R3   -- Ergebnis am Ende von EX bereit, in WB geschrieben
  SUB R4, R1, R5   -- braucht R1 in seinem eigenen EX, einen Zyklus später
```

Ohne Hilfe liest SUB das *alte* R1. Die Lösung ist **Forwarding** (auch Bypassing genannt): Leite das ALU-Ergebnis direkt vom EX/MEM-Ausgang von N zurück zum ALU-Eingang von N+1, statt zu warten, bis es sich durch WB und wieder aus dem Registersatz heraus quält. Forwarding beseitigt die meisten Daten-Stalls – aber nicht alle. Ein **Load-Use-Hazard** ist der hartnäckige: Der Wert eines Loads ist erst am Ende von MEM verfügbar, eine Stufe *nach* EX, also muss ein Befehl, der ihn sofort nutzt, trotzdem **einen Zyklus stallen** (eine Blase einfügen), egal wie gut dein Forwarding ist.

**Control-Hazard** – Befehl N ist ein Sprung, und die Pipeline hat N+1 (und vielleicht N+2) schon geholt, bevor sie weiß, ob der Sprung genommen wird. Lösungen:

- **Stall / Flush** – friere das Fetch ein, bis der Sprung aufgelöst ist, oder lass die Befehle auf dem falschen Pfad starten und wirf sie weg, falls die Vermutung falsch war. Den Sprung früher aufzulösen (in ID statt EX) schrumpft die Strafe auf eine einzige Blase.
- **Statische Vorhersage** – rate zur Kompilierzeit immer in eine Richtung (z. B. „Rückwärtssprünge werden genommen" fängt Schleifen gut).
- **Dynamische Vorhersage** – lerne zur Laufzeit mit einer Tabelle aus **2-Bit-Sättigungszählern**, indiziert über die Adresse des Sprungs. Der Zähler braucht *zwei* falsche Vermutungen in Folge, bevor er seine Vorhersage umkippt, sodass eine Schleife, die 99-mal genommen wird und dann einmal aussteigt, nicht sofort ihre Meinung ändert. Sage „genommen" vorher, wenn der Zähler in seinen beiden hohen Zuständen ist, „nicht genommen" in den beiden niedrigen; stupse ihn bei jedem genommenen Sprung Richtung genommen und sonst Richtung nicht-genommen. Billig, und es ist fast so gut wie weit aufwendigere Schemata.

Jede folgende Lektion in diesem Modul implementiert eine Lösung nach der anderen auf der 8-Bit-CPU des Curriculums, beginnend mit Forwarding – passend zur ESCOM-Progression estructural → datos → bypassing/forwarding → riesgo de control.

## Was du behalten solltest

- Pipelining tauscht **Latenz pro Befehl** (unverändert, sogar leicht höher) gegen **Durchsatz** (viel höher): ideales CPI → 1.
- Die Takt-Beschleunigung ist durch die längste *Stufe* gedeckelt, nicht durch den ganzen Pfad oder die kürzeste Stufe.
- Zwischenstufen-Register tragen Daten *und* Steuersignale vorwärts; sie sind das, was die Überlappung sicher macht.
- Forwarding löscht die meisten Daten-Hazards aus, aber ein Load-Use-Paar kostet trotzdem einen Stall. Sprünge kosten ein Flush, außer du sagst sie vorher – ein 2-Bit-Sättigungszähler ist der Arbeitspferd-Prädiktor.
- Hazards kosten Zyklen; Gegenmaßnahmen kosten Gatter. Reales CPU-Design ist dieser Trade-off im großen Stil.
