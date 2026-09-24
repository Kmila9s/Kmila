## Half-adder

Build a 1-bit **half-adder**. Given two single-bit inputs `a` and `b`,
produce the bitwise sum and the carry-out.

### Truth table

| `a` | `b` | `sum` | `cout` |
|:---:|:---:|:-----:|:------:|
|  0  |  0  |   0   |   0    |
|  0  |  1  |   1   |   0    |
|  1  |  0  |   1   |   0    |
|  1  |  1  |   0   |   1    |

### Entity (locked)

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

### Hint

One gate per output: XOR for `sum`, AND for `cout`.
