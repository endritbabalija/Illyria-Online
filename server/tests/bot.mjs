// A second player for trying the Unity client online without a second device: logs in as its own
// device, joins the same zone channel, follows the first other player it sees and fights whatever
// monster that player is next to. Prints what it sees, then a summary.
//
//   node server/tests/bot.mjs --url http://127.0.0.1:7350 --seconds 90
//   options: --url (default NAKAMA_URL or http://127.0.0.1:17350), --seconds 60, --zone oak_valley,
//            --character zanas_chosen, --name "Bora Bot", --device wuxia-bot-1,
//            --cast swift_cut (a skill form to use on its target every --castEvery 6 s; spends the
//            skill points its book needs first), --dashEvery 0 (seconds between ground dashes; 0 = never),
//            --swap 1 (wield the other weapon first, e.g. the Kreshnik's bow)
//
// Node 22+, no dependencies.

const args = {};
for (let i = 2; i + 1 < process.argv.length; i += 2) args[process.argv[i].replace(/^--/, '')] = process.argv[i + 1];
if (args.url) process.env.NAKAMA_URL = args.url;
const { BASE, Op, sleep, login, rpc, Client, waitFor } = await import('./nakama.mjs');

const seconds = Number(args.seconds || 60);
const zone = args.zone || 'oak_valley';
const characterId = args.character || 'zanas_chosen';
const name = args.name || 'Bora Bot';
const device = args.device || 'wuxia-bot-1';
const castForm = args.cast || null;
const castEvery = Number(args.castEvery || 6);
const dashEvery = Number(args.dashEvery || 0);

const token = await login(device);
const list = await rpc(token, 'character_list');
if (!list.characters.some((c) => c.characterId === characterId)) await rpc(token, 'character_create', { characterId, name });
const join = await rpc(token, 'zone_join', { zone });
const bot = new Client(name, token);
await bot.connect();
for (let attempt = 0; ; attempt++) {
    try { await bot.join(join.matchId, characterId); break; } catch (e) {
        if (attempt >= 8) throw e;
        console.log('join refused (' + e.message + '), retrying');
        await sleep(2000);
    }
}
console.log(`${name} joined ${zone} channel ${join.channel} at ${BASE}; ${bot.players.size - 1} other player(s) here`);

if (args.swap) bot.send(Op.Action, { a: 'swap' });
if (castForm) {
    const { readFileSync } = await import('node:fs');
    const skills = JSON.parse(readFileSync(new URL('../../shared/gamedata/skills.json', import.meta.url), 'utf8'));
    const book = skills.books.find((b) => b.forms.some((f) => f.id === castForm));
    if (!book) throw new Error('no form ' + castForm);
    const form = book.forms.find((f) => f.id === castForm);
    const have = (bot.self.skills.books.find((b) => b.id === book.id) || { level: 0 }).level;
    for (let level = have; level < form.unlockLevel; level++) bot.send(Op.Action, { a: 'levelBook', book: book.id });
    console.log(`using ${castForm} (${book.id} to level ${Math.max(have, form.unlockLevel)})`);
}
let nextCast = Date.now() + 2000, nextDash = Date.now() + dashEvery * 1000;

let target = null, leaderName = null;
const seenEvents = new Set();
const end = Date.now() + seconds * 1000;
while (Date.now() < end) {
    const leader = [...bot.players.values()].find((p) => p.id !== bot.you);
    const me = bot.me();
    if (leader && me && !me.dead) {
        if (leaderName !== leader.name) { leaderName = leader.name; console.log('following ' + leader.name); }
        // Walk at the leader's ground height once close; far off (just revived at the Han) their
        // height says nothing about ours, and the server corrects any step that's off.
        if (Math.hypot(leader.x - me.x, leader.z - me.z) < 4) me.y = leader.y;
        const near = [...bot.monsters.values()]
            .filter((m) => m.state !== 'dead' && Math.hypot(m.x - leader.x, m.z - leader.z) < 6)
            .sort((a, b) => Math.hypot(a.x - leader.x, a.z - leader.z) - Math.hypot(b.x - leader.x, b.z - leader.z))[0];
        if (near) {
            if (target !== near.id) {
                target = near.id;
                bot.send(Op.Target, { id: target });
                console.log(`targeting ${near.monster} ${near.id} next to ${leader.name}`);
            }
            bot.stepToward(near.x + 1, near.z, 1.2);
            if (castForm && Date.now() >= nextCast && Math.hypot(near.x - me.x, near.z - me.z) < 3) {
                nextCast = Date.now() + castEvery * 1000;
                bot.send(Op.Cast, { form: castForm, target: near.id });
                console.log(`  cast ${castForm} at ${near.monster}`);
            }
        } else {
            target = null;
            bot.stepToward(leader.x + 2, leader.z, 2);
            if (dashEvery > 0 && Date.now() >= nextDash) {
                nextDash = Date.now() + dashEvery * 1000;
                bot.send(Op.Leap, { dir: [leader.x - me.x || 1, leader.z - me.z] });
                console.log('  dash');
            }
        }
    }
    for (const e of bot.events) {
        if (seenEvents.has(e)) continue;
        seenEvents.add(e);
        const who = (id) => id === bot.you ? name : (bot.players.get(id) || {}).name || (bot.monsters.get(id) || {}).monster || id;
        if (e.k === 'hit' && (e.src === bot.you || e.dst === bot.you || (target && e.dst === target)))
            console.log(`  hit ${who(e.src)} -> ${who(e.dst)}: ${e.miss ? 'miss' : e.dmg + (e.crit ? '!' : '')} (${e.kind})`);
        if (e.k === 'die') console.log(`  ${who(e.id)} died${e.by ? ' (kill: ' + who(e.by) + ')' : ''}`);
        if (e.k === 'join' || e.k === 'leave') console.log(`  ${e.k}: ${e.player ? e.player.name : who(e.id)}`);
    }
    await sleep(100);
}

const hits = bot.events.filter((e) => e.k === 'hit');
const shared = new Set(hits.filter((e) => e.src === bot.you).map((e) => e.dst));
const leaderHitsOnShared = hits.filter((e) => e.src !== bot.you && bot.players.has(e.src) && shared.has(e.dst)).length;
console.log(`summary: ${name} hit ${shared.size} monster(s) ${hits.filter((e) => e.src === bot.you).length} time(s); ` +
    `other players hit those same monsters ${leaderHitsOnShared} time(s); kills credited to ${name}: ${bot.self ? bot.self.kills : '?'}`);
await Promise.race([bot.request({ match_leave: { match_id: bot.matchId } }).catch(() => { }), sleep(1000)]);
bot.close();
process.exit(0);
