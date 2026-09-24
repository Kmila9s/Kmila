## 4 ビット加算器

キャリー入力 `cin` 付きの 4 ビット加算器を実装します。`sum = a + b + cin` とキャリー出力 `cout` を計算します。

### 仕様

- 入力:`a`、`b` は 4 ビット `STD_LOGIC_VECTOR`、`cin` は 1 ビット。
- 出力:`sum` は 4 ビット、`cout` は 1 ビット。

### ヒント

`IEEE.NUMERIC_STD.ALL` と 5 ビットの `unsigned` 一時信号を使います:

```vhdl
signal s : unsigned(4 downto 0);
s    <= ('0' & unsigned(a)) + ('0' & unsigned(b)) + ("0000" & cin);
sum  <= std_logic_vector(s(3 downto 0));
cout <= s(4);
```
