# シグナル vs 変数

これは VHDL で最も誤解されている区別です。学生は「変数は C のローカル変数のようなもの」と読み、間違った方向に走り出します。地に足を付けましょう。

## VHDL の 2 種類の名前

VHDL にはアーキテクチャレベルで `signal` で宣言される **シグナル**と、プロセス内で `variable` で宣言される **変数** があります。どちらも値を保持でき、どちらも代入できます。それらは **互換ではありません**。

## 黄金律

- **シグナル代入は現在のシミュレーションティックの終わりに起こる。**
- **変数代入はプロセス内で即座に起こる。**

それだけです。シグナルと変数のすべての「落とし穴」は、この規則の言い換えです。

## デモ

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- request: set s to 1 (not visible yet)
      s <= 2;           -- request: set s to 2 (overrides the first)
      -- what is s here?  Still 0. Neither assignment has taken effect.
    end if;
  end process;
end architecture demo;
```

クロックエッジが完了した後、`s` は **2** になります (同じティック内で複数の代入があるとき、最後の代入だけが「勝つ」)。プロセス内では、両方の代入の後でも `s` はまだ `0` です — プロセスが終わるまで新しい値は伝播しないからです。

変数でやると:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- variable lives inside the process
  begin
    if rising_edge(clk) then
      v := 1;           -- takes effect immediately
      v := 2;           -- takes effect immediately
      -- v is 2 here, right now, during this tick
    end if;
  end process;
end architecture demo;
```

2 つのことに注目:

- 変数は `:=` を使い、シグナルは `<=` を使う。
- 変数は単一のプロセスにローカル。

## なぜこれがハードウェアで重要か

シグナルは **ワイヤまたはフリップフロップをモデル化** します。同じ瞬間にワイヤの値を 2 回変えることはできません — 1 ティックの間にワイヤを駆動するものが何であれ、そのティックの終わりに 1 つの値に落ち着きます。

変数は **プロセス内部の一時的な計算をモデル化** します。シンセサイザはそれを組合せロジックとして実装したり、完全に最適化して消し去ったりできます。

## いつどちらを使うか

- **シグナル** — デフォルト。すべてのポート、すべてのレジスタ、波形ビューアで見るすべてのバス: シグナル。
- **変数** — プロセス内部で C ライクなセマンティクスが欲しい、ローカルなループカウンタや中間的な算術結果用。

## よくある罠

一部の学生はこう書きます:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- register 1
    b <= a;    -- register 2 — "swaps" a and b
  end if;
end process;
```

彼らはこれが `a` と `b` を入れ替えると期待します。シグナル代入はティックの終わりに起こるので、実際に **入れ替えます**: エッジで、`a` は `b` の **古い** 値を得て、`b` は `a` の **古い** 値を得ます。これが実は 1 サイクルで 2 つのレジスタを入れ替える慣用的な方法です。

しかし変数に切り替えるとセマンティクスが変わります:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a now equals b
    b := a;    -- b gets the NEW a — so b keeps its old value, and a changes
  end if;
end process;
```

入れ替えなし。両方とも古い `b` に等しい。

## 覚えておくべきこと

- `signal`: ティックの終わりに更新。`<=` 代入。ワイヤとレジスタをモデル化。
- `variable`: 即座に更新。`:=` 代入。プロセスにローカル。中間計算をモデル化。
- 迷ったら — シグナルを使う。
