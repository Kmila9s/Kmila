## 4-to-1 multiplexer — fill the whole select block

The entity declares the four data inputs `a, b, c, d`, the 2-bit
select `sel` and the single output `y`. The architecture is empty
except for the editable region.

You must implement the entire 4-way selection in one concurrent
assignment to `y`:

| `sel` | `y` |
|:---:|:---:|
| `00` | `a` |
| `01` | `b` |
| `10` | `c` |
| `11` | `d` |

### Where to write

Edit only between `-- BEGIN_EDIT` and `-- END_EDIT`. The entity
declaration is locked.

### Hint

Chain three `when … else` clauses and let the final `else` cover
`sel = "11"`.
