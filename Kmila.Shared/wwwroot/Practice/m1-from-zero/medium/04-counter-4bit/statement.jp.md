## 4 ビット同期カウンタ

立ち上がりエッジごとにインクリメントする 4 ビットカウンタを実装します。非同期アクティブハイの `reset` でカウントを `0000` に戻します。

### 仕様

- 内部信号:`count : unsigned(3 downto 0) := (others => '0')`。
- `rising_edge(clk)` で:`count <= count + 1;`。
- 出力:`q <= std_logic_vector(count);`。

### ヒント

`IEEE.NUMERIC_STD.ALL` を使えば `unsigned` に対して `+ 1` が使えます。
