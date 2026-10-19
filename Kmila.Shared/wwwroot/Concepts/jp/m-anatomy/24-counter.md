# カウンタ

**カウンタ**は各クロックエッジで記憶値に 1 を足し、0, 1, 2, … と進み折り返します。+1 加算器を帰還させたレジスタです。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/24-counter.html" title="クロック駆動のインタラクティブな多ビット順序ブロック" loading="lazy"></iframe></div>

左でフリーランのクロックが刻みます。4 ビットの状態は **Q ランプ**の列と、ライブ値（十進＋二進）の両方で表示されます。各クロックエッジで状態が更新され、エッジ間では保持されます。

VHDL では：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q + 1;   -- increment every edge
  end if;
end process;
```

カウンタは時間やイベントを測り、アドレスを生成し、クロックを分周します。最下位ビットが毎エッジ、上位ビットが半分の速さで反転するのを見てください——動く二進計数です。
