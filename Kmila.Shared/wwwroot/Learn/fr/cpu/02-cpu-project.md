# Projet : CPU 8 bits monocycle

C'est le couronnement. Vous assemblez tout ce qui vient des modules précédents — une ROM, un registre, une ALU, un contrôle façon FSM — en une petite machine qui exécute vraiment des programmes.

## La machine

```mermaid
flowchart TD
    RST(["reset = 0000"]) -.-> PC["PC (4 bits)"]
    PC -- addr --> ROM["ROM<br/>16 × 8 octets<br/>(préchargée à la synthèse)"]
    ROM -- "instr (8 bits)" --> SPLIT{{"opcode = instr(7..4)<br/>imm4 = instr(3..0)"}}
    SPLIT --> DECEXE["décode / execute"]
    SPLIT --> PCNXT["logique PC next"]
    DECEXE --> R0["R0 (8 bits)"]
    PCNXT --> PCN["PC_next"]
```

## Ce que le squelette vous donne

Un CPU complet et fonctionnel. Lisez le starter attentivement :

```vhdl
process(clk, reset)
begin
  if reset = '1' then
    pc   <= (others => '0');
    r0   <= (others => '0');
    halt <= '0';
  elsif rising_edge(clk) then
    if halt = '0' then
      case instr(7 downto 4) is
        when "0000" => r0 <= resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0001" => r0 <= r0 + resize(unsigned(instr(3 downto 0)), 8); pc <= pc + 1;
        when "0010" => pc <= unsigned(instr(3 downto 0));
        when "1111" => halt <= '1';
        when others => pc <= pc + 1;
      end case;
    end if;
  end if;
end process;
```

Il implémente déjà :

- LOAD (`0000`)
- ADD  (`0001`)
- JUMP (`0010`)
- HALT (`1111`)

La ROM préchargée exécute `R0 = 3 ; R0 += 4 ; R0 += 2 ; HALT`.

## Votre boulot

Lancez-le tel quel d'abord. Simulez pendant 200 µs avec les stimuli d'horloge + reset fournis. Dans l'onglet Formes d'onde, vous devriez voir `r0_out` passer par `3 → 7 → 9`, puis `halted` monter et y rester.

Une fois la baseline qui marche, **étendez** l'ISA :

### Tâche 1 : SUB

Opcode `0011`. `R0 <= R0 - imm4`. Modification d'une ligne.

### Tâche 2 : Saut conditionnel

Opcode `0100` : `JZ addr` — saute à `addr` si `R0 = 0`, sinon juste `pc <= pc + 1`.

Écrivez un petit programme qui décompte depuis une valeur et boucle quand c'est fini. Utilisez `JZ` comme sortie de boucle.

### Tâche 3 : Un second registre

Ajoutez `r1` comme second registre 8 bits. Définissez `LOAD R1, #i` comme opcode `0101` et `ADD R0, R1` (sans immédiat) comme opcode `0110`. Il vous faudra élargir le case de décodage et ajouter un second port de sortie pour la visibilité.

### Tâche 4 : Mémoire de données externe

Branchez le module RAM du track Mémoires sur ce CPU. Ajoutez les opcodes `LOAD R0, [addr]` et `STORE R0, [addr]`. Attention : un design monocycle exige que le port de lecture mémoire ait sa sortie valide dans le même cycle que vous donnez l'adresse — utilisez une lecture combinatoire pour la ROM et une lecture registrée pour la RAM de données, et planifiez le pipeline soigneusement.

## Pourquoi faire ça ?

Parce que la première fois que vous voyez votre CPU dérouler un programme que vous avez écrit (dans des octets que vous avez écrits dans une ROM), quelque chose clique. Toute abstraction au-dessus — assembleur, compilateur, système d'exploitation — repose exactement sur ce mécanisme. Et chacune, jusqu'au navigateur qui exécute le JavaScript que vous avez écrit la semaine dernière, c'est la même astuce répétée à plus grande échelle.

## Pour aller plus loin

- Implémentez le pipeline classique cinq étages sur cette même ISA. Ajoutez la détection d'aléas et une unité de forwarding. Vous avez alors une version micro de ce que les manuels RISC-V enseignent.
- Reciblez la ROM : au lieu de constantes, chargez-la depuis un fichier externe à la simulation, puis construisez une toolchain qui assemble du texte vers ces octets.
- Branchez l'UART TX du projet précédent et faites imprimer `'HI'` par le CPU dans le chronogramme. Félicitations — vous avez un SoC.
