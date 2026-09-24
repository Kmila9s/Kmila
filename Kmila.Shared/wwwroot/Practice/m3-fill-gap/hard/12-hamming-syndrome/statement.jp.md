## ハミング(7,4) 症候群 — 穴埋め

⚠ 翻訳待ち — 説明は英語で表示されます。

Compute the 3-bit syndrome for a Hamming(7,4) code word `code(6 downto 0)`. Each syndrome bit is the parity of a specific subset of code bits — when the code is valid the syndrome is `"000"`.

```vhdl
s(0) <= code(6) xor code(4) xor code(2) xor code(0);
s(1) <= code(5) xor code(4) xor code(1) xor code(0);
s(2) <= code(3) xor code(2) xor code(1) xor code(0);
```
