## مشفِّر هامينغ(7,4)

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

## Hamming(7,4) encoder

Encode a 4-bit data word `d = d3 d2 d1 d0` into a 7-bit Hamming codeword by appending three parity bits.

### Parity equations

| Bit | Formula                |
|:---:|:-----------------------|
| `p1`| `d0 xor d1 xor d3`     |
| `p2`| `d0 xor d2 xor d3`     |
| `p3`| `d1 xor d2 xor d3`     |

### Output layout

```
code <= p1 & p2 & d0 & p3 & d1 & d2 & d3;
```

Combinational only.
