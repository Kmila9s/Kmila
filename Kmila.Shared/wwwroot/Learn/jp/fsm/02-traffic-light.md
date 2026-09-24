# サンプル:信号機 FSM

信号機は教科書的な Moore FSM:3 つの状態(`RED`、`GREEN`、`YELLOW`)、色ごとに 1 出力、そして「各状態を数サイクル保持してから進む」と言うタイマー。

## 構造

サンプルは **2 プロセスパターン** に従う:

- プロセス 1 は**状態レジスタ** — `state` を保存し、保持カウンタを刻む古典的な D フリップフロップ。
- プロセス 2 は**次状態論理** — 純粋に組合せ、現在の状態から `next_state` を決める。
- 出力代入(アーキテクチャの末尾)は組合せの Moore 出力 — 値は `state` だけに依存。

## コードを追う

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 色ごとに 7 ティック
```

`state_t` は **列挙型** — 実際の 2 進エンコードはシンセサイザが選び、私たちは名前付き状態を参照するだけ。`count` は現在状態にいる時間を追跡。`HOLD` は滞在時間。

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

次状態論理:

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

出力(純粋 Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

## サンプルが行うこと

クロックは約 400 µs 走る。8 クロックごと(7 保持 + 1 遷移)に FSM が進む。3 つの矩形波が順番に回るのが見える:`red` が 8 ティック High、次に `green` が 8 ティック、次に `yellow`、そして `red` に戻る。

## あなたの番

- **歩行者要求入力を追加**。`ped = '1'` かつ状態が `S_GREEN` のとき、`next_state <= S_YELLOW` を強制(歩行者が渡れるようサイクルを短縮)。これで Moore マシンは Mealy 寄りになる — 出力 `yellow` が `state` **と** `ped` の両方に依存するようになる。
- **`walk` 信号を出す**。`state = S_RED` のときだけ High。新しい Moore 出力。
- **`HOLD` を generic にする** — シミュレーションには 7 で十分だが、50 MHz の FPGA で 1 秒保持するなら数億サイクル必要。
