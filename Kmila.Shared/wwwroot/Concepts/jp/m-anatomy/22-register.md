# レジスタ

**レジスタ**はフリップフロップの束で、語全体を一度に記憶します。クロックエッジで（ロード時）D の全ビットがまとめて Q に取り込まれます。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/22-register.html" title="クロック駆動のインタラクティブな多ビット順序ブロック" loading="lazy"></iframe></div>

左でフリーランのクロックが刻みます。4 ビットの状態は **Q ランプ**の列と、ライブ値（十進＋二進）の両方で表示されます。各クロックエッジで状態が更新され、エッジ間では保持されます。

VHDL では：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if ld = '1' then q <= d; end if;  -- load on edge
  end if;
end process;
```

CPU は作業値をレジスタに置き、どのパイプライン段もレジスタです。前に MUX（新値 vs. 現在の Q）を置けば、ロードイネーブルは保持になります。
