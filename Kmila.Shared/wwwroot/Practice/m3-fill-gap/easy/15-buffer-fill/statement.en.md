## Tri-state buffer — fill

Implement a tri-state buffer: when `en = '1'`, output `a`; otherwise drive `'Z'` (high impedance).

```vhdl
y <= a when en = '1' else 'Z';
```

Only the line between the markers is graded.
