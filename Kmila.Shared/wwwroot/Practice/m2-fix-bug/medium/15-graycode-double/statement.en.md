## 3-bit binary-to-Gray — Gray property broken

A standard binary-to-Gray converter was specified. The classic recipe for `g`, given binary `b`, is:

* `g(2) = b(2)`
* `g(1) = b(2) xor b(1)`
* `g(0) = b(1) xor b(0)`

This guarantees that adjacent binary values (`b` and `b+1`) differ in exactly one Gray bit.

| `b`  | Gray |
|:----:|:----:|
| 000  | 000  |
| 001  | 001  |
| 010  | 011  |
| 011  | 010  |
| 100  | 110  |

### What's wrong

The starter mis-derives `g(2)`, folding `b(0)` into it. As a result, going from `b = 000` to `b = 001` flips both `g(0)` **and** `g(2)` — two bits change at once, which is precisely what Gray code is supposed to avoid.

### Hint

`g(2)` should depend only on the **MSB** of the binary input.
