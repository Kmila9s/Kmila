## アップ/ダウン・カウンタ — 方向行を埋める

4 ビット同期カウンタは**ほぼ**完成。リセット、立ち上がりエッジ検出、
`q <= std_logic_vector(count)` はすでに配線済み。

立ち上がりエッジで方向制御を実装してください:

* `dir = '1'` → `count ← count + 1`
* `dir = '0'` → `count ← count - 1`

### 仕様

* 非同期リセット:`count ← 0000`。
* 4 ビット unsigned で自由にラップ。

### 編集箇所

`-- BEGIN_EDIT` と `-- END_EDIT` の間だけ。リセットと出力はロック。

### ヒント

短い `if dir = '1' then … else … end if;` で OK。
