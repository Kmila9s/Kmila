## Multiplicador de Booth — shift erróneo

Una iteración de Booth debe hacer un shift derecho **aritmético** sobre el acumulador (extensión de signo). El starter hace un shift derecho **lógico** (rellena con 0), corrompiendo cualquier valor negativo.

Encuentra la línea que convierte a `unsigned` antes del shift y reemplázala por `shift_right(signed(...), 1)` para que el bit de signo se propague.
