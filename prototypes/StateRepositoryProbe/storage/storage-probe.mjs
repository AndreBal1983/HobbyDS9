// Isolated storage-engine experiment. No game, .NET-provider or Unity dependency.
import { DatabaseSync, backup } from 'node:sqlite';
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as os from 'node:os';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { fork } from 'node:child_process';
import assert from 'node:assert/strict';

const here = path.dirname(fileURLToPath(import.meta.url));
const sha = bytes => createHash('sha256').update(bytes).digest('hex');
const size = p => fs.existsSync(p) ? fs.statSync(p).size : 0;
const timed = fn => { const t = performance.now(); const value = fn(); return { ms: performance.now() - t, value }; };
const rounded = n => Math.round(n * 1000) / 1000;
const chunkSize = 64 * 1024;
function open(file) {
  const db = new DatabaseSync(file);
  db.exec('PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL; PRAGMA busy_timeout=1000; PRAGMA wal_autocheckpoint=0;');
  return db;
}
function schema(db) {
  db.exec(`
    PRAGMA user_version=1;
    CREATE TABLE snapshots(id TEXT PRIMARY KEY, parent TEXT REFERENCES snapshots(id), hash TEXT NOT NULL,
      byte_length INTEGER NOT NULL CHECK(byte_length>=0), payload BLOB, mode TEXT NOT NULL CHECK(mode IN ('full','chunks')));
    CREATE TABLE chunks(hash TEXT PRIMARY KEY, payload BLOB NOT NULL);
    CREATE TABLE manifest(snapshot TEXT NOT NULL REFERENCES snapshots(id), ordinal INTEGER NOT NULL,
      chunk TEXT NOT NULL REFERENCES chunks(hash), PRIMARY KEY(snapshot,ordinal));
    CREATE TABLE branches(id TEXT PRIMARY KEY, head TEXT REFERENCES snapshots(id), epoch INTEGER NOT NULL DEFAULT 0);
    CREATE TABLE refs(id TEXT PRIMARY KEY, kind TEXT NOT NULL CHECK(kind IN ('manual','auto')),
      branch TEXT NOT NULL REFERENCES branches(id), snapshot TEXT NOT NULL REFERENCES snapshots(id), sequence INTEGER NOT NULL);
    CREATE TRIGGER immutable_snapshot BEFORE UPDATE ON snapshots BEGIN SELECT RAISE(ABORT,'immutable snapshot'); END;
    INSERT INTO branches(id) VALUES('main');
  `);
  db.exec('PRAGMA wal_checkpoint(TRUNCATE)');
}
function store(db, id, parent, bytes, mode) {
  db.prepare('INSERT INTO snapshots VALUES(?,?,?,?,?,?)').run(id, parent, sha(bytes), bytes.length, mode === 'full' ? bytes : null, mode);
  let inserted = mode === 'full' ? bytes.length : 0;
  if (mode === 'chunks') {
    for (let offset = 0, ordinal = 0; offset < bytes.length; offset += chunkSize, ordinal++) {
      const part = bytes.subarray(offset, offset + chunkSize), hash = sha(part);
      const result = db.prepare('INSERT OR IGNORE INTO chunks VALUES(?,?)').run(hash, part);
      if (result.changes) inserted += part.length;
      else assert.deepEqual(Buffer.from(db.prepare('SELECT payload FROM chunks WHERE hash=?').get(hash).payload), part);
      db.prepare('INSERT INTO manifest VALUES(?,?,?)').run(id, ordinal, hash);
    }
  }
  return inserted;
}
function commit(db, { id, branch = 'main', parent, expectedEpoch, bytes, mode = 'full', refKind = 'auto', sequence = 0 }) {
  db.exec('BEGIN IMMEDIATE');
  try {
    const inserted = store(db, id, parent, bytes, mode);
    const cas = db.prepare('UPDATE branches SET head=?,epoch=epoch+1 WHERE id=? AND head IS ? AND epoch=?')
      .run(id, branch, parent, expectedEpoch);
    if (cas.changes !== 1) throw new Error('StaleHead');
    db.prepare('INSERT INTO refs VALUES(?,?,?,?,?)').run(id, refKind, branch, id, sequence);
    db.exec('COMMIT');
    return inserted;
  } catch (e) { db.exec('ROLLBACK'); throw e; }
}
function restore(db, id) {
  const row = db.prepare('SELECT * FROM snapshots WHERE id=?').get(id);
  assert.ok(row);
  const bytes = row.mode === 'full' ? Buffer.from(row.payload) : Buffer.concat(db.prepare(
    'SELECT c.hash,c.payload FROM manifest m JOIN chunks c ON c.hash=m.chunk WHERE m.snapshot=? ORDER BY m.ordinal')
    .all(id).map(c => { const b = Buffer.from(c.payload); assert.equal(sha(b), c.hash); return b; }));
  assert.equal(bytes.length, row.byte_length); assert.equal(sha(bytes), row.hash);
  return bytes;
}
function stats(db, file) {
  const scalar = sql => Object.values(db.prepare(sql).get())[0];
  return { mainFileBytes: size(file), walFileBytes: size(file + '-wal'), shmFileBytes: size(file + '-shm'),
    logicalDatabaseBytes: scalar('PRAGMA page_count') * scalar('PRAGMA page_size'),
    freePages: scalar('PRAGMA freelist_count'),
    storedPayloadBytes: scalar('SELECT coalesce(sum(length(payload)),0) FROM snapshots') + scalar('SELECT coalesce(sum(length(payload)),0) FROM chunks'),
    snapshotCount: scalar('SELECT count(*) FROM snapshots'), chunkCount: scalar('SELECT count(*) FROM chunks') };
}
function durableReplace(file, bytes) {
  const temp = file + '.temporary';
  const fd = fs.openSync(temp, 'wx');
  try { fs.writeFileSync(fd, bytes); fs.fsyncSync(fd); } finally { fs.closeSync(fd); }
  fs.renameSync(temp, file);
  // Windows directory entry flush / power-loss durability is NOT established here.
}
function jsonBench(dir, a, b) {
  fs.mkdirSync(dir);
  const file = path.join(dir, 'save.json');
  const first = timed(() => durableReplace(file, a));
  const next = timed(() => durableReplace(file, b));
  assert.deepEqual(fs.readFileSync(file), b);
  return { firstMs: first.ms, successorMs: next.ms, submittedPayloadBytes: a.length + b.length,
    finalFileBytes: size(file), retainedSnapshots: 1 };
}
function jsonCatalogBench(dir, a, b) {
  fs.mkdirSync(dir); const roots = [];
  function save(id, bytes) {
    durableReplace(path.join(dir, id + '.json'), bytes);
    roots.push({ id, hash: sha(bytes), bytes: bytes.length });
    // Publish a complete replacement catalog only after its root exists.
    durableReplace(path.join(dir, 'catalog.json'), Buffer.from(JSON.stringify({ head: id, roots })));
  }
  const first = timed(() => save('a', a)); const next = timed(() => save('b', b));
  const catalog = JSON.parse(fs.readFileSync(path.join(dir, 'catalog.json'), 'utf8'));
  assert.equal(catalog.head, 'b');
  for (const [i, root] of catalog.roots.entries()) assert.deepEqual(fs.readFileSync(path.join(dir, root.id + '.json')), [a,b][i]);
  return { firstMs: first.ms, successorMs: next.ms, submittedPayloadBytes: a.length + b.length,
    finalFileBytes: fs.readdirSync(dir).reduce((n, f) => n + size(path.join(dir, f)), 0), retainedSnapshots: 2 };
}
function sqlBench(dir, a, b, mode) {
  fs.mkdirSync(dir); const file = path.join(dir, 'save.db'), db = open(file); schema(db);
  const start = stats(db, file);
  const first = timed(() => commit(db, { id: 'a', parent: null, expectedEpoch: 0, bytes: a, mode }));
  const afterFirst = stats(db, file);
  const next = timed(() => commit(db, { id: 'b', parent: 'a', expectedEpoch: 1, bytes: b, mode, sequence: 1 }));
  const beforeCheckpoint = stats(db, file);
  const restored = timed(() => restore(db, 'b')); assert.deepEqual(restored.value, b);
  assert.deepEqual(restore(db, 'a'), a);
  const checkpoint = timed(() => db.exec('PRAGMA wal_checkpoint(TRUNCATE)'));
  const afterCheckpoint = stats(db, file); db.close();
  const reopened = open(file); assert.deepEqual(restore(reopened, 'b'), b); reopened.close();
  return { firstMs: first.ms, successorMs: next.ms, restoreMs: restored.ms, checkpointMs: checkpoint.ms,
    firstNewPayloadBytes: first.value, successorNewPayloadBytes: next.value,
    firstWalGrowthBytes: afterFirst.walFileBytes - start.walFileBytes,
    successorWalGrowthBytes: beforeCheckpoint.walFileBytes - afterFirst.walFileBytes,
    beforeCheckpoint, afterCheckpoint, retainedSnapshots: 2 };
}
async function killUncommitted(file) {
  const child = fork(fileURLToPath(import.meta.url), ['--crash-child', file], { silent: true, execArgv: [] });
  let staged = false, walBytes = 0, output = '';
  child.stderr.on('data', b => { output += b; });
  return await new Promise((resolve, reject) => {
    const timer = setTimeout(() => { child.kill('SIGKILL'); reject(new Error('Crash child timeout: ' + output)); }, 10000);
    child.once('error', e => { clearTimeout(timer); reject(e); });
    child.on('message', msg => {
      if (msg.stage === 'before-commit') { staged = true; walBytes = size(file + '-wal'); child.kill('SIGKILL'); }
    });
    child.once('exit', (code, signal) => {
      clearTimeout(timer);
      if (!staged) reject(new Error('Crash child did not stage: ' + output));
      else resolve({ staged, exitCode: code, signal, walBytesBeforeKill: walBytes });
    });
  });
}
async function proofs(dir, a, b) {
  fs.mkdirSync(dir); const file = path.join(dir, 'proof.db'); let db = open(file); schema(db);
  commit(db, { id: 'a', parent: null, expectedEpoch: 0, bytes: a, refKind: 'manual' });
  commit(db, { id: 'b', parent: 'a', expectedEpoch: 1, bytes: b, sequence: 1 });
  // Insertion happens before CAS so this also proves rollback of a tentative payload.
  assert.throws(() => commit(db, { id: 'stale', parent: 'a', expectedEpoch: 1, bytes: b }), /StaleHead/);
  assert.equal(db.prepare("SELECT count(*) AS n FROM snapshots WHERE id='stale'").get().n, 0);
  assert.throws(() => db.exec("UPDATE snapshots SET hash='changed' WHERE id='a'"), /immutable/);
  db.prepare('INSERT INTO branches VALUES(?,?,0)').run('fork', 'a');
  commit(db, { id: 'fork-b', branch: 'fork', parent: 'a', expectedEpoch: 0, bytes: b, sequence: 2 });
  assert.equal(db.prepare("SELECT head FROM branches WHERE id='main'").get().head, 'b');
  assert.equal(db.prepare("SELECT head FROM branches WHERE id='fork'").get().head, 'fork-b');
  commit(db, { id: 'c', parent: 'b', expectedEpoch: 2, bytes: b, sequence: 3 });
  db.exec("DELETE FROM refs WHERE kind='auto' AND branch='main' AND id NOT IN (SELECT id FROM refs WHERE kind='auto' AND branch='main' ORDER BY sequence DESC,id DESC LIMIT 1)");
  assert.equal(db.prepare("SELECT count(*) AS n FROM refs WHERE kind='manual'").get().n, 1);
  assert.equal(db.prepare("SELECT count(*) AS n FROM refs WHERE kind='auto' AND branch='main'").get().n, 1);
  assert.deepEqual(restore(db, 'a'), a);
  // A second connection cannot obtain the reserved writer while the first owns it.
  const second = open(file); second.exec('PRAGMA busy_timeout=1'); db.exec('BEGIN IMMEDIATE');
  assert.throws(() => second.exec('BEGIN IMMEDIATE'), /locked/); db.exec('ROLLBACK'); second.close();
  const backupFile = path.join(dir, 'backup.db'); await backup(db, backupFile);
  const copy = open(backupFile); assert.deepEqual(restore(copy, 'c'), b); copy.close();
  const expected = db.prepare('SELECT id,head,epoch FROM branches ORDER BY id').all();
  const rows = db.prepare('SELECT count(*) AS n FROM snapshots').get().n;
  db.exec('PRAGMA wal_checkpoint(TRUNCATE)'); db.close();
  const crash = await killUncommitted(file);
  db = open(file);
  assert.deepEqual(db.prepare('SELECT id,head,epoch FROM branches ORDER BY id').all(), expected);
  assert.equal(db.prepare('SELECT count(*) AS n FROM snapshots').get().n, rows);
  assert.deepEqual(restore(db, 'c'), b);
  const integrity = db.prepare('PRAGMA integrity_check').get().integrity_check;
  assert.equal(integrity, 'ok'); assert.equal(db.prepare('PRAGMA foreign_key_check').all().length, 0); db.close();
  return { staleCasRollsBackPayload: true, immutableSnapshotUpdateRejected: true,
    branchHeadsIndependent: true, manualRefSurvivesAutoRetention: true,
    secondWriterBlocked: true, backupRestoreEqual: true, killedBeforeCommitRecoveryEqual: true,
    integrity, crash, limitations: 'Branch continuations reuse opaque input bytes; not distinct gameplay simulations. Retention removes refs only, not snapshots/chunk garbage collection. Process kill is not power-loss testing.' };
}
function percentile(values, p) {
  const a = [...values].sort((x,y) => x-y);
  return rounded(p === .5 && a.length % 2 === 0 ? (a[a.length/2-1] + a[a.length/2])/2 : a[Math.ceil(p * a.length) - 1]);
}
function summary(rows) {
  const result = { samples: rows.length };
  for (const key of ['firstMs','successorMs','restoreMs','checkpointMs']) if (key in rows[0])
    result[key] = { median: percentile(rows.map(r=>r[key]), .5), p95: percentile(rows.map(r=>r[key]), .95), p99: percentile(rows.map(r=>r[key]), .99) };
  result.firstSampleSizes = Object.fromEntries(Object.entries(rows[0]).filter(([key]) => !key.endsWith('Ms')));
  return result;
}
function synthetic() {
  const state = { label: 'SYNTHETIC STORAGE BYTES; NOT A GAME SAVE', revision: 1,
    rows: Array.from({length: 4000}, (_, i) => ({ id: `fixture-${String(i).padStart(5,'0')}`, value: 100, padding: 'x'.repeat(200) })) };
  const a = Buffer.from(JSON.stringify(state)); state.revision = 2; state.rows[2000].value = 101;
  return [a, Buffer.from(JSON.stringify(state))];
}
if (process.argv[2] === '--crash-child') {
  const db = open(process.argv[3]); db.exec('PRAGMA cache_size=8; BEGIN IMMEDIATE');
  const bytes = Buffer.alloc(2 * 1024 * 1024, 81);
  store(db, 'uncommitted', 'c', bytes, 'full');
  db.exec("UPDATE branches SET head='uncommitted',epoch=epoch+1 WHERE id='main'");
  process.send({ stage: 'before-commit' }); setInterval(() => {}, 1000);
} else {
  const args = Object.fromEntries(Array.from({length: (process.argv.length-2)/2}, (_, i) => [process.argv[2+i*2], process.argv[3+i*2]]));
  const inputs = args['--a'] && args['--b'] ? [fs.readFileSync(args['--a']), fs.readFileSync(args['--b'])] : synthetic();
  const samples = Number(args['--samples'] || 12); assert.ok(Number.isInteger(samples) && samples >= 1 && samples <= 100);
  const local = path.join(here, '.local'); fs.mkdirSync(local, {recursive:true});
  const run = fs.mkdtempSync(path.join(local, 'run-'));
  const all = { json: [], jsonCatalog: [], full: [], chunks: [] };
  for (let n = 0; n < samples; n++) {
    all.json.push(jsonBench(path.join(run, `json-${n}`), ...inputs));
    all.jsonCatalog.push(jsonCatalogBench(path.join(run, `json-catalog-${n}`), ...inputs));
    all.full.push(sqlBench(path.join(run, `full-${n}`), ...inputs, 'full'));
    all.chunks.push(sqlBench(path.join(run, `chunks-${n}`), ...inputs, 'chunks'));
  }
  const env = new DatabaseSync(':memory:'); const sqliteVersion = env.prepare('SELECT sqlite_version() AS v').get().v; env.close();
  const result = {
    label: args['--label'] || 'synthetic', atUtc: new Date().toISOString(), node: process.version, sqliteVersion,
    platform: `${os.platform()} ${os.release()} ${os.arch()}`, cpu: os.cpus()[0]?.model, logicalCpus: os.cpus().length,
    totalMemoryBytes: os.totalmem(), storagePath: run, chunkSizeBytes: chunkSize,
    inputs: inputs.map(b => ({ bytes: b.length, sha256: sha(b) })), samples,
    settings: { journal: 'WAL', synchronous: 'FULL', foreignKeys: true, autoCheckpoint: 0, controlledCheckpointAfterEachPair: true },
    metrics: Object.fromEntries(Object.entries(all).map(([k,v]) => [k, summary(v)])),
    proofs: await proofs(path.join(run, 'proofs'), ...inputs),
    limitations: [
      'Node storage-engine proof only; not Microsoft.Data.Sqlite, SQLitePCLRaw, Unity, IL2CPP or native-plugin packaging evidence.',
      'Pre-exported opaque bytes; includes hash/chunk preparation in SQLite commit timing but excludes game staging, validation, export and JSON serialization.',
      'JSON timing covers file write+fsync+same-directory rename and retains only latest; jsonCatalog keeps both roots plus a replacement catalog, comparable retention to SQLite. Neither JSON path implements complete slot/branch/CAS manager semantics.',
      'No physical-device write/flush counters, disk utilization, GC pause or cold-cache control. WAL growth/file sizes are not actual device bytes written.',
      'Sequential methods, fresh files, repeated identical A/B pair. With 12 samples p95/p99 both equal maximum; no reliable tail-latency or general speed conclusion.',
      'Chunk manifests reconstruct exact bytes with length/root/chunk hashes; hashes are integrity checks, not authentication. Fixed boundaries may lose dedup after insertions.',
      'No production activation; no complete schema migration, disk-full/read-only, corruption injection, chunk GC, power-loss or gameplay replay proof.'
    ]
  };
  const output = args['--output'] || path.join(run, 'summary.json');
  fs.mkdirSync(path.dirname(path.resolve(output)), {recursive:true}); fs.writeFileSync(output, JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify({ output, inputs: result.inputs, metrics: result.metrics, proofs: result.proofs }, null, 2));
}
