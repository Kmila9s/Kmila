# ALU — arithmetic logic unit

The **ALU** is the calculator at the heart of a CPU: it takes two operands A and B and, chosen by an **op-select**, produces A+B, A−B, A AND B, A OR B, and more.

<div class="lesson-figure"><iframe src="/_content/Kmila.Shared/anatomia/17-alu.html" title="Interactive ALU with an op-select drum" loading="lazy"></iframe></div>

The op-select drum on the module chooses the operation; the operand plungers set **A** and **B**. The list highlights the current op and the readout shows **A op B = R**. It is purely combinational — no clock.

In VHDL:

```vhdl
with op select
  r <= a + b     when "00",   -- ADD
       a - b     when "01",   -- SUB
       a and b   when "10",   -- AND
       a or b    when others; -- OR
```

An ALU is **combinational logic** (adders, the gates, and MUXes selecting the result) with no memory of its own — the registers around it hold the operands and the result. The op-select is just a MUX over the candidate results.
