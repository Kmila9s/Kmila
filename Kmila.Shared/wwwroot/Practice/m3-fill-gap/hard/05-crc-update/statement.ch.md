## CRC-8 update step — fill the gap

⚠ 翻译未完成 — 描述以英文显示。

A serial CRC-8 register is almost complete. Implement the
one-bit step on every rising clock edge.

### Hint

```vhdl
fb := r(7) xor din;
if fb = '1' then
    r <= (r(6 downto 0) & '0') xor POLY;
else
    r <= r(6 downto 0) & '0';
end if;
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
