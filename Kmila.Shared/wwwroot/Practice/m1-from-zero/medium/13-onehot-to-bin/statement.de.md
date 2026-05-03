## One-Hot zu Binär

Wandle einen 4-Bit-One-Hot-Eingang `oh` in seinen 2-Bit-Binärindex `b` um. Genau ein Bit von `oh` ist `1`.

| `oh` | `b` |
|:---:|:---:|
| 0001 | 00 |
| 0010 | 01 |
| 0100 | 10 |
| 1000 | 11 |

### Hinweis

Eine einfache `when ... else`-Kaskade reicht. Die Ausgabe ist die Position der einzelnen `1`.
