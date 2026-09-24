# プロジェクト:UART 送信器

UART(Universal Asynchronous Receiver / Transmitter)は、ほとんどの組込み設計者が最初に作る本物のプロトコル。ポイントツーポイント、全二重、必要な線は 3 本だけ(TX、RX、GND)。あらゆる FTDI の USB シリアル変換器、Arduino の `Serial.print`、古いターミナル — どれも底は UART。

このプロジェクトは**送信**側を作る。受信側はこれを終えたあとの素晴らしい次プロジェクト。

## フレーム形式(パリティなし、8 ビットデータ、1 ストップ)

```
  Idle  Start   Bit0  Bit1  Bit2  Bit3  Bit4  Bit5  Bit6  Bit7   Stop   Idle
  ────┐      ┌───── ... ─────────────────────────────── ... ─────┐──────
      │      │
      └──────┘
    線は通常 High ('1');スタートビットは立ち下がりエッジ。
```

各ビットは `tx` 上で正確に `clk_freq / baud` クロックサイクルだけ保持される。50 MHz クロックで 115 200 ボーなら、ビットあたり約 434 サイクル。

## あなたの仕事

1. 約 434 サイクルごとに 1 サイクル幅の `baud_tick` を出すクロックカウンタを作る。
2. `send = '1'` で `data` を 10 ビットシフトレジスタにラッチ:`{ stop_bit, data[7..0], start_bit } = 1 & data & 0`。`busy <= '1'` を駆動。
3. 各 `baud_tick` でレジスタを右シフト(スタートビットが先、次にデータの LSB から MSB、最後にストップビット)。
4. 10 番目の tick の後、`busy` を下げ、`tx` を idle High にする。

## 雛形

entity と idle 状態は提供済み;`tx` はデフォルト High、`busy` はデフォルト Low。それらのデフォルトの間のすべてはあなたの責任。

```vhdl
architecture rtl of uart_tx is
  signal tx_reg : std_logic := '1';
begin
  tx   <= tx_reg;
  busy <= '0';       -- TODO
end architecture rtl;
```

## 推奨構造

カウンタ 2 つ + シフトレジスタ 1 つ:

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
  - `clk_count = BAUD_PERIOD - 1` のとき:
    - `tx_reg <= shift_reg(0);`
    - `shift_reg <= '1' & shift_reg(9 downto 1);` — 右シフト、上を idle で詰める
    - `bit_count <= bit_count + 1;`
    - `clk_count <= 0;`
  - `bit_count = 10` のとき:
    - `sending <= '0';`

`BAUD_PERIOD` を generic にすれば、シミュレーション用に小さくし(例:5 サイクル/ビット)、同じロジックがハードウェアではフルスピードで動く。

## テストすべきこと

`data = "01001000"`(ASCII の `'H'`)で `send = '1'` を駆動。フレーム完了後、`tx` 波形には正確に 10 ビット周期の活動が現れるはず:

1. スタート(Low)
2. データのビット 0(LSB:`0`)
3. `0`
4. `0`
5. `1`
6. `0`
7. `0`
8. `1`
9. `0`(MSB)
10. ストップ(High)

その後、idle High に戻る。Waveforms タブで拡大し、ビット単位でパターンを確認する。

## さらに進めるなら

- **FIFO** を追加すれば複数バイトをキューできる;`send` は「push」になる。
- **RX** 側を作る:最初のビットを 1.5 × ビット周期でサンプル、以後は 1 × でサンプル。
- フレームをパラメータ化:パリティ、7 ビット対 8 ビットデータ、ストップビット 1 対 2。
