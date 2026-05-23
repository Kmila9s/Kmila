# テストベンチ思考

ここまでのすべてのレッスンで、Kmila があなたのテストベンチだった — Ports タブにスティミュラスをドラッグし、Run で設計を実行し、波形を目視した。学習中はそれで構わない。プロのデジタル設計はそうやらない。

## 本物のテストベンチとは

**テストベンチ**とは、*テスト対象設計*(DUT)を駆動するためだけに存在する別の VHDL entity。誰も駆動しないのでポートを持たない;DUT を component として宣言し、自身の内部信号を DUT のポートに配線し、その後 **process** で時間に沿ってスティミュラス列を流す。

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
            report "200 ns 時点でカウンタは 10 のはず、実際は " & to_string(q)
            severity error;
        wait;
    end process;
end architecture sim;
```

3 つの新しい概念:

1. **`wait`** — process が言ったときだけ時間が進む。
2. **`assert … report … severity`** — 自己検証。1000 行の波形を「pass」か「fail」に縮約。
3. **ポートなしの entity** — テストベンチは自己完結;シミュレータが直接インスタンス化する。

## なぜ重要か

- **リグレッション**:昨日書いたテストベンチは今日も動く。昨日見つめた波形はもうない。
- **カバレッジ**:対話セッションでは到達しないスティミュラスパターンを流せる(ランダム入力、エッジケース、コーナー周波数)。
- **自動化**:CI システムがコミットごとにテストベンチを実行。波形は CI に収まらない。

## Kmila の現在の限界

Kmila シミュレータは、テストベンチを普通の entity として書けば問題なく実行する — 今日欠けているのは、`assert` 文の pass/fail を自動的に Output ログに抽出することだけ。これはロードマップに載っている。

*さらにレッスンが来る — このモジュールは執筆中。*
