## Árbitro round-robin — rotación de grant

Un árbitro round-robin de 4 entradas guarda `last_grant` (índice del último que obtuvo el bus). En cada ciclo otorga al siguiente solicitante en orden cíclico tras `last_grant`. Si ningún bit de `req` está activo, no se otorga.

Implementa el `case` que asigna `nxt_grant` según `last_grant` y `req`. La máquina de estados y el registro `last_grant` ya están cableados fuera de los marcadores.

Sólo se evalúan las líneas entre `-- BEGIN_EDIT` y `-- END_EDIT`.
