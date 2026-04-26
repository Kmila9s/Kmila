# テストベンチのマインドセット

ここまでのすべてのレッスンで、Kmila があなたのテストベンチでした — 刺激を Ports タブにドラッグし、Run で設計を実行し、波形を目で見ました。学習にはそれでいいのです。プロフェッショナルなデジタル設計の働き方ではありません。

## 実際のテストベンチとは

**テストベンチ** は、*被試験設計* (DUT: Design Under Test) を動かすことを唯一の仕事とする別の VHDL エンティティです。何も駆動していないのでポートがなく、DUT をコンポーネントとして宣言し、自身の内部信号を DUT のポートに接続し、そして **プロセス** を使って時間の中で刺激シーケンスを歩きます。

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
            report "Counter should be 10 at 200 ns, got " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

3 つの新しいアイデア:

1. **`wait`** — プロセスが指定するときだけ時間が進む。
2. **`assert … report … severity`** — セルフチェック。1000 行の波形を「合格」または「不合格」に変える。
3. **ポートゼロのエンティティ** — テストベンチは自己完結; シミュレータが直接インスタンス化する。

## なぜこれが重要か

- **リグレッション**: 昨日書いたテストベンチは今日もまだ動く。昨日眺めた波形は消えている。
- **カバレッジ**: 対話的セッションでは到達できない刺激パターンを駆動できる(ランダム入力、エッジケース、コーナー周波数)。
- **自動化**: CI システムはコミットごとにテストベンチを実行する。波形は CI に収まらない。

## Kmila の現在の制限

Kmila シミュレータは、通常のエンティティとしてテストベンチを書けば問題なく実行します — 今日欠けているのは `assert` 文からの自動的な合否抽出を Output ログに出すことだけです。これはロードマップ上にあります。

*さらにレッスンが来ます — このモジュールは現在執筆中です。*
