## 4-zu-1-Multiplexer — Auswahl unterdekodiert

Verlangt war ein 4-zu-1-Multiplexer mit 2-Bit-`sel`:

| `sel` | `y` |
|:-----:|:---:|
| `00`  | `a` |
| `01`  | `b` |
| `10`  | `c` |
| `11`  | `d` |

### Was schiefläuft

Der Autor prüft nur `sel(0)`. Das `case` schrumpft zu einem 2-zu-1-Mux, der nur `a` (bei `sel(0) = '0'`) oder `b` (bei `sel(0) = '1'`) durchreicht. Die Eingänge `c` und `d` sind **nicht erreichbar**.

### Hinweis

Verwende im `case` den **gesamten** `sel`-Vektor und gib für jeden Binärwert `"00"`, `"01"`, `"10"`, `"11"` einen Zweig an.
