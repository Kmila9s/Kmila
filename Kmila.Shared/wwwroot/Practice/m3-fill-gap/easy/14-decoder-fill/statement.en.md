## 2-to-4 decoder Y0 line — fill

Three of the four decoder outputs are wired. Implement the **Y0** line (asserts when `sel = "00"`).

```vhdl
y0_int <= '1' when sel = "00" else '0';
```

Only the line between the markers is graded.
