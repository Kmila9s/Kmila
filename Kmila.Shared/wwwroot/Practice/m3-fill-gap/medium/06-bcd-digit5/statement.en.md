## BCD-to-7-segment — fill in digit 5

A BCD-to-7-segment decoder is **almost** complete: digits 0-4 and 6-9
are already wired up in a `case` statement. Only the entry for digit 5
is missing.

You must write the line that maps `digit = "0101"` to its 7-segment
pattern. The output bit order is `seg(6..0) = a..g`, **active high**
(a lit segment is `'1'`).

For digit 5 the lit segments are `a, c, d, f, g` (segments `b` and `e`
are off), giving the pattern:

```
seg = "1011011"
```

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The other `case`
arms are locked.

### Hint

Just one `when "0101" => seg <= "1011011";` line.
