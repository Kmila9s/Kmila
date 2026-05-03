## Registro de desplazamiento de 8 bits — dirección equivocada

Se pedía un registro serie-entrada / paralelo-salida que desplaza **a la izquierda** en cada flanco.

### Especificación

* En `rising_edge(clk)`: `r <- r(6 downto 0) & din` — `din` entra por el bit 0, el MSB se descarta.
* `reset` asíncrono activo en alto pone `r` a ceros.
* `q` refleja `r`.

### Qué falla

La concatenación está invertida: `din` se inserta por el lado del MSB, así que tras varios ciclos con `din = '1'` se llenan primero los bits altos en lugar de los bajos.

### Pista

En `r <= a & b`, el operando de la **izquierda** se convierte en los MSB. Para desplazar a la izquierda, descarta el MSB y coloca `din` a la derecha.
