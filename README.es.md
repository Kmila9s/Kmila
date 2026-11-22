<p align="right"><a href="README.md">English</a></p>

<p align="center"><img src="docs/assets/banner-es.png" alt="Kmila — Escribe, simula y depura HDL sin FPGA" width="100%"></p>

<p align="center"><a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-3c82f6" alt="License: MIT"></a> <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10-475569" alt=".NET 10"></a> <a href="https://kmila.com.mx"><img src="https://img.shields.io/badge/HDL-VHDL-475569" alt="VHDL"></a> <a href="https://github.com/Kmila9s/Kmila/releases"><img src="https://img.shields.io/github/v/release/Kmila9s/Kmila?color=3c82f6&label=release" alt="Release"></a></p>

<p align="center">
  <a href="https://kmila.com.mx/demo"><b>Prueba la demo en el navegador</b></a> · <a href="https://kmila.com.mx">Sitio web</a> · <a href="https://github.com/Kmila9s/Documentation/releases">Manual de usuario (PDF) y tesis</a>
</p>

## Qué es

Kmila es un laboratorio local para lenguajes de descripción de hardware. Escribe VHDL, simúlalo y mira lo que hace: formas de onda, un tablero visual interactivo, el esquemático sintetizado, repetición paso a paso y diagramas de flujo. También enseña: lecciones de conceptos con figuras animadas, ejercicios de bloques y prácticas autocalificadas. Sin FPGA y sin cuenta.

- **Cabezas:** escritorio y móvil (.NET MAUI: Windows, macOS, Android, iOS), web (Blazor Server, Docker), demo en el navegador (WebAssembly) y terminal (`kmila sim`, `kmila ui`, `kmila check`, `kmila diff`).
- **Comparado contra GHDL:** el banco de exactitud simula cada caso en ambos simuladores y compara las formas de onda de forma estricta; cada práctica se valida igual.
- **Sin telemetría:** todo corre en tu equipo.

<p align="center"><img src="docs/assets/screenshot.png" alt="" width="90%"></p>

## Inicio rápido

```bash
git clone https://github.com/Kmila9s/Kmila.git
Kmila/build.sh --native --run      # clona los repos del motor, compila y sirve http://localhost:8080
# o bien: Kmila/build.sh --docker

# cabeza de terminal
cd Kmila && dotnet run --project Kmila.CLI -- sim --top-file design.vhd --duration 200ns
```

## Dónde encaja

```mermaid
flowchart LR
  kmila["Kmila<br/>app · web · CLI"]
  debugger["Debugger"]
  interpreter["Interpreter"]
  parser["Parser"]
  librarycompiler["LibraryCompiler"]
  timemachine["TimeMachine"]
  practicevalidator["PracticeValidator"]
  simulatorbench["SimulatorBench"]
  tests["Tests"]
  ghdl["GHDL 6<br/>(referencia)"]
  kmila-->debugger
  kmila-->interpreter
  kmila-->librarycompiler
  debugger-->interpreter
  interpreter-->parser
  interpreter-->timemachine
  interpreter-->librarycompiler
  librarycompiler-->parser
  practicevalidator-->kmila
  simulatorbench-->kmila
  tests-->kmila
  practicevalidator-.->ghdl
  simulatorbench-.->ghdl
  classDef me fill:#3c82f6,stroke:#1d4ed8,color:#fff
  classDef other fill:#475569,stroke:#334155,color:#fff
  classDef ext fill:#94a3b8,stroke:#64748b,color:#0f172a
  class debugger,interpreter,parser,librarycompiler,timemachine,practicevalidator,simulatorbench,tests other
  class ghdl ext
  class kmila me
  click kmila "https://github.com/Kmila9s/Kmila"
  click debugger "https://github.com/Kmila9s/Debugger"
  click interpreter "https://github.com/Kmila9s/Interpreter"
  click parser "https://github.com/Kmila9s/Parser"
  click librarycompiler "https://github.com/Kmila9s/LibraryCompiler"
  click timemachine "https://github.com/Kmila9s/TimeMachine"
  click practicevalidator "https://github.com/Kmila9s/PracticeValidator"
  click simulatorbench "https://github.com/Kmila9s/SimulatorBench"
  click tests "https://github.com/Kmila9s/Tests"
```

<sub>Las flechas van de un proyecto a lo que usa (referencias de proyecto). El recuadro resaltado es este repositorio.</sub>

## Documentación

- [Referencia técnica (arquitectura, API, historial de cambios)](docs/reference.md)
- [Manual de usuario (PDF) y tesis](https://github.com/Kmila9s/Documentation/releases)

## La familia Kmila

| | | |
|---|---|---|
| <img src="https://github.com/Kmila9s/Parser/raw/main/docs/assets/icon.png" width="28" alt=""> | [**Parser**](https://github.com/Kmila9s/Parser) | Analizador léxico y sintáctico de VHDL para Kmila |
| <img src="https://github.com/Kmila9s/Interpreter/raw/main/docs/assets/icon.png" width="28" alt=""> | [**Interpreter**](https://github.com/Kmila9s/Interpreter) | Motor de simulación por ciclos delta y síntesis |
| <img src="https://github.com/Kmila9s/Debugger/raw/main/docs/assets/icon.png" width="28" alt=""> | [**Debugger**](https://github.com/Kmila9s/Debugger) | Validación estructural y lint de diseños VHDL |
| <img src="https://github.com/Kmila9s/LibraryCompiler/raw/main/docs/assets/icon.png" width="28" alt=""> | [**LibraryCompiler**](https://github.com/Kmila9s/LibraryCompiler) | Librerías IEEE, cláusulas use y primitivas |
| <img src="https://github.com/Kmila9s/TimeMachine/raw/main/docs/assets/icon.png" width="28" alt=""> | [**TimeMachine**](https://github.com/Kmila9s/TimeMachine) | Base de tiempo discreta: ticks, relojes y eventos |
| <img src="https://github.com/Kmila9s/PracticeValidator/raw/main/docs/assets/icon.png" width="28" alt=""> | [**PracticeValidator**](https://github.com/Kmila9s/PracticeValidator) | Valida cada práctica contra GHDL |
| <img src="https://github.com/Kmila9s/SimulatorBench/raw/main/docs/assets/icon.png" width="28" alt=""> | [**SimulatorBench**](https://github.com/Kmila9s/SimulatorBench) | Banco de exactitud Kmila contra GHDL |
| <img src="https://github.com/Kmila9s/Tests/raw/main/docs/assets/icon.png" width="28" alt=""> | [**Tests**](https://github.com/Kmila9s/Tests) | Pruebas unitarias, de extremo a extremo y de rendimiento |
| <img src="https://github.com/Kmila9s/Documentation/raw/main/docs/assets/icon.png" width="28" alt=""> | [**Documentation**](https://github.com/Kmila9s/Documentation/releases) | Tesis y manual de usuario (PDF) |

## Contribuir

Se aceptan issues y merge requests; consulta [CONTRIBUTING.es.md](CONTRIBUTING.es.md).

## Licencia

MIT © 2026 Ulrich Tamayo Daniel. Consulta [LICENSE](LICENSE).

<sub>Desarrollado como Trabajo Terminal TT 2026-B059 en el IPN ESCOM (México).</sub>
