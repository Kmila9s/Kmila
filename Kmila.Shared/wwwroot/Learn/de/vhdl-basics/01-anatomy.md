# Anatomie einer VHDL-Datei

Jede VHDL-Datei hat dieselben vier Abschnitte, immer in derselben Reihenfolge. Sobald du die Form erkennst, wird das Lesen unbekannten Codes nicht mehr einschüchternd — du weißt einfach, wo du nach jedem Stück suchen musst.

## Die vier Abschnitte

```vhdl
library IEEE;                          -- 1. Bibliotheken
use IEEE.STD_LOGIC_1164.ALL;           -- 2. verwendete Pakete

entity my_design is                    -- 3. Entity (der "Header")
  port (
    a : in  std_logic;
    b : in  std_logic;
    y : out std_logic
  );
end entity my_design;

architecture rtl of my_design is       -- 4. Architecture (der "Rumpf")
begin
  y <= a and b;
end architecture rtl;
```

Gehen wir sie durch.

### 1. `library`

`library IEEE;` importiert die Standard-IEEE-Bibliothek. Es ist das Digitaldesign-Äquivalent zu `#include <stdio.h>` — fast jede Datei, die du jemals schreiben wirst, beginnt damit.

### 2. `use`

`use IEEE.STD_LOGIC_1164.ALL;` zieht jeden Namen aus dem Paket `STD_LOGIC_1164` in den Scope. Das ist, was dir `std_logic`, `std_logic_vector`, `rising_edge()` und Freunde gibt.

Oft siehst du eine zweite `use`-Anweisung:

```vhdl
use IEEE.NUMERIC_STD.ALL;
```

Die gibt dir `unsigned`, `signed`, `to_integer`, `resize` und Arithmetik für Vektor-Typen. Füge sie hinzu, wann immer du Arithmetik auf Vektoren betreibst.

### 3. `entity`

Die Entity ist die **Blackbox-Schnittstelle**. Sie sagt:

- wie das Design heißt (`my_design`)
- welche Ports es hat
- für jeden Port: seine **Richtung** (`in`, `out`, `inout`, `buffer`) und seinen **Typ**

Alles innerhalb `port (…)` ist, was die Außenwelt sieht. Nichts darüber, wie das Design funktioniert, wird hier deklariert.

### 4. `architecture`

Die Architecture ist die **Implementierung**. Eine Entity kann mehrere Architectures haben (für dieselbe Schnittstelle) — du könntest eine `rtl`-, `behavioral`- und `tb`-Architecture derselben `cpu`-Entity haben und pro Konfiguration eine auswählen. In der Praxis hat die meisten Dateien genau eine.

Der Rumpf zwischen `begin` und `end architecture` ist, wo Signale und Gatter tatsächlich beschrieben werden.

## Wie Kmila das parst

Kmilas VHDL-Parser liest deine Datei von oben nach unten und baut einen Baum:

```
File
├── library IEEE
├── use IEEE.STD_LOGIC_1164.ALL
└── entity my_design
    ├── port a (in, std_logic)
    ├── port b (in, std_logic)
    ├── port y (out, std_logic)
    └── architecture rtl
        └── concurrent: y <= a and b
```

Wenn du auf **Build** klickst, werden die Ports zu den Einträgen im **Ports**-Dock-Tab, die Entity zum Eintrag in **Entities**, und die Zuweisungen innerhalb der Architecture zur Verdrahtung, die der Simulator ausführt.

## Namenskonventionen

VHDL ist groß-/kleinschreibungs-unempfindlich. `SIGNAL`, `Signal` und `signal` sind dasselbe Schlüsselwort. Aber halte einen einheitlichen Stil in einer gegebenen Datei ein — die meisten Teams verwenden **Kleinschreibung** für Schlüsselwörter und Signalnamen, **snake_case** für Bezeichner und **UPPER_CASE** für Konstanten.

## Was zu merken ist

- `library` + `use` — deine Imports.
- `entity` — die Schnittstelle (Ports und Richtungen).
- `architecture` — die Implementierung (was tatsächlich passiert).
- Eine Entity kann mehrere Architectures haben; die meisten Projekte liefern eine namens `rtl`.
