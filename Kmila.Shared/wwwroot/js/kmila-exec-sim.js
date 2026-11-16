/*
 * kmila-exec-sim.js — the Scratch-style exercises' interpreter (Concepts › Execution models).
 *
 * Runs ANY block program the student builds (not just pre-baked answers) inside a small "world"
 * and returns a lesson-player timeline (ExecutionTimelineDoc shape), a verdict and stars:
 *
 *   window.kmilaExecSim.run(world, goalJson, workspaceJson, lang) -> JSON string
 *       { ok, stars, verdict:{lang:text}, timeline:{code,language,signalOrder,steps}, plan? }
 *   window.kmilaExecSim.code(world, workspaceJson, lang)            -> program listing / VHDL
 *   window.kmilaExecSim.stars(exerciseId[, n])                      -> best stars (per device)
 *   window.kmilaExecSim.showPlan(plan)                              -> drives the restaurant figure
 *
 * Worlds: recipe (e1), kitchen (e2, parallel), logic (e3), clocked (e4), restaurant (e5), signals (e6).
 * Also installs window.kmilaBlockText (block-label i18n hook used by kmila-blocks.js) and defines
 * the new block types for e5/e6. Must load AFTER kmila-blocks.js.
 */
(function () {
    'use strict';
    const LANGS = ['en', 'es', 'fr', 'de', 'ch', 'jp', 'ar'];
    const curLang = () => {
        const l = (document.documentElement.getAttribute('lang') || 'en').toLowerCase();
        if (l.startsWith('zh')) return 'ch';
        if (l.startsWith('ja')) return 'jp';
        return LANGS.includes(l.slice(0, 2)) ? l.slice(0, 2) : 'en';
    };

    /* ───────────────────────── block labels (i18n, no emoji) ───────────────────────── */
    const BT = {
        '🍕 Knead the dough':     ['Knead the dough', 'Amasar la masa', 'Pétrir la pâte', 'Teig kneten', '揉面团', '生地をこねる', 'اعجن العجين'],
        '🥫 Spread the sauce':    ['Spread the sauce', 'Untar la salsa', 'Étaler la sauce', 'Soße verteilen', '抹酱', 'ソースを塗る', 'افرد الصلصة'],
        '🧀 Add the cheese':      ['Add the cheese', 'Agregar el queso', 'Ajouter le fromage', 'Käse darauf', '加奶酪', 'チーズをのせる', 'أضف الجبن'],
        '🌿 Add the toppings':    ['Add the toppings', 'Agregar los ingredientes', 'Ajouter la garniture', 'Belag darauf', '加配料', '具材をのせる', 'أضف الإضافات'],
        '🔥 Bake at 220°C':       ['Bake at 220 °C', 'Hornear a 220 °C', 'Cuire à 220 °C', 'Bei 220 °C backen', '220 °C 烘烤', '220 °C で焼く', 'اخبز على 220 °م'],
        '🍕 Slice and serve':     ['Slice and serve', 'Rebanar y servir', 'Découper et servir', 'Schneiden und servieren', '切块上桌', '切って出す', 'قطّع وقدّم'],
        '🥕 Chop the vegetables': ['Chop the vegetables', 'Picar las verduras', 'Couper les légumes', 'Gemüse schneiden', '切蔬菜', '野菜を切る', 'قطّع الخضار'],
        '🥩 Marinate the meat':   ['Marinate the meat', 'Marinar la carne', 'Mariner la viande', 'Fleisch marinieren', '腌肉', '肉を漬け込む', 'تبّل اللحم'],
        '🍳 Fry the eggs':        ['Fry the eggs', 'Freír los huevos', 'Faire frire les œufs', 'Eier braten', '煎鸡蛋', '卵を焼く', 'اقلِ البيض'],
        '🍝 Boil the pasta':      ['Boil the pasta', 'Hervir la pasta', 'Cuire les pâtes', 'Nudeln kochen', '煮意面', 'パスタをゆでる', 'اسلق المعكرونة'],
        'repeat while there is work:': ['repeat while there is work:', 'repetir mientras haya trabajo:', 'répéter tant qu\'il y a du travail :', 'wiederholen, solange es Arbeit gibt:', '有活就重复：', '仕事がある間くり返す：', 'كرّر ما دام هناك عمل:'],
        'take the next order':   ['take the next order', 'tomar la siguiente orden', 'prendre la commande suivante', 'nächste Bestellung aufnehmen', '接下一桌的点单', '次の注文を取る', 'خذ الطلب التالي'],
        'wait at the table until its dish is ready': ['wait at the table until its dish is ready', 'esperar en la mesa hasta que el platillo esté listo', 'attendre à la table que le plat soit prêt', 'am Tisch warten, bis das Gericht fertig ist', '在桌边等到菜做好', '料理ができるまでその卓で待つ', 'انتظر عند الطاولة حتى يجهز طبقها'],
        'go round and serve the ready dishes': ['go round and serve the ready dishes', 'dar una vuelta y servir lo que esté listo', 'faire le tour et servir les plats prêts', 'eine Runde drehen und Fertiges servieren', '巡一圈，上做好的菜', '一周してできた料理を出す', 'لُفّ وقدّم الأطباق الجاهزة'],
        'rest until a bell rings, then serve that table': ['rest until a bell rings, then serve that table', 'descansar hasta que suene una campana y servir esa mesa', 'se reposer jusqu\'à une sonnette, puis servir cette table', 'ruhen, bis eine Klingel läutet, dann diesen Tisch bedienen', '休息到铃响，再去上那桌的菜', 'ベルが鳴るまで休み、その卓に出す', 'استرح حتى يرنّ جرس، ثم قدّم لتلك الطاولة'],
    };
    window.kmilaBlockText = function (text) {
        const row = BT[text];
        if (!row) return text;
        return row[Math.max(0, LANGS.indexOf(curLang()))] || row[0];
    };
    const stepName = (label, lang) => { const row = BT[label]; return row ? row[Math.max(0, LANGS.indexOf(lang))] : label; };
    const STEP_LABEL = { knead: '🍕 Knead the dough', sauce: '🥫 Spread the sauce', cheese: '🧀 Add the cheese', toppings: '🌿 Add the toppings', bake: '🔥 Bake at 220°C', serve: '🍕 Slice and serve', chop: '🥕 Chop the vegetables', marinate: '🥩 Marinate the meat', fry: '🍳 Fry the eggs', boil: '🍝 Boil the pasta' };

    /* ───────────────────────── new block types (e5 restaurant, e6 signals) ───────────────────────── */
    function defineNewBlocks() {
        const def = window.kmilaBlocksDefine;
        if (typeof def !== 'function') return false;
        def('kmila_rs_repeat', { style: 'clock', prev: 'kmila_rs', next: 'kmila_rs', rows: [[{ kind: 'label', text: 'repeat while there is work:' }]], statement: { name: 'BODY', check: 'kmila_rs' } });
        def('kmila_rs_take',  { style: 'step', prev: 'kmila_rs', next: 'kmila_rs', rows: [[{ kind: 'label', text: 'take the next order' }]] });
        def('kmila_rs_wait',  { style: 'step', prev: 'kmila_rs', next: 'kmila_rs', rows: [[{ kind: 'label', text: 'wait at the table until its dish is ready' }]] });
        def('kmila_rs_serve', { style: 'step', prev: 'kmila_rs', next: 'kmila_rs', rows: [[{ kind: 'label', text: 'go round and serve the ready dishes' }]] });
        def('kmila_rs_bell',  { style: 'accent', prev: 'kmila_rs', next: 'kmila_rs', rows: [[{ kind: 'label', text: 'rest until a bell rings, then serve that table' }]] });
        def('kmila_sig_assign', { style: 'accent', prev: 'kmila_sig', next: 'kmila_sig', rows: [[
            { kind: 'dropdown', name: 'TARGET', options: [['tmp :=', 'tmp'], ['s <=', 's'], ['result <=', 'result']] },
            { kind: 'value', name: 'VALUE', check: 'kmila_sexpr' }]] });
        def('kmila_sig_op', { style: 'gate', output: 'kmila_sexpr', inline: true, rows: [[
            { kind: 'value', name: 'LHS', check: 'kmila_sexpr' },
            { kind: 'dropdown', name: 'OP', options: [['+', '+'], ['*', '*']] },
            { kind: 'value', name: 'RHS', check: 'kmila_sexpr' }]] });
        def('kmila_sig_ref', { style: 'signal', output: 'kmila_sexpr', rows: [[
            { kind: 'dropdown', name: 'NAME', options: [['a', 'a'], ['b', 'b'], ['tmp', 'tmp'], ['s', 's'], ['1', '1'], ['2', '2']] }]] });
        return true;
    }
    if (!defineNewBlocks()) document.addEventListener('DOMContentLoaded', defineNewBlocks);

    /* ───────────────────────── messages (7 languages) ───────────────────────── */
    // Each entry: [en, es, fr, de, ch, jp, ar]; {name} placeholders.
    const M = {
        empty: ['Add some blocks first, then press Run.', 'Primero agrega bloques y luego presiona Ejecutar.', 'Ajoute d\'abord des blocs, puis appuie sur Exécuter.', 'Füge zuerst Blöcke hinzu und drücke dann Ausführen.', '先添加积木，再点击运行。', 'まずブロックを置いてから実行を押そう。', 'أضف بعض الكتل أولًا ثم اضغط تشغيل.'],
        emptySlot: ['A block has an empty slot — fill every slot.', 'Un bloque tiene un hueco vacío: llena todos los huecos.', 'Un bloc a un emplacement vide : remplis-les tous.', 'Ein Block hat einen leeren Platz — fülle alle Plätze.', '有积木的插槽是空的——把每个插槽填满。', '空いている穴があるブロックがある。すべて埋めよう。', 'في إحدى الكتل خانة فارغة — املأ كل الخانات.'],
        // recipe
        r_start: ['The kitchen is empty; the chef is about to begin.', 'La cocina está vacía; el chef está por empezar.', 'La cuisine est vide ; le chef va commencer.', 'Die Küche ist leer; gleich fängt der Koch an.', '厨房空着，厨师准备开工。', '厨房は空っぽ。シェフがこれから始める。', 'المطبخ فارغ، والطاهي على وشك البدء.'],
        r_do: ['Step {n}: {step}. Each step waits for the previous one — that is sequential.', 'Paso {n}: {step}. Cada paso espera al anterior: eso es secuencial.', 'Étape {n} : {step}. Chaque étape attend la précédente : c\'est séquentiel.', 'Schritt {n}: {step}. Jeder Schritt wartet auf den vorherigen — das ist sequenziell.', '第 {n} 步：{step}。每一步都等上一步完成——这就是顺序执行。', '手順 {n}：{step}。各手順は前の手順を待つ——これが順次。', 'الخطوة {n}: {step}. كل خطوة تنتظر السابقة — هذا هو التسلسل.'],
        r_need: ['Step {n}: you can\'t "{step}" yet — "{need}" hasn\'t happened. Order matters!', 'Paso {n}: aún no puedes "{step}": "{need}" todavía no ha ocurrido. ¡El orden importa!', 'Étape {n} : impossible de « {step} » — « {need} » n\'a pas encore eu lieu. L\'ordre compte !', 'Schritt {n}: „{step}" geht noch nicht — „{need}" ist noch nicht passiert. Die Reihenfolge zählt!', '第 {n} 步：还不能「{step}」——「{need}」还没做。顺序很重要！', '手順 {n}：まだ「{step}」はできない——「{need}」がまだだ。順番が大事！', 'الخطوة {n}: لا يمكنك «{step}» بعد — «{need}» لم يحدث. الترتيب مهم!'],
        r_dup: ['Step {n}: "{step}" again? It is already done — this line changes nothing.', 'Paso {n}: ¿"{step}" otra vez? Ya está hecho: esta línea no cambia nada.', 'Étape {n} : « {step} » encore ? C\'est déjà fait — cette ligne ne change rien.', 'Schritt {n}: Nochmal „{step}"? Ist schon erledigt — diese Zeile ändert nichts.', '第 {n} 步：又「{step}」？已经做过了——这一行什么也没改变。', '手順 {n}：また「{step}」？もう済んでいる——この行は何も変えない。', 'الخطوة {n}: «{step}» مرة أخرى؟ تم ذلك بالفعل — هذا السطر لا يغيّر شيئًا.'],
        r_extra: ['Step {n}: "{step}" is not part of this recipe — it runs, but the pizza does not need it.', 'Paso {n}: "{step}" no es parte de esta receta: se ejecuta, pero la pizza no lo necesita.', 'Étape {n} : « {step} » ne fait pas partie de cette recette — ça s\'exécute, mais la pizza n\'en a pas besoin.', 'Schritt {n}: „{step}" gehört nicht zu diesem Rezept — es läuft, aber die Pizza braucht es nicht.', '第 {n} 步：「{step}」不属于这个食谱——会执行，但披萨用不到。', '手順 {n}：「{step}」はこのレシピに含まれない——実行はされるがピザには不要。', 'الخطوة {n}: «{step}» ليست جزءًا من هذه الوصفة — تُنفَّذ، لكن البيتزا لا تحتاجها.'],
        r_ok: ['Served! Every step ran in a valid order.', '¡Servida! Cada paso corrió en un orden válido.', 'Servie ! Chaque étape s\'est exécutée dans un ordre valide.', 'Serviert! Jeder Schritt lief in einer gültigen Reihenfolge.', '上桌了！每一步都按有效顺序执行。', '完成！すべての手順が正しい順序で実行された。', 'قُدِّمت! نُفِّذت كل خطوة بترتيب صحيح.'],
        r_missing: ['The recipe ended before the pizza was served — missing: {miss}.', 'La receta terminó antes de servir la pizza; falta: {miss}.', 'La recette s\'est terminée avant que la pizza soit servie — il manque : {miss}.', 'Das Rezept endete, bevor die Pizza serviert wurde — es fehlt: {miss}.', '披萨还没上桌食谱就结束了——缺少：{miss}。', 'ピザが出る前にレシピが終わった——足りないもの：{miss}。', 'انتهت الوصفة قبل تقديم البيتزا — ينقص: {miss}.'],
        // kitchen
        k_start: ['Four cooks wait at four stations.', 'Cuatro cocineros esperan en cuatro estaciones.', 'Quatre cuisiniers attendent à quatre postes.', 'Vier Köche warten an vier Stationen.', '四位厨师站在四个工位前待命。', '4 人の料理人が 4 つの持ち場で待っている。', 'أربعة طهاة ينتظرون عند أربع محطات.'],
        k_go: ['Every task starts at the same instant — one cook per task, all working in parallel.', 'Todas las tareas empiezan en el mismo instante: un cocinero por tarea, todos en paralelo.', 'Toutes les tâches démarrent au même instant — un cuisinier par tâche, tous en parallèle.', 'Alle Aufgaben starten im selben Moment — ein Koch pro Aufgabe, alle parallel.', '所有任务同时开始——每个任务一位厨师，全部并行。', 'すべての作業が同じ瞬間に始まる——作業 1 つに料理人 1 人、全員が並列に。', 'تبدأ كل المهام في اللحظة نفسها — طاهٍ لكل مهمة، والجميع يعمل بالتوازي.'],
        k_queue: ['There are more tasks than cooks: {n} task(s) wait for a free station.', 'Hay más tareas que cocineros: {n} tarea(s) esperan una estación libre.', 'Il y a plus de tâches que de cuisiniers : {n} tâche(s) attendent un poste libre.', 'Es gibt mehr Aufgaben als Köche: {n} Aufgabe(n) warten auf eine freie Station.', '任务比厨师多：{n} 个任务要等空出的工位。', '作業が料理人より多い：{n} 件が空いた持ち場を待つ。', 'المهام أكثر من الطهاة: {n} مهمة تنتظر محطة شاغرة.'],
        k_done: ['{task} is ready (minute {min}).', '{task} está listo (minuto {min}).', '{task} est prêt (minute {min}).', '{task} ist fertig (Minute {min}).', '{task} 完成（第 {min} 分钟）。', '{task} ができた（{min} 分）。', '{task} جاهز (الدقيقة {min}).'],
        k_ok: ['Everything is ready after {total} min — one cook doing it all in sequence would need {seq} min.', 'Todo está listo a los {total} min; un solo cocinero en secuencia necesitaría {seq} min.', 'Tout est prêt après {total} min — un seul cuisinier, en séquence, mettrait {seq} min.', 'Alles ist nach {total} min fertig — ein einzelner Koch nacheinander bräuchte {seq} min.', '{total} 分钟后全部完成——一位厨师按顺序做需要 {seq} 分钟。', '{total} 分ですべて完成——1 人で順番にやると {seq} 分かかる。', 'كل شيء جاهز بعد {total} دقيقة — طاهٍ واحد بالتسلسل يحتاج {seq} دقيقة.'],
        k_missing: ['Some dishes were never started: {miss}.', 'Algunos platillos nunca se empezaron: {miss}.', 'Certains plats n\'ont jamais été commencés : {miss}.', 'Einige Gerichte wurden nie begonnen: {miss}.', '有些菜根本没开始做：{miss}。', '始めてもいない料理がある：{miss}。', 'بعض الأطباق لم تبدأ أبدًا: {miss}.'],
        // logic
        l_start: ['Cold start: inputs and outputs are U.', 'Inicio en frío: entradas y salidas en U.', 'Démarrage à froid : entrées et sorties à U.', 'Kaltstart: Ein- und Ausgänge sind U.', '冷启动：输入和输出都是 U。', 'コールドスタート：入力も出力も U。', 'بداية باردة: المداخل والمخارج U.'],
        l_inputs: ['Inputs change: a = {a}, b = {b}.', 'Las entradas cambian: a = {a}, b = {b}.', 'Les entrées changent : a = {a}, b = {b}.', 'Eingänge ändern sich: a = {a}, b = {b}.', '输入变化：a = {a}，b = {b}。', '入力が変わる：a = {a}、b = {b}。', 'تتغيّر المداخل: a = {a}، b = {b}.'],
        l_eval: ['All assignments evaluate together, in one delta cycle.', 'Todas las asignaciones se evalúan juntas, en un ciclo delta.', 'Toutes les affectations sont évaluées ensemble, dans un cycle delta.', 'Alle Zuweisungen werden zusammen ausgewertet, in einem Delta-Zyklus.', '所有赋值在同一个 delta 周期里一起求值。', 'すべての代入が 1 つのデルタサイクルで一緒に評価される。', 'تُقيَّم كل الإسنادات معًا في دورة دلتا واحدة.'],
        l_commit: ['Delta ends: {outs}.', 'Fin del delta: {outs}.', 'Fin du delta : {outs}.', 'Delta endet: {outs}.', 'delta 结束：{outs}。', 'デルタ終了：{outs}。', 'تنتهي الدلتا: {outs}.'],
        l_wrong: ['{out} is wrong for a = {a}, b = {b}: got {got}, expected {exp}.', '{out} es incorrecto con a = {a}, b = {b}: da {got}, se esperaba {exp}.', '{out} est faux pour a = {a}, b = {b} : obtenu {got}, attendu {exp}.', '{out} ist falsch für a = {a}, b = {b}: {got} statt {exp}.', 'a = {a}、b = {b} 时 {out} 不对：得到 {got}，应为 {exp}。', 'a = {a}、b = {b} のとき {out} が違う：{got} だが {exp} のはず。', '{out} خاطئ عند a = {a}، b = {b}: الناتج {got} والمتوقع {exp}.'],
        l_missing: ['Nothing drives {out}: add an assignment for it.', 'Nada maneja {out}: agrega una asignación para ella.', 'Rien ne pilote {out} : ajoute une affectation.', 'Nichts treibt {out}: füge eine Zuweisung hinzu.', '没有任何语句驱动 {out}：给它加一个赋值。', '{out} を駆動するものがない：代入を追加しよう。', 'لا شيء يقود {out}: أضف إسنادًا لها.'],
        l_ok: ['The truth table matches a half-adder: sum = a xor b, cout = a and b.', 'La tabla de verdad coincide con un medio sumador: sum = a xor b, cout = a and b.', 'La table de vérité correspond à un demi-additionneur : sum = a xor b, cout = a and b.', 'Die Wahrheitstabelle passt zu einem Halbaddierer: sum = a xor b, cout = a and b.', '真值表与半加器一致：sum = a xor b，cout = a and b。', '真理値表が半加算器と一致：sum = a xor b、cout = a and b。', 'جدول الحقيقة يطابق نصف الجامع: sum = a xor b، cout = a and b.'],
        // clocked
        c_start: ['Cold start: count = {c}.', 'Inicio en frío: count = {c}.', 'Démarrage à froid : count = {c}.', 'Kaltstart: count = {c}.', '冷启动：count = {c}。', 'コールドスタート：count = {c}。', 'بداية باردة: count = {c}.'],
        c_edge: ['Rising edge #{n}: the clocked process wakes up.', 'Flanco de subida #{n}: el proceso con reloj despierta.', 'Front montant n°{n} : le process cadencé se réveille.', 'Steigende Flanke Nr. {n}: der getaktete Prozess wacht auf.', '第 {n} 个上升沿：时钟进程被唤醒。', '立ち上がりエッジ #{n}：クロック付き process が起きる。', 'الحافة الصاعدة رقم {n}: يستيقظ الـ process المتزامن مع الساعة.'],
        c_commit: ['Delta ends: count = {c}.', 'Fin del delta: count = {c}.', 'Fin du delta : count = {c}.', 'Delta endet: count = {c}.', 'delta 结束：count = {c}。', 'デルタ終了：count = {c}。', 'تنتهي الدلتا: count = {c}.'],
        c_free: ['count <= count + 1 sits outside any clocked process: it feeds itself forever (a combinational loop), so the simulation never settles.', 'count <= count + 1 está fuera de un proceso con reloj: se alimenta a sí mismo sin fin (un lazo combinacional) y la simulación nunca se estabiliza.', 'count <= count + 1 est hors de tout process cadencé : il se nourrit lui-même sans fin (boucle combinatoire) et la simulation ne se stabilise jamais.', 'count <= count + 1 steht außerhalb eines getakteten Prozesses: Es speist sich endlos selbst (kombinatorische Schleife), die Simulation kommt nie zur Ruhe.', 'count <= count + 1 不在任何时钟进程里：它会无限地自己驱动自己（组合环路），仿真永远无法稳定。', 'count <= count + 1 がクロック付き process の外にある：自分自身を延々と駆動する（組み合わせループ）ので、シミュレーションが落ち着かない。', 'count <= count + 1 خارج أي process متزامن مع الساعة: يغذّي نفسه بلا نهاية (حلقة تركيبية)، فلا تستقر المحاكاة أبدًا.'],
        c_noinc: ['The clocked process has an empty body — count never changes.', 'El proceso con reloj tiene el cuerpo vacío: count nunca cambia.', 'Le process cadencé a un corps vide — count ne change jamais.', 'Der getaktete Prozess ist leer — count ändert sich nie.', '时钟进程的主体是空的——count 永远不变。', 'クロック付き process の中身が空——count は変わらない。', 'جسم الـ process المتزامن فارغ — لا تتغيّر count أبدًا.'],
        c_by: ['The counter advances by {k} on every edge; the goal is +1.', 'El contador avanza {k} en cada flanco; la meta es +1.', 'Le compteur avance de {k} à chaque front ; l\'objectif est +1.', 'Der Zähler springt bei jeder Flanke um {k}; Ziel ist +1.', '计数器每个边沿加 {k}；目标是 +1。', 'カウンタはエッジごとに {k} 進む。目標は +1。', 'يتقدّم العدّاد بمقدار {k} عند كل حافة؛ والهدف +1.'],
        c_twice: ['count <= count + 1 appears {k} times, but all of them read the OLD count and the last assignment wins: the counter still advances by 1, not {k}.', 'count <= count + 1 aparece {k} veces, pero todas leen el count VIEJO y gana la última asignación: el contador sigue avanzando 1, no {k}.', 'count <= count + 1 apparaît {k} fois, mais toutes lisent l\'ANCIEN count et la dernière affectation gagne : le compteur avance toujours de 1, pas de {k}.', 'count <= count + 1 steht {k}-mal da, aber alle lesen den ALTEN count und die letzte Zuweisung gewinnt: Der Zähler springt trotzdem nur um 1, nicht um {k}.', 'count <= count + 1 出现了 {k} 次，但它们读的都是旧的 count，最后一次赋值生效：计数器仍然只加 1，而不是 {k}。', 'count <= count + 1 が {k} 回あるが、どれも古い count を読み、最後の代入が勝つ：カウンタは {k} ではなく 1 しか進まない。', 'يظهر count <= count + 1 {k} مرات، لكنها كلها تقرأ count القديمة وآخر إسناد هو الذي يفوز: يتقدّم العدّاد بمقدار 1 فقط لا {k}.'],
        c_ok: ['count goes 0 → 1 → 2 → 3 → 4: exactly one step per rising edge.', 'count va 0 → 1 → 2 → 3 → 4: un paso exacto por flanco de subida.', 'count va de 0 → 1 → 2 → 3 → 4 : un pas par front montant.', 'count läuft 0 → 1 → 2 → 3 → 4: genau ein Schritt pro steigender Flanke.', 'count 依次为 0 → 1 → 2 → 3 → 4：每个上升沿正好加一。', 'count は 0 → 1 → 2 → 3 → 4：立ち上がりエッジごとにちょうど 1 つ。', 'تمرّ count بـ 0 → 1 → 2 → 3 → 4: خطوة واحدة تمامًا لكل حافة صاعدة.'],
        // restaurant
        s_start: ['Ten tables, one waitress, nobody served yet.', 'Diez mesas, una mesera, nadie servido aún.', 'Dix tables, une serveuse, personne n\'est encore servi.', 'Zehn Tische, eine Kellnerin, noch niemand bedient.', '十张桌子，一名服务员，还没人上菜。', '10 卓、ウェイトレス 1 人、まだ誰も料理をもらっていない。', 'عشر طاولات ونادلة واحدة، ولم يُخدَم أحد بعد.'],
        s_take: ['Takes the order of table {i}.', 'Toma la orden de la mesa {i}.', 'Prend la commande de la table {i}.', 'Nimmt die Bestellung von Tisch {i} auf.', '接第 {i} 桌的点单。', '{i} 番卓の注文を取る。', 'تأخذ طلب الطاولة {i}.'],
        s_serve: ['Serves table {i}.', 'Sirve la mesa {i}.', 'Sert la table {i}.', 'Bedient Tisch {i}.', '给第 {i} 桌上菜。', '{i} 番卓に料理を出す。', 'تقدّم للطاولة {i}.'],
        s_check: ['Checks table {i}: not ready yet — a wasted trip.', 'Revisa la mesa {i}: aún no está listo, un viaje desperdiciado.', 'Vérifie la table {i} : pas encore prêt — un trajet perdu.', 'Schaut bei Tisch {i}: noch nicht fertig — ein umsonst gemachter Weg.', '查看第 {i} 桌：还没好——白跑一趟。', '{i} 番卓を確認：まだできていない——無駄足。', 'تتفقّد الطاولة {i}: ليس جاهزًا بعد — رحلة ضائعة.'],
        s_wait: ['Waits at table {i} for {d} s, doing nothing else.', 'Espera en la mesa {i} durante {d} s sin hacer nada más.', 'Attend à la table {i} pendant {d} s, sans rien faire d\'autre.', 'Wartet {d} s an Tisch {i} und tut nichts anderes.', '在第 {i} 桌干等 {d} 秒，别的什么也不做。', '{i} 番卓で {d} 秒、何もせずに待つ。', 'تنتظر عند الطاولة {i} مدة {d} ث دون أن تفعل شيئًا آخر.'],
        s_rest: ['Rests until the next bell rings ({d} s).', 'Descansa hasta que suena la siguiente campana ({d} s).', 'Se repose jusqu\'à la prochaine sonnette ({d} s).', 'Ruht bis zur nächsten Klingel ({d} s).', '休息到下一次铃响（{d} 秒）。', '次のベルが鳴るまで休む（{d} 秒）。', 'تستريح حتى يرنّ الجرس التالي ({d} ث).'],
        s_done: ['All tables served in {t} s ({checks} wasted checks, {idle} s standing idle).', 'Todas las mesas servidas en {t} s ({checks} revisiones desperdiciadas, {idle} s parada).', 'Toutes les tables servies en {t} s ({checks} vérifications perdues, {idle} s à attendre).', 'Alle Tische in {t} s bedient ({checks} unnötige Kontrollen, {idle} s untätig).', '{t} 秒内服务完所有桌子（白跑 {checks} 次，干站 {idle} 秒）。', '{t} 秒で全卓に提供（無駄な確認 {checks} 回、待ちぼうけ {idle} 秒）。', 'خُدمت كل الطاولات في {t} ث ({checks} تفقّدات ضائعة، و{idle} ث وقوفًا بلا عمل).'],
        s_left: ['{n} table(s) were never served — the program stopped too early.', '{n} mesa(s) nunca se sirvieron: el programa se detuvo antes de tiempo.', '{n} table(s) jamais servie(s) — le programme s\'est arrêté trop tôt.', '{n} Tisch(e) nie bedient — das Programm hat zu früh aufgehört.', '有 {n} 桌一直没上菜——程序停得太早。', '{n} 卓が最後まで料理をもらえなかった——プログラムが早く止まりすぎた。', '{n} طاولة لم تُخدَم أبدًا — توقّف البرنامج مبكرًا جدًا.'],
        s_stars: ['Two stars at 40 s or less, three stars at 26 s or less.', 'Dos estrellas con 40 s o menos, tres con 26 s o menos.', 'Deux étoiles à 40 s ou moins, trois à 26 s ou moins.', 'Zwei Sterne bei höchstens 40 s, drei bei höchstens 26 s.', '40 秒以内得两颗星，26 秒以内得三颗星。', '40 秒以内で星 2 つ、26 秒以内で星 3 つ。', 'نجمتان عند 40 ث أو أقل، وثلاث عند 26 ث أو أقل.'],
        // signals
        g_start: ['Cold start: a = 3, b = 5, tmp = 0, s = 0, result = 0.', 'Inicio en frío: a = 3, b = 5, tmp = 0, s = 0, result = 0.', 'Démarrage à froid : a = 3, b = 5, tmp = 0, s = 0, result = 0.', 'Kaltstart: a = 3, b = 5, tmp = 0, s = 0, result = 0.', '冷启动：a = 3，b = 5，tmp = 0，s = 0，result = 0。', 'コールドスタート：a = 3、b = 5、tmp = 0、s = 0、result = 0。', 'بداية باردة: a = 3، b = 5، tmp = 0، s = 0، result = 0.'],
        g_var: ['tmp := {v} — a variable updates immediately.', 'tmp := {v}: una variable se actualiza de inmediato.', 'tmp := {v} — une variable se met à jour immédiatement.', 'tmp := {v} — eine Variable ändert sich sofort.', 'tmp := {v}——variable 立即更新。', 'tmp := {v}——variable はすぐに更新される。', 'tmp := {v} — المتغيّر يتحدّث فورًا.'],
        g_sig: ['{t} <= {v} is only scheduled: {t} still shows {old} until the delta ends.', '{t} <= {v} solo queda programado: {t} sigue mostrando {old} hasta que termine el delta.', '{t} <= {v} est seulement planifié : {t} affiche encore {old} jusqu\'à la fin du delta.', '{t} <= {v} ist nur eingeplant: {t} zeigt bis zum Delta-Ende weiter {old}.', '{t} <= {v} 只是被排程：在 delta 结束前 {t} 仍显示 {old}。', '{t} <= {v} は予約されただけ：デルタが終わるまで {t} は {old} のまま。', '{t} <= {v} مُجدوَل فقط: يبقى {t} يعرض {old} حتى تنتهي الدلتا.'],
        g_old: ['This line read {n} = {old}: its OLD value — the new one is still pending.', 'Esta línea leyó {n} = {old}: su valor VIEJO; el nuevo sigue pendiente.', 'Cette ligne a lu {n} = {old} : son ANCIENNE valeur — la nouvelle est encore en attente.', 'Diese Zeile las {n} = {old}: den ALTEN Wert — der neue ist noch ausstehend.', '这一行读到的 {n} = {old}：是旧值——新值还在排队。', 'この行が読んだ {n} = {old} は古い値——新しい値はまだ保留中。', 'قرأ هذا السطر {n} = {old}: قيمته القديمة — والجديدة ما زالت معلّقة.'],
        g_commit: ['Delta ends: pending signals commit — {list}.', 'Fin del delta: las señales pendientes se confirman: {list}.', 'Fin du delta : les signaux en attente sont validés — {list}.', 'Delta endet: ausstehende Signale werden übernommen — {list}.', 'delta 结束：排队的 signal 提交——{list}。', 'デルタ終了：保留中の signal が確定——{list}。', 'تنتهي الدلتا: تُثبَّت الإشارات المعلّقة — {list}.'],
        g_none: ['Delta ends: no signal was assigned.', 'Fin del delta: no se asignó ninguna señal.', 'Fin du delta : aucun signal n\'a été affecté.', 'Delta endet: Kein Signal wurde zugewiesen.', 'delta 结束：没有给任何 signal 赋值。', 'デルタ終了：どの signal にも代入されなかった。', 'تنتهي الدلتا: لم تُسنَد أي إشارة.'],
        g_ok: ['result = 16. The variable carried the intermediate value instantly.', 'result = 16. La variable llevó el valor intermedio al instante.', 'result = 16. La variable a transporté la valeur intermédiaire instantanément.', 'result = 16. Die Variable hat den Zwischenwert sofort weitergegeben.', 'result = 16。variable 立刻传递了中间值。', 'result = 16。variable が途中の値をすぐに運んだ。', 'result = 16. نقل المتغيّر القيمة الوسيطة فورًا.'],
        g_bad: ['result = {r}, but the goal is 16.', 'result = {r}, pero la meta es 16.', 'result = {r}, mais l\'objectif est 16.', 'result = {r}, aber das Ziel ist 16.', 'result = {r}，但目标是 16。', 'result = {r}。目標は 16。', 'result = {r}، لكن الهدف 16.'],
        g_badold: ['result = {r}, but the goal is 16. A line read {n} before its new value arrived — keep intermediate values in the variable tmp.', 'result = {r}, pero la meta es 16. Una línea leyó {n} antes de que llegara su nuevo valor: guarda los valores intermedios en la variable tmp.', 'result = {r}, mais l\'objectif est 16. Une ligne a lu {n} avant l\'arrivée de sa nouvelle valeur — garde les valeurs intermédiaires dans la variable tmp.', 'result = {r}, aber das Ziel ist 16. Eine Zeile las {n}, bevor der neue Wert ankam — halte Zwischenwerte in der Variablen tmp.', 'result = {r}，但目标是 16。有一行在新值到达前读取了 {n}——中间值请放在 variable tmp 里。', 'result = {r}。目標は 16。ある行が新しい値の届く前に {n} を読んだ——途中の値は variable の tmp に入れよう。', 'result = {r}، لكن الهدف 16. قرأ سطرٌ {n} قبل وصول قيمته الجديدة — احفظ القيم الوسيطة في المتغيّر tmp.'],
    };
    function N(key, p) { // → {lang: text} for all 7 languages
        const row = M[key] || [key], out = {};
        LANGS.forEach((l, i) => {
            let s = row[i] || row[0];
            for (const k in (p || {})) {
                const v = p[k];
                s = s.split('{' + k + '}').join(typeof v === 'object' && v ? (v[l] || v.en) : v);
            }
            out[l] = s;
        });
        return out;
    }
    const join = (...notes) => { const o = {}; LANGS.forEach((l) => { o[l] = notes.filter(Boolean).map((n) => n[l]).join(' '); }); return o; };
    const L = (fn) => { const o = {}; LANGS.forEach((l) => { o[l] = fn(l); }); return o; };

    /* ───────────────────────── workspace helpers ───────────────────────── */
    function parse(workspaceJson) {
        try { const ws = JSON.parse(workspaceJson || '{}'); return ((ws.blocks || {}).blocks || []).slice().sort((a, b) => (a.y || 0) - (b.y || 0)); }
        catch (e) { return []; }
    }
    const chain = (b) => { const out = []; while (b) { out.push(b); b = b.next && b.next.block; } return out; };
    const flat = (tops) => tops.flatMap(chain);
    const child = (b, name) => b && b.inputs && b.inputs[name] && b.inputs[name].block;
    const field = (b, name) => b && b.fields ? b.fields[name] : undefined;
    const result = (o) => JSON.stringify(o);
    const fail = (key, p, timeline) => result({ ok: false, stars: 0, verdict: N(key, p), timeline: timeline || null });

    /* ───────────────────────── world: recipe (e1, sequential) ───────────────────────── */
    const PIZZA = ['knead', 'sauce', 'cheese', 'bake', 'serve'];
    const NEED = { sauce: 'knead', cheese: 'sauce', toppings: 'sauce', bake: 'cheese', serve: 'bake' };
    const MIN = { knead: 5, sauce: 2, cheese: 1, toppings: 2, bake: 12, serve: 1, chop: 4, marinate: 6, fry: 3, boil: 10 };
    const STAGE = { knead: 'dough', sauce: 'sauce', cheese: 'cheese', toppings: 'toppings', bake: 'baked', serve: 'served' };
    function recipe(tops) {
        const steps = flat(tops).filter((b) => b.type === 'kmila_recipe_step').map((b) => field(b, 'STEP'));
        if (!steps.length) return fail('empty');
        const nm = (s) => L((l) => stepName(STEP_LABEL[s], l));
        const done = new Set(); let t = 0, errors = 0, extras = 0, stage = '—';
        const tl = { code: steps.map((s, i) => (i + 1) + '. ' + stepName(STEP_LABEL[s], 'en')).join('\n'), language: 'plaintext', signalOrder: ['stage', 'elapsed'], steps: [] };
        const push = (lines, note) => tl.steps.push({ t: tl.steps.length, highlightLines: lines, signals: { stage, elapsed: t + ' min' }, note });
        push([], N('r_start'));
        steps.forEach((s, i) => {
            const n = i + 1;
            if (!(s in NEED) && s !== 'knead') { extras++; t += MIN[s] || 1; push([n], N('r_extra', { n, step: nm(s) })); return; }
            if (done.has(s)) { extras++; push([n], N('r_dup', { n, step: nm(s) })); return; }
            if (NEED[s] && !done.has(NEED[s])) { errors++; push([n], N('r_need', { n, step: nm(s), need: nm(NEED[s]) })); return; }
            done.add(s); t += MIN[s]; stage = STAGE[s]; push([n], N('r_do', { n, step: nm(s) }));
        });
        const miss = PIZZA.filter((s) => !done.has(s));
        if (miss.length) { const v = N('r_missing', { miss: L((l) => miss.map((s) => stepName(STEP_LABEL[s], l)).join(', ')) }); push([], v); return result({ ok: false, stars: 0, verdict: v, timeline: tl }); }
        const v = N('r_ok'); push([], v);
        return result({ ok: errors === 0, stars: errors ? 0 : extras ? 2 : 3, verdict: v, timeline: tl });
    }

    /* ───────────────────────── world: kitchen (e2, parallel) ───────────────────────── */
    const DISHES = ['chop', 'marinate', 'fry', 'boil'];
    function kitchen(tops) {
        const tasks = flat(tops).filter((b) => b.type === 'kmila_recipe_step').map((b) => field(b, 'STEP'));
        if (!tasks.length) return fail('empty');
        const nm = (s) => L((l) => stepName(STEP_LABEL[s], l));
        const COOKS = 4, free = Array(COOKS).fill(0), fin = [];
        tasks.forEach((s, i) => { const c = free.indexOf(Math.min(...free)); const start = free[c]; free[c] = start + (MIN[s] || 1); fin.push({ s, i, start, end: free[c] }); });
        const total = Math.max(...fin.map((f) => f.end)), seq = fin.reduce((a, f) => a + (MIN[f.s] || 1), 0);
        const order = ['elapsed', ...tasks.map((s, i) => (i + 1) + '')];
        const tl = { code: tasks.map((s, i) => (i + 1) + '. ' + stepName(STEP_LABEL[s], 'en')).join('\n'), language: 'plaintext', signalOrder: order, steps: [] };
        const sig = (t) => { const o = { elapsed: t + ' min' }; fin.forEach((f) => { o[(f.i + 1) + ''] = t < f.start ? 'waiting' : t < f.end ? 'cooking' : 'ready'; }); return o; };
        tl.steps.push({ t: 0, highlightLines: [], signals: sig(-1), note: N('k_start') });
        const started = fin.filter((f) => f.start === 0).map((f) => f.i + 1);
        const queued = fin.length - started.length;
        tl.steps.push({ t: 1, highlightLines: started, signals: sig(0), note: queued ? join(N('k_go'), N('k_queue', { n: queued })) : N('k_go') });
        [...new Set(fin.map((f) => f.end))].sort((a, b) => a - b).forEach((te) => {
            const ending = fin.filter((f) => f.end === te);
            const running = fin.filter((f) => f.start <= te && f.end > te).map((f) => f.i + 1);
            tl.steps.push({ t: tl.steps.length, highlightLines: running, signals: sig(te), note: join(...ending.map((f) => N('k_done', { task: nm(f.s), min: te }))) });
        });
        const miss = DISHES.filter((d) => !tasks.includes(d));
        if (miss.length) { const v = N('k_missing', { miss: L((l) => miss.map((s) => stepName(STEP_LABEL[s], l)).join(', ')) }); tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: sig(total), note: v }); return result({ ok: false, stars: 0, verdict: v, timeline: tl }); }
        const v = N('k_ok', { total, seq }); tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: sig(total), note: v });
        const extras = tasks.length - DISHES.length;
        return result({ ok: true, stars: extras ? 2 : 3, verdict: v, timeline: tl });
    }

    /* ───────────────────────── world: logic (e3, concurrent half-adder) ───────────────────────── */
    function gateExpr(b) {
        if (!b) return null;
        if (b.type === 'kmila_signal_in') return { k: 'in', n: field(b, 'NAME') || 'a' };
        const op = { kmila_gate_xor: 'xor', kmila_gate_and: 'and', kmila_gate_or: 'or' }[b.type];
        if (!op) return null;
        return { k: 'op', op, l: gateExpr(child(b, 'LHS')), r: gateExpr(child(b, 'RHS')) };
    }
    const exprOk = (e) => !!e && (e.k === 'in' || (exprOk(e.l) && exprOk(e.r)));
    const exprSrc = (e) => !e ? '?' : e.k === 'in' ? e.n : (e.l && e.l.k === 'op' ? '(' + exprSrc(e.l) + ')' : exprSrc(e.l)) + ' ' + e.op + ' ' + (e.r && e.r.k === 'op' ? '(' + exprSrc(e.r) + ')' : exprSrc(e.r));
    const evalG = (e, v) => e.k === 'in' ? v[e.n] : ({ xor: (x, y) => x ^ y, and: (x, y) => x & y, or: (x, y) => x | y })[e.op](evalG(e.l, v), evalG(e.r, v));
    function logicAssigns(tops) { return flat(tops).filter((b) => b.type === 'kmila_output_assign').map((b) => ({ target: field(b, 'TARGET') || 'sum', e: gateExpr(child(b, 'VALUE')) })); }
    function logicCode(as) { return ['architecture rtl of half_adder is', 'begin', ...as.map((a) => '  ' + (a.target === 'sum' ? 'sum ' : 'cout') + ' <= ' + exprSrc(a.e) + ';'), 'end architecture;'].join('\n'); }
    function logic(tops) {
        const as = logicAssigns(tops);
        if (!as.length) return fail('empty');
        if (as.some((a) => !exprOk(a.e))) return fail('emptySlot');
        const tl = { code: logicCode(as), language: 'vhdl', signalOrder: ['a', 'b', 'sum', 'cout'], steps: [] };
        const lines = as.map((_, i) => i + 3);
        let cur = { a: 'U', b: 'U', sum: 'U', cout: 'U' };
        tl.steps.push({ t: 0, highlightLines: [], signals: { ...cur }, note: N('l_start') });
        const wrong = [];
        for (const [a, b] of [[0, 0], [0, 1], [1, 0], [1, 1]]) {
            cur = { ...cur, a: a + '', b: b + '' };
            tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: { ...cur }, note: N('l_inputs', { a, b }) });
            tl.steps.push({ t: tl.steps.length, highlightLines: lines, signals: { ...cur }, note: N('l_eval') });
            for (const x of as) cur[x.target] = evalG(x.e, { a, b }) + '';
            const outs = ['sum', 'cout'].filter((o) => as.some((x) => x.target === o)).map((o) => o + ' = ' + cur[o]).join(', ');
            tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: { ...cur }, note: N('l_commit', { outs }) });
            const exp = { sum: a ^ b, cout: a & b };
            for (const o of ['sum', 'cout']) if (as.some((x) => x.target === o) && cur[o] !== exp[o] + '' && !wrong.some((w) => w.out === o)) wrong.push({ out: o, a, b, got: cur[o], exp: exp[o] });
        }
        const missing = ['sum', 'cout'].filter((o) => !as.some((x) => x.target === o));
        let v;
        if (missing.length) v = N('l_missing', { out: missing.join(', ') });
        else if (wrong.length) v = N('l_wrong', wrong[0]);
        else v = N('l_ok');
        tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: { ...cur }, note: v });
        const ok = !missing.length && !wrong.length;
        const dup = as.length > 2;
        return result({ ok, stars: ok ? (dup ? 2 : 3) : 0, verdict: v, timeline: tl });
    }

    /* ───────────────────────── world: clocked (e4, counter) ───────────────────────── */
    function clockedShape(tops) {
        let k = 0, free = 0, proc = 0;
        for (const b of flat(tops)) {
            if (b.type === 'kmila_on_clock') { proc++; k += chain(child(b, 'BODY')).filter((x) => x.type === 'kmila_increment_count').length; }
            else if (b.type === 'kmila_increment_count') free++;
        }
        return { k, free, proc };
    }
    function clockedCode(sh) {
        const out = ['architecture rtl of counter is', 'begin'];
        if (sh.proc) { out.push('  process(clk)', '  begin', '    if rising_edge(clk) then'); for (let i = 0; i < sh.k; i++) out.push('      count <= count + 1;'); out.push('    end if;', '  end process;'); }
        for (let i = 0; i < sh.free; i++) out.push('  count <= count + 1;  -- no clock');
        out.push('end architecture;');
        return out.join('\n');
    }
    function clocked(tops) {
        const sh = clockedShape(tops);
        if (!sh.proc && !sh.free) return fail('empty');
        const tl = { code: clockedCode(sh), language: 'vhdl', signalOrder: ['clk', 'count'], steps: [] };
        let c = 0;
        tl.steps.push({ t: 0, highlightLines: [], signals: { clk: '0', count: '0' }, note: N('c_start', { c }) });
        if (sh.free) {
            const ln = tl.code.split('\n').findIndex((l) => l.includes('-- no clock')) + 1;
            const v = N('c_free');
            tl.steps.push({ t: 1, highlightLines: [ln], signals: { clk: '0', count: 'X' }, note: v });
            return result({ ok: false, stars: 0, verdict: v, timeline: tl });
        }
        const incLines = []; for (let i = 0; i < sh.k; i++) incLines.push(6 + i);
        for (let e = 1; e <= 4; e++) {
            tl.steps.push({ t: tl.steps.length, highlightLines: [3, 5], signals: { clk: '1', count: c + '' }, note: N('c_edge', { n: e }) });
            if (sh.k) tl.steps.push({ t: tl.steps.length, highlightLines: incLines, signals: { clk: '1', count: c + '' }, note: N('g_sig', { t: 'count', v: c + 1, old: c }) });
            c += sh.k ? 1 : 0; // count <= count + 1 several times in one process: the LAST assignment wins
            tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: { clk: '0', count: c + '' }, note: N('c_commit', { c }) });
        }
        let v, ok = false;
        if (!sh.k) v = N('c_noinc');
        else { v = sh.k > 1 ? join(N('c_twice', { k: sh.k }), N('c_ok')) : N('c_ok'); ok = true; }
        tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: { clk: '0', count: c + '' }, note: v });
        return result({ ok, stars: ok ? (sh.k === 1 ? 3 : 2) : 0, verdict: v, timeline: tl });
    }

    /* ───────────────────────── world: restaurant (e5, scheduling) ───────────────────────── */
    // Same geometry and constants as the exec-restaurant figure (model.js), so the plan replays there.
    const RS = (function () {
        const X = [-104, -52, 0, 52, 104];
        return { SPOTS: [...X.map((x) => [x, -17]), ...X.map((x) => [x, 33])], HOME: [-136, 8], TAKE: 0.6, SERVE: 0.5, CHECK: 0.5, SPEED: 160, COOK: [6.4, 3.2, 11.0, 4.4, 5.6, 2.4, 9.6, 4.0, 2.8, 6.0] };
    })();
    const RS_LINE = { kmila_rs_take: 'take the next order', kmila_rs_wait: 'wait at the table until its dish is ready', kmila_rs_serve: 'go round and serve the ready dishes', kmila_rs_bell: 'rest until a bell rings, then serve that table', kmila_rs_repeat: 'repeat while there is work:' };
    function rsListing(tops, lang) { // numbered program; returns {code, lineOf: Map(blockId→line)}
        const lines = [], lineOf = new Map();
        const walk = (b, depth) => { for (const x of chain(b)) { if (!RS_LINE[x.type]) continue; lines.push('  '.repeat(depth) + stepName(RS_LINE[x.type], lang)); lineOf.set(x.id || x, lines.length); if (x.type === 'kmila_rs_repeat') walk(child(x, 'BODY'), depth + 1); } };
        for (const t of tops) walk(t, 0);
        return { code: lines.join('\n'), lineOf };
    }
    function restaurant(tops, lang) {
        const { code, lineOf } = rsListing(tops, lang);
        if (!lineOf.size) return fail('empty');
        const n = RS.SPOTS.length, tables = Array.from({ length: n }, () => ({ order: -1, ready: -1, served: -1 }));
        const segs = []; let t = 0, at = RS.HOME.slice(), cur = -1, idle = 0, checks = 0, budget = 4000;
        const tl = { code, language: 'plaintext', signalOrder: ['time', 'served', 'checks', 'idle'], steps: [] };
        const snap = (ln, note) => tl.steps.push({ t: tl.steps.length, highlightLines: ln ? [ln] : [], signals: { time: t.toFixed(1) + ' s', served: tables.filter((x) => x.served >= 0).length + '/10', checks: checks + '', idle: idle.toFixed(1) + ' s' }, note });
        const walk = (to, table) => { const d = Math.hypot(to[0] - at[0], to[1] - at[1]); if (d < 0.01) return; const dt = d / RS.SPEED; segs.push({ w: 0, kind: 'walk', table, t0: t, t1: t + dt, from: at.slice(), to: to.slice() }); t += dt; at = to.slice(); };
        const act = (kind, table, dt) => { segs.push({ w: 0, kind, table, t0: t, t1: t + dt, from: at.slice(), to: at.slice() }); if (kind === 'wait') idle += dt; if (kind === 'check') checks++; t += dt; };
        const ops = {
            kmila_rs_take(ln) { const i = tables.findIndex((x) => x.order < 0); if (i < 0) return false; walk(RS.SPOTS[i], i); act('take', i, RS.TAKE); tables[i].order = t; tables[i].ready = t + RS.COOK[i]; cur = i; snap(ln, N('s_take', { i: i + 1 })); return true; },
            kmila_rs_wait(ln) { if (cur < 0 || tables[cur].served >= 0 || tables[cur].ready <= t) return false; walk(RS.SPOTS[cur], cur); const d = tables[cur].ready - t; act('wait', cur, d); snap(ln, N('s_wait', { i: cur + 1, d: d.toFixed(1) })); return true; },
            kmila_rs_serve(ln) { let any = false; for (let i = 0; i < n; i++) { const x = tables[i]; if (x.order < 0 || x.served >= 0) continue; walk(RS.SPOTS[i], i); any = true; if (t >= x.ready) { act('serve', i, RS.SERVE); x.served = t; snap(ln, N('s_serve', { i: i + 1 })); } else { act('check', i, RS.CHECK); snap(ln, N('s_check', { i: i + 1 })); } } return any; },
            kmila_rs_bell(ln) { let best = -1; for (let i = 0; i < n; i++) { const x = tables[i]; if (x.order >= 0 && x.served < 0 && (best < 0 || x.ready < tables[best].ready)) best = i; } if (best < 0) return false; const d = tables[best].ready - t; if (d > 0) { act('wait', -1, d); snap(ln, N('s_rest', { d: d.toFixed(1) })); } walk(RS.SPOTS[best], best); act('serve', best, RS.SERVE); tables[best].served = t; snap(ln, N('s_serve', { i: best + 1 })); return true; },
        };
        const allServed = () => tables.every((x) => x.served >= 0);
        const exec = (b) => { // returns true when something happened
            if (budget-- <= 0) return false;
            const ln = lineOf.get(b.id || b);
            if (b.type === 'kmila_rs_repeat') {
                let any = false;
                for (let guard = 0; guard < 300 && !allServed(); guard++) { let p = false; for (const x of chain(child(b, 'BODY'))) p = exec(x) || p; if (!p) break; any = true; }
                return any;
            }
            return ops[b.type] ? ops[b.type](ln) : false;
        };
        snap(0, N('s_start'));
        for (const top of tops) for (const b of chain(top)) exec(b);
        const left = tables.filter((x) => x.served < 0).length;
        const plan = { mode: 'custom', total: t, segs, tables, idle, checks, waitresses: 1 };
        if (left) { const v = N('s_left', { n: left }); snap(0, v); return result({ ok: false, stars: 0, verdict: v, timeline: tl, plan }); }
        const v = join(N('s_done', { t: t.toFixed(1), checks, idle: idle.toFixed(1) }), N('s_stars'));
        snap(0, N('s_done', { t: t.toFixed(1), checks, idle: idle.toFixed(1) }));
        return result({ ok: true, stars: t <= 26 ? 3 : t <= 40 ? 2 : 1, verdict: v, timeline: tl, plan });
    }

    /* ───────────────────────── world: signals (e6, variable vs signal) ───────────────────────── */
    function sexpr(b) {
        if (!b) return null;
        if (b.type === 'kmila_sig_ref') return { k: 'ref', n: field(b, 'NAME') || 'a' };
        if (b.type === 'kmila_sig_op') return { k: 'op', op: field(b, 'OP') || '+', l: sexpr(child(b, 'LHS')), r: sexpr(child(b, 'RHS')) };
        return null;
    }
    const sOk = (e) => !!e && (e.k === 'ref' || (sOk(e.l) && sOk(e.r)));
    const sSrc = (e) => !e ? '?' : e.k === 'ref' ? e.n : (e.l && e.l.k === 'op' ? '(' + sSrc(e.l) + ')' : sSrc(e.l)) + ' ' + e.op + ' ' + (e.r && e.r.k === 'op' ? '(' + sSrc(e.r) + ')' : sSrc(e.r));
    const sRefs = (e) => !e ? [] : e.k === 'ref' ? [e.n] : [...sRefs(e.l), ...sRefs(e.r)];
    function sigStmts(tops) { return flat(tops).filter((b) => b.type === 'kmila_sig_assign').map((b) => ({ target: field(b, 'TARGET') || 'tmp', e: sexpr(child(b, 'VALUE')) })); }
    function sigCode(st) { return ['process(a, b)', '  variable tmp : integer := 0;', 'begin', ...st.map((s) => '  ' + s.target + (s.target === 'tmp' ? ' := ' : ' <= ') + sSrc(s.e) + ';'), 'end process;'].join('\n'); }
    function signals(tops) {
        const st = sigStmts(tops);
        if (!st.length) return fail('empty');
        if (st.some((s) => !sOk(s.e))) return fail('emptySlot');
        const tl = { code: sigCode(st), language: 'vhdl', signalOrder: ['a', 'b', 'tmp', 's', 'result'], steps: [] };
        const sig = { a: 3, b: 5, s: 0, result: 0 }, pend = {}; let tmp = 0, readOld = null;
        const show = () => ({ a: sig.a + '', b: sig.b + '', tmp: tmp + '', s: sig.s + (pend.s !== undefined ? ' → ' + pend.s : ''), result: sig.result + (pend.result !== undefined ? ' → ' + pend.result : '') });
        tl.steps.push({ t: 0, highlightLines: [], signals: show(), note: N('g_start') });
        const val = (e) => e.k === 'ref' ? (e.n === '1' ? 1 : e.n === '2' ? 2 : e.n === 'tmp' ? tmp : sig[e.n]) : (e.op === '+' ? val(e.l) + val(e.r) : val(e.l) * val(e.r));
        st.forEach((s, i) => {
            const v = val(s.e);
            const stale = sRefs(s.e).find((r) => (r === 's' || r === 'result') && pend[r] !== undefined);
            if (stale && !readOld) readOld = stale;
            let note;
            if (s.target === 'tmp') { tmp = v; note = N('g_var', { v }); }
            else { const old = sig[s.target]; pend[s.target] = v; note = N('g_sig', { t: s.target, v, old }); }
            if (stale) note = join(note, N('g_old', { n: stale, old: sig[stale] }));
            tl.steps.push({ t: tl.steps.length, highlightLines: [i + 4], signals: show(), note });
        });
        const list = Object.keys(pend).map((k) => k + ' = ' + pend[k]).join(', ');
        Object.assign(sig, pend); for (const k of Object.keys(pend)) delete pend[k];
        tl.steps.push({ t: tl.steps.length, highlightLines: [st.length + 4], signals: show(), note: list ? N('g_commit', { list }) : N('g_none') });
        const ok = sig.result === 16;
        const v = ok ? N('g_ok') : readOld ? N('g_badold', { r: sig.result, n: readOld }) : N('g_bad', { r: sig.result });
        tl.steps.push({ t: tl.steps.length, highlightLines: [], signals: show(), note: v });
        return result({ ok, stars: ok ? (st.length <= 3 ? 3 : 2) : 0, verdict: v, timeline: tl });
    }

    /* ───────────────────────── public API ───────────────────────── */
    const WORLDS = { recipe, kitchen, logic, clocked, restaurant, signals };
    window.kmilaExecSim = {
        run(world, goalJson, workspaceJson, lang) {
            const fn = WORLDS[world];
            if (!fn) return null;
            try { return fn(parse(workspaceJson), LANGS.includes(lang) ? lang : curLang()); }
            catch (e) { console.error('[kmilaExecSim]', e); return null; }
        },
        code(world, workspaceJson, lang) {
            const tops = parse(workspaceJson), lg = LANGS.includes(lang) ? lang : curLang();
            try {
                if (world === 'logic') { const as = logicAssigns(tops); return as.length ? logicCode(as) : ''; }
                if (world === 'clocked') { const sh = clockedShape(tops); return sh.proc || sh.free ? clockedCode(sh) : ''; }
                if (world === 'signals') { const st = sigStmts(tops); return st.length ? sigCode(st) : ''; }
                if (world === 'restaurant') return rsListing(tops, lg).code;
                if (world === 'recipe' || world === 'kitchen') return flat(tops).filter((b) => b.type === 'kmila_recipe_step').map((b, i) => (i + 1) + '. ' + stepName(STEP_LABEL[field(b, 'STEP')], lg)).join('\n');
            } catch (e) { /* half-built program — show nothing */ }
            return '';
        },
        stars(id, n) {
            const key = 'kmila.exercise.stars.' + id;
            try {
                const best = +(localStorage.getItem(key) || 0);
                if (n != null && +n > best) { localStorage.setItem(key, String(+n)); return +n; }
                return best;
            } catch (e) { return n != null ? +n : 0; }
        },
        showPlan(plan) {
            document.querySelectorAll('.kmila-exercise-figure iframe').forEach((f) => {
                try { f.contentWindow && f.contentWindow.postMessage({ kmilaFig: 'plan', plan }, '*'); } catch (e) { }
            });
        },
    };
})();
