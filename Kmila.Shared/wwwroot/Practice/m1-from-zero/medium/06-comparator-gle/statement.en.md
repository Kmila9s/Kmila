## Greater / Equal / Less comparator

Compare two 4-bit unsigned values `a` and `b` and drive three flags:

- `gt = 1` when `a > b`
- `eq = 1` when `a = b`
- `lt = 1` when `a < b`

Exactly one flag is `1` at any given time.

### Hint

Use `IEEE.NUMERIC_STD.ALL` and the conditional concurrent form:

```vhdl
gt <= '1' when unsigned(a) > unsigned(b) else '0';
```
