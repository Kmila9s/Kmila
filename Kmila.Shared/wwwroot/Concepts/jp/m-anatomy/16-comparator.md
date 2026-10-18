# 比較器

**比較器**は 2 つの数 A と B を比べ、どの場合かを示します：A < B、A = B、A > B。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/16-comparator.html" title="モジュール台の上のインタラクティブな組合せ回路" loading="lazy"></iframe></div>

カーソル（または矢印キー）を動かして、すべての入力の組み合わせを順に試します。入力プランジャがビットを設定し、モジュールが計算し、出力ランプが点灯し、**表の現在行がハイライト**されるので、関数をそのまま読み取れます。

VHDL では：

```vhdl
lt <= '1' when a < b else '0';
eq <= '1' when a = b else '0';
gt <= '1' when a > b else '0';
```

これは純粋な**組合せ**論理です。出力は現在の入力だけで決まり、クロックもメモリもありません。 3 つの出力のうちちょうど一つが 1 です。大小比較器はビットごとに、最上位ビットから下へ拡張します。
