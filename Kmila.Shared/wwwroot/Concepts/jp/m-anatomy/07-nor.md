# NORゲート

**NOR** ゲートは OR のあとに NOT を付けたものです。両方の入力が 0 のときだけ 1 を出力します。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/07-nor.html" title="モジュール台の上のインタラクティブな論理ゲート" loading="lazy"></iframe></div>

カーソル（または矢印キー）を動かして、すべての入力の組み合わせを順に試します。モジュールが結果を計算し、出力ランプ **Y** が真理値表どおりに点灯します。

VHDL では：

```vhdl
y <= a nor b;
```

これは純粋な**組合せ**論理です。出力は現在の入力だけで決まり、クロックもメモリもありません。 バブルが反転を表します。**NOR も万能**です。あらゆる回路を NOR だけで作れます。
