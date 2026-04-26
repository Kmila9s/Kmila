# Anatomy of a VHDL file

Every VHDL file has the same four sections, always in the same order. Once you recognise the shape, reading unfamiliar code stops being intimidating — you just know where to look for each piece.

## The four sections

```vhdl
library IEEE;                          -- 1. libraries
use IEEE.STD_LOGIC_1164.ALL;           -- 2. packages in use

entity my_design is                    -- 3. entity (the "header")
  port (
    a : in  std_logic;
    b : in  std_logic;
    y : out std_logic
  );
end entity my_design;

architecture rtl of my_design is       -- 4. architecture (the "body")
begin
  y <= a and b;
end architecture rtl;
```

Let's go through each one.

### 1. `library`

`library IEEE;` imports the standard IEEE library. It's the digital-design equivalent of `#include <stdio.h>` — almost every file you'll ever write starts with it.

### 2. `use`

`use IEEE.STD_LOGIC_1164.ALL;` pulls every name from the `STD_LOGIC_1164` package into scope. That's what gives you `std_logic`, `std_logic_vector`, `rising_edge()`, and friends.

You'll often see a second `use`:

```vhdl
use IEEE.NUMERIC_STD.ALL;
```

That one gives you `unsigned`, `signed`, `to_integer`, `resize`, and arithmetic for vector types. Add it whenever you do arithmetic on vectors.

### 3. `entity`

The entity is the **black-box interface**. It says:

- what the design is called (`my_design`)
- what ports it has
- for each port: its **direction** (`in`, `out`, `inout`, `buffer`) and its **type**

Everything inside `port (…)` is what the outside world sees. Nothing about how the design works is declared here.

### 4. `architecture`

The architecture is the **implementation**. An entity can have multiple architectures (for the same interface) — you might have `rtl`, `behavioral`, and `tb` architectures of the same `cpu` entity and pick one per configuration. In practice, most files have exactly one.

The body between `begin` and `end architecture` is where signals and gates actually get described.

## How Kmila parses this

Kmila's VHDL parser reads your file top-to-bottom and builds a tree:

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

When you click **Build**, the ports become the entries in the **Ports** dock tab, the entity becomes the entry in **Entities**, and the assignments inside the architecture become the wiring the simulator runs.

## Naming conventions

VHDL is case-insensitive. `SIGNAL`, `Signal`, and `signal` are the same keyword. But keep a single style in a given file — most teams use **lowercase** for keywords and signal names, **snake_case** for identifiers, and **UPPER_CASE** for constants.

## What to remember

- `library` + `use` — your imports.
- `entity` — the interface (ports and directions).
- `architecture` — the implementation (what actually happens).
- An entity can have multiple architectures; most projects ship one called `rtl`.
