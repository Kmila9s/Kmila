// VHDL 言語ドキュメント — 日本語。
// vhdl-data.en.js と同じ構造。

(function () {
    window.kmilaVhdlDocs = window.kmilaVhdlDocs || {};
    window.kmilaVhdlDocs.jp = {

        keywords: {
            // --- 構造宣言 ---
            entity:       { summary: "ハードウェアモジュールの外部インタフェースを宣言する。", description: "**entity** は設計単位に名前を付け、ポート（入出力）とジェネリック（パラメータ）を列挙する。ブラックボックスの視点であり、実装は architecture が提供する。", example: "entity counter is\n  port (\n    clk : in  std_logic;\n    q   : out std_logic_vector(7 downto 0)\n  );\nend entity counter;" },
            architecture: { summary: "entity の実装本体。", description: "**architecture** は entity の具体的な実装を提供する。1 つの entity は複数の architecture（例: `behavioral`、`rtl`、`structural`）を持てる。", example: "architecture behavioral of counter is\n  signal cnt : unsigned(7 downto 0) := (others => '0');\nbegin\n  process(clk)\n  begin\n    if rising_edge(clk) then cnt <= cnt + 1; end if;\n  end process;\n  q <= std_logic_vector(cnt);\nend architecture behavioral;" },
            component:    { summary: "entity のインタフェースをインスタンス化可能にした宣言。", description: "**component** 宣言により、architecture から別の entity をインスタンス化できる。モダンな直接エンティティインスタンス化が登場する以前は必須だった。", example: "component fifo is\n  port ( clk : in std_logic; din : in std_logic_vector(7 downto 0) );\nend component;" },
            configuration:{ summary: "コンポーネントを特定の entity/architecture に結びつける。", description: "合成可能なコードではほぼ使わない。1 つの entity に複数の architecture がある場合のシミュレーション用。" },
            package:      { summary: "再利用可能な型・定数・サブプログラムの集合。", description: "**package** は、`use` で他の設計単位に取り込める宣言をまとめる。", example: "package my_pkg is\n  constant WIDTH : integer := 8;\n  type state_t is (idle, busy, done);\nend package my_pkg;" },
            body:         { summary: "package の実装側。", description: "`package body` には、`package` で宣言したサブプログラムの実装が入る。" },

            // --- ライブラリと use 句 ---
            library:      { summary: "ライブラリを可視にする。", description: "VHDL ライブラリをインポートする。`std_logic_1164` と `numeric_std` を使うために `ieee` をほぼ常に指定する。", example: "library ieee;" },
            use:          { summary: "package の中身を可視にする。", description: "package から名前を取り込み、修飾なしで使えるようにする。", example: "use ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;" },

            // --- ポートとジェネリック ---
            port:         { summary: "entity のポート（境界をまたぐ信号）を宣言する。", description: "entity の中に現れる。各ポートは名前・方向（`in`、`out`、`inout`、`buffer`）・型を持つ。", example: "port (\n  clk  : in  std_logic;\n  data : out std_logic_vector(7 downto 0)\n);" },
            generic:      { summary: "entity のコンパイル時パラメータを宣言する。", description: "ジェネリックはテンプレートパラメータのようなもので、インスタンス化側が値を選び、設計がスケールする（バス幅、FIFO 深度など）。", example: "generic ( WIDTH : integer := 8 );" },
            map:          { summary: "インスタンスのポート/ジェネリックを周囲の信号に接続する。", description: "コンポーネントインスタンス化時に `port map (...)` と `generic map (...)` で使う。", example: "u_counter : counter port map (\n  clk => sys_clk,\n  q   => led\n);" },
            in:           { summary: "ポート方向 — 入力のみ。", description: "設計はこのポートを読むだけで、駆動はできない。" },
            out:          { summary: "ポート方向 — 出力のみ。", description: "設計はこのポートを駆動する。同じ entity 内で値を読み戻すことはできない（必要なら `buffer` か内部信号を使う）。" },
            inout:        { summary: "双方向ポート。", description: "同じ物理ラインを駆動・読み出しの両方に使う。同時にアクティブなドライバは 1 つだけ — 他は `'Z'`（ハイインピーダンス）でなければならない。" },
            buffer:       { summary: "内部からも読める出力。", description: "`out` と同様だが、entity 内部からも読める。古い書き方とされる — 内部信号と `out` の組み合わせを推奨。" },

            // --- 信号宣言 ---
            signal:       { summary: "信号（配線）を宣言する。", description: "**signal** は時間にわたって値を運ぶ。process 内では信号代入（`<=`）は更新を即時適用せずスケジュールする。", example: "signal counter : unsigned(7 downto 0) := (others => '0');" },
            variable:     { summary: "変数（process ローカル）を宣言する。", description: "**variable** は process やサブプログラム内に存在する。signal と異なり `:=` は即時代入される — process 内の中間計算に便利。", example: "process\n  variable tmp : integer := 0;\nbegin\n  tmp := tmp + 1;\nend process;" },
            constant:     { summary: "名前付き定数を宣言する。", description: "固定値に読みやすい名前を付ける。任意のスコープで使用可。", example: "constant CLOCK_HZ : integer := 50_000_000;" },
            shared:       { summary: "複数のプロセスからアクセスできる共有変数。", description: "VHDL-2008 でのみ意味を持つ。注意して使うこと — 通常は protected type か整数。" },

            // --- 型システム ---
            type:         { summary: "新しい型を定義する。", description: "列挙、範囲、配列、レコードに使う。", example: "type state_t is (idle, run, done);" },
            subtype:      { summary: "既存型に制約を加えたバージョンを宣言する。", description: "新しい型を作らずに範囲や解決の制約を追加する。", example: "subtype byte is std_logic_vector(7 downto 0);" },
            array:        { summary: "配列型を宣言する。", description: "1 次元または多次元。制約付き（固定サイズ）と制約なし（使用時にサイズ確定）がある。", example: "type memory_t is array(0 to 255) of std_logic_vector(7 downto 0);" },
            record:       { summary: "名前付きフィールドを持つ複合型を宣言する。", description: "VHDL における C の `struct` 相当。", example: "type axi_cmd_t is record\n  addr : std_logic_vector(31 downto 0);\n  len  : unsigned(7 downto 0);\nend record;" },
            range:        { summary: "スカラ型の範囲制約。", description: "通常 `A to B`（昇順）または `A downto B`（降順）。for ループでも使う。", example: "for i in 0 to 7 loop ... end loop;" },
            downto:       { summary: "降順の範囲方向。", description: "`MSB downto LSB` — bit 7 を左に置くベクトル添字の標準的な書き方。", example: "std_logic_vector(7 downto 0)" },
            to:           { summary: "昇順の範囲方向。", description: "`A to B` — ループや昇順インデックス配列で使う。", example: "for i in 0 to 15 loop ... end loop;" },
            others:       { summary: "アグリゲートの既定値ワイルドカード。", description: "アグリゲートの名前なし位置すべてを設定する。", example: "signal zeros : std_logic_vector(7 downto 0) := (others => '0');" },
            units:        { summary: "物理型の単位を宣言する。", description: "`time` のような独自物理型を定義するときに使う。" },
            access:       { summary: "ポインタ的なアクセス型。", description: "ほぼ合成不可 — シミュレーション内のリンクリスト構造に使う。" },
            file:         { summary: "シミュレーション中の入出力に使うファイル型。", description: "合成不可。テストベンチで刺激ファイルを読むのに一般的。" },

            // --- 順序文 / 制御フロー ---
            process:      { summary: "順序文のブロック。", description: "**process** は文を順次実行し、感度リストの信号が変化するたびに再実行される。クロック付きロジックの中核。", example: "process(clk, reset)\nbegin\n  if reset = '1' then\n    q <= (others => '0');\n  elsif rising_edge(clk) then\n    q <= d;\n  end if;\nend process;" },
            if:           { summary: "条件実行。", description: "process 内の標準構文 `if … then … elsif … else … end if;`。", example: "if rising_edge(clk) then\n  q <= d;\nend if;" },
            then:         { summary: "`if` / `elsif` で条件と本体を区切る。", description: "" },
            elsif:        { summary: "`if` 文の追加分岐。", description: "" },
            else:         { summary: "`if` / `when` のフォールバック分岐。", description: "" },
            case:         { summary: "値による多分岐。", description: "C の `switch` のような構文。長い `if`/`elsif` 連鎖よりも見通しが良い。", example: "case state is\n  when idle => …\n  when busy => …\n  when others => null;\nend case;" },
            when:         { summary: "`case` の分岐セレクタ、または条件付き信号代入の構成要素。", description: "", example: "q <= '1' when enable = '1' else '0';" },
            select:       { summary: "選択代入の選択肢を選ぶ。", description: "並行文 `with … select`。古典的な並行条件文の 1 つ。", example: "with state select\n  q <= a when idle,\n       b when busy,\n       c when others;" },
            loop:         { summary: "汎用ループ構文。", description: "無条件（無限）、`for` 添字付き、`while` 条件付きのいずれも可。", example: "for i in 0 to 7 loop\n  result(i) <= data(7 - i);\nend loop;" },
            for:          { summary: "添字付きループ。", description: "範囲を反復する。process 内でも `generate` 内でも有用。", example: "for i in 0 to 15 loop … end loop;" },
            while:        { summary: "条件ベースのループ。", description: "条件が信号に依存する場合は合成不可になりがち。" },
            next:         { summary: "ループの次の反復へ進む。", description: "" },
            exit:         { summary: "ループから脱出する。", description: "" },
            return:       { summary: "function から値を返す、または procedure を抜ける。", description: "" },
            wait:         { summary: "process を一時停止する。", description: "`wait on <信号>` / `wait until <条件>` / `wait for <時間>`。合成可能なのは特定の形（例: `wait until rising_edge(clk)`）のみ。", example: "wait until rising_edge(clk);" },
            null:         { summary: "no-op 文。", description: "構文上文が必要な箇所のプレースホルダ。" },

            // --- 並行 / generate ---
            begin:        { summary: "文ブロックの開始。", description: "`architecture`、`process`、`function`、`procedure`、`generate` の本体を開く。" },
            end:          { summary: "ブロックを閉じる。", description: "`entity`、`architecture`、`process`、`if`、`case`、`loop` などと対応する。" },
            generate:     { summary: "ハードウェアを条件付き／反復的に生成する。", description: "コンパイル時の for ループや if 文のようなもので、複製や条件付き構造を出力する。", example: "gen_bits : for i in 0 to 7 generate\n  q(i) <= d(7 - i);\nend generate;" },
            block:        { summary: "スコープ付きの並行文領域。", description: "guard 条件のもとで文をまとめる。モダンなコードではほぼ使わない。" },
            assert:       { summary: "シミュレーション時のチェック。", description: "条件が偽なら `report` を発し、severity に応じて停止する場合もある。合成不可。", example: "assert data'length = 8 report \"幅が違います\" severity error;" },
            report:       { summary: "シミュレーションメッセージを出力する。", description: "`assert` と組み合わせることが多い。severity は `note`、`warning`、`error`、`failure`。" },
            severity:     { summary: "`assert`/`report` の severity レベル。", description: "" },

            // --- サブプログラム ---
            function:     { summary: "値を返す純粋なサブプログラム。", description: "引数から値を計算する — 信号への副作用はない。", example: "function max(a, b : integer) return integer is\nbegin\n  if a > b then return a; else return b; end if;\nend function;" },
            procedure:    { summary: "信号を駆動できるサブプログラム。", description: "function と異なり、procedure は `out`/`inout` パラメータを持てる。" },
            impure:       { summary: "外部状態に依存しうる function を示す。", description: "スコープ外で宣言された信号の読み出しを許す。" },
            pure:         { summary: "外部依存のない function を示す。", description: "`function` の既定値 — 明示することは少ない。" },
            open:         { summary: "ポートを未接続にする、またはファイルを開く。", description: "port map で: `some_port => open;`。ファイル I/O で: `file F : text open read_mode is ...;`" },

            // --- 演算子（キーワード） ---
            and:          { summary: "論理 AND。", description: "std_logic_vector / bit_vector 上で要素ごと。" },
            or:           { summary: "論理 OR。", description: "ベクトル上で要素ごと。" },
            not:          { summary: "論理 NOT。", description: "単項否定。" },
            nand:         { summary: "論理 NAND。", description: "" },
            nor:          { summary: "論理 NOR。", description: "" },
            xor:          { summary: "論理 XOR。", description: "" },
            xnor:         { summary: "論理 XNOR。", description: "" },
            mod:          { summary: "剰余 (modulo)。", description: "結果は除数の符号を持つ（`rem` と異なる）。" },
            rem:          { summary: "剰余 (remainder)。", description: "結果は被除数の符号を持つ。" },
            abs:          { summary: "絶対値。", description: "" },
            sla:          { summary: "算術左シフト。", description: "" },
            sra:          { summary: "算術右シフト。", description: "" },
            sll:          { summary: "論理左シフト。", description: "" },
            srl:          { summary: "論理右シフト。", description: "" },
            rol:          { summary: "左ローテート。", description: "" },
            ror:          { summary: "右ローテート。", description: "" },

            // --- その他 ---
            is:           { summary: "区切りキーワード。", description: "`entity X is`、`architecture Y of X is`、`type T is` などに登場する。" },
            of:           { summary: "区切りキーワード。", description: "`architecture A of X`、`array(…) of …` などに登場する。" },
            all:          { summary: "ワイルドカード。", description: "`use x.y.all;` や属性／感度リスト等で使う。" },
            new:          { summary: "アクセス型（ポインタ）オブジェクトを確保する。", description: "ほぼ合成不可。" },
            label:        { summary: "文に名前を付ける。", description: "process、generate、ループに後から参照しやすくするため。" },
            alias:        { summary: "オブジェクトやその一部に別名を付ける。", description: "", example: "alias sign : std_logic is data(7);" },
            attribute:    { summary: "メタデータタグまたは事前定義のクエリ。", description: "`x'length`、`x'range`、`signal'event` などのクエリ。" },
            group:        { summary: "属性適用のために名前付き要素をまとめる。", description: "あまり見ない。" },
            bus:          { summary: "古い signal 修飾子。", description: "歴史的。" },
            register:     { summary: "古い signal 修飾子。", description: "歴史的。" },
            disconnect:   { summary: "guarded signal の切断時間を制御する。", description: "歴史的／稀。" },
            after:        { summary: "transport 遅延指定。", description: "`a <= b after 5 ns;` — シミュレーション専用の遅延。" },
            reject:       { summary: "inertial 遅延の拒否パルス。", description: "" },
            inertial:     { summary: "inertial 遅延（既定）。", description: "短いパルスをフィルタする。" },
            transport:    { summary: "transport（フィルタしない）遅延。", description: "" },
            guarded:      { summary: "ブロックや信号代入を修飾する。", description: "稀。" },
            linkage:      { summary: "他言語連携用のポート方向。", description: "VHDL/Verilog 混在フローでのみ意味を持つ。" },
            postponed:    { summary: "process やアサーションをデルタサイクルの末尾まで遅らせる。", description: "シミュレーション専用。" },
            literal:      { summary: "属性関連のキーワード。", description: "稀。" },
            unaffected:   { summary: "条件付き信号代入の「何もしない」プレースホルダ。", description: "" },
            until:        { summary: "`wait until <条件>` で使う。", description: "" },
            with:         { summary: "選択信号代入を始める。", description: "`with expr select …`。" },
            on:           { summary: "`wait on <信号リスト>` で使う。", description: "" }
        },

        types: {
            std_logic:        { summary: "IEEE 9 値ロジック: 1 本の配線。", description: "値: `'0'`、`'1'`、`'Z'`（ハイインピーダンス）、`'X'`（不明）、`'U'`（未初期化）、`'W'`、`'L'`、`'H'`、`'-'`（don't-care）。合成における 1 ビット信号の既定。", example: "signal clk : std_logic := '0';" },
            std_logic_vector: { summary: "std_logic のベクトル — バス。", description: "範囲付きで宣言する: `(high downto low)` が標準的な書き方。配線には適するが、算術には `unsigned`/`signed` に変換すること。", example: "signal data : std_logic_vector(7 downto 0);" },
            std_ulogic:       { summary: "std_logic の未解決版。", description: "ドライバ 1 本のみ。直接は稀。" },
            std_ulogic_vector:{ summary: "std_ulogic の未解決ベクトル。", description: "" },
            bit:              { summary: "単純な 2 値ロジック: `'0'` か `'1'`。", description: "std_logic より古い。合成では std_logic を使うこと — bit は Z や X を表現できない。" },
            bit_vector:       { summary: "`bit` のベクトル。", description: "" },
            boolean:          { summary: "Boolean 型: `true` / `false`。", description: "条件で使う。配線へ直接合成できない（std_logic に変換すること）。" },
            integer:          { summary: "整数 — 少なくとも 32 ビット符号付き。", description: "カウンタや添字に最適。`integer range 0 to 255` のように部分範囲で制約すると、合成器がビット幅を把握できる。" },
            natural:          { summary: "`integer range 0 to integer'high`。", description: "非負整数。" },
            positive:         { summary: "`integer range 1 to integer'high`。", description: "厳密に正の整数。" },
            real:             { summary: "浮動小数点型。", description: "合成不可 — シミュレーション／定数のみ。" },
            character:        { summary: "ASCII / ISO-8859-1 文字。", description: "" },
            string:           { summary: "character の配列。", description: "主に `report` メッセージとファイル I/O で使う。" },
            time:             { summary: "時間を表す物理型。", description: "単位: `fs`、`ps`、`ns`、`us`、`ms`、`sec`、`min`、`hr`。シミュレーションのみ。" },
            signed:           { summary: "ieee.numeric_std の数値ベクトル（符号付き）。", description: "符号付き算術に使う。std_logic_vector からは `signed()` キャストで変換。", example: "signal x : signed(7 downto 0);" },
            unsigned:         { summary: "ieee.numeric_std の数値ベクトル（符号なし）。", description: "符号なし算術に使う。`unsigned()` / `std_logic_vector()` でキャスト。", example: "signal cnt : unsigned(7 downto 0) := (others => '0');" },
            line:             { summary: "テキスト行へのアクセス — ファイル I/O 用。", description: "テストベンチで `readline` / `writeline` と組み合わせて使う。" },
            text:             { summary: "textio パッケージのファイル型。", description: "" },
            severity_level:   { summary: "`assert`/`report` の severity レベル。", description: "値: `note`、`warning`、`error`、`failure`。" },
            side:             { summary: "textio の補助型。", description: "" },
            width:            { summary: "textio の補助型。", description: "" }
        },

        builtins: {
            rising_edge:    { summary: "クロックの 0→1 遷移で真。", signature: "rising_edge(s : std_ulogic) return boolean", description: "`clk'event and clk = '1'` よりこちらが推奨。std_logic と std_ulogic の両方で動く。", example: "if rising_edge(clk) then q <= d; end if;" },
            falling_edge:   { summary: "1→0 遷移で真。", signature: "falling_edge(s : std_ulogic) return boolean", description: "`rising_edge` の対。" },
            to_integer:     { summary: "signed/unsigned を integer に変換する。", signature: "to_integer(arg : unsigned) return natural", description: "`ieee.numeric_std` から。算術後の典型的なキャスト。", example: "variable n : integer := to_integer(unsigned(data));" },
            to_unsigned:    { summary: "integer を unsigned ベクトルに変換する。", signature: "to_unsigned(arg : natural; size : natural) return unsigned", description: "", example: "counter <= to_unsigned(42, 8);" },
            to_signed:      { summary: "integer を signed ベクトルに変換する。", signature: "to_signed(arg : integer; size : natural) return signed", description: "" },
            resize:         { summary: "signed/unsigned ベクトルのサイズを変更する。", signature: "resize(arg : unsigned; new_size : natural) return unsigned", description: "unsigned はゼロ拡張、signed は符号拡張。" },
            shift_left:     { summary: "論理左シフト。", signature: "shift_left(arg : unsigned; count : natural) return unsigned", description: "右側をゼロで埋める。" },
            shift_right:    { summary: "unsigned は論理右シフト、signed は算術右シフト。", signature: "shift_right(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_left:    { summary: "循環左シフト。", signature: "rotate_left(arg : unsigned; count : natural) return unsigned", description: "" },
            rotate_right:   { summary: "循環右シフト。", signature: "rotate_right(arg : unsigned; count : natural) return unsigned", description: "" },
            conv_integer:   { summary: "古い integer 変換。", signature: "conv_integer(arg : std_logic_vector) return integer", description: "`ieee.std_logic_arith` から。新しいコードでは numeric_std の `to_integer` を推奨。" },
            conv_std_logic_vector: { summary: "古いベクトル変換。", signature: "conv_std_logic_vector(arg : integer; size : integer) return std_logic_vector", description: "新しいコードでは `std_logic_vector(to_unsigned(...))` を推奨。" },
            now:            { summary: "現在のシミュレーション時間。", signature: "now : time", description: "シミュレーションのみ。" },
            readline:       { summary: "textio ファイルから 1 行読む。", signature: "readline(file f : text; l : out line)", description: "テストベンチ I/O。" },
            writeline:      { summary: "textio ファイルへ 1 行書く。", signature: "writeline(file f : text; l : inout line)", description: "テストベンチ I/O。" },
            endline:        { summary: "行末判定。", signature: "endline(l : inout line) return boolean", description: "textio ヘルパ。" },
            endfile:        { summary: "ファイル末尾判定。", signature: "endfile(file f : text) return boolean", description: "" },
            write:          { summary: "textio の書き込みプリミティブ。", signature: "write(l : inout line; value : … )", description: "" },
            read:           { summary: "textio の読み込みプリミティブ。", signature: "read(l : inout line; value : out … )", description: "" }
        },

        snippets: {
            entity: {
                label: "entity（雛形）",
                summary: "最小の entity 宣言を挿入",
                insertText: "entity ${1:name} is\n\tport (\n\t\t${2:clk}   : in  std_logic;\n\t\t${3:reset} : in  std_logic;\n\t\t${4:q}     : out std_logic_vector(${5:7} downto 0)\n\t);\nend entity ${1:name};\n\narchitecture behavioral of ${1:name} is\nbegin\n\t$0\nend architecture behavioral;"
            },
            process_clocked: {
                label: "process（同期、非同期 reset）",
                summary: "標準的な同期 process と非同期 reset",
                insertText: "process(${1:clk}, ${2:reset})\nbegin\n\tif ${2:reset} = '1' then\n\t\t$3 <= (others => '0');\n\telsif rising_edge(${1:clk}) then\n\t\t$0\n\tend if;\nend process;"
            },
            process_combinational: {
                label: "process（組合せ）",
                summary: "感度リスト付きの組合せ process",
                insertText: "process(${1:all})\nbegin\n\t$0\nend process;"
            },
            if_else: {
                label: "if … elsif … else",
                summary: "3 分岐の条件文",
                insertText: "if ${1:condition} then\n\t$2\nelsif ${3:other} then\n\t$4\nelse\n\t$5\nend if;"
            },
            case_stmt: {
                label: "case … when … end case",
                summary: "case 文の雛形",
                insertText: "case ${1:expr} is\n\twhen ${2:value} => $3\n\twhen others    => $0\nend case;"
            },
            for_loop: {
                label: "for i in 0 to N-1 loop",
                summary: "添字付きループ",
                insertText: "for ${1:i} in 0 to ${2:N}-1 loop\n\t$0\nend loop;"
            },
            signal: {
                label: "signal 宣言",
                summary: "signal を宣言する",
                insertText: "signal ${1:name} : ${2:std_logic_vector}(${3:7} downto 0) := (others => '0');"
            },
            counter: {
                label: "カウンタテンプレート",
                summary: "ラップアラウンドカウンタ",
                insertText: "signal cnt : unsigned(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk}, ${3:reset})\nbegin\n\tif ${3:reset} = '1' then\n\t\tcnt <= (others => '0');\n\telsif rising_edge(${2:clk}) then\n\t\tcnt <= cnt + 1;\n\tend if;\nend process;\n${4:q} <= std_logic_vector(cnt);"
            },
            shift_register: {
                label: "シフトレジスタテンプレート",
                summary: "N ビットシフトレジスタ",
                insertText: "signal sr : std_logic_vector(${1:7} downto 0) := (others => '0');\n-- …\nprocess(${2:clk})\nbegin\n\tif rising_edge(${2:clk}) then\n\t\tsr <= sr(${1:7}-1 downto 0) & ${3:d};\n\tend if;\nend process;\n${4:q} <= sr;"
            },
            library_ieee: {
                label: "library ieee + use 句",
                summary: "標準の IEEE インポート",
                insertText: "library ieee;\nuse ieee.std_logic_1164.all;\nuse ieee.numeric_std.all;\n\n$0"
            }
        },

        ui: {
            declaredOnLine: "宣言行:",
            kindSignal:     "信号",
            kindVariable:   "変数",
            kindConstant:   "定数",
            kindPort:       "ポート",
            categoryKeyword:  "VHDL キーワード",
            categoryType:     "型",
            categoryBuiltin:  "IEEE 関数",
            categoryUser:     "ユーザー宣言",
            categorySnippet:  "スニペット",
            translationPending: "⚠ 翻訳待ち — 説明は英語で表示されます。"
        }
    };
})();
