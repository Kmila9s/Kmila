# VHDL ファイルの解剖

すべての VHDL ファイルは、常に同じ順序で同じ 4 つのセクションを持ちます。この形状を認識できれば、見慣れないコードを読むことが威圧的ではなくなります — 各部分をどこで探せばよいかがただわかります。

## 4 つのセクション

```vhdl
library IEEE;                          -- 1. libraries
use IEEE.STD_LOGIC_1164.ALL;           -- 2. packages in use

entity my_design is                    -- 3. entity (the "header")
  port (
    a : in  std_logic;
    b : in  std_logic;
    y : out std_logic
  );
end entity my_design;

architecture rtl of my_design is       -- 4. architecture (the "body")
begin
  y <= a and b;
end architecture rtl;
```

それぞれ見ていきましょう。

### 1. `library`

`library IEEE;` は標準 IEEE ライブラリをインポートします。これはデジタル設計版の `#include <stdio.h>` — これまで書くほとんどすべてのファイルはこれで始まります。

### 2. `use`

`use IEEE.STD_LOGIC_1164.ALL;` は `STD_LOGIC_1164` パッケージのすべての名前をスコープに引き込みます。それが `std_logic`、`std_logic_vector`、`rising_edge()` などを使えるようにするものです。

よく 2 つ目の `use` を見ます:

```vhdl
use IEEE.NUMERIC_STD.ALL;
```

こちらは `unsigned`、`signed`、`to_integer`、`resize`、およびベクタ型の算術を提供します。ベクタで算術を行うときはいつでも追加しましょう。

### 3. `entity`

エンティティは **ブラックボックスのインタフェース** です。以下を指定します:

- 設計の名前 (`my_design`)
- どのポートを持つか
- 各ポートについて: その **方向** (`in`、`out`、`inout`、`buffer`) とその **型**

`port (…)` の中にあるすべては外の世界から見えるものです。ここでは設計がどう動くかについては何も宣言されていません。

### 4. `architecture`

アーキテクチャは **実装** です。1 つのエンティティは複数のアーキテクチャ(同じインタフェースの)を持てます — 同じ `cpu` エンティティに対して `rtl`、`behavioral`、`tb` アーキテクチャがあり、設定ごとに 1 つを選ぶことができます。実際には、ほとんどのファイルにはちょうど 1 つあります。

`begin` と `end architecture` の間の本体が、信号とゲートが実際に記述される場所です。

## Kmila がこれをどうパースするか

Kmila の VHDL パーサはファイルを上から下へ読み、ツリーを構築します:

```
File
├── library IEEE
├── use IEEE.STD_LOGIC_1164.ALL
└── entity my_design
    ├── port a (in, std_logic)
    ├── port b (in, std_logic)
    ├── port y (out, std_logic)
    └── architecture rtl
        └── concurrent: y <= a and b
```

**Build** をクリックすると、ポートは **Ports** ドックタブのエントリになり、エンティティは **Entities** のエントリになり、アーキテクチャ内の代入はシミュレータが実行する配線になります。

## 命名規則

VHDL は大文字小文字を区別しません。`SIGNAL`、`Signal`、`signal` は同じキーワードです。しかし、1 つのファイルでは 1 つのスタイルを保ちましょう — ほとんどのチームはキーワードと信号名には **小文字**、識別子には **snake_case**、定数には **UPPER_CASE** を使います。

## 覚えておくべきこと

- `library` + `use` — インポート。
- `entity` — インタフェース(ポートと方向)。
- `architecture` — 実装(実際に何が起こるか)。
- エンティティは複数のアーキテクチャを持てる; ほとんどのプロジェクトは `rtl` と呼ばれるものを 1 つ出荷する。
