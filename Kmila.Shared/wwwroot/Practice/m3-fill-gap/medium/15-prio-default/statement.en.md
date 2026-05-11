## Priority encoder — fill the default

A 4-bit priority encoder picks the highest set bit and outputs its index. The four priority cases (`d(3)`, `d(2)`, `d(1)`, `d(0)`) are already in place. Provide the **catch-all default** for when no bit is set.

Implement the line between `-- BEGIN_EDIT` and `-- END_EDIT`:

```vhdl
y <= "00";
```

Only the marked line is graded.
