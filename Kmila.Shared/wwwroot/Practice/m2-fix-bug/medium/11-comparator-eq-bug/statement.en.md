## 4-bit comparator — equality output stuck

A 4-bit unsigned comparator was specified with three flag outputs:

* `gt = '1'` when `a > b`.
* `eq = '1'` when `a = b`.
* `lt = '1'` when `a < b`.

### What's wrong

`gt` and `lt` are correct. But `eq` is hardwired to `'0'`, so equal inputs (e.g. `a = b = 0101`) leave **all three** flags low — an inconsistent state for a comparator.

### Hint

Replace the constant assignment to `eq` with the same kind of conditional expression used for `gt` and `lt`, but checking equality instead of ordering.
