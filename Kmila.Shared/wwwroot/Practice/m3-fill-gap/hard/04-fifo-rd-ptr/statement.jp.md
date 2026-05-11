## FIFO read-pointer — fill the gap

⚠ 翻訳待ち — 説明は英語で表示されます。

A small FIFO skeleton has its write pointer, empty flag and
reset already wired. Advance the read pointer only when
`pop = '1'` AND the FIFO is not empty.

### Hint

```vhdl
if pop = '1' and is_empty = '0' then
    rd_ptr <= rd_ptr + 1;
end if;
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
