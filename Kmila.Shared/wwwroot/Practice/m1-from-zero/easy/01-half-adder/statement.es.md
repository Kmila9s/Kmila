## Medio sumador

Construye un **medio sumador** de 1 bit. Dados dos bits `a` y `b`,
entrega su suma bit a bit y el acarreo de salida.

### Tabla de verdad

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Entidad (bloqueada)

```vhdl
entity half_adder is
    port (
        a   : in  STD_LOGIC;
        b   : in  STD_LOGIC;
        sum : out STD_LOGIC;
        cout: out STD_LOGIC
    );
end entity half_adder;
```

### Pista

Una compuerta por salida: XOR para `sum`, AND para `cout`.
