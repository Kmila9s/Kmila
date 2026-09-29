# Laboratorio: explorador de tablas de verdad

Este laboratorio es la primera vez que manejarás un diseño VHDL real. La entidad tiene dos entradas (`a`, `b`) y seis salidas — una por cada compuerta elemental de dos entradas.

## Qué pasa al pulsar *Abrir en el editor*

1. Kmila crea un proyecto desechable llamado algo así como "Laboratorio: explorador de tablas de verdad".
2. Lo siembra con `gates.vhd` que contiene las seis asignaciones concurrentes.
3. Carga una **configuración de simulación** que estimula `a` y `b` en todas las combinaciones.
4. El área de trabajo del editor se abre con la configuración ya seleccionada en la pastilla de arriba.

Desde ahí:

- Pulsa **Compilar** para analizar y elaborar el diseño.
- Pulsa **Ejecutar** para simular.
- Cambia a la pestaña **Formas de onda** del dock inferior.

Verás siete filas de ondas cuadradas: tus dos entradas arriba y cada salida reaccionando en tiempo real.

## Qué observar

Usa la barra o el teclado para alinear los flancos de entrada con las transiciones de salida. Intenta verificar cada compuerta sin volver a las tablas de verdad:

- `o_and`: se mantiene bajo hasta que `a` y `b` son 1 — entonces sube.
- `o_nand`: lo contrario. Suele estar alto y baja solo cuando ambas entradas son 1.
- `o_xor`: alto exactamente cuando las entradas difieren.

Si algo no coincide con lo que esperas, revisa la pestaña **Salida** — ahí aparecen las advertencias de la compilación.

## Pruébalo tú

Cuando el ejemplo funcione, experimenta:

- Agrega una tercera entrada `c` y calcula `a xor b xor c` (paridad de tres entradas).
- Renombra una salida y reconstruye — la pestaña **Puertos** se actualiza sola.
- Añade otra línea a la configuración: voltea `a` y `b` al mismo tiempo y mira qué salidas cambian.

Cuantas más palancas muevas antes de escribir tu propio diseño, menos se sentirá VHDL como adivinar.
