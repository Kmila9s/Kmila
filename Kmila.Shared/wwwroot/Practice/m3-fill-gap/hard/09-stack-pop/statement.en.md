## Stack pop with empty-guard — fill the gap

A simple stack pointer is **almost** complete. The reset, the
**push** branch (`sp <= sp + 1` when `push = '1'`) and the
combinational `empty` flag (`'1' when sp = 0 else '0'`) are
already wired.

Your task is to write the **pop** branch with a proper
under-flow guard.

### Specification

* Inside the existing `rising_edge(clk)` clause, after the push
  branch, decrement `sp` only if a pop is being requested AND
  the stack is not already empty:
  ```vhdl
  if pop = '1' and sp /= 0 then
      sp <= sp - 1;
  end if;
  ```
* Pops to an empty stack must be **silently ignored**.
* Do not touch the push branch or the empty flag.

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
