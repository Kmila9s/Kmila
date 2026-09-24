## Detector de flanco ascendente

Emite un pulso de un ciclo en `pulse` cada vez que `d` pasa de `0` a `1`. Un `reset` asíncrono activo en alto limpia el estado interno.

### Especificación

- Interno: `signal d_prev : std_logic := '0';`.
- En cada flanco: `d_prev <= d;`.
- Salida combinacional: `pulse = '1'` cuando `d = '1' and d_prev = '0'`.

### Pista

Compara la entrada actual `d` con su valor muestreado previo para detectar el flanco ascendente.
