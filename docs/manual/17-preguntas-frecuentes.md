# 17. Preguntas frecuentes (FAQ)

[← Uso por terminal (CLI)](16-cli-y-terminal.md) · [Índice](README.md) · Siguiente: [Solución de problemas →](18-solucion-de-problemas.md)

---

## Sobre Kmila en general

**¿Necesito una tarjeta FPGA o algún hardware?**
No. Kmila **simula todo en tu computadora**. Es justo su propósito: aprender y
depurar VHDL sin hardware físico.

**¿Necesito conexión a internet?**
No. La app de escritorio y la web local funcionan **sin conexión**. Solo
necesitas internet para descargar la app o para el control de versiones con
GitHub.

**¿Es gratis?**
Sí, Kmila es una herramienta **gratuita y de código abierto** (su repositorio
está enlazado en la pantalla de inicio).

**¿En qué dispositivos funciona?**
En **Windows, macOS, Android, iPhone/iPad**, en el **navegador** (web local o
demo) y en la **terminal**. Ver el [Capítulo 1](01-primeros-pasos.md).

**¿Kmila es para VHDL o también Verilog?**
Kmila está centrado en **VHDL**. Es el lenguaje que escribes, simulas y depuras.

---

## Proyectos y guardado

**¿Dónde se guardan mis proyectos?**
**Localmente, en tu equipo** (no en la nube). La demo del navegador los guarda
**dentro del propio navegador**. Si quieres respaldo en la nube, usa el control
de versiones con GitHub ([Cap. 13](13-control-de-versiones.md)).

**¿Cómo comparto un proyecto o lo entrego?**
**Expórtalo** como paquete desde **Configuración ▸ Paquete de proyecto**, y la
otra persona lo **importa** ([Cap. 3](03-proyectos.md)).

**¿Necesito crear una cuenta?**
No. Puedes usar Kmila como **invitado**. Solo necesitas iniciar sesión (en
GitHub) si quieres el control de versiones.

---

## Editar y simular

**¿«Bloques» y «Código» son lo mismo?**
Sí: son **dos vistas del mismo diseño**. Lo que armas con bloques se vuelve VHDL
y viceversa ([Cap. 5](05-editor-bloques.md)).

**¿Cuál es la diferencia entre «Compilar» y «Ejecutar»?**
**Compilar** solo revisa que el diseño **construya** (sin simular). **Ejecutar**
corre la simulación ([Cap. 6](06-simulacion.md)).

**¿Y entre el modo «Rápido» y «Visual»?**
**Rápido** simula de corrido y muestra las ondas al final; **Visual** simula en
vivo, paso a paso, y permite breakpoints.

**¿Puedo usar un banco de pruebas (testbench)?**
Sí. Si tu proyecto tiene un archivo `_tb.vhd`, elígelo para ejecutar; él mismo
genera el reloj y los estímulos.

**¿Qué tan preciso es el simulador?**
La salida del motor se **valida contra GHDL** (un simulador de referencia
estándar) sobre muchos casos. Las ondas que ves son resultado real, no ejemplos.

---

## Resultados

**¿Cómo abro las ondas en otra herramienta?**
Exporta a **VCD** (botón *VCD* del visor) y ábrelo en **Surfer** o GTKWave. En
esta Mac, usa **Surfer** ([Cap. 7](07-formas-de-onda.md)).

**¿Para qué sirve el esquemático si ya tengo el código?**
Para **ver** el circuito que describe tu VHDL y entender qué hardware implica
([Cap. 8](08-esquematico.md)).

**¿El uso de recursos es exacto?**
Es una **estimación** (margen ±20 %), útil como guía. Pines y relojes sí son
exactos ([Cap. 10](10-entidades-y-recursos.md)).

---

## Interfaz e idioma

**¿Puedo cambiar el idioma o el tema?**
Sí, desde el **menú de perfil** (arriba a la derecha) o en **Configuración**. Hay
7 idiomas y temas claro/oscuro/automático ([Cap. 2](02-interfaz-general.md)).

**¿Por qué algunos mensajes de error están en inglés?**
Es **a propósito**: los mensajes técnicos de compilación coinciden con las
herramientas estándar de la industria. En el [Cap. 18](18-solucion-de-problemas.md)
los explicamos en español.

**¿Cómo vuelvo a ver el tutorial de bienvenida?**
En **Configuración ▸ Apariencia ▸ Repetir tours**.

**¿Puedo reiniciar mi progreso de lecciones?**
Sí, desde **Configuración** (sección de aprendizaje).

---

¿Tu duda no está aquí? Revisa el capítulo de la sección que te interesa, o la
[Solución de problemas](18-solucion-de-problemas.md).

---

[← Uso por terminal (CLI)](16-cli-y-terminal.md) · [Índice](README.md) · Siguiente: [Solución de problemas →](18-solucion-de-problemas.md)
