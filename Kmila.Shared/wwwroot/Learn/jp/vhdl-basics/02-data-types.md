# データ型: `std_logic`、ベクタ、`unsigned`、`integer`

VHDL は型付き言語です。ポートが `in std_logic` で宣言されているのを見たら、コンパイラはそれを異なる型のものに接続させません。各信号に適切な型を選ぶことは、きれいな VHDL を書くことの半分です。

## `std_logic` — ユニバーサルなワイヤ

**9 値** IEEE ロジックの 1 ビット。そう、9 つです:

| 値    | 意味 |
|-------|------|
| `'0'` | 強い駆動ロー |
| `'1'` | 強い駆動ハイ |
| `'Z'` | 高インピーダンス(トライステートオフ) |
| `'L'` | 弱いロー(プルダウン) |
| `'H'` | 弱いハイ(プルアップ) |
| `'X'` | 不定 / 複数のドライバが不一致 |
| `'U'` | 未初期化 — 信号が代入されなかった |
| `'W'` | 弱い不定 |
| `'-'` | don't care (case 文で使用) |

合成では `'0'`、`'1'`、`'Z'` だけがハードウェアに届きます。他はシミュレーションの診断です — 波形で `'U'` が見えたら、書き込まれる前に読まれている信号があります。

## `std_logic_vector` — バス

`std_logic` の固定幅配列:

```vhdl
signal data : std_logic_vector(7 downto 0);  -- 8 bits, indexes 7..0
signal addr : std_logic_vector(0 to 15);     -- 16 bits, indexes 0..15
```

`downto` が圧倒的によく使われる方向です — これは数字の書き方(左に MSB)と一致します。

リテラル:

```vhdl
data <= "10101010";     -- binary literal, one char per bit
addr <= x"CAFE";        -- hex literal (must be multiple of 4 bits)
data <= (others => '0');-- shorthand for all-zeros
data <= (0 => '1', others => '0'); -- only the LSB high
```

## `unsigned` / `signed` — 計算できるベクタ

`std_logic_vector` は「ただのビットの袋」です。数値的意味を持ちません。加減算するには `unsigned` または `signed` が必要です:

```vhdl
use IEEE.NUMERIC_STD.ALL;

signal a, b, sum : unsigned(7 downto 0);
...
sum <= a + b;   -- works
```

それらの間の移動:

```vhdl
-- vector → unsigned for math
u <= unsigned(slv);

-- unsigned → vector for the outside world
slv <= std_logic_vector(u);

-- unsigned → integer for indexing or constants
idx <= to_integer(u);

-- integer → unsigned (specify width)
u <= to_unsigned(42, 8);
```

経験則: **ポート** では `std_logic_vector` を使い(最も広い対象)、実際に算術を行うアーキテクチャ内部では `unsigned` / `signed` に変換しましょう。

## `integer` — インデックスとカウント用

ループカウンタ、定数、およびポート境界を越えないものには `integer` (または制約付きサブタイプ) を使います:

```vhdl
constant DEPTH : integer := 32;
for i in 0 to DEPTH - 1 loop ...
```

無制限の `integer` はシミュレーションでは通常 32 ビットです。ハードウェアには制約付き範囲が欲しいです:

```vhdl
signal counter : integer range 0 to 255;
```

するとシンセサイザはちょうど 8 ビットを与えます。

## 列挙型 — FSM 状態用

状態機械のために読みやすい型を宣言します:

```vhdl
type state_t is (S_IDLE, S_START, S_ACTIVE, S_DONE);
signal state : state_t;
```

すると `state <= "010"` ではなく `state <= S_ACTIVE` と書けます。シンセサイザがバイナリ符号化を選んでくれます(気にするなら属性でヒントできます)。

## 覚えておくべきこと

- 単一ワイヤ → `std_logic`。バス → `std_logic_vector`。
- 算術が必要? `unsigned` (または `signed`) と `IEEE.NUMERIC_STD` を使う。
- **ポート** を `std_logic_vector` で宣言するとエンティティの移植性が最大になる。
- FSM 状態には列挙型を使う — コードが仕様のように読める。
