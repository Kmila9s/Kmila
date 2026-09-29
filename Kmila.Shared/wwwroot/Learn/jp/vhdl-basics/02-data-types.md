# データ型:`std_logic`、ベクトル、`unsigned`、`integer`

VHDL は型付き言語。ポートが `in std_logic` と宣言されているのを見たら、コンパイラはそれを別の型のものに配線させない。各信号に正しい型を選ぶことが、クリーンな VHDL を書く半分の仕事。

## `std_logic` — 万能線

**9 値**の IEEE ロジックの 1 ビット。そう、9 つ:

| 値     | 意味 |
|--------|------|
| `'0'`  | 強駆動 Low |
| `'1'`  | 強駆動 High |
| `'Z'`  | ハイインピーダンス(トライステートオフ) |
| `'L'`  | 弱 Low(プルダウン) |
| `'H'`  | 弱 High(プルアップ) |
| `'X'`  | 不明 / 複数ドライバが矛盾 |
| `'U'`  | 未初期化 — 信号が一度も代入されていない |
| `'W'`  | 弱不明 |
| `'-'`  | don't care(case 文で使う) |

合成では `'0'`、`'1'`、`'Z'` だけがハードウェアに到達する。他はシミュレーション診断 — 波形に `'U'` を見つけたら、書かれる前に読まれた信号がある。

## `std_logic_vector` — バス

`std_logic` の固定幅配列:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 ビット、インデックス 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 ビット、インデックス 0..15
```

`downto` は圧倒的に一般的な方向 — 数値の書き方(MSB が左)と合う。

リテラル:

```vhdl
data <= "10101010";     -- バイナリリテラル、1 文字 1 ビット
addr <= x"CAFE";        -- 16 進リテラル(4 ビットの倍数)
data <= (others => '0');-- 全ゼロの短縮形
data <= (0 => '1', others => '0'); -- LSB だけ High
```

## `unsigned` / `signed` — 算術できるベクトル

`std_logic_vector` は「ビットの袋」にすぎない。数値的意味を持たない。加算や減算には `unsigned` か `signed` が必要:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- 動く
```

相互変換:

```vhdl
-- ベクトル → 算術用 unsigned
u <= unsigned(slv);

-- unsigned → 外向けのベクトル
slv <= std_logic_vector(u);

-- unsigned → インデックスや定数用 integer
idx <= to_integer(u);

-- integer → unsigned(幅を指定)
u <= to_unsigned(42, 8);
```

経験則:**ポート**には `std_logic_vector` を使い(対象が最も広い)、実際に算術するアーキテクチャ内部で `unsigned` / `signed` に変換する。

## `integer` — インデックスと計数

ループカウンタ、定数、ポート境界を越えないものには `integer`(または制約付きサブタイプ)を使う:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

無制約の `integer` はシミュレーションでは通常 32 ビット。ハードウェアには制約付きの範囲が欲しい:

```vhdl
signal counter : integer range 0 to 255;
```

そうするとシンセサイザは正確に 8 ビットを与える。

## 列挙型 — FSM 状態用

ステートマシンに読みやすい型を宣言:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

`state <= S_ACTIVE` と書ける、`state <= "010"` ではなく。シンセサイザがバイナリエンコーディングを選ぶ(気にするなら属性で示唆できる)。

## 覚えておくべきこと

- 単線 → `std_logic`。バス → `std_logic_vector`。
- 算術が必要?`unsigned`(または `signed`)と `IEEE.NUMERIC_STD` を使う。
- **ポート**を `std_logic_vector` で宣言すると entity の移植性が最大になる。
- FSM 状態には列挙型を使う — コードが仕様書のように読める。
