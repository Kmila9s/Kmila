## FIFO read-pointer — fill the gap

A small FIFO skeleton has its **write pointer**, **empty flag**
and **reset** already wired. The empty signal is computed as
`is_empty <= '1' when wr_ptr = rd_ptr else '0';`.

Your task is to advance the **read pointer** correctly: it must
only increment when the host issues `pop = '1'` **and** the FIFO
is **not empty** (so we never pop from an empty queue).

### Specification

* Inside the existing `rising_edge(clk)` branch, write the guard:
  ```vhdl
  if pop = '1' and is_empty = '0' then
      rd_ptr <= rd_ptr + 1;
  end if;
  ```
* Do not touch `wr_ptr` or `is_empty`.
* Pops that arrive while empty must be **silently ignored**.

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded. The pointer declarations, the empty-flag formula and
the write-pointer logic are locked.
