# プロジェクト: UART 送信機

UART (Universal Asynchronous Receiver / Transmitter) は、ほとんどの組込み設計者が作る最初の本格的なプロトコルです。ポイントツーポイント、全二重、必要なワイヤは 3 本だけ(TX、RX、GND)。すべての FTDI USB シリアルアダプタ、すべての Arduino の `Serial.print`、すべてのレガシー端末は UART が下敷きです。

このプロジェクトは **送信** 側を構築します。受信側はこれを完成させた後の優れた次のプロジェクトです。

## フレームフォーマット (パリティなし、8 ビットデータ、ストップ 1)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    line is normally high ('1'); start bit is a falling edge.
```

各ビットは `tx` 上で正確に `clk_freq / baud` クロックサイクル保持されます。50 MHz クロックと 115,200 baud の場合、約 434 サイクル/ビットです。

## あなたの仕事

1. 約 434 サイクルごとに 1 サイクルの `baud_tick` を生成するクロックカウンタを構築する。
2. `send = '1'` で `data` を 10 ビットのシフトレジスタにラッチする: `{ stop_bit, data[7..0], start_bit } = 1 & data & 0`。`busy <= '1'` を駆動する。
3. 各 `baud_tick` でレジスタを右シフトする(そうするとスタートビットが最初に出て行き、次にデータの LSB から MSB、そしてストップビット)。
4. 10 番目のティック後、`busy` を落として `tx` をハイにアイドル化する。

## スキャフォールド

エンティティとアイドル状態が提供されます; `tx` はデフォルトでハイ、`busy` はデフォルトでロー。それらのデフォルトの間のすべてはあなたの責任です。

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## 推奨構造

2 つのカウンタ + 1 つのシフトレジスタ:

```vhdl
signal clk_count  : unsigned(9 downto 0);   -- 0..434
signal bit_count  : unsigned(3 downto 0);   -- 0..9
signal shift_reg  : std_logic_vector(9 downto 0);
signal sending    : std_logic := '0';
```

- `send = '1'` かつ `sending = '0'` のとき:
  - `shift_reg <= '1' & data & '0';` をロード
  - `sending <= '1';  clk_count <= 0;  bit_count <= 0;`
- `sending = '1'` の間:
  - `clk_count <= clk_count + 1;`
  - `clk_count = BAUD_PERIOD - 1` なら:
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — 右シフト、上部はアイドルでパッド
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - `bit_count = 10` なら:
    - `sending <= '0';`

`BAUD_PERIOD` を generic にしておけば、シミュレーション用に縮小(例: 5 サイクル/ビット)しても、ハードウェアではフル速度で同じロジックが動きます。

## 何をテストするか

`data = "01001000"` (ASCII `'H'`) で `send = '1'` を駆動します。フレーム完了後、`tx` 波形は正確に 10 ビット周期のアクティビティを示すはずです:

1. スタート (ロー)
2. データのビット 0 (LSB: `0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0` (MSB)
10. ストップ (ハイ)

その後アイドルのハイに戻ります。Waveforms タブでズームインし、ビットごとにパターンを検証してください。

## さらに進める

- **FIFO** を追加して複数バイトをキューイングできるようにする; `send` は「push」になる。
- **RX** 側を作る: 各着信ビットを 1.5 × ビット周期でサンプルし、それ以降は 1 × でサンプルする。
- フレームをパラメータ化する: パリティ、7 ビット vs 8 ビットデータ、1 vs 2 ストップビット。
