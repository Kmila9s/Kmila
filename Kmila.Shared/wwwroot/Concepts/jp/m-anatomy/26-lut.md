# ルックアップテーブル (LUT)

**ルックアップテーブル (LUT)** は FPGA が任意のゲートになる仕組みです：真理値表を収めた小さなメモリ。入力はそこへの**アドレス**を作り、出力はそのアドレスに蓄えられたビットです。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/26-lut.html" title="インタラクティブな図" loading="lazy"></iframe></div>

入力は 4 つの構成セル（ロードされた真理値表）をアドレスし、出力は選ばれたビットです。構成が**再プログラム**される様子を見てください——同じ LUT が AND、次に OR、次に XOR になります。これが再構成可能な論理です。

VHDL では：

```vhdl
y <= a and b;   -- the synthesizer loads this
                -- truth table into ONE LUT
```

設計全体が LUT に写像されます：合成器は各ブール式を真理値表に変え、電源投入時に構成ビットストリームからロードします。N 入力 LUT は N 入力の**任意**の関数になれます。
