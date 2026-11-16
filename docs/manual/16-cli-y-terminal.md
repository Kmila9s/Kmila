# 16. Uso por terminal (CLI)

[← Configuración](15-configuracion.md) · [Índice](README.md) · Siguiente: [Preguntas frecuentes →](17-preguntas-frecuentes.md)

---

Además de la interfaz gráfica, Kmila trae una herramienta de **línea de comandos**
llamada **`kmila`**. Usa el **mismo motor** de simulación, pero desde la terminal.
Es ideal para **automatizar**, trabajar por **SSH**, integrarlo en procesos de
**CI** o simplemente si te sientes cómodo en la consola.

> 💡 Si no usas la terminal, **puedes saltarte este capítulo sin problema**: todo
> esto también se hace desde la app gráfica.

La instalación de la herramienta `kmila` se explica en el [Capítulo 1](01-primeros-pasos.md).
Todos los comandos aceptan `--help`, y el idioma por defecto es español (`--lang en`
para inglés).

---

## Los comandos de un vistazo

| Comando | Qué hace |
|---|---|
| `kmila sim` | Simula y guarda el resultado (VCD, TXT, CSV, JSON o SVG). |
| `kmila ui` | Interfaz de **pantalla completa** en la terminal (editor + ondas). |
| `kmila check` | Revisa que el diseño **compile** (sin simular). |
| `kmila diff` | Compara **dos** archivos VCD y muestra las diferencias. |
| `kmila synth` | Genera el **esquemático** (SVG). |
| `kmila flow` | Genera el **diagrama de flujo** (PlantUML). |
| `kmila fit` | **Estima recursos**, opcionalmente contra una FPGA. |
| `kmila blocks` | Convierte entre **VHDL** y **bloques** (JSON). |
| `kmila practice` | Lista, muestra y **califica** los ejercicios de práctica. |
| `kmila serve` | Levanta la **API local** `/api/v1`. |
| `kmila completion` | Script de autocompletado para tu shell. |

---

## Simular: `kmila sim`

La forma básica:

```bash
kmila sim --top-file mi_diseno.vhdl --duration 1000ns
```

Opciones más útiles:

- `--top-file <archivo>` — el archivo VHDL principal (obligatorio).
- `--duration <N[unidad]>` — cuánto tiempo simular (`ns`, `us`, `ms`, `s`…). Un
  número solo se toma como nanosegundos. Por defecto `1000ns`.
- `--clockMHZ <n>` — el reloj del sistema en MHz.
- `--fpga <modelo>` — la FPGA de referencia (p. ej. `iCE40HX8K`, `EP4CE6`,
  `XC7A35T`). Usa `--fpga list` para ver todas.
- `--stimulus <puerto@tiempo=valor>` — mueve una entrada (se puede repetir).
- `--output-format <vcd|txt|csv|json|svg>` y `--output-file <archivo>` — formato
  y archivo de salida.

**Ejemplos:**

```bash
# Simular 1000 ns y guardar las ondas en un VCD
kmila sim --top-file contador.vhdl > contador.vcd

# Una compuerta con estímulos manuales
kmila sim -t nor.vhdl --duration 100ns \
          --stimulus a@0=0 --stimulus b@0=0 --stimulus a@40ns=1

# Diseño de varios archivos, elegir la entidad y exportar JSON
kmila sim -t alu.vhdl --file alu_pkg.vhdl --entity alu --output-format json -o alu.json

# Ver la lista de FPGAs disponibles
kmila sim --fpga list
```

> ✅ **Consejo** — En esta Mac, abre el `.vcd` resultante en **Surfer** (GTKWave
> se cierra solo en macOS reciente).

---

## La interfaz de terminal: `kmila ui`

Una aplicación de **pantalla completa** en la consola, sin salir de la terminal:

```bash
kmila ui --top-file mi_diseno.vhdl        # abre el archivo
kmila ui --open corrida.vcd               # solo ver un VCD existente
```

Tiene tres paneles: **Archivos** (explorador), **Editor** (con resaltado de
sintaxis y `Ctrl+S` para guardar) y **Ondas** (visor de formas de onda en texto,
con cursor, medición y exportación). Teclas más usadas:

| Tecla | Acción |
|---|---|
| `Tab` | Cambiar de panel (Archivos → Editor → Ondas) |
| `Enter` | (Archivos) abrir el archivo seleccionado |
| `F5` o `r` | Simular el archivo actual |
| `Ctrl+S` | Guardar |
| `p` | Editar parámetros (duración / reloj / FPGA) |
| `+` `-` / `←` `→` | (Ondas) acercar/alejar / mover el cursor |
| `m` | (Ondas) marcar y medir (Δt y frecuencia) |
| `S` | (Ondas) exportar (VCD o SVG) |
| `?` o `F1` | Ayuda |
| `Ctrl+Q` | Salir desde cualquier panel |

---

## Otros comandos útiles

```bash
kmila check mi_diseno.vhdl          # ¿compila? (sin simular)
kmila diff antes.vcd despues.vcd    # ¿en qué difieren dos corridas?
kmila synth mi_diseno.vhdl          # esquematico en SVG
kmila fit mi_diseno.vhdl --fpga iCE40HX8K   # estimar recursos
kmila practice list                 # ver ejercicios de practica
kmila serve                         # levantar la API local /api/v1
```

> 💡 **¿Sabías que…?** `kmila check`, `diff` y `practice` **terminan con error**
> cuando algo falla, así que encajan perfecto en una tubería de integración
> continua (CI).

---

[← Configuración](15-configuracion.md) · [Índice](README.md) · Siguiente: [Preguntas frecuentes →](17-preguntas-frecuentes.md)
