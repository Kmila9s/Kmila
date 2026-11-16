# 顺序：一次执行一步

在一个 `process` 主体内部，VHDL 的行为很像你已经熟悉的那些语言：语句**自上而下、按源代码顺序**执行。挪动一行的位置，结果就会改变。

但有一个小陷阱会让所有人踩一次：**variable** 和 **signal** 的行为并不一样。

| 种类 | 语法 | 新值什么时候出现？ |
|---|---|---|
| **variable** | `tmp := a + 1` | **立即。** 下一条语句就能读到新的 `tmp`。 |
| **signal** | `result <= tmp + b` | **在 delta 周期结束时。** 在此之前，读到的仍然是旧值。 |

播放器会走过一个同时用到两者的小 process。留意 `tmp` 在执行到第 5 行时立刻更新，而 `result` 要**等到** process 退出**之后**才发生变化。

<!-- exec-fig2:start -->
## 记事本和邮箱

**variable** 就像职员口袋里的记事本：第 4 行或第 5 行一执行，新数字马上记上去，下一行就能读到。**signal** 则像一封信：`result <= tmp + b` 把信（13）放进发件盘，但 `result` 一直显示 U，直到 **邮车** 经过——也就是 delta 周期结束，用 δ 标记——把信送到。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-mailbox.html" title="带记事本（variable）和发件盘的书桌，一辆邮车和一个邮箱（signal）" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

图中演示的步骤与下方播放器相同。点击代码下方的圆点可跳到某一步，点击画面可暂停。
<!-- exec-fig2:end -->

## 为什么这件事重要

假设你写下：

```vhdl
result <= 1;
result <= 2;
```

在 C 里，跑完这两行之后 `result` 显然会是 `2`。而在 VHDL 里？ **最后一次赋值胜出**——但要等到 delta 结束时才生效。 process 运行期间，`result` 依然是旧值；模拟器会悄悄丢弃 `<= 1` 的事务，因为在 delta 结束之前 `<= 2` 就把它替换掉了。

> **试一试：** 单步执行到第 7 行，看信号表。此时 `tmp` 是 `8`，而 `result` 依然是 `U`。再走一步，看着 `result` 在 process 主体结束**之后**跳到 `13`。
