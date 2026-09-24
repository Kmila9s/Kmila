## 2-zu-1-Multiplexer

Leitet `a` oder `b` je nach `sel` an `y`. Bei `sel = 0` ist die Ausgabe `a`, bei `sel = 1` ist sie `b`.

| `sel` | `y`   |
|:-----:|:-----:|
|  0    |  `a`  |
|  1    |  `b`  |

### Hinweis

`when ... else` ist sauber:
`y <= a when sel = '0' else b;`
