# 并发：一切同时触发

在任何 process 之外——直接写在 `architecture` 的 `begin` 和 `end` 之间——你可以写**并发信号赋值**。它们看起来像顺序赋值，但遵循一条完全不同的规则：

> **所有对某个事件敏感的并发语句都会在同一个 delta 周期里触发。** 源代码里的先后顺序并不重要。

想象一栋楼，不同房间里有四个电灯开关。当电力接通时，每个开关的状态都会在同一瞬间生效——电工是先接了哪个房间的线其实无所谓。

下面的播放器有四条并发语句，用同样的输入 `a` 和 `b` 驱动四条不同的输出。按下播放，看四条输出线在同一步里一起更新。

<!-- exec-fig:start -->
## 回到餐厅：挨桌查看 vs 听铃声

这里仍然 **只有一名服务员**，所以这是并发，不是并行。比较她轮流照看各桌的两种方式：

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-restaurant.html?modes=poll,bell&mode=poll" title="餐厅：挨桌查看的服务员 vs 听铃声的服务员" loading="lazy"></iframe></div>

- **1 ↻** — 她一圈圈走，**每桌都去看看**，不管菜好没好。读数里每个 ↻ 都是一趟白跑。
- **1 <svg width="1em" height="1em" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="vertical-align:-0.15em" aria-hidden="true"><path d="M3 12H13M4.6 12C4.6 6 5.8 3 8 3S11.4 6 11.4 12M6.6 14.2A1.5 1.5 0 0 0 9.4 14.2"/></svg>** — 她先点完所有桌的菜，之后 **只去铃响的那桌**（菜做好时，桌上的铃会亮起来）。

VHDL 仿真器用的是第二种方式。它不会一直重新执行所有语句，只唤醒 **敏感信号列表** 收到 **事件** 的 process。铃就是敏感信号列表，铃响就是事件。同一个服务员、同样的桌子，步骤更少（约 23 秒而不是 37 秒，白跑 0 次）。
<!-- exec-fig:end -->

<!-- exec-fig2:start -->
## 摄影师：一张照片，一次闪光

为什么代码顺序不重要？因为在一个 delta 周期里，所有并发语句读取的是同一张输入的 **照片**，在 **闪光** 之前，谁也看不到别人的结果。

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/exec-photo.html" title="摄影棚：四张桌子照着同一张照片抄写；闪光让四块面板同时翻转" loading="lazy" style="aspect-ratio:640/340"></iframe></div>

- 相机按下快门：照片冻结 `a` 和 `b`。
- 每张桌子（每条语句一张）根据同一张照片写下自己的 **草稿**。
- 闪光就是 delta 结束：四块输出面板 **同时** 翻转。
<!-- exec-fig2:end -->

## 两种验证方式

- **在心里交换两行的顺序**——结果会变吗？不会。试一下。
- **试着从 architecture 里删掉输入，看所有输出会怎样。** 它们会一起回到 `U`，因为根本没有先后顺序——每个 delta 周期都是一次并行求值。

## 并发还是顺序——什么时候用哪一个？

| 用途 | 选哪种 |
|---|---|
| 组合逻辑（门、mux、无时钟的运算） | **并发** |
| 时序行为（寄存器、FSM、计数器） | **顺序**（带 `rising_edge(clk)` 的 process） |
| 读多个信号、做点运算、输出一个结果 | 都行——并发写起来更像电路，顺序写起来更像程序 |

> **试一试：** 把播放器停在四条线同时高亮的那一步。那一个瞬间就是**一个 delta 周期**。四条赋值全部在完全相同的仿真时间上求值。
