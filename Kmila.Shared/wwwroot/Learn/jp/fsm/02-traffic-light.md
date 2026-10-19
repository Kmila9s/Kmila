# サンプル:信号機 FSM

信号機は典型的な Moore FSM だ:3 つの状態(`RED`、`GREEN`、`YELLOW`)、色ごとに 1 つの出力、そして「次へ進む前に各状態を数サイクル保持する」と告げるタイマーがある。

## 構造

このサンプルは **2 プロセスパターン** に従っている:

- プロセス 1 は **状態レジスタ** — `state` を保存し、保持カウンタを刻む古典的な D フリップフロップだ。
- プロセス 2 は **次状態論理** — 純粋な組合せ論理で、現在の状態から `next_state` が何になるかを決める。
- 出力代入(アーキテクチャの末尾)は組合せの Moore 出力だ — その値は `state` だけに依存する。

## コードを追う

```vhdl
type state_t is (S_RED, S_GREEN, S_YELLOW);
signal state, next_state : state_t;
signal count : unsigned(3 downto 0) := (others => '0');
constant HOLD : unsigned(3 downto 0) := "0111";  -- 色ごとに 7 ティック
```

`state_t` は **列挙型** だ — 実際の 2 進エンコードはシンセサイザが選び、私たちは名前付きの状態を参照するだけだ。`count` は現在の状態にどれだけ留まっているかを追跡する。`HOLD` は滞在時間だ。

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

名前を挙げておく価値のある細部が 2 つある。まず、これは **非同期リセット** だ(`reset` が感度リストにあり、`rising_edge` より前に判定される) — リセットが立った瞬間に信号機は `S_RED` にパチッと戻る。同期リセットなら `reset` を感度リストから外し、`rising_edge` の分岐の中でだけ判定する。どちらも正しいので、スタイルを 1 つ選んで一貫させよう。次に、このカウンタは 1 回の状態遷移を 8 サイクルの滞在に変えている — 状態は `count = HOLD` のサイクルでしか進まない。

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

このプロセスはすべての分岐で `next_state` を代入し、`case` は `state_t` を網羅しているので、ここに推論ラッチは生まれない。感度リストは `(state)` だけ — これで正しいのは、`next_state` が他の何にも依存しないからだ。次状態を入力に依存させた瞬間、その入力を感度リストに加えなければならない(さもないと組合せプロセスのシミュレーションが間違う)。そして保険として、1 行目に `next_state <= state;` のデフォルトを置くべきだ。

出力(純粋な Moore):

```vhdl
red    <= '1' when state = S_RED    else '0';
green  <= '1' when state = S_GREEN  else '0';
yellow <= '1' when state = S_YELLOW else '0';
```

各出力は `state` だけの関数だ — だからレジスタが刻むときにしか変わらず、エッジの合間にグリッチすることがない。物理的な信号を駆動するものにとって、これこそが Moore マシンの魅力のすべてだ。

## サンプルが行うこと

クロックはおよそ 400 µs 走る。8 クロックごと(7 保持 + 1 遷移)に FSM が進む。3 つの矩形波が順番に回るのが見える:`red` が 8 ティック High になり、次に `green` が 8 ティック、次に `yellow`、そして `red` に戻る。

## シミュレーションから本物の交差点へ

`HOLD = 7` はシミュレーション上の便宜だ。50 MHz の FPGA では 1 ティックが 20 ns なので、7 ティックは 140 ns — 信号機ではなくストロボだ。本物の 1 秒の滞在にするには 50,000,000 まで数える必要があり、つまり `count` と `HOLD` 定数を広げなければならない。きれいなやり方は **generic** を使うことだ:

```vhdl
generic ( HOLD : natural := 7 );
...
signal count : unsigned(31 downto 0);
```

小さな `HOLD` でシミュレーションし、大きな `HOLD` で合成する — 同じ RTL で、編集は不要だ。本物の交差点では、相反する青どうしの間に **全赤の安全状態** も加える。これを 1 つ加えるのは、Moore マシンのグリッチフリー保証を壊さずに拡張する良い練習になる。

## あなたの番

- **歩行者ボタン入力を追加する。** `ped = '1'` かつ状態が `S_GREEN` のとき、次状態を `S_YELLOW` に強制する(歩行者が渡れるようサイクルを短縮)。これで Moore マシンは Mealy 寄りになる — 次状態が `state` **と** `ped` 入力の両方に依存するようになるので、`ped` を感度リストに加え、デフォルト代入を入れよう。
- **`walk` 信号を出す。** `state = S_RED` のときだけ High になる信号だ。これは新しい Moore 出力だ。
- **`HOLD` を generic でパラメータ化する。** シミュレーションには 7 で十分だが、50 MHz の FPGA で 1 秒保持したいなら 5000 万サイクルが必要だ。

## コースとのつながり

このサンプルは単元 II の *aplicaciones con autómatas y lenguaje de descripción de hardware*(オートマトンと HDL の応用)の縮図だ:決定的な Moore オートマトンを 3 状態の図として描き、それを HDL で記述する。各状態をタイマーで滞在に引き伸ばすカウンタは、状態機械の中に住む単元 III(カウンタ)の最初の味見でもある。

## 覚えておくべきこと

- 信号機は教科書的な Moore FSM だ:出力は `state` だけに依存するので、グリッチフリーだ。
- 2 プロセスの分割(クロック付きレジスタ + 組合せ次状態)が、設計を読みやすくラッチフリーに保つ。
- 状態レジスタの中のカウンタが 1 回の遷移をタイマー付きの滞在に変える。滞在時間を generic でパラメータ化すれば、1 つの RTL がシミュレーションとシリコンの両方に使える。
- 入力が次状態に影響を与えた瞬間、それを感度リストに加え、`next_state` にデフォルトを代入しよう。
