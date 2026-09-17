// Client-side persistence for the scoreboard, backed by a real SQLite database
// compiled to WebAssembly (sql.js). The database lives in memory while the app
// runs and is exported to IndexedDB after every write so state survives reloads
// and browser/Pi restarts.

let sqlPromise = null;
let db = null;

const DB_STORE_NAME = "scoreboard-sqlite";
const DB_STORE_KEY = "scoreboard.db";
const OBJECT_STORE = "files";

function openMetaDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_STORE_NAME, 1);
        request.onupgradeneeded = () => {
            request.result.createObjectStore(OBJECT_STORE);
        };
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function loadPersistedBytes() {
    const idb = await openMetaDb();
    return new Promise((resolve, reject) => {
        const tx = idb.transaction(OBJECT_STORE, "readonly");
        const request = tx.objectStore(OBJECT_STORE).get(DB_STORE_KEY);
        request.onsuccess = () => resolve(request.result ?? null);
        request.onerror = () => reject(request.error);
    });
}

async function persistBytes(bytes) {
    const idb = await openMetaDb();
    return new Promise((resolve, reject) => {
        const tx = idb.transaction(OBJECT_STORE, "readwrite");
        tx.objectStore(OBJECT_STORE).put(bytes, DB_STORE_KEY);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function persist() {
    await persistBytes(db.export());
}

function loadSqlJsScript() {
    return new Promise((resolve, reject) => {
        if (window.initSqlJs) {
            resolve();
            return;
        }
        const script = document.createElement("script");
        script.src = "lib/sqljs/sql-wasm.js";
        script.onload = () => resolve();
        script.onerror = () => reject(new Error("Failed to load sql-wasm.js"));
        document.head.appendChild(script);
    });
}

export async function init() {
    if (db) {
        return;
    }

    if (!sqlPromise) {
        sqlPromise = loadSqlJsScript().then(() =>
            window.initSqlJs({ locateFile: file => `lib/sqljs/${file}` })
        );
    }
    const SQL = await sqlPromise;

    const existing = await loadPersistedBytes();
    db = existing ? new SQL.Database(new Uint8Array(existing)) : new SQL.Database();

    db.run(`
        CREATE TABLE IF NOT EXISTS scoreboard_state (
            id INTEGER PRIMARY KEY CHECK (id = 1),
            player1_name TEXT,
            player2_name TEXT,
            player1_score INTEGER,
            player2_score INTEGER,
            inning INTEGER,
            match_target INTEGER,
            player1_avg REAL,
            player1_high_run INTEGER,
            current_points INTEGER,
            player2_avg REAL,
            player2_high_run INTEGER,
            shot_clock_seconds INTEGER,
            shot_clock_remaining REAL,
            shot_clock_active INTEGER,
            updated_at TEXT,
            synced INTEGER DEFAULT 0
        );
    `);

    // Older local databases (created before the shot clock was added, or before
    // player2_best_avg was renamed to player2_avg) won't match this shape yet -
    // migrate them in place so existing browsers/Pis don't lose their local state.
    let columns = db.exec("PRAGMA table_info(scoreboard_state)")[0]?.values.map(v => v[1]) ?? [];
    if (columns.includes("player2_best_avg") && !columns.includes("player2_avg")) {
        db.run("ALTER TABLE scoreboard_state RENAME COLUMN player2_best_avg TO player2_avg");
        columns = db.exec("PRAGMA table_info(scoreboard_state)")[0]?.values.map(v => v[1]) ?? [];
    }
    if (!columns.includes("shot_clock_seconds")) {
        db.run("ALTER TABLE scoreboard_state ADD COLUMN shot_clock_seconds INTEGER DEFAULT 40");
    }
    if (!columns.includes("shot_clock_remaining")) {
        db.run("ALTER TABLE scoreboard_state ADD COLUMN shot_clock_remaining REAL DEFAULT 40");
    }
    if (!columns.includes("shot_clock_active")) {
        db.run("ALTER TABLE scoreboard_state ADD COLUMN shot_clock_active INTEGER DEFAULT 0");
    }

    await persist();
}

export async function saveState(stateJson) {
    const s = JSON.parse(stateJson);

    db.run(
        `INSERT INTO scoreboard_state (
            id, player1_name, player2_name, player1_score, player2_score,
            inning, match_target, player1_avg, player1_high_run, current_points,
            player2_avg, player2_high_run, shot_clock_seconds, shot_clock_remaining,
            shot_clock_active, updated_at, synced
        ) VALUES (1, $p1n, $p2n, $p1s, $p2s, $inn, $mt, $p1avg, $p1hr, $cp, $p2avg, $p2hr,
            $scs, $scr, $sca, $updated, 0)
        ON CONFLICT(id) DO UPDATE SET
            player1_name = excluded.player1_name,
            player2_name = excluded.player2_name,
            player1_score = excluded.player1_score,
            player2_score = excluded.player2_score,
            inning = excluded.inning,
            match_target = excluded.match_target,
            player1_avg = excluded.player1_avg,
            player1_high_run = excluded.player1_high_run,
            current_points = excluded.current_points,
            player2_avg = excluded.player2_avg,
            player2_high_run = excluded.player2_high_run,
            shot_clock_seconds = excluded.shot_clock_seconds,
            shot_clock_remaining = excluded.shot_clock_remaining,
            shot_clock_active = excluded.shot_clock_active,
            updated_at = excluded.updated_at,
            synced = 0;`,
        {
            $p1n: s.player1Name,
            $p2n: s.player2Name,
            $p1s: s.player1Score,
            $p2s: s.player2Score,
            $inn: s.inning,
            $mt: s.matchTarget,
            $p1avg: s.player1Avg,
            $p1hr: s.player1HighRun,
            $cp: s.currentPoints,
            $p2avg: s.player2BestAvg,
            $p2hr: s.player2HighRun,
            $scs: s.shotClockSeconds,
            $scr: s.shotClockRemaining,
            $sca: s.shotClockActive ? 1 : 0,
            $updated: s.updatedAt,
        }
    );

    await persist();
}

export async function loadState() {
    const res = db.exec("SELECT * FROM scoreboard_state WHERE id = 1");
    if (!res.length) {
        return null;
    }

    const { columns, values } = res[0];
    const row = {};
    columns.forEach((c, i) => (row[c] = values[0][i]));
    return JSON.stringify(row);
}

export async function isUnsynced() {
    const res = db.exec("SELECT synced FROM scoreboard_state WHERE id = 1");
    return res.length > 0 && res[0].values[0][0] === 0;
}

export async function markSynced() {
    db.run("UPDATE scoreboard_state SET synced = 1 WHERE id = 1");
    await persist();
}

export function getClientId() {
    return localStorage.getItem("scoreboard-client-id");
}

export function setClientId(id) {
    localStorage.setItem("scoreboard-client-id", id);
}
