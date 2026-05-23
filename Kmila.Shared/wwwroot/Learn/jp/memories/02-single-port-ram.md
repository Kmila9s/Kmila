# サンプル:write-first 読み出しのシングルポート RAM

読み書き可能メモリは「BRAM 推論」のもう半分。同じ `type…array…of` パターンが動くが、配列は今度は **signal** で、あるクロックエッジで書き、別のエッジで読む。

## テンプレート

```vhdl
type mem_t is array(0 to 31) of std_logic_vector(7 downto 0);
signal mem : mem_t := (others => (others => '0'));
...
process(clk)
begin
  if rising_edge(clk) then
    if we = '1' then
      mem(to_integer(unsigned(addr))) <= din;
      dout <= din;                             -- write-first 読み出し
    else
      dout <= mem(to_integer(unsigned(addr)));
    end if;
  end if;
end process;
```

## 3 つの読み出しポリシー

すべての同期 RAM は**書き込み中**に `dout` が何をするか決めなければならない。3 つの慣例:

- **Write-first(上で示したもの)**:書き込み時、`dout` は新しく書かれたデータを示す。最もきれいな意味論 — 同じアドレスへの同一サイクルでの読み書きは新しい値を返す。
- **Read-first**:書き込み時、`dout` はセルの**古い**内容を示す。一部の CPU のレジスタファイルがこれを使う。
- **No-change**:書き込み時、`dout` は更新しない。一部の低電力設計。

3 つすべて、現代のあらゆる FPGA で block RAM にマップされるが、シンセサイザは process の正確な形に応じてモードを選ぶ。write-first テンプレートが最も移植性が高い — 迷ったらこれを書け。

## サンプルが行うこと

最初の 30 µs、sim は 3 つのアドレス(`0`、`1`、`2`)に 3 つの異なるバイトパターンを書く。次に `we` が下がり、`addr` は 40 µs で 0 にリセットされる。Waveforms タブでは、書き込み中は `dout` が `din` に追従し、その後の読み出しでは格納された値が出てくるのが見える。

## あなたの番

- **真のデュアルポート RAM にアップグレード**。`addr`/`din`/`dout`/`we` のフルセット 2 つ、両方とも `clk` でクロック。各ベンダの FIFO とデュアルクロック DPRAM が裏で使っているのがこのパターン。
- **出力レジスタを追加**。Xilinx の block RAM はメモリ配列の後に 2 つ目のパイプラインレジスタをサポート。2 つ目のクロック付きブロックで `dout_reg <= dout;` を追加し、波形で何が変わるか見る(ヒント:レイテンシが 1 サイクル増えるが、タイミングクロージャは格段に良くなる)。
- **初期内容を変える**。`signal mem` の初期化子を変えて、最初のいくつかのエントリを認識できるパターンでプリロードする。すべてのシンセサイザが移植性 VHDL の BRAM 初期化子を尊重するわけではない — プロダクションで頼る前にターゲットを確認すること。
