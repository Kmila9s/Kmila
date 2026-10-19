# ROM メモリ

**メモリ**は**アドレス**で読み戻せる語を蓄えます。ROM は内容が固定で、各アドレスが 1 語を選び、それが出力に現れます。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/25-memory.html" title="インタラクティブな図" loading="lazy"></iframe></div>

アドレスのプランジャがセルを選び、その語が出力ランプを点け、表が各アドレス → データを示します。

VHDL では：

```vhdl
type rom_t is array(0 to 3) of unsigned(2 downto 0);
constant ROM : rom_t := ("101","011","110","001");
...
data <= ROM(to_integer(addr));  -- addressed read
```

読み出しは純粋なアドレッシング——出力ビットごとに見れば ROM はまさに N 対 1 の MUX です。RAM は書き込み経路を加え、ブロック RAM（BRAM）は FPGA 内蔵の専用メモリです。
