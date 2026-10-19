# T フリップフロップ

**T フリップフロップは反転**します：各エッジで T = 1 なら記憶ビットが反転（0↔1）、T = 0 なら保持。クロック周波数を 2 分周します。 これはラダー最初の**順序**素子です：ゲートと違い、その出力は現在の入力だけでなく、クロックと自身の過去に依存します。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/20-t-ff.html" title="ライブなタイミング図付きのインタラクティブな順序素子" loading="lazy"></iframe></div>

この素子は**記憶**を持ちます。タイミング図を見てください：上でクロック（**CLK**）が刻み、出力 **Q** はクロックのときだけ変化し——その間は入力が何をしようと値を**保持**します。Q は色で描かれ、破線がクロックエッジを示します。

VHDL では：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if t = '1' then q <= not q; end if;
  end if;
end process;
```

T フリップフロップを連ねると各段がさらに 2 分周——まさに二進**リップルカウンタ**です。T は D を Q xor T につないだ DFF です。
