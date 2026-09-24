## Tabla de semáforo — completa el hueco

Un controlador de semáforo de 3 estados está **casi** listo.
El registro de estado, el reset asíncrono y la asignación de
salida `state <= cur;` ya están cableados.

Tu tarea es escribir el **CASE combinacional de siguiente estado**
que selecciona `nxt` a partir de `cur`.

### Especificación

* La FSM recorre tres estados codificados:
  * `00` → `01` → `10` → `00` → …
* Usa `case cur is … end case;` con ramas explícitas para
  `"00"`, `"01"`, `"10"` y un `when others =>` que vuelva a
  `"00"`.
* El bloque es puramente combinacional — **no** añadas flanco
  de reloj.

### Dónde escribir

Sólo las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT` se
evalúan. Todo lo de fuera (entidad, registro, reset, salida)
es estructura bloqueada.
