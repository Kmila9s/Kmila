-- ============================================================================
-- expr_engine.vhd — shunting-yard driven RPN evaluator for the capstone.
--
-- Kmila full audit 2026-07-08, Phase G, Ring 3.
--
-- Consumes a token stream (operands + operator opcodes) and produces a
-- single 32-bit q16_t result.
--
-- FSM states:
--   S_IDLE       — waiting for `start` pulse.
--   S_ACCEPT     — token available on `tok_valid`; classify as operand or op.
--   S_PUSH_OPND  — push operand onto operand-stack slot in RAM.
--   S_PUSH_OP    — push operator (after precedence-based popping) into queue.
--   S_DRAIN      — end of stream: drain remaining operator stack to output.
--   S_EVAL       — pop RPN token, dispatch to ALU, push result back.
--   S_DONE       — result ready in `result_out`; assert `done`.
--   S_ERROR      — TOO_LONG / DIV0 / OVERFLOW; assert `err_code`.
--
-- This is a *skeleton* implementation for the audit: FSM transitions +
-- register file only. Full RAM interfacing + ALU wiring will land in
-- calc_top.vhd (Ring 4). The purpose here is to exercise Kmila's
-- FSM parsing, subtype ports (post F-G-04 fix), and enum-like state
-- constants declared in the architecture.
-- ============================================================================

library ieee;
use ieee.std_logic_1164.all;
use ieee.numeric_std.all;
library work;
use work.types_pkg.all;

entity expr_engine is
    port (
        clk         : in  std_logic;
        rst_n       : in  std_logic;

        start       : in  std_logic;       -- begin new evaluation
        tok_valid   : in  std_logic;       -- token available this cycle
        tok_is_op   : in  std_logic;       -- 1 = token is an operator opcode
        tok_operand : in  q16_t;
        tok_op      : in  opcode_t;
        stream_end  : in  std_logic;       -- upstream done producing tokens

        alu_result  : in  q16_t;           -- from ALU
        alu_ovf     : in  std_logic;
        alu_div0    : in  std_logic;

        alu_a       : out q16_t;
        alu_b       : out q16_t;
        alu_op      : out opcode_t;

        result_out  : out q16_t;
        err_code    : out err_code_t;
        done        : out std_logic;
        busy        : out std_logic
    );
end entity expr_engine;

architecture rtl of expr_engine is
    -- FSM state encoding.
    constant S_IDLE       : std_logic_vector(2 downto 0) := "000";
    constant S_ACCEPT     : std_logic_vector(2 downto 0) := "001";
    constant S_PUSH_OPND  : std_logic_vector(2 downto 0) := "010";
    constant S_PUSH_OP    : std_logic_vector(2 downto 0) := "011";
    constant S_DRAIN      : std_logic_vector(2 downto 0) := "100";
    constant S_EVAL       : std_logic_vector(2 downto 0) := "101";
    constant S_DONE       : std_logic_vector(2 downto 0) := "110";
    constant S_ERROR      : std_logic_vector(2 downto 0) := "111";

    signal state       : std_logic_vector(2 downto 0) := S_IDLE;

    -- Operand stack — small on-chip; RAM would front this in the full design.
    type opnd_stack_t is array (0 to MAX_OPERANDS - 1) of q16_t;
    signal opnd_stack  : opnd_stack_t := (others => Q16_ZERO);
    signal opnd_sp     : integer range 0 to MAX_OPERANDS := 0;

    -- Operator queue (RPN output).
    type opq_t is array (0 to MAX_OPS - 1) of opcode_t;
    signal opq         : opq_t := (others => OP_NONE);
    signal opq_len     : integer range 0 to MAX_OPS := 0;

    signal err_reg     : err_code_t := ERR_NONE;
    signal result_reg  : q16_t := Q16_ZERO;
begin

    process (clk, rst_n)
        variable a_top, b_top : q16_t;
    begin
        if rst_n = '0' then
            state      <= S_IDLE;
            opnd_sp    <= 0;
            opq_len    <= 0;
            err_reg    <= ERR_NONE;
            result_reg <= Q16_ZERO;
        elsif rising_edge(clk) then
            case state is
                when S_IDLE =>
                    if start = '1' then
                        opnd_sp    <= 0;
                        opq_len    <= 0;
                        err_reg    <= ERR_NONE;
                        state      <= S_ACCEPT;
                    end if;

                when S_ACCEPT =>
                    if stream_end = '1' then
                        state <= S_DRAIN;
                    elsif tok_valid = '1' then
                        if tok_is_op = '1' then
                            state <= S_PUSH_OP;
                        else
                            state <= S_PUSH_OPND;
                        end if;
                    end if;

                when S_PUSH_OPND =>
                    if opnd_sp = MAX_OPERANDS then
                        err_reg <= ERR_TOOLONG;
                        state   <= S_ERROR;
                    else
                        opnd_stack(opnd_sp) <= tok_operand;
                        opnd_sp             <= opnd_sp + 1;
                        state               <= S_ACCEPT;
                    end if;

                when S_PUSH_OP =>
                    if opq_len = MAX_OPS then
                        err_reg <= ERR_TOOLONG;
                        state   <= S_ERROR;
                    else
                        opq(opq_len) <= tok_op;
                        opq_len      <= opq_len + 1;
                        state        <= S_ACCEPT;
                    end if;

                when S_DRAIN =>
                    if opq_len = 0 then
                        if opnd_sp > 0 then
                            result_reg <= opnd_stack(opnd_sp - 1);
                        end if;
                        state <= S_DONE;
                    else
                        state <= S_EVAL;
                    end if;

                when S_EVAL =>
                    -- Pop two operands, dispatch top-of-queue opcode.
                    if opnd_sp < 2 then
                        err_reg <= ERR_TOOLONG;
                        state   <= S_ERROR;
                    else
                        -- Combinational path drives alu_a/b/op; the ALU's
                        -- result appears on alu_result next cycle. Store it.
                        opnd_stack(opnd_sp - 2) <= alu_result;
                        opnd_sp                 <= opnd_sp - 1;
                        if alu_ovf = '1' then
                            err_reg <= ERR_OVERFLOW;
                            state   <= S_ERROR;
                        elsif alu_div0 = '1' then
                            err_reg <= ERR_DIV0;
                            state   <= S_ERROR;
                        elsif opq_len = 1 then
                            result_reg <= alu_result;
                            opq_len    <= 0;
                            state      <= S_DONE;
                        else
                            -- Shift queue down by 1 (bounded by MAX_OPS).
                            for i in 0 to MAX_OPS - 2 loop
                                opq(i) <= opq(i + 1);
                            end loop;
                            opq(MAX_OPS - 1) <= OP_NONE;
                            opq_len          <= opq_len - 1;
                            state            <= S_EVAL;
                        end if;
                    end if;

                when S_DONE =>
                    if start = '1' then
                        state <= S_IDLE;
                    end if;

                when S_ERROR =>
                    if start = '1' then
                        state <= S_IDLE;
                    end if;

                when others =>
                    state <= S_IDLE;
            end case;
        end if;
    end process;

    -- Combinational output of top-two operands + top-of-queue opcode.
    alu_a  <= opnd_stack(opnd_sp - 2) when opnd_sp >= 2 else Q16_ZERO;
    alu_b  <= opnd_stack(opnd_sp - 1) when opnd_sp >= 1 else Q16_ZERO;
    alu_op <= opq(0);

    result_out <= result_reg;
    err_code   <= err_reg;
    done       <= '1' when state = S_DONE else '0';
    busy       <= '0' when (state = S_IDLE or state = S_DONE or state = S_ERROR) else '1';

end architecture rtl;
