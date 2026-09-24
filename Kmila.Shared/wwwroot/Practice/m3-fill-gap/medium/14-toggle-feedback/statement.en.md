## Toggle register feedback — fill

A toggle register is wired but the feedback line is missing. When `en = '1'` on a rising clock edge, `qi` should flip; otherwise it holds.

Implement the line between `-- BEGIN_EDIT` and `-- END_EDIT`:

```vhdl
qi <= not qi;
```

Reset is asynchronous. Only the marked line is graded.
