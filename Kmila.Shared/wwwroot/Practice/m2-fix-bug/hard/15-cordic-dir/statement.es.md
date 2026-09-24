## CORDIC — dirección invertida

Una iteración CORDIC debe rotar antihorario cuando `dir = '1'`:
* `x' = x - y`
* `y' = y + x`

Y horario cuando `dir = '0'` (signos invertidos).

El starter cablea `dir` al revés: `dir = '1'` produce rotación horaria. Encuentra y corrige la condicional que selecciona las fórmulas.
