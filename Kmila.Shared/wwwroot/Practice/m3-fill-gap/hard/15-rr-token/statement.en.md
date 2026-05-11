## Round-robin token — fill

A 4-bit one-hot token rotates left by one position every clock cycle. Reset puts the token back at `"0001"`. Implement the rotation line.

```vhdl
tok <= tok(2 downto 0) & tok(3);
```

Only the line between the markers is graded.
