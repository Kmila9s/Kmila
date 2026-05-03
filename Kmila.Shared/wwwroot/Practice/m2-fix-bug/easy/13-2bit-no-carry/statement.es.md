## Sumador de 2 bits — encuentra y corrige el error

Este circuito debe sumar dos números de 2 bits `a` y `b` más un
acarreo de entrada `cin`, produciendo `sum` de 2 bits y un
`cout` final. El autor escribió cada bit de salida como un
medio sumador independiente y **olvidó propagar el acarreo** del
bit 0 al bit 1.

Encuentra el error y conecta bien los acarreos.

### Especificación

* `{cout, sum} = a + b + cin` (resultado de 3 bits sin signo).

### Pista

El bit 1 de la suma debe tener en cuenta un posible acarreo
generado por el bit 0.
