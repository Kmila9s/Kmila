## Pop de pila con guarda de vacío — completa el hueco

Un puntero de pila simple está **casi** listo. El reset, la
rama de **push** (`sp <= sp + 1` cuando `push = '1'`) y la
señal combinacional `empty` (`'1' when sp = 0 else '0'`) ya
están cableados.

Tu tarea es escribir la rama de **pop** con la debida guarda
de underflow.

### Especificación

* Dentro del `rising_edge(clk)` existente, tras la rama de push,
  decrementa `sp` sólo si hay pop y la pila no está vacía:
  ```vhdl
  if pop = '1' and sp /= 0 then
      sp <= sp - 1;
  end if;
  ```
* Los pop a pila vacía deben **ignorarse en silencio**.
* No toques la rama de push ni el flag empty.

### Dónde escribir

Sólo las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT` se
evalúan.
