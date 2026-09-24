## Codificador Hamming(7,4)

Codifica una palabra de 4 bits `d = d3 d2 d1 d0` en una palabra Hamming de 7 bits añadiendo tres bits de paridad.

### Ecuaciones

| Bit | Fórmula                |
|:---:|:-----------------------|
| `p1`| `d0 xor d1 xor d3`     |
| `p2`| `d0 xor d2 xor d3`     |
| `p3`| `d1 xor d2 xor d3`     |

### Layout de salida

```
code <= p1 & p2 & d0 & p3 & d1 & d2 & d3;
```

### Especificación

Sólo combinacional. Usa señales intermedias para los bits de paridad — facilita la lectura.

### Pista

Cada paridad cubre un subconjunto distinto de `d` — sale directo de la matriz generadora Hamming.
