# シフトレジスタ

**シフトレジスタ**は各クロックエッジでビットを 1 つずつ送り、一端から新しいビットが入ります——データのバケツリレーです。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/23-shift.html" title="クロック駆動のインタラクティブな多ビット順序ブロック" loading="lazy"></iframe></div>

左でフリーランのクロックが刻みます。4 ビットの状態は **Q ランプ**の列と、ライブ値（十進＋二進）の両方で表示されます。各クロックエッジで状態が更新され、エッジ間では保持されます。

VHDL では：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    q <= q(2 downto 0) & si;  -- shift left, SI in
  end if;
end process;
```

シフトレジスタは直列↔並列変換（UART 受信）、2 倍/2 分の 1、擬似乱数列用の LFSR を作ります。
