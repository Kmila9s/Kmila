# 「运行」到底是什么意思？

当你在 Kmila 里按下 **Run** 时，模拟器并没有真的去点亮一块 FPGA。它只是一步一步地走过你的 VHDL 代码——但**怎么**走，取决于代码写在哪里。

有三种执行模型，人们经常把它们搞混：

| 模型 | 含义 | 在 VHDL 里出现的位置 |
|---|---|---|
| **顺序（Sequential）** | 一次一条语句，按源代码顺序执行。 | `process` 主体内部。 |
| **并发（Concurrent）** | 多条语句「一起」求值——文件里的先后顺序无关紧要。 | architecture 里 `begin` 和 `end` 之间的并发语句。 |
| **并行（Parallel）** | 在真实的墙上时钟同一瞬间发生多件事。 | **只在真实硬件上。** 仿真中永远不会。 |

下面的例子刻意做得很小——一个 process 读入两个输入 `a` 和 `b`，写出一个输出 `sum`。在播放器里按 ▶，看看模拟器此刻正在「看」哪一行。

```text
Sequential:   line 1  →  line 2  →  line 3  …
Concurrent:   lines 1 and 2 and 3 all evaluate this instant
Parallel:     not a thing in simulation. Only happens on the FPGA itself.
```

<!-- exec-fig:start -->
## 餐厅类比

想象一家有 **10 张桌子** 的餐厅。**服务员** 就是干活的那一方（CPU，或者 Kmila 的仿真器）。每张 **桌子** 是一项需要照看的工作，每道 **菜** 都要做一段时间。点击图上方的按钮，切换餐厅的运营方式：

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=seq,poll,par" title="餐厅：一名服务员、一名轮流照看各桌的服务员、五名服务员" loading="lazy"></iframe></div>

- **1 →** — *顺序*：一名服务员一次只照顾一桌。她点完菜后 **一直站在那里等菜上来**，哪怕另外九桌都在等。
- **1 ↻** — *并发*：仍然只有一名服务员，但菜没好时她 **先去下一桌**：点下一桌的菜，再回来看第一桌，哪道菜好了就上哪道。
- **5 ‖** — *并行*：五名服务员，每人两桌，**在同一时刻** 一起工作。

怎么看图表：每张桌子一行；**蓝色条** = 服务员在这桌，**红线** = 菜正在做，**虚线** = 菜已做好但还没上， **绿点** = 已上菜。读数显示 <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><circle cx="8" cy="9" r="5.5"/><path d="M8 9V6.2M6.5 1.8H9.5M8 1.8V3.5"/></svg> 已用时间、<svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M4.5 2H11.5M4.5 14H11.5M5.2 2C5.2 5.5 10.8 6.8 10.8 14M10.8 2C10.8 5.5 5.2 6.8 5.2 14"/></svg> 服务员干站着的时间、↻「好了吗？」的查看次数、✓ 已服务的桌数。每种模式跑完后，图表上会留下一条虚线终点线，方便比较。

| 餐厅 | 执行模型 |
|---|---|
| 服务员 | 执行者：一个 CPU 线程，或仿真器 |
| 桌子 | 一个 `process`（或一条并发语句） |
| 菜在做 | 时间在流逝（`after 10 ns`、等待时钟边沿……） |
| 菜做好了 | 一个 **事件**：process 在等的事情发生了 |
| 五名服务员 | 真实硬件：FPGA，电路的每一部分都是自己的「服务员」 |

> **注意：** 并发的服务员并没有走得更快，她只是 **从不干站着**。顺序大约 70 秒，并发大约 37 秒，并行大约 12 秒。
<!-- exec-fig:end -->

## 为什么这件事重要

初学者读到 `sum <= b;` 常常会以为 `sum` 的值会**立刻** 变成 `b`，就像 C 语言里的 `sum = b;` 那样。其实不是。这个赋值只是**登记了一个事务**，新值要等到当前 delta 周期结束后才会真正可见。

右侧的播放器把一段短短的执行过程写死在里面——它并没有真的在跑仿真。这样做是为了让讲解保持诚实：每一步都会附带一条教学注解，你不必猜测真实模拟器在想什么。

> **试一试：** 手动前进几步，观察 `sum` 一直停留在 `U` （未初始化）——即便走到第 3 步之后依然如此，它只有等到第 4 步 delta 周期结束时才会改变。
