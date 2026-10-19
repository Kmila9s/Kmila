# ALU — 算術論理演算装置

**ALU**（算術論理演算装置）は CPU の心臓にある計算機です：2 つのオペランド A と B を取り、**演算選択**に応じて A+B、A−B、A AND B、A OR B などを作ります。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/17-alu.html" title="演算選択ドラム付きのインタラクティブな ALU" loading="lazy"></iframe></div>

モジュール上の選択ドラムが演算を選び、オペランドのプランジャが **A** と **B** を設定します。リストが現在の演算を強調し、表示は **A op B = R** を示します。純粋な組合せ論理で、クロックはありません。

VHDL では：

```vhdl
with op select
  r <= a + b     when "00",   -- ADD
       a - b     when "01",   -- SUB
       a and b   when "10",   -- AND
       a or b    when others; -- OR
```

ALU は**組合せ論理**（加算器、各ゲート、結果を選ぶ MUX）で、それ自体に記憶はありません——周囲のレジスタがオペランドと結果を保持します。演算選択は候補結果に対する MUX にすぎません。
