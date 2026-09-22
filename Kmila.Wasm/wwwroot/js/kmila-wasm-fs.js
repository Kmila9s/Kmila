// kmila-wasm-fs.js — durability for the Blazor WebAssembly demo.
//
// The .NET wasm runtime keeps the app's writable tree (/kmila: the SQLite DB +
// every project's VHDL files) in an in-memory Emscripten filesystem that is
// wiped on reload. This module mirrors that tree to IndexedDB so a returning
// visitor finds their projects intact. BrowserFsSync.cs (C#) owns the policy;
// this file is just the store:
//   kmilaFs.hydrate()            -> [{path, data(base64)}]  (all persisted files)
//   kmilaFs.persist(entries)     -> replace the store with this full snapshot
//   kmilaFs.registerFlush(ref)   -> flush once more on pagehide / tab-hide
(function () {
    const DB_NAME = 'kmila-fs';
    const STORE = 'files';

    function openDb() {
        return new Promise((resolve, reject) => {
            const req = indexedDB.open(DB_NAME, 1);
            req.onupgradeneeded = () => {
                const db = req.result;
                if (!db.objectStoreNames.contains(STORE)) {
                    db.createObjectStore(STORE, { keyPath: 'path' });
                }
            };
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    async function hydrate() {
        try {
            const db = await openDb();
            return await new Promise((resolve, reject) => {
                const tx = db.transaction(STORE, 'readonly');
                const rq = tx.objectStore(STORE).getAll();
                rq.onsuccess = () => resolve(rq.result || []);
                rq.onerror = () => reject(rq.error);
            });
        } catch (e) {
            console.warn('[kmilaFs] hydrate failed', e);
            return [];
        }
    }

    async function persist(entries) {
        try {
            const db = await openDb();
            await new Promise((resolve, reject) => {
                const tx = db.transaction(STORE, 'readwrite');
                const st = tx.objectStore(STORE);
                st.clear();
                for (const e of (entries || [])) st.put(e);
                tx.oncomplete = () => resolve();
                tx.onerror = () => reject(tx.error);
            });
        } catch (e) {
            console.warn('[kmilaFs] persist failed', e);
        }
    }

    let flushRef = null;
    function registerFlush(dotNetRef) {
        flushRef = dotNetRef;
        const flush = () => { try { flushRef && flushRef.invokeMethodAsync('FlushNowAsync'); } catch (e) { /* best effort */ } };
        window.addEventListener('pagehide', flush);
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'hidden') flush();
        });
    }

    window.kmilaFs = { hydrate, persist, registerFlush };
})();
