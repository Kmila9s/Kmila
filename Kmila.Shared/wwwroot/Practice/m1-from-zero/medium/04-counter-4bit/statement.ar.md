## عدّاد متزامن 4 بت

ابنِ عدّادًا 4 بتات يزيد عند كل حافة صاعدة للساعة. `reset` غير المتزامن النشط مرتفعًا يصفّر العداد إلى `0000`.

### المواصفات

- داخليًا: `count : unsigned(3 downto 0) := (others => '0')`.
- عند `rising_edge(clk)`: `count <= count + 1;`.
- الخرج: `q <= std_logic_vector(count);`.

### تلميح

استخدم `IEEE.NUMERIC_STD.ALL` ليعمل `+ 1` على نوع `unsigned`.
