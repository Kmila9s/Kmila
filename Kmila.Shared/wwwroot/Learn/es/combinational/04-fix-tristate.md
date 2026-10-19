# Arréglalo: el buffer tristate que siempre maneja

Un buffer tristate es una salida que se puede apagar y encender. Cuando la señal de output-enable (`oe`) está en alto, deja pasar sus datos al bus. Cuando `oe` está en bajo, **libera** el bus — maneja alta impedancia (`'Z'`) para que otros dispositivos puedan hablar. Es el único lugar de esta unidad donde el valor `'Z'` de la lógica de nueve valores se gana su lugar.

## Qué está mal

Abre el ejemplo y mira el cuerpo:

```vhdl
y <= d when oe = '0' else 'Z';
```

Léelo en voz alta: "`y` vale `d` **cuando `oe` es 0**, si no, `'Z'`." Está al revés. Quieres que el buffer deje pasar los datos cuando el enable esté **alto**, y que libere cuando esté **bajo**.

## Tu misión

- Abre el ejemplo en el editor.
- Arregla la condición de la asignación `y <=` para que el sentido de `oe` sea correcto:

```vhdl
y <= d when oe = '1' else 'Z';
```

- Ejecuta la simulación y verifica en la pestaña de formas de onda:
  - Con `oe = '0'`, `y` debe ser `'Z'`.
  - Con `oe = '1'`, `y` debe seguir a `d`.

## Por qué `'Z'` es especial aquí

`'Z'` no es un tercer *nivel* lógico — significa "no estoy manejando". En la forma de onda, GTKWave lo muestra como una línea estacionada en el medio, ni alta ni baja. Todo el punto de un tristate es permitir que un cable tenga **varios** drivers potenciales mientras solo uno está activo a la vez. Eso solo es seguro porque `std_logic` es un tipo *resuelto*: cuando un driver está en `'Z'` y otro maneja `'0'` o `'1'`, la resolución deja ganar al valor fuerte. Si todos los drivers están en `'Z'`, el cable flota.

## Extra: cuidado con `'X'`

Si dos buffers tristate manejan el mismo bus al mismo tiempo con valores **distintos**, un circuito real pelearía consigo mismo y se sobrecalentaría. La función de resolución modela esto como `'X'` (desconocido fuerte) en la forma de onda — una bandera roja clara de que tus enables se traslapan. Construye un pequeño testbench con dos de tus buffers cableados al mismo `y`, enciende ambas líneas `oe` con `d` distintos, y observa el bus volverse `'X'`.

## Por qué importa el tristate (y dónde vive hoy)

Los buffers tristate son la forma en que varios drivers comparten un mismo cable (un bus) sin cortocircuitarse. Una advertencia importante para el hardware al que realmente apuntarás: **los FPGA no tienen buffers tristate internos** en su tela lógica — la síntesis reemplaza automáticamente un bus tristate interno por multiplexores. Los tristates solo sobreviven en los **pads de I/O**, donde son exactamente cómo funcionan los pines bidireccionales: las líneas de datos de memoria DDR, el `sda` de I²C y otros buses compartidos encienden y apagan el driver del pin con un `oe`. Ese pad bidireccional se modela con un puerto `inout`:

```vhdl
io_pin <= drive_value when oe = '1' else 'Z';  -- maneja o libera el pad
read_value <= io_pin;                           -- muestrea siempre lo que haya
```

## Qué recordar

- Un tristate deja pasar los datos cuando está habilitado y maneja `'Z'` (liberado) cuando no — acierta con el sentido de `oe`.
- `'Z'` significa "no estoy manejando", que es lo que permite a varios drivers compartir un mismo cable `std_logic` resuelto.
- Dos drivers activos con valores distintos se resuelven a `'X'` — un bug que puedes ver en la forma de onda.
- Dentro de un FPGA, los tristates se vuelven muxes; los tristates reales viven en los pads de I/O para pines bidireccionales (`inout`).
