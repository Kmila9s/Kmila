## Paso de actualización CRC-8 — completa el hueco

Un registro CRC-8 serial está **casi** completo. Te damos
listos: el registro de 8 bits `r`, la constante polinomio
`POLY = x"07"`, el reset asíncrono y la salida `crc <= r;`.
Dentro del proceso ya está declarada la variable `fb : STD_LOGIC`.

Tu tarea es escribir el **paso de un bit del CRC** que se ejecuta
en cada flanco ascendente del reloj.

### Especificación

* Calcula el bit de realimentación:
  ```vhdl
  fb := r(7) xor din;
  ```
* Si `fb = '1'`, desplaza `r` a la izquierda (inserta `'0'` en
  el LSB) y haz XOR con `POLY`. Si no, sólo desplaza:
  ```vhdl
  if fb = '1' then
      r <= (r(6 downto 0) & '0') xor POLY;
  else
      r <= r(6 downto 0) & '0';
  end if;
  ```

### Dónde escribir

Sólo las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT` se
evalúan. El reset, el registro y la asignación de salida están
bloqueados.
