## Pila — corrige el desfase de un ciclo en `empty`

Una pila simple informa de su estado `empty` con un ciclo de
retraso. Tras un push, `empty` permanece a `'1'` un ciclo de más;
tras el último pop, `empty` vuelve a `'1'` un ciclo después de
que `count` ya valga 0. El bloque consumidor ve una bandera
desfasada y o se pierde el primer elemento válido o lee de una
pila vacía.

### Especificación

* Reset síncrono activo-alto.
* `count` incrementa en `push` (y no `pop`), decrementa en `pop`
  (y no `push`, mientras `count > 0`).
* `empty = '1'` exactamente cuando `count = 0`, **sin un ciclo de
  retraso**.

### Pista

No registres la comparación: `empty` debe ser una salida
combinacional sobre `count`.
