## 8 ビット左シフトレジスタ

8 ビットのシフトレジスタを構築します。立ち上がりエッジごとに内容を左へ 1 ビット移動し、直列入力 `din` をビット 0 に取り込みます。非同期アクティブハイの `reset` でクリアします。

### 仕様

- 内部信号:`r : std_logic_vector(7 downto 0)`。
- 更新規則:`r <= r(6 downto 0) & din;`。
- 出力:`q <= r;`。

### ヒント

`process(clk, reset)` で `if reset = '1' then ... elsif rising_edge(clk) then ...` を使います。
