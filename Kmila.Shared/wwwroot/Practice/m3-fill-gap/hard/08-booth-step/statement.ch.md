## ⚠ 翻译待补 — Booth multiplier — fill the gap

A 4×4 Booth multiplier is almost complete. Implement the
conditional add/subtract case based on `lsb_pair = q0 & q_minus1`.

### Hint

```vhdl
case lsb_pair is
    when "01"   => acc <= acc + m_ext;
    when "10"   => acc <= acc - m_ext;
    when others => acc <= acc;
end case;
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
