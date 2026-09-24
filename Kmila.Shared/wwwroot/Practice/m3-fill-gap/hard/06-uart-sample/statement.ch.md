## UART mid-bit sampler — fill the gap

⚠ 翻译未完成 — 描述以英文显示。

In a 16× over-sampled UART receiver, the bit decision happens
at the centre of the bit window — when the oversample counter
reaches 8. Drive `sample_now <= '1'` only when `bit_count = "1000"`.

### Hint

```vhdl
if unsigned(bit_count) = "1000" then
    sample_now <= '1';
else
    sample_now <= '0';
end if;
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
