# 敏感信号列表与 delta 周期

你已经看到，信号要到「delta 周期结束时」才会更新。
现在我们来见识由这条规则派生出的经典坑。

**敏感信号列表**就是 `process` 后面那个 `( … )`。它告诉
仿真器：*只在这些信号中的某一个发生变化时唤醒本 process*。

**delta 周期**是仿真时间里最小的一格。在一个 delta 周期
内部，信号赋值只是**登记**新值——真正的值要等到 delta
结束才会出现。在此期间，读该信号得到的是它的**旧**值。

把这两条规则拼在一起，就得到下面这个陷阱。播放器会跑
一个只有两条信号赋值的小 process：

```vhdl
process(a, b)
begin
  out1 <= a;
  out2 <= b and out1;   -- ← 这里读到的是什么？
end process;
```

看起来天真无害。学生通常会想：「第 3 行把 `out1` 设成
`a`，然后第 4 行读 `out1` 并把它与 `b` 做 AND。」但第 4
行读到的是**旧**的 `out1`——第 3 行登记的事务还没有生效。

看播放器，你会发现 `out2` 出来是 `U`，即便 `a` 和 `b`
都是 `1`。第一次遇见时确实反直觉。

## 两种修复方式

### 方式 A —— 用 variable

variable 的更新是立即的，因此在下一行读 `tmp` 时就已经
是刚刚赋进去的值：

```vhdl
process(a, b)
  variable tmp : std_logic;
begin
  tmp  := a;
  out1 <= tmp;
  out2 <= b and tmp;   -- 读到的是新的 tmp —— 生效。
end process;
```

### 方式 B —— 等待第二个 delta

把 `out1` 加进敏感信号列表。第一个 delta 提交 `out1`
之后，process 会被再唤醒一次并重跑：

```vhdl
process(a, b, out1)
begin
  out1 <= a;
  out2 <= b and out1;   -- 第二次唤醒读到的就是已提交的 out1
end process;
```

两种方式都能用；方式 A 通常更受青睐，因为它省一个 delta，
而且数据流更明确。

> **试一试：** 单步走过播放器，盯着信号表看。就算
> `a=1` 且 `b=1`，`out2` 最后仍然是 `U`——而不是 `1`。
> delta 周期规则每学期都会咬新学生一口。
