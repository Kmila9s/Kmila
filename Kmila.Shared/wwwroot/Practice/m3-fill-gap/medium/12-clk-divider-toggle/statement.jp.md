## クロック分周器 — トグルを埋める

4 ビット・クロック分周器は**ほぼ**完成。モジュロ 10 のカウンタ
(`TOP = 9`)、リセット、出力 `clk_out <= clk_reg` は配線済み。
カウンタが `TOP` に達すると `0` に戻ります。

カウンタがオーバーフローする**同じ瞬間**に `clk_reg` をどうするか
記述してください:反転すべきです。それ以外の瞬間は `clk_reg` を保持。

### 仕様

* 非同期リセット:`count ← 0`、`clk_reg ← '0'`。
* `rising_edge(clk)` で:
  * `count = TOP` → `count ← 0`、`clk_reg ← not clk_reg`
  * それ以外 → `count ← count + 1`

### 編集箇所

`-- BEGIN_EDIT` と `-- END_EDIT` の間だけ。カウンタ、リセット、
出力はロック。

### ヒント

`clk_reg <= not clk_reg;`
