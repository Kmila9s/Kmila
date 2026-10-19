# テストベンチ思考

ここまでのすべてのレッスンで、Kmila があなたのテストベンチだった — Ports タブにスティミュラスをドラッグし、Run で設計を実行し、波形を目で見てきた。学習中はそれで構わない。しかし、プロのデジタル設計はそうはやらない。

## 本物のテストベンチとは

**テストベンチ** とは、*テスト対象設計*(DUT)を動かすことだけを仕事にする、もう 1 つの VHDL entity だ。それを駆動するものは何もないので、ポートを持たない。DUT を component として宣言し、自身の内部信号を DUT のポートに配線し、それから **process** を使って時間に沿ってスティミュラスの列をたどる。

骨格:

```vhdl
entity tb_counter is
end entity tb_counter;

architecture sim of tb_counter is
    component counter
        port ( clk, reset : in  std_logic;
               q          : out std_logic_vector(3 downto 0) );
    end component;

    signal clk   : std_logic := '0';
    signal reset : std_logic := '1';
    signal q     : std_logic_vector(3 downto 0);
begin
    dut : counter port map (clk, reset, q);

    clk <= not clk after 10 ns;            -- 50 MHz

    process
    begin
        wait for 20 ns;
        reset <= '0';
        wait for 200 ns;
        assert q = "1010"
            report "200 ns 時点でカウンタは 10 のはず、得られた値は " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

3 つの新しい考え方:

1. **`wait`** — プロセスがそう言ったときにだけ時間が進む。
2. **`assert … report … severity`** — 自己検証。1000 行の波形を「pass」か「fail」に縮約する。
3. **ポートなしの entity** — テストベンチは自己完結していて、シミュレータが直接インスタンス化する。

## クロックを生成する

あの 1 行、`clk <= not clk after 10 ns;` がクロックのすべてだ:10 ns ごとに反転し、20 ns 周期(50 MHz)を作る。`clk` の初期値が `'0'` なので、きれいにスタートする。デューティ比が 50% でないクロックや、あとで止めたいクロックには、代わりにプロセスを使う:

```vhdl
clk_gen : process
begin
    clk <= '0'; wait for 10 ns;
    clk <= '1'; wait for 10 ns;
    if done then wait; end if;   -- テストが終わったら凍結する
end process;
```

クロック生成とスティミュラスは **別々の** プロセスに分けよう。クロックはただ刻むだけ。スティミュラスプロセスは入力を駆動し、出力を確認する。DUT に揃うよう、たいていは `wait until rising_edge(clk)` で同期をとる。

## 自己検証:assert、severity、ゴールデン値

テストベンチの要点は、それが *自分自身を* 確認することだ。`assert` は条件を評価し、それが偽なら `report` の文字列を、選んだ `severity` で表示する。4 つの severity レベルは **note**、**warning**、**error**、**failure** だ — そして多くのシミュレータは、あるレベルのものが発火した瞬間に停止するよう指示できる。これが「正しそうに見えた」を、はっきりした pass/fail に変える方法だ。

ちょっとした規模以上の設計では、期待値を 1 つ 1 つ手で書くのはやめよう。**ゴールデンモデル** — 単純で明らかに正しい参照(テストベンチ内のただのソフトウェア風の計算でも構わない) — から計算し、毎サイクル DUT をそれと比較する。この参照との比較こそが **スコアボード** の核心的な考え方で、カウンタから CPU までスケールする。

```vhdl
expected <= std_logic_vector(unsigned(expected) + 1) when rising_edge(clk);
assert q = expected
    report "不一致:得られた値 " & to_string(q) & "、期待値 " & to_string(expected)
    severity error;
```

(`std_logic_vector` 向けの `to_string` は VHDL-2008 の便利機能だ — 使うにはテストベンチを 2008 モードでコンパイルしよう。)

## シミュレーションをきれいに終わらせる

波形だけのセッションは永遠に走り続ける。自己検証するテストベンチは、スティミュラスを使い切ったら **自分で止まる** べきだ。VHDL-2008 でのきれいなやり方は、`std.env` package の `finish` を使うことだ:

```vhdl
use std.env.all;
...
report "すべてのテストに合格";
finish;          -- シミュレーションを終了する;stop なら調査のために一時停止する
```

`finish` は実行を終了する(自動化/CI 用途に理想的)。`stop` はそれを一時停止し、対話的にあちこち覗けるようにする。古い手法である `assert … severity failure` を強制して止めるやり方もどの VHDL バージョンでも効くが、合格した実行でも *failure* を報告してしまい、リグレッションツールを混乱させる — `finish` のほうが好ましい。

## 指向テスト vs ランダムテスト

- **指向(directed)** テストは、特定の手で選んだケースを突く:リセットの振る舞い、カウンタの折り返し、わかっている厄介なシーケンスなど。バグを突き止めるのにも、仕様の受け入れケースにも最適だ。
- **ランダム / 制約付きランダム(constrained-random)** テストは、合法だが計画されていない入力を大量に DUT に投げつけ、手では思いつかないコーナーを捕まえる。結果を手で予測できないので、ゴールデンモデルに頼る。

実際の検証のほとんどは両方を混ぜる:重要だとわかっているケースには指向テスト、わからないケースにはランダムテストだ。

## なぜこれが重要か

- **リグレッション:** 昨日書いたテストベンチは、今日もまだ走る。昨日見つめた波形は、もう消えている。
- **カバレッジ:** 対話セッションでは決して到達しないスティミュラスパターン(ランダム入力、エッジケース、コーナーの周波数)を駆動できる。
- **自動化:** CI システムがコミットごとにテストベンチを実行し、終了コードから pass/fail を読み取る。波形は CI に収まらない。

## コースとのつながり

検証は、*Diseño de Sistemas Digitales*(デジタルシステム設計)の実験シーケンス全体を静かに支える背骨だ。すべての実習 — レジスタ、順序システム、カウンタ、メモリ、ASM マシン — は、実のところ「HDL で作り、それが動くことを証明する」ことだ。自己検証するテストベンチは、各 *reporte de práctica*(実習レポート)を「波形のスクリーンショットだ」から「合格するテストだ」へと変える方法であり、まさにこの単元の問題解決型の評価が求める厳密さそのものだ。

## Kmila の現在の限界

Kmila シミュレータは、テストベンチを普通の entity として書けば問題なく実行する — 今日欠けているのは、`assert` 文の pass/fail を Output ログへ自動的に抽出することだけだ。これはロードマップに載っている。

## 覚えておくべきこと

- テストベンチは、DUT をインスタンス化し、クロックを生成し、スティミュラスを駆動し、`assert` で **自分自身を確認する** ポートなしの entity だ。
- クロックとスティミュラスは別々のプロセスに分け、確認は `wait until rising_edge(clk)` で同期しよう。
- 期待値を 1 つ 1 つ手で書く代わりに、ゴールデンモデル(スコアボード)と比較しよう。
- 自動実行は `std.env.finish` で終わらせ、本物の失敗でシミュレータが停止できるよう severity レベルを使おう。
- 指向テスト(わかっているケース)とランダムテスト(思いつかないコーナー)を混ぜよう。
