# signal と variable

VHDL で最も誤解されている区別。学生は「variable は C のローカル変数のようなもの」と読んで間違った方向に走り出す。地に足を着けよう。

## VHDL の 2 種類の名前

VHDL は **signal**(`signal` を使ってアーキテクチャレベルで宣言)と **variable**(`variable` を使って process 内で宣言)を持つ。両方とも値を保持できる。両方とも代入できる。両者は**交換可能ではない**。

## 黄金律

- **signal 代入は現在のシミュレーション tick の終わりに起きる。**
- **variable 代入は即座に、process 内で起きる。**

それだけ。signal vs variable のあらゆる「落とし穴」はこのルールの言い換え。

## デモ

```vhdl
architecture demo of ex is
  signal s : integer := 0;
begin
  process(clk)
  begin
    if rising_edge(clk) then
      s <= 1;           -- 要求:s を 1 に(まだ見えない)
      s <= 2;           -- 要求:s を 2 に(最初を上書き)
      -- ここで s は何?まだ 0。どちらの代入も効いていない。
    end if;
  end process;
end architecture demo;
```

クロックエッジが完了したあと、`s` は **2** になる(同じ tick で複数起きると最後の代入だけが「勝つ」)。process 内部では、両代入後でも `s` はまだ `0` — 新しい値は process が終わるまで伝播しないから。

variable を使うと:

```vhdl
architecture demo of ex is
begin
  process(clk)
    variable v : integer := 0;   -- variable は process 内に生きる
  begin
    if rising_edge(clk) then
      v := 1;           -- 即座に効く
      v := 2;           -- 即座に効く
      -- v はここで、今、この tick の間、2
    end if;
  end process;
end architecture demo;
```

2 つのことに注意:

- variable は `:=` を使う;signal は `<=`。
- variable は 1 つの process にローカル。

## なぜハードウェアにとって重要か

signal は**線かフリップフロップをモデル化する**。線の値を同じ瞬間に 2 回変えられない — 1 tick の間に線を駆動するもの何であれ、その tick の終わりに 1 つの値に落ち着く。

variable は process 内部の**一時的な計算をモデル化する**。シンセサイザはこれを組合せ論理として実装することも、完全に最適化で消すこともできる。

## どちらをいつ使うか

- **signal** — デフォルト。あらゆるポート、あらゆるレジスタ、波形ビューアで見るあらゆるバス:signal。
- **variable** — 1 つの process 内部のローカルなループカウンタや中間算術結果。その process 内で C のような意味論が欲しいとき。

## よくある罠

ある学生はこう書く:

```vhdl
process(clk)
begin
  if rising_edge(clk) then
    a <= b;    -- レジスタ 1
    b <= a;    -- レジスタ 2 — a と b を「入れ替える」
  end if;
end process;
```

これで `a` と `b` を入れ替えるつもり。signal 代入は tick の終わりに起きるので、実際に**入れ替わる**:エッジで `a` は `b` の**古い**値を、`b` は `a` の**古い**値を取る。これは 1 サイクルで 2 つのレジスタを入れ替える慣用的な方法。

しかし variable に切り替えると意味論が変わる:

```vhdl
process(clk)
  variable a, b : integer;
begin
  if rising_edge(clk) then
    a := b;    -- a は今 b に等しい
    b := a;    -- b は新しい a を取る — つまり b は古い値を保ち、a が変わる
  end if;
end process;
```

入れ替わらない。両方とも古い `b` に等しいだけ。

## 覚えておくべきこと

- `signal`:tick の終わりに更新。`<=` 代入。線とレジスタをモデル化。
- `variable`:即座に更新。`:=` 代入。process にローカル。中間計算をモデル化。
- 迷ったら — signal を使え。
