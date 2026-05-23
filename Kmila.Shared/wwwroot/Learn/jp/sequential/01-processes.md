# プロセスと感度リスト

VHDL で process の外にあるものはすべて並行 — 線とゲート。回路に 1 クロックサイクルから次のサイクルへ**何かを覚えて**おきたい瞬間、process が必要。

## process とは

`process` は VHDL アーキテクチャ内の順次コードのブロック。その行はソフトウェアと同様、上から下に読まれる。しかし process そのものは、その**感度リスト**にある何かが変わったときだけ「動く」。

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= '0';
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

このブロックの意味:「`clk` か `reset` が変わるたびに、私の本体を再評価しろ」。

## 99% の場合に見る 2 つの形

### 組合せ process

感度リスト = 内部で読まれるすべての入力。クロックなし。出力は入力の純関数。

```vhdl
process(a, b, sel)
begin
  if sel = '0' then
    y <= a;
  else
    y <= b;
  end if;
end process;
```

2 対 1 mux に合成される。記憶なし。

### クロック付き(順次)process

感度リスト = `(clk, reset)`。代入は `rising_edge(clk)` の中だけで起きる(または `falling_edge`、運が悪ければ)。出力は **レジスタ化** されている — クロックエッジでのみ変わる。

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    q <= (others => '0');
  elsif rising_edge(clk) then
    q <= d;
  end if;
end process;
```

これが古典的な D フリップフロップのパターン。

## 感度リストの罠

本体が読む信号を列挙し忘れると、シミュレーションと合成が食い違う:

- **シミュレーション**:その信号が変わっても process は再評価しない。波形がおかしく見える。
- **合成**:ツールは本体を読み、すべての読みを見て、いずれにせよ正しいハードウェアを構築する。

結果:テストベンチは通るがボードでは動かない — あるいはその逆。VHDL-2008 ではこれを完全に避けるために `process(all)` と書ける:

```vhdl
process(all)  -- 本体で読まれるすべての信号を含む
begin
  ...
end process;
```

Kmila のパーサは `process(all)` を受け付け、新しい設計ではこれを使うチームがほとんど。

## 経験則

- 感度リスト = `(clk, reset)` だけ → あなたは**レジスタ**(フリップフロップ)を記述している。
- 感度リスト = 読むすべての入力 → あなたは**組合せ論理**を記述している。
- 感度リストにクロックとそれ以外が混在 → ほぼ確実にバグ。

## 覚えておくべきこと

- process は、本来並行のファイル内における順次コードの単位。
- クロック付き process(`rising_edge(clk)`)はフリップフロップに合成される。これがメモリを得る方法。
- 読むすべての信号を列挙するか、`process(all)` でツールに任せる。
