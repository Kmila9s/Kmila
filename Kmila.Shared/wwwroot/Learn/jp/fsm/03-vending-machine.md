# プロジェクト:コイン投入式自動販売機

これがあなたの最初の本格プロジェクト。仕様は固定で、スタータコードが配管を渡してくれる。あなたの仕事はそれを完成させるロジックを書くこと。

## 何を作るか

顧客が投入した金額を追跡し、合計が 25¢ に達したら商品を出す(1 つの出力パルス `dispense`)FSM。入力:

- `nickel` — 1 サイクルパルス:「5¢ 硬貨が今落ちた」
- `dime` — 1 サイクルパルス:「10¢ 硬貨が今落ちた」
- `clk`、`reset`

出力:

- `dispense` — 合計が ≥ 25¢ になった瞬間、ちょうど 1 クロック分 High パルス
- `change` — 合計が 25¢ **より多い** 場合、`dispense` と同時に 1 サイクル High パルス(おつりを返す必要があるから)

## 受け入れ基準

| 投入シーケンス       | 期待動作                      |
|---------------------|-------------------------------|
| nickel 5 枚         | `dispense=1, change=0`        |
| dime 2 + nickel 1   | `dispense=1, change=0`        |
| dime 3 (= 30¢)      | `dispense=1, change=1`        |
| nickel 2 + dime 1   | dispense なし(20¢ のみ)     |

## スタータが与えるもの

entity と `total` アキュムレータは配線済み。`nickel` パルスごとに 5、`dime` パルスごとに 10 が加算される。しかしスタータは常に `dispense = '0'` と `change = '0'` を駆動する。**ここを変えるのがあなたの仕事。**

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    total <= (others => '0');
  elsif rising_edge(clk) then
    if    nickel = '1' then total <= total + 5;
    elsif dime   = '1' then total <= total + 10;
    end if;
  end if;
end process;

dispense <= '0';  -- TODO
change   <= '0';  -- TODO
```

## 推奨アプローチ

1. **販売条件を検出**。立ち上がりエッジごとに、現在の硬貨を加算した *後* に `total >= 25` か確認。(注意:前サイクルの値ではなく、*新しい* 合計を計算する。)
2. **`dispense` を厳密に 1 サイクル パルス**。コツ:販売を検出したらフラグ信号を立て、`total` をゼロに戻す。フラグは 1 サイクルだけ生きる。
3. **`change` を計算**。販売瞬間の合計が > 25 なら、`change` も同じくパルスする。

ヒント:「次の合計」をローカル signal(またはプロセス内の `variable`)で計算すると楽:

```vhdl
variable next_total : unsigned(5 downto 0);
...
next_total := total;
if    nickel = '1' then next_total := total + 5;
elsif dime   = '1' then next_total := total + 10;
end if;

if next_total >= 25 then
  dispense <= '1';
  change   <= '1' when next_total > 25 else '0';
  total    <= (others => '0');
else
  dispense <= '0';
  change   <= '0';
  total    <= next_total;
end if;
```

## さらに進めるなら

- **払い戻し**ボタンを追加して、商品を出さずに `total` をゼロにする。
- **quarter**(25¢)硬貨を対応 — 即座に商品を出すべき。
- 売れた商品数を別の 8 ビットカウンタで追跡し、出力として可視化。これが実際の自動販売機が上流に送るフィードバック。
