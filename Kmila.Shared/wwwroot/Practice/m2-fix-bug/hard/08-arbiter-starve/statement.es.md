## Árbitro de 4 vías — corrige la inanición

Cuatro agentes excitan `req(3..0)`. El árbitro debe entregar un
único grant por ciclo, pero la implementación actual es un simple
codificador de prioridad fija: la petición activa de menor índice
siempre gana. Mientras `req(0)` esté activa, `req(3)` queda
**en inanición permanente**.

### Especificación

* Reset síncrono activo-alto.
* En cada ciclo hay exactamente un bit de `grant` a `'1'` (o
  `0000` si `req = 0000`).
* Si varias peticiones están activas, el árbitro debe rotar —
  cada solicitante obtiene su turno en un máximo de 4 ciclos.

### Pista

Un árbitro round-robin recuerda el **último índice concedido** y
escanea empezando por `last_grant + 1` (mod 4). La primera petición
activa que encuentre en ese barrido es la ganadora.
