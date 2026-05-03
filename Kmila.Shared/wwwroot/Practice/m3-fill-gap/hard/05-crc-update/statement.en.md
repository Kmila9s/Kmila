## CRC-8 update step — fill the gap

A serial CRC-8 register is **almost** complete. The 8-bit
register `r`, the polynomial constant `POLY = x"07"`, the
asynchronous reset and the output `crc <= r;` are wired for you.
A `variable fb : STD_LOGIC` is declared inside the process.

Your task is to write the **one-bit CRC step** that runs on each
rising clock edge.

### Specification

* Compute the feedback bit:
  ```vhdl
  fb := r(7) xor din;
  ```
* If `fb = '1'`, shift `r` left by one (insert `'0'` in LSB) and
  XOR the result with `POLY`. Otherwise just shift left by one:
  ```vhdl
  if fb = '1' then
      r <= (r(6 downto 0) & '0') xor POLY;
  else
      r <= r(6 downto 0) & '0';
  end if;
  ```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded. The reset, the register declaration and the output
assignment are locked.
