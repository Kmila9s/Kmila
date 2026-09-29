# Arréglalo: el buffer tri-estado que siempre maneja

Un buffer tri-estado es una salida que se puede "apagar". Cuando el output-enable (`oe`) está alto, pasa sus datos al bus. Cuando `oe` está bajo, **libera** el bus — conduce alta impedancia (`'Z'`) para que otros dispositivos puedan hablar.

## Qué está mal

Abre el ejemplo y mira el cuerpo:

```vhdl
y <= d when oe = '0' else 'Z';
```

Léelo en voz alta: "`y` vale `d` **cuando `oe` es 0**, si no, `'Z'`." Está al revés. Queremos que el buffer pase datos cuando el enable esté **alto** y libere cuando esté **bajo**.

## Tu misión

- Abre el ejemplo en el editor.
- Arregla la condición de la línea 12 (la asignación `y <=`).
- Ejecuta la simulación y verifica en la pestaña de formas de onda:
  - Con `oe = '0'`, `y` debe ser `'Z'`.
  - Con `oe = '1'`, `y` debe seguir a `d`.

## Por qué importa el tri-estado

Los buffers tri-estado son la forma en que varios drivers comparten un mismo cable (un bus) sin cortocircuitarse. Los FPGA no tienen tri-estados en su tela interna — usan multiplexores — pero los tri-estados **externos** (en los pads I/O) siguen siendo la manera en que DDR y PCIe comparten pines.

## Extra: cuidado con `'X'`

Si dos buffers tri-estado manejan el mismo bus a la vez con valores distintos, un circuito real echa humo. El simulador lo muestra como `'X'` (desconocido) en la forma de onda. Intenta construir un testbench con dos buffers cableados al mismo `y` y observa qué pasa.
