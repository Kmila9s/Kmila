## 4-bit comparator, equality output stuck

⚠ 翻译未完成 — 描述以英文显示。

A 4-bit unsigned comparator with `gt`, `eq`, `lt` flags. `gt` and `lt` are correct but `eq` is hardwired to `'0'`, so equal inputs leave all three flags low.

**Hint:** replace the constant `eq <= '0';` with a conditional expression that checks equality, like the ones used for `gt` and `lt`.
