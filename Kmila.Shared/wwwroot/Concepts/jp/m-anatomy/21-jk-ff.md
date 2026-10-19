# JK フリップフロップ

**JK フリップフロップ**は最も汎用的です：各エッジで J は Q を 1 に、K は 0 にし、両方同時なら**反転**、どちらも 0 なら保持します。 これはラダー最初の**順序**素子です：ゲートと違い、その出力は現在の入力だけでなく、クロックと自身の過去に依存します。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/21-jk-ff.html" title="ライブなタイミング図付きのインタラクティブな順序素子" loading="lazy"></iframe></div>

この素子は**記憶**を持ちます。タイミング図を見てください：上でクロック（**CLK**）が刻み、出力 **Q** はクロックのときだけ変化し——その間は入力が何をしようと値を**保持**します。Q は色で描かれ、破線がクロックエッジを示します。

VHDL では：

```vhdl
process (clk)
begin
  if rising_edge(clk) then
    if    j='1' and k='1' then q <= not q;
    elsif j='1' then q <= '1';
    elsif k='1' then q <= '0';
    end if;
  end if;
end process;
```

JK は古い SR ラッチの禁止状態を除きます。どのフリップフロップも他から作れ、D・T・JK は前に少し論理を足せば置き換え可能です。
