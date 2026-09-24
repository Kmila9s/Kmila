## 2-to-1 multiplexer

Route `a` or `b` to `y` depending on `sel`. When `sel = 0` the output is `a`; when `sel = 1` it is `b`.

| `sel` | `y`   |
|:-----:|:-----:|
|  0    |  `a`  |
|  1    |  `b`  |

### Hint

The `when ... else` conditional assignment is one clean way:
`y <= a when sel = '0' else b;`
