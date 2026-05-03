## Handshake — corrige la carrera metaestable

Un sencillo handshake `req`/`ack` falla de forma intermitente. Cuando
el bloque emisor pone `req` a `1` durante un solo ciclo (un glitch
debido al cruce de dominios de reloj o a un bus ruidoso), `ack` lo
sigue inmediatamente y el bloque receptor inicia falsamente una
transacción.

### Especificación

* Reset síncrono activo-alto.
* `ack` solo puede activarse cuando se ha observado `req` **estable
  en alto durante al menos un ciclo de reloj completo**.
* Un glitch de un solo ciclo en `req` **nunca** debe producir
  `ack = '1'`.

### Pista

Dos flip-flops en serie forman un pequeño "filtro de estabilidad":
la salida se pone a `1` solo cuando ambas etapas ya han capturado
un `'1'`. Es el patrón canónico del sincronizador.
