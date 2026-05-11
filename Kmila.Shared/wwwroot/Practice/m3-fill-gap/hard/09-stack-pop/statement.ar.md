## Stack pop with empty-guard — fill the gap

⚠ الترجمة معلقة — الوصف معروض بالإنجليزية.

A simple stack pointer with the push branch and empty flag
already wired. Decrement `sp` on pop, but only when not empty.

### Hint

```vhdl
if pop = '1' and sp /= 0 then
    sp <= sp - 1;
end if;
```

### Where to write

Only the lines between `-- BEGIN_EDIT` and `-- END_EDIT` are
graded.
