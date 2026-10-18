# Compuerta NOT (inversor)

La **compuerta NOT** es la lógica más simple que existe: una entrada, una salida, y la salida siempre vale lo **contrario** de la entrada — si entra 0 sale 1, si entra 1 sale 0.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/02-not.html" title="Compuerta NOT interactiva — un balancín que invierte su entrada" loading="lazy"></iframe></div>

**Mueve el cursor** para poner la entrada **A** en 0 o 1. La figura es un balancín de brazos iguales: empuja la entrada (A) hacia abajo y la salida (Y) sube, y al revés —iguales y opuestos—. El pequeño círculo sobre la salida es la burbuja de inversión.

## La burbuja

Ese **círculo** sobre la salida es la *burbuja de inversión*, el símbolo del NOT en todo esquemático. Cada vez que lo veas en la salida de una compuerta (NAND, NOR, XNOR…) significa lo mismo: «y además, invierte».

| A | Y |
|---|---|
| 0 | 1 |
| 1 | 0 |

En VHDL el inversor es el operador `not`:

```vhdl
y <= not a;
```

Es puramente **combinacional**: la salida depende solo de la entrada actual —sin reloj ni memoria—. En cuanto cambia la entrada, la salida se invierte tras un pequeño retardo de propagación.
