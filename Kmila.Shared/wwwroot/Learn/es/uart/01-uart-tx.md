# Proyecto: transmisor UART

Un UART (Universal Asynchronous Receiver/Transmitter, transmisor-receptor asíncrono universal) es el primer protocolo real que construye la mayoría de los diseñadores de sistemas embebidos. Es punto a punto, full duplex, y necesita solo tres cables (TX, RX, GND). Todo adaptador USB-serial FTDI, todo `Serial.print` de Arduino, toda terminal vieja es un UART por dentro.

Este proyecto construye el lado de **transmisión**. El receptor es un excelente proyecto para hacer a continuación, una vez que termines este.

## Asíncrono: no hay cable de reloj

La "A" de UART es todo el reto. A diferencia de SPI o I²C, los dos extremos **no comparten ninguna línea de reloj**. El receptor tiene que recuperar la temporización a partir de los propios datos. Eso solo funciona si ambos extremos se ponen de acuerdo de antemano en la **velocidad de transmisión** (bits por segundo) y en el **formato de trama**. Equivócate en cualquiera de las dos y recibirás basura: el síntoma clásico de un desajuste de baudios.

En el curso de la ESCOM esto cae bajo el estándar RS-232. Vale la pena separar dos cosas que la gente confunde: el **UART** es la lógica digital que serializa los bytes (lo que construyes aquí, con niveles lógicos de 0 V / 3.3 V), mientras que **RS-232** es el estándar eléctrico que desplaza esos niveles a aproximadamente ±12 V con polaridad *invertida* para manejar un cable. Un controlador de línea tipo MAX232 se coloca entre el pin de tu UART y un conector DB-9 real. Tu VHDL solo trata con el lado limpio de nivel lógico.

## Formato de trama (sin paridad, 8 bits de datos, 1 de paro)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    la línea normalmente está alta ('1'); el bit de inicio es un flanco de bajada.
```

Esto es "8N1": **8** bits de datos, **N**inguna paridad (No parity), **1** bit de paro: 10 bits en el cable por cada byte. Las reglas que importan:

- El estado **idle** (reposo) es alto. Una línea rota o desconectada se lee como idle, no como una ráfaga de ceros.
- El **bit de inicio** es bajo. Su flanco de bajada es la única referencia de temporización que recibe el receptor.
- Los **datos** salen con el **LSB primero**: el bit 0 antes que el bit 7.
- El **bit de paro** es alto, lo que además garantiza que haya un flanco de bajada nuevo disponible para la siguiente trama.

Cada bit se sostiene en `tx` durante exactamente `clk_freq / baud` ciclos de reloj. Para un reloj de 50 MHz y 115 200 baudios, eso son 50 000 000 / 115 200 ≈ 434 ciclos por bit.

## Tu trabajo

1. Construye un contador de reloj que produzca un `baud_tick` de un ciclo cada ~434 ciclos.
2. Al pulsarse `send = '1'`, captura `data` en un registro de desplazamiento de 10 bits: `{ stop_bit, data[7..0], start_bit } = 1 & data & 0`. Levanta `busy <= '1'`.
3. En cada `baud_tick`, desplaza el registro a la derecha (para que el bit de inicio salga primero, luego del LSB al MSB de los datos, y por último el bit de paro).
4. Tras el décimo tick, baja `busy` y deja `tx` en alto idle.

## Andamiaje

La entidad y el estado idle están dados; `tx` por defecto es alto, `busy` por defecto es bajo. Todo lo que hay entre esos valores por defecto es tu responsabilidad.

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- POR HACER
end architecture rtl;
```

## Estructura sugerida

Dos contadores + un registro de desplazamiento:

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- Cuando `send = '1'` y `sending = '0'`:
  - carga `shift_reg <= '1' & data & '0';`
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- Mientras `sending = '1'`:
  - `clk_count <= clk_count + 1;`
  - si `clk_count = BAUD_PERIOD - 1`:
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — desplaza a la derecha, rellena arriba con idle
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - si `bit_count = 10`:
    - `sending <= '0';`

Haz de `BAUD_PERIOD` un generic para poder encogerlo en simulación (p. ej. 5 ciclos/bit) y que la misma lógica siga funcionando a velocidad real en el hardware.

## Cuánto error de temporización te puedes permitir

Como no hay reloj compartido, el receptor muestrea cerca del centro de cada bit y se desfasa un poco a lo largo de la trama. A lo largo de 10 períodos de bit, el error acumulado debe quedarse por debajo de medio bit, así que el error de baudios de cada extremo tiene que mantenerse por debajo de alrededor de ±2–3 %. Los divisores enteros rara vez caen exactos: a 50 MHz, 434 ciclos/bit dan 115 207 baudios, un error de 0.006 %, completamente seguro. Un reloj incómodo puede redondear feo, y por eso muchos UARTs reales agregan un divisor fraccionario. El lado receptor gana margen mediante **sobremuestreo** a 16× la velocidad de transmisión: encuentra el flanco de inicio, luego muestrea en la cuenta 8 (el centro del bit) y cada 16 ticks después.

## Qué probar

Maneja `send = '1'` con `data = "01001000"` (la `'H'` en ASCII). Tras completarse la trama, la forma de onda de `tx` debe mostrar exactamente 10 períodos de bit de actividad:

1. Inicio (bajo)
2. Bit 0 de los datos (que es el LSB: `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. Paro (alto)

Y luego de vuelta a idle alto. Haz zoom en la pestaña de Formas de onda y verifica el patrón bit por bit.

## Errores comunes

- **Orden de bits equivocado.** Enviar el MSB primero es el error más común de todos. El `'1' & data & '0'` del andamiaje más un desplazamiento a la derecha te da el LSB primero gratis; no lo "corrijas".
- **Error por uno en la cuenta de bits.** 8N1 son *10* ticks, no 8. Olvidar el bit de inicio o el de paro trunca la trama.
- **Re-disparar a media trama.** Atiende `send` solo cuando `sending = '0'`, o una segunda petición corromperá la que está en curso. Para eso sirve `busy`.
- **Desajuste de baudios.** Si el receptor muestra la cantidad correcta de bytes pero con valores revueltos, sospecha del divisor antes que de la lógica.

## Para ir más lejos

- Agrega un **FIFO** para poder encolar varios bytes; `send` se convierte en "push".
- Construye el lado **RX**: sobremuestrea a 16×, encuentra el flanco de inicio, muestrea en el centro del bit.
- Parametriza la trama: paridad, datos de 7 vs 8 bits, 1 vs 2 bits de paro.

## Qué recordar

- El UART es **asíncrono**: no hay cable de reloj, así que ambos extremos deben acordar de antemano la velocidad de transmisión y el formato de trama.
- 8N1 son **10 bits** en el cable: idle alto, inicio bajo, 8 bits de datos **con el LSB primero**, paro alto.
- Un bit dura `clk_freq / baud` ciclos; un `baud_tick` de un ciclo marca el ritmo del registro de desplazamiento.
- Mantén el error de baudios de cada extremo por debajo de ~2 %; el lado RX gana margen mediante **sobremuestreo y muestreando en el centro del bit**.
- El **UART** es la lógica; **RS-232** es la capa eléctrica de ±12 V que un controlador de línea agrega encima.
