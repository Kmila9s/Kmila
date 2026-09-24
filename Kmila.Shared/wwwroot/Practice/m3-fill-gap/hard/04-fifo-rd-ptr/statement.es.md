## Puntero de lectura de la FIFO — completa el hueco

El esqueleto de una FIFO ya tiene cableados el **puntero de
escritura**, el **flag empty** y el **reset**. La señal vacía se
calcula como `is_empty <= '1' when wr_ptr = rd_ptr else '0';`.

Tu tarea es avanzar correctamente el **puntero de lectura**:
sólo debe incrementarse cuando el host emite `pop = '1'` **y**
la FIFO **no está vacía** (para nunca leer en cola vacía).

### Especificación

* Dentro de la rama `rising_edge(clk)` existente, escribe la
  guarda:
  ```vhdl
  if pop = '1' and is_empty = '0' then
      rd_ptr <= rd_ptr + 1;
  end if;
  ```
* No toques `wr_ptr` ni `is_empty`.
* Los pops que lleguen estando vacía deben **ignorarse en
  silencio**.

### Dónde escribir

Sólo las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT` se
evalúan. Las declaraciones de los punteros, la fórmula del
flag vacío y la lógica de escritura están bloqueadas.
