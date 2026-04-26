# サンプル: 信号機 FSM

信号機は典型的なムーア FSM です: 3 状態 (`RED`、`GREEN`、`YELLOW`)、色ごとに 1 つの出力、そして「次に進む前に各状態を数サイクル保持する」と指示するタイマー。

## 構造

サンプルは **2 プロセスパターン** に従います:

- プロセス 1 は **状態レジスタ** — `state` を格納しホールドカウンタをチックする古典的な D フリップフロップ。
- プロセス 2 は **次状態ロジック** — 純粋に組合せ、現在の状態から `next_state` が何かを決める。
- 出力代入(アーキテクチャの下部)は組合せムーア出力 — 値は `state` のみに依存。

## コードを歩く

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 7 ticks per colour
```

`state_t` は **列挙型** — シンセサイザが実際のバイナリ符号化を選び、私たちは名前付き状態で参照するだけです。`count` は現在の状態にいた時間を追跡します。`HOLD` は滞在時間です。

状態レジスタ:

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    state <= S_RED;
    count <= (others => '0');
  elsif rising_edge(clk) then
    if count = HOLD then
      state <= next_state;
      count <= (others => '0');
    else
      count <= count + 1;
    end if;
  end if;
end process;
```

次状態ロジック:

```vhdl
process(state)
begin
  case state is
    when S_RED    => next_state <= S_GREEN;
    when S_GREEN  => next_state <= S_YELLOW;
    when S_YELLOW => next_state <= S_RED;
  end case;
end process;
```

出力 (純ムーア):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## サンプルが行うこと

クロックは約 400 µs の間動きます。8 クロックごと(7 ホールド + 1 遷移)に FSM は進みます。3 つの矩形波が順番に回転するのが見えるはずです: `red` が 8 ティックハイ、次に `green` が 8 ティック、次に `yellow`、そして `red` に戻る。

## 自分でやってみる

- **歩行者要求入力を追加する。** `ped = '1'` かつ状態が `S_GREEN` のとき、次状態を強制的に `S_YELLOW` にします(歩行者を渡らせるためにサイクルを短くする)。これでムーア機械がミーリっぽくなります — 出力 `yellow` が `state` **と** `ped` 入力の両方に依存するようになります。
- **`state = S_RED` のときだけハイになる `walk` 信号を出す。** これは新しいムーア出力です。
- **`HOLD` を generic としてパラメータ化する** — 7 はシミュレーション用には十分ですが、50 MHz の FPGA 上では 1 秒の保持を得るために数億サイクル必要です。
