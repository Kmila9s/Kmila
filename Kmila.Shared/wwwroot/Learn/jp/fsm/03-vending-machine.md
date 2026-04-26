# プロジェクト: コイン式自動販売機

これが最初の本格的なプロジェクトです。仕様は固定、スタータコードは配線部分をあなたに渡します、そしてあなたの仕事はそれを完成させるロジックを書くことです。

## 作るもの

顧客が投入したお金を追跡し、合計が 25¢ に達すると商品を出す(1 パルスの出力 `dispense`)FSM。入力:

- `nickel` — 「5¢ コインがちょうど投入された」を意味する 1 サイクルパルス
- `dime`   — 「10¢ コインがちょうど投入された」を意味する 1 サイクルパルス
- `clk`、`reset`

そして出力:

- `dispense` — 25¢ 以上に達したとき、正確に 1 クロックサイクルの間ハイにパルス
- `change`   — 合計が 25¢ **超** のときに `dispense` と並行して 1 サイクルハイになるパルス(つまり、機械がお金を返す必要がある)

## 受け入れ基準

| コインの投入順序     | 期待される動作                 |
|----------------------|--------------------------------|
| ニッケル 5 枚        | `dispense=1, change=0`         |
| ダイム 2 枚 + ニッケル 1 枚 | `dispense=1, change=0`    |
| ダイム 3 枚 (= 30¢)  | `dispense=1, change=1`         |
| ニッケル 2 枚 + ダイム 1 枚 | 出金なし (20¢ のみ)          |

## スターターが提供するもの

エンティティと `total` アキュムレータは配線されています。`nickel` パルスごとに 5 を加え、`dime` パルスごとに 10 を加えます。しかしスターターは常に `dispense = '0'` と `change = '0'` を駆動しています。**これを変更する必要があります。**

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

1. **販売条件を検出する。** 各クロック立ち上がりエッジで、現在のコインを加算した後 `total >= 25` かどうかをチェックします。(注意: 前のサイクルのものではなく *新しい* 合計を計算する必要があります。)
2. **`dispense` を正確に 1 サイクルパルスさせる。** トリック: 販売を検出したとき、フラグ信号を設定し `total` をゼロに戻します。フラグは 1 サイクル生きます。
3. **`change` を計算する。** 販売の瞬間に合計が 25 を超えていた場合、`change` もパルスします。

ヒント: 「次の合計」をローカル信号(またはプロセス内の `variable`)として計算する方が簡単です:

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

## さらに進める

- 出金せずに `total` をゼロにする **返金** ボタンを追加する。
- **クォーター** (25¢) コインをサポートする — 即座に出金すべき。
- 出力として見える別の 8 ビットカウンタで販売アイテム数を追跡する。これは実際の自動販売機が上流に送り返すフィードバックの種類です。
