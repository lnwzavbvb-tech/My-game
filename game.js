// COUNTER / ARENA - Enhanced Combat Demo
// Feature 1: Enemy attacks NEVER interrupt/lock out player attacks
// Feature 2: Attack Command combos (LMB, RMB, sequences like L-L-L, L-H, L-L-H, H-L, H-H)
// Feature 3: Enemy consecutive attack chains / multi-hit combo strings

const $ = id => document.getElementById(id);
const canvas = $('game');
const ctx = canvas.getContext('2d');

// --- ACTIONS CONFIG ---
const actions = [
    ['LMB / Space', 'Light Attack', 'ฟันเร็ว • ผสมคอมโบ', 'light'],
    ['RMB / F', 'Heavy Attack', 'ฟันหนัก / แทงทะลวง', 'heavy'],
    ['Shift', 'Dash', 'Stamina 25 • หลบ 0.45s', 'dash'],
    ['E', 'Parry', 'Stamina 20 • แพรี่ 0.30s', 'parry'],
    ['Q', 'Character skill', 'ฟื้น HP 18 + ดาเมจ 15', 'character'],
    ['W', 'Weapon skill', 'ดาเมจ 32 • ฟาดผ่าเกราะ', 'weapon'],
    ['R', 'Ultimate', 'ใช้เกจ 100 • ดาเมจ 70', 'ultimate']
];

const cooldown = { light: 0.28, heavy: 0.42, dash: 0.45, character: 8.5, weapon: 4.5, parry: 0.3, ultimate: 1 };
const costs = { dash: 25, parry: 20 };

// --- ENEMY COMBO CHAINS ---
// Consecutive multi-hit attack strings
const bossChains = [
    {
        name: 'TWIN FANG (2-Hit Combo)',
        moves: [
            { name: 'QUICK THRUST', hint: 'แสงฟ้า • ท่าแทง แนะนำแพรี่ (Parry)', color: '#8fe6ff', parry: true, duration: 0.72, damage: 18 },
            { name: 'HEAVY CLEAVE', hint: 'แสงส้ม • ท่าฟาดหนัก ต้องหลบ (Dash)', color: '#ffab62', parry: false, duration: 0.85, damage: 26 }
        ]
    },
    {
        name: 'CRUSHER BARRAGE (2-Hit Combo)',
        moves: [
            { name: 'HEAVY CLEAVE', hint: 'แสงส้ม • ท่าฟาดแรก หลบจังหวะหนึ่ง', color: '#ffab62', parry: false, duration: 0.85, damage: 24 },
            { name: 'EARTH CRUSHER', hint: 'แสงส้ม • ทุบกระแทกพื้นซ้ำ หลบต่อเนื่อง!', color: '#ff785b', parry: false, duration: 0.78, damage: 30 }
        ]
    },
    {
        name: 'IRON TEMPEST (3-Hit Combo)',
        moves: [
            { name: 'QUICK THRUST', hint: 'แสงฟ้า • แทงฮิต 1 แพรี่!', color: '#8fe6ff', parry: true, duration: 0.65, damage: 14 },
            { name: 'RAPID JAB', hint: 'แสงฟ้า • แทงฮิต 2 แพรี่ต่อเนื่อง!', color: '#8fe6ff', parry: true, duration: 0.55, damage: 16 },
            { name: 'EARTHSHAKER CLEAVE', hint: 'แสงส้ม • ฟาดปิดท้าย ต้องหลบ!', color: '#ffab62', parry: false, duration: 0.88, damage: 32 }
        ]
    },
    {
        name: 'VORTEX RIPOSTE (3-Hit Combo)',
        moves: [
            { name: 'HEAVY SWEEP', hint: 'แสงส้ม • ฟันกวาด หลบ!', color: '#ffab62', parry: false, duration: 0.78, damage: 20 },
            { name: 'COUNTER THRUST', hint: 'แสงฟ้า • แทงสวน แพรี่!', color: '#8fe6ff', parry: true, duration: 0.60, damage: 18 },
            { name: 'OVERHEAD IMPACT', hint: 'แสงส้ม • สแลมปิดฉาก หลบ!', color: '#ff5722', parry: false, duration: 0.85, damage: 30 }
        ]
    }
];

// --- SOUND ENGINE (Web Audio API) ---
class AudioEngine {
    constructor() { this.ctx = null; }
    init() {
        if (this.ctx) return;
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        this.ctx = new AudioContext();
    }
    resume() {
        if (this.ctx && this.ctx.state === 'suspended') this.ctx.resume();
    }
    ping() {
        if (!this.ctx) return;
        this.resume();
        const t = this.ctx.currentTime;
        for (const [freq, vol] of [[1760, 0.05], [2640, 0.02]]) {
            const o = this.ctx.createOscillator();
            const g = this.ctx.createGain();
            o.type = 'sine';
            o.frequency.setValueAtTime(freq, t);
            o.frequency.exponentialRampToValueAtTime(freq * 0.7, t + 0.16);
            g.gain.setValueAtTime(vol, t);
            g.gain.exponentialRampToValueAtTime(0.0001, t + 0.18);
            o.connect(g);
            g.connect(this.ctx.destination);
            o.start(t);
            o.stop(t + 0.2);
        }
    }
    slash(pitch = 1000, vol = 0.3) {
        if (!this.ctx) return;
        this.resume();
        const t = this.ctx.currentTime;
        const buf = this.ctx.createBuffer(1, this.ctx.sampleRate * 0.13, this.ctx.sampleRate);
        const data = buf.getChannelData(0);
        for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
        const src = this.ctx.createBufferSource();
        src.buffer = buf;
        const f = this.ctx.createBiquadFilter();
        f.type = 'bandpass';
        f.frequency.setValueAtTime(pitch, t);
        f.frequency.exponentialRampToValueAtTime(220, t + 0.12);
        const g = this.ctx.createGain();
        g.gain.setValueAtTime(vol, t);
        g.gain.exponentialRampToValueAtTime(0.001, t + 0.12);
        src.connect(f);
        f.connect(g);
        g.connect(this.ctx.destination);
        src.start(t);
        src.stop(t + 0.13);
    }
    impact() {
        if (!this.ctx) return;
        this.resume();
        const t = this.ctx.currentTime;
        const o = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        o.type = 'triangle';
        o.frequency.setValueAtTime(140, t);
        o.frequency.exponentialRampToValueAtTime(35, t + 0.15);
        g.gain.setValueAtTime(0.5, t);
        g.gain.exponentialRampToValueAtTime(0.01, t + 0.15);
        o.connect(g);
        g.connect(this.ctx.destination);
        o.start(t);
        o.stop(t + 0.15);
    }
    parryClang() {
        if (!this.ctx) return;
        this.resume();
        const t = this.ctx.currentTime;
        const o1 = this.ctx.createOscillator();
        const o2 = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        o1.type = 'sine';
        o1.frequency.setValueAtTime(1900, t);
        o1.frequency.exponentialRampToValueAtTime(900, t + 0.3);
        o2.type = 'triangle';
        o2.frequency.setValueAtTime(3800, t);
        o2.frequency.exponentialRampToValueAtTime(1400, t + 0.25);
        g.gain.setValueAtTime(0.65, t);
        g.gain.exponentialRampToValueAtTime(0.001, t + 0.3);
        o1.connect(g);
        o2.connect(g);
        g.connect(this.ctx.destination);
        o1.start(t);
        o2.start(t);
        o1.stop(t + 0.3);
        o2.stop(t + 0.3);
    }
    dash() {
        if (!this.ctx) return;
        this.resume();
        const t = this.ctx.currentTime;
        const o = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        o.type = 'sine';
        o.frequency.setValueAtTime(420, t);
        o.frequency.exponentialRampToValueAtTime(80, t + 0.2);
        g.gain.setValueAtTime(0.3, t);
        g.gain.exponentialRampToValueAtTime(0.01, t + 0.2);
        o.connect(g);
        g.connect(this.ctx.destination);
        o.start(t);
        o.stop(t + 0.2);
    }
    sunder() {
        if (!this.ctx) return;
        this.resume();
        const t = this.ctx.currentTime;
        const o = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        o.type = 'sawtooth';
        o.frequency.setValueAtTime(240, t);
        o.frequency.exponentialRampToValueAtTime(25, t + 0.35);
        g.gain.setValueAtTime(0.65, t);
        g.gain.exponentialRampToValueAtTime(0.001, t + 0.35);
        o.connect(g);
        g.connect(this.ctx.destination);
        o.start(t);
        o.stop(t + 0.35);
    }
    heal() {
        if (!this.ctx) return;
        this.resume();
        const notes = [523.25, 659.25, 783.99, 1046.5];
        notes.forEach((freq, idx) => {
            const t = this.ctx.currentTime + idx * 0.06;
            const o = this.ctx.createOscillator();
            const g = this.ctx.createGain();
            o.type = 'sine';
            o.frequency.setValueAtTime(freq, t);
            g.gain.setValueAtTime(0.2, t);
            g.gain.exponentialRampToValueAtTime(0.001, t + 0.25);
            o.connect(g);
            g.connect(this.ctx.destination);
            o.start(t);
            o.stop(t + 0.25);
        });
    }
    ultDetonate() {
        if (!this.ctx) return;
        this.resume();
        const t = this.ctx.currentTime;
        const o = this.ctx.createOscillator();
        const g = this.ctx.createGain();
        o.type = 'sawtooth';
        o.frequency.setValueAtTime(160, t);
        o.frequency.exponentialRampToValueAtTime(20, t + 0.6);
        g.gain.setValueAtTime(0.85, t);
        g.gain.exponentialRampToValueAtTime(0.001, t + 0.6);
        o.connect(g);
        g.connect(this.ctx.destination);
        o.start(t);
        o.stop(t + 0.6);
    }
}
const sfx = new AudioEngine();

// --- GAME STATE ---
let s, now = 0, last = 0;
let particles = [];
let floats = [];
let shockwaves = [];
let afterimages = [];
let screenShake = 0;
let hitStopTimer = 0;

// Setup Actions UI
for (const [key, name, desc, id] of actions) {
    let b = document.createElement('button');
    b.className = 'action';
    b.id = id;
    b.innerHTML = `<kbd>${key}</kbd><b>${name}</b><small>${desc}</small>`;
    b.onclick = () => act(id);
    $('controls').append(b);
}

// Reset Game State
function reset() {
    sfx.init();
    sfx.resume();

    s = {
        hp: 100,
        stamina: 100,
        regenAt: 0,
        enemy: 320,
        maxEnemy: 320,
        energy: 0,
        score: 0,
        phase: 'opening',
        until: now + 2.0,
        cd: {},
        dash: 0,
        parry: 0,
        flash: 0,
        slash: 0,
        round: 0,
        active: true,

        // --- BOSS CONSECUTIVE COMBO CHAIN STATE ---
        currentChainObj: bossChains[0],
        chainIndex: 0,
        move: bossChains[0].moves[0],
        duration: bossChains[0].moves[0].duration,
        cueAt: Infinity,
        cueUntil: 0,
        cuePlayed: false,

        // --- PLAYER COMMAND COMBO SYSTEM ---
        cmdQueue: [],          // array of 'L' or 'H'
        cmdTimeout: 0,
        cmdTitle: '',

        // --- ANIMATIONS ---
        playerAnim: {
            current: 'idle',
            timer: 0,
            duration: 0.1,
            slashTrail: []
        },
        enemyAnim: {
            stagger: 0,
            hitFlash: 0
        },
        ultimatePhase: 0,
        ultTimer: 0,
        ultSlashes: []
    };

    particles = [];
    floats = [];
    shockwaves = [];
    afterimages = [];
    $('overlay').style.display = 'none';
    hideCommandChainUI();
    say('เริ่มการต่อสู้ — กดคลิกซ้าย/ขวา หรือ Space ผสมคอมโบโจมตีได้อิสระ!');
}

$('start').onclick = reset;

function say(t) { $('log').textContent = t; }
function gain(n) {
    s.score += n;
    s.energy = Math.min(100, s.energy + n);
}
function pop(text, x, y, color) {
    floats.push({ text, x, y, color, life: 1.2 });
}
function burst(x, y, color, count = 22, spd = 360) {
    for (let i = 0; i < count; i++) {
        const ang = Math.random() * Math.PI * 2;
        const vel = (Math.random() * 0.7 + 0.3) * spd;
        particles.push({
            x, y,
            vx: Math.cos(ang) * vel,
            vy: Math.sin(ang) * vel,
            life: 0.55,
            color
        });
    }
}
function triggerShockwave(x, y, maxR = 90, color = '#63edd2') {
    shockwaves.push({ x, y, r: 10, maxR, color, life: 0.35, maxLife: 0.35 });
}
function hitStop(durationMs) {
    hitStopTimer = durationMs / 1000;
}

function damage(n) {
    s.enemy = Math.max(0, s.enemy - n);
    s.slash = now + 0.25;
    s.enemyAnim.stagger = 0.22;
    s.enemyAnim.hitFlash = 0.15;

    pop('-' + n, 600, 240, '#63edd2');
    burst(600, 300, '#63edd2', 20, 380);
    gain(Math.round(n / 2));
    sfx.impact();

    if (!s.enemy) finish(true);
}

function finish(win) {
    s.active = false;
    $('overlay').style.display = 'flex';
    $('result').innerHTML = win ? 'WARDEN DEFEATED' : 'YOU FELL';
    $('intro').innerHTML = `คะแนน <strong>${s.score}</strong> • ผสมคอมโบและจับจังหวะการโจมตีชุดสำเร็จ!<br>กดปุ่มด้านล่างเพื่อประลองฝีมือใหม่อีกครั้ง`;
    $('start').textContent = 'PLAY AGAIN →';
    say(win ? 'ชนะการต่อสู้!' : 'พ่ายแพ้ในการต่อสู้');
}

// Start a new Boss Combo Chain
function startBossChain() {
    const chain = bossChains[s.round++ % bossChains.length];
    s.currentChainObj = chain;
    s.chainIndex = 0;
    startChainStep(0);
}

function startChainStep(index) {
    const m = s.currentChainObj.moves[index];
    s.move = m;
    s.duration = m.duration;
    s.phase = 'windup';
    s.until = now + s.duration;
    s.cueAt = s.until - 0.35;
    s.cueUntil = s.cueAt + 0.24;
    s.cuePlayed = false;

    // Update phase & chain indicator
    const totalHits = s.currentChainObj.moves.length;
    const badge = $('chainBadge');
    badge.style.display = 'inline-block';
    badge.textContent = `COMBO ${index + 1}/${totalHits}`;
    badge.style.borderColor = m.parry ? '#8fe6ff' : '#ffab62';
    badge.style.color = m.parry ? '#8fe6ff' : '#ffab62';

    say(`[${s.currentChainObj.name}] ฮิต ${index + 1}/${totalHits} — ${m.hint}`);
}

// ==========================================
// --- COMMAND COMBO INPUT SYSTEM ---
// ==========================================

function handleCommandInput(type) { // 'L' (Light) or 'H' (Heavy)
    if (!s?.active) return;

    // Reset queue if timed out
    if (now > s.cmdTimeout) {
        s.cmdQueue = [];
    }

    s.cmdQueue.push(type);
    s.cmdTimeout = now + 1.1;

    // Determine Combo by pattern
    const key = s.cmdQueue.join('-');
    let comboName = '';
    let dmg = 12;
    let anim = 'attack_light1';
    let soundPitch = 900;
    let shake = 0;

    // --- COMBO BRANCHES ---
    // Single Light
    if (key === 'L') {
        comboName = 'LIGHT SLASH I';
        dmg = 12;
        anim = 'attack_light1';
        soundPitch = 900;
    }
    // L -> L
    else if (key === 'L-L') {
        comboName = 'LIGHT SLASH II';
        dmg = 16;
        anim = 'attack_light2';
        soundPitch = 1150;
    }
    // L -> L -> L (Triple Finisher)
    else if (key === 'L-L-L') {
        comboName = 'TRIPLE BLADE WALTZ';
        dmg = 26;
        anim = 'attack_triple';
        soundPitch = 1450;
        shake = 7;
        triggerShockwave(600, 310, 80, '#63edd2');
        s.cmdQueue = []; // Finisher resets combo
    }
    // L -> H (Launcher)
    else if (key === 'L-H') {
        comboName = 'RISING FALCON CLEAVE';
        dmg = 34;
        anim = 'attack_rising';
        soundPitch = 1300;
        shake = 9;
        triggerShockwave(600, 310, 110, '#f0c572');
        s.cmdQueue = [];
    }
    // L -> L -> H (Cyclone Execution)
    else if (key === 'L-L-H') {
        comboName = 'CYCLONE EXECUTION';
        dmg = 45;
        anim = 'attack_cyclone';
        soundPitch = 1600;
        shake = 13;
        hitStop(50);
        triggerShockwave(600, 310, 150, '#63edd2');
        burst(600, 310, '#63edd2', 30, 420);
        s.cmdQueue = [];
    }
    // Single Heavy
    else if (key === 'H') {
        comboName = 'STINGER THRUST';
        dmg = 25;
        anim = 'attack_stinger';
        soundPitch = 800;
        shake = 5;
    }
    // H -> L
    else if (key === 'H-L') {
        comboName = 'VIPER RIPOSTE';
        dmg = 30;
        anim = 'attack_viper';
        soundPitch = 1200;
        shake = 7;
        s.cmdQueue = [];
    }
    // H -> H (Crag Breaker Hammer)
    else if (key === 'H-H') {
        comboName = 'CRAG BREAKER';
        dmg = 44;
        anim = 'attack_crag';
        soundPitch = 600;
        shake = 14;
        hitStop(70);
        triggerShockwave(600, 320, 160, '#ffab62');
        burst(600, 320, '#ffab62', 35, 440);
        s.cmdQueue = [];
    }
    // Fallback if exceeded length
    else {
        s.cmdQueue = [type];
        handleCommandInput(type);
        return;
    }

    // Play animation & sound
    playPlayerAnim(anim, 0.35);
    sfx.slash(soundPitch, 0.4);
    if (shake > 0) screenShake = shake;

    // Apply Damage
    damage(dmg);

    // Update Command HUD
    showCommandChainUI(s.cmdQueue.length ? s.cmdQueue : key.split('-'), comboName);
    say(`คอมโบ [${comboName}] โดนเป้าหมาย ${dmg} ดาเมจ!`);
}

function showCommandChainUI(queue, title) {
    const container = $('commandChain');
    container.innerHTML = '';

    queue.forEach((cmd, idx) => {
        if (idx > 0) {
            const arr = document.createElement('span');
            arr.className = 'cmd-arrow';
            arr.textContent = '➔';
            container.appendChild(arr);
        }
        const b = document.createElement('span');
        b.className = `cmd-badge ${cmd === 'L' ? 'light' : 'heavy'}`;
        b.textContent = cmd === 'L' ? 'LMB' : 'RMB';
        container.appendChild(b);
    });

    const name = document.createElement('span');
    name.className = 'cmd-name';
    name.textContent = title;
    container.appendChild(name);

    container.classList.add('active');
    clearTimeout(container._timer);
    container._timer = setTimeout(() => {
        container.classList.remove('active');
    }, 1200);
}

function hideCommandChainUI() {
    $('commandChain').classList.remove('active');
}

// ==========================================
// --- PLAYER ACTION TRIGGER ---
// ==========================================

function act(id) {
    if (!s?.active || now < (s.cd[id] || 0)) return;

    // --- REQUIREMENT 1: ENEMY ATTACKS NEVER STOP PLAYER ATTACKS ---
    // (Notice: No check for danger blocking player attacks! Player can attack at will!)

    if (id === 'ultimate' && s.energy < 100) {
        say('Ultimate ต้องใช้เกจ 100');
        return;
    }

    if (costs[id]) {
        if (s.stamina < costs[id]) {
            say('Stamina ไม่พอ');
            return;
        }
        if (s.dash > now || s.parry > now) {
            say('รอท่าป้องกันปัจจุบันจบก่อน');
            return;
        }
        s.stamina -= costs[id];
        s.regenAt = now + 0.8;
    }

    s.cd[id] = now + cooldown[id];

    // --- MOUSE COMMAND: LIGHT ATTACK ---
    if (id === 'light') {
        handleCommandInput('L');
        return;
    }

    // --- MOUSE COMMAND: HEAVY ATTACK ---
    if (id === 'heavy') {
        handleCommandInput('H');
        return;
    }

    // --- DASH (Shift) ---
    if (id === 'dash') {
        s.dash = now + 0.45;
        playPlayerAnim('dash', 0.45);
        sfx.dash();
        say('Dash — หลบพุ่งตัวชั่วคราว (i-frames)');
        return;
    }

    // --- PARRY (E) ---
    if (id === 'parry') {
        s.parry = now + 0.3;
        playPlayerAnim('parry', 0.3);
        say('Parry — ตั้งดาบป้องกัน หน้าต่าง 0.30 วินาที');
        return;
    }

    // --- CHARACTER SKILL (Q) ---
    if (id === 'character') {
        s.hp = Math.min(100, s.hp + 18);
        pop('+18 HP', 600, 480, '#63edd2');
        playPlayerAnim('character', 0.55);
        sfx.heal();
        triggerShockwave(600, 520, 110, '#63edd2');
        burst(600, 520, '#63edd2', 25, 240);

        setTimeout(() => {
            if (s?.active) {
                damage(15);
                triggerShockwave(600, 310, 80, '#63edd2');
            }
        }, 180);

        say('Character skill — วงเวทย์วายุฟื้นฟูเลือดและสวนแทงดาบ');
        return;
    }

    // --- WEAPON SKILL (W) ---
    if (id === 'weapon') {
        playPlayerAnim('weapon', 0.6);
        say('Weapon skill — กระโดดฟาดดาบยักษ์ผ่าเกราะ!');

        setTimeout(() => {
            if (s?.active) {
                damage(32);
                sfx.sunder();
                screenShake = 12;
                hitStop(60);
                triggerShockwave(600, 320, 140, '#8fe6ff');
                burst(600, 320, '#8fe6ff', 35, 420);
            }
        }, 220);

        return;
    }

    // --- ULTIMATE (R) ---
    if (id === 'ultimate') {
        s.energy = 0;
        playPlayerAnim('ultimate', 1.4);
        startUltimateSequence();
        say('ULTIMATE — รอยแยกมิติพิพากษา Omni-Slash!');
        return;
    }
}

function playPlayerAnim(name, duration) {
    s.playerAnim.current = name;
    s.playerAnim.timer = duration;
    s.playerAnim.duration = duration;
}

// Mouse controls on canvas
canvas.addEventListener('mousedown', e => {
    e.preventDefault();
    if (!s?.active) return;
    if (e.button === 0) {
        act('light');
    } else if (e.button === 2) {
        act('heavy');
    }
});
canvas.addEventListener('contextmenu', e => e.preventDefault());

// Keyboard shortcuts
window.addEventListener('keydown', e => {
    const id = ({
        Space: 'light',
        KeyF: 'heavy',
        KeyK: 'heavy',
        ShiftLeft: 'dash',
        ShiftRight: 'dash',
        KeyQ: 'character',
        KeyW: 'weapon',
        KeyE: 'parry',
        KeyR: 'ultimate'
    })[e.code];
    if (id) {
        e.preventDefault();
        if (!e.repeat) act(id);
    }
});

// --- ULTIMATE CINEMATIC SEQUENCE ---
function startUltimateSequence() {
    s.ultimatePhase = 1;
    s.ultTimer = 0;
    s.ultSlashes = [];
    screenShake = 5;

    setTimeout(() => {
        if (!s?.active) return;
        s.ultimatePhase = 2;
        const slashTargets = [
            { x1: 450, y1: 360, x2: 750, y2: 240 },
            { x1: 750, y1: 360, x2: 450, y2: 240 },
            { x1: 420, y1: 290, x2: 780, y2: 290 },
            { x1: 600, y1: 180, x2: 600, y2: 390 }
        ];

        slashTargets.forEach((sl, idx) => {
            setTimeout(() => {
                if (!s?.active) return;
                s.ultSlashes.push(sl);
                sfx.slash(1400, 0.4);
                screenShake = 7;
                hitStop(30);
                burst((sl.x1 + sl.x2) / 2, (sl.y1 + sl.y2) / 2, '#8fe6ff', 12, 300);
            }, idx * 110);
        });
    }, 300);

    setTimeout(() => {
        if (!s?.active) return;
        s.ultimatePhase = 3;
        sfx.parryClang();
    }, 850);

    setTimeout(() => {
        if (!s?.active) return;
        s.ultimatePhase = 4;
        damage(70);
        s.flash = now + 0.4;
        sfx.ultDetonate();
        screenShake = 18;
        hitStop(90);
        triggerShockwave(600, 290, 240, '#8fe6ff');
        burst(600, 290, '#ffffff', 40, 500);
    }, 1050);

    setTimeout(() => {
        if (!s?.active) return;
        s.ultimatePhase = 0;
    }, 1400);
}

// ==========================================
// --- BOSS IMPACT & CONSECUTIVE CHAIN ADVANCE ---
// ==========================================

function impact() {
    s.phase = 'strike';
    s.until = now + 0.22;

    const currentMove = s.move;

    // Check EVADE
    if (s.dash > now) {
        gain(12);
        pop('EVADE +12', 600, 440, '#63edd2');
        triggerShockwave(600, 520, 70, '#63edd2');
        say('หลบสำเร็จ +12 คะแนน & Ultimate!');
    }
    // Check PERFECT PARRY
    else if (s.parry > now && currentMove.parry) {
        gain(20);
        damage(18);
        playPlayerAnim('riposte', 0.4);
        pop('PERFECT PARRY +20', 600, 430, '#8fe6ff');
        say('Perfect Parry! สวนกลับทะลวงเกราะ +20');
        sfx.parryClang();
        hitStop(110);
        screenShake = 10;
        triggerShockwave(600, 480, 110, '#8fe6ff');
        burst(600, 480, '#8fe6ff', 30, 450);
    }
    // TAKE DAMAGE
    else {
        s.hp = Math.max(0, s.hp - currentMove.damage);
        s.flash = now + 0.22;
        screenShake = 14;
        hitStop(80);
        pop('-' + currentMove.damage + ' HP', 600, 470, '#ff655f');
        burst(600, 520, '#ff655f', 24, 340);
        playPlayerAnim('hurt', 0.35);

        say(s.parry > now && !currentMove.parry ? 'ท่าฟาดหนัก (แสงส้ม) แพรี่ไม่ได้ — ต้องหลบเท่านั้น!' : `รับดาเมจ ${currentMove.damage} — กะจังหวะหลบหรือแพรี่ตามสีเตือน`);
        if (!s.hp) {
            finish(false);
            return;
        }
    }
}

// Advance to next attack in chain, or finish chain
function advanceChainOrEnd() {
    const totalMoves = s.currentChainObj.moves.length;

    // If there are more moves in this consecutive attack string:
    if (s.chainIndex < totalMoves - 1) {
        s.chainIndex++;
        // Very fast seamless transition to next attack in chain!
        startChainStep(s.chainIndex);
    } else {
        // Entire chain finished -> Boss Enters Recovery (Player Opening!)
        s.phase = 'recovery';
        s.until = now + 1.1; // Generous opening to unleash full player combos!
        $('chainBadge').style.display = 'none';
        say('บอสพักหายใจ — จังหวะสวนกลับด้วยคอมโบเต็มชุด!');
    }
}

// Tick loop
function tick(dt) {
    if (s?.active && s.phase === 'windup' && !s.cuePlayed && now >= s.cueAt) {
        s.cuePlayed = true;
        sfx.ping();
    }
    if (s?.active && now >= s.regenAt) {
        s.stamina = Math.min(100, s.stamina + 22 * dt);
    }

    if (s?.active && now >= s.until) {
        if (s.phase === 'opening') {
            startBossChain();
        } else if (s.phase === 'windup') {
            impact();
        } else if (s.phase === 'strike') {
            advanceChainOrEnd();
        } else {
            // After recovery -> Opening
            s.phase = 'opening';
            s.until = now + 1.8;
            $('chainBadge').style.display = 'none';
        }
    }

    // Update Player Animation Timers
    if (s?.playerAnim) {
        if (s.playerAnim.timer > 0) {
            s.playerAnim.timer -= dt;
            if (s.playerAnim.timer <= 0) {
                s.playerAnim.current = 'idle';
            }
        }
    }

    // Update Enemy Anim Timers
    if (s?.enemyAnim) {
        if (s.enemyAnim.stagger > 0) s.enemyAnim.stagger -= dt;
        if (s.enemyAnim.hitFlash > 0) s.enemyAnim.hitFlash -= dt;
    }

    // Update HUD Bars
    if (s) {
        $('stamina').style.width = s.stamina + '%';
        $('staminaText').textContent = 'STAMINA ' + Math.floor(s.stamina) + ' / 100';
        $('enemyHP').style.width = (s.enemy / s.maxEnemy) * 100 + '%';
        $('enemyText').textContent = s.enemy + ' / ' + s.maxEnemy;
        $('playerHP').style.width = s.hp + '%';
        $('playerText').textContent = s.hp + ' / 100';
        $('energy').style.width = s.energy + '%';
        $('energyText').textContent = 'ULTIMATE ' + s.energy + ' / 100';
        $('score').textContent = s.score;

        const danger = ['windup', 'strike'].includes(s.phase);
        $('phase').textContent = !s.active ? 'BATTLE ENDED' : danger ? s.move.name : s.phase === 'recovery' ? 'WARDEN RECOVERING' : 'YOUR OPENING';
        $('hint').textContent = danger ? s.move.hint : s.phase === 'recovery' ? 'โอกาสทอง! รัวคอมโบ LMB / RMB ใส่บอส' : 'คลิกซ้าย (เบา) / คลิกขวา (หนัก) ผสมคอมโบ • Shift หลบ • E แพรี่';

        // Action Buttons State (Never disabled by danger!)
        for (const [,,, id] of actions) {
            let b = $(id);
            let left = Math.max(0, (s.cd[id] || 0) - now);
            b.disabled = !s.active || left > 0 || (costs[id] && (s.stamina < costs[id] || s.dash > now || s.parry > now)) || (id === 'ultimate' && s.energy < 100);
            b.querySelector('small').textContent = left > 0 ? left.toFixed(1) + 's' : actions.find(a => a[3] === id)[2];
        }
    }

    // Particles update
    for (let p of particles) {
        p.x += p.vx * dt;
        p.y += p.vy * dt;
        p.vx *= 0.94;
        p.vy *= 0.94;
        p.life -= dt;
    }
    particles = particles.filter(p => p.life > 0);

    // Floating text update
    for (let f of floats) {
        f.y -= dt * 42;
        f.life -= dt;
    }
    floats = floats.filter(f => f.life > 0);

    // Shockwaves update
    for (let sw of shockwaves) {
        sw.life -= dt;
        sw.r += (sw.maxR - sw.r) * 12 * dt;
    }
    shockwaves = shockwaves.filter(sw => sw.life > 0);

    // Afterimages update
    for (let ai of afterimages) {
        ai.alpha -= dt * 3.5;
    }
    afterimages = afterimages.filter(ai => ai.alpha > 0);

    // Screen shake decay
    if (screenShake > 0) {
        screenShake *= Math.pow(0.01, dt);
        if (screenShake < 0.2) screenShake = 0;
    }
}

// ==========================================
// --- GRAPHICS & ANIMATION RENDERING ENGINE ---
// ==========================================

function draw() {
    ctx.clearRect(0, 0, 1200, 650);

    ctx.save();
    // Screen shake
    if (screenShake > 0) {
        const ox = (Math.random() - 0.5) * screenShake;
        const oy = (Math.random() - 0.5) * screenShake;
        ctx.translate(ox, oy);
    }

    // Arena Floor & Perspective Grid
    drawArenaBackground();

    // Shockwaves
    drawShockwaves();

    // Draw Iron Warden (Enemy)
    drawWarden();

    // Draw Player (with full attack animation rig!)
    drawPlayer();

    // Cue Indicator (Star flash on Warden)
    const danger = s?.phase === 'windup';
    let ey = 300 + (danger ? Math.sin(now * 20) * 3 : Math.sin(now * 2) * 5);
    if (danger && now >= s.cueAt && now < s.cueUntil) {
        drawCue(675, ey - 25, (now - s.cueAt) / 0.24, s.move.color);
    }

    // Ultimate Cutlines
    if (s?.ultimatePhase >= 2) {
        drawUltimateEffects();
    }

    // Particles
    drawParticles();

    // Floating Damage/Heal Numbers
    drawFloatingTexts();

    // Screen Flash Overlay (Hurt / Ultimate)
    if (s?.flash > now) {
        ctx.fillStyle = s.hp === 0 ? 'rgba(255, 101, 95, 0.35)' : 'rgba(240, 197, 114, 0.25)';
        ctx.fillRect(0, 0, 1200, 650);
    }

    // Cinematic Letterbox during Ultimate
    if (s?.ultimatePhase > 0) {
        ctx.fillStyle = '#000000';
        ctx.fillRect(0, 0, 1200, 45);
        ctx.fillRect(0, 605, 1200, 45);
    }

    ctx.restore();
}

function drawArenaBackground() {
    let g = ctx.createLinearGradient(0, 0, 0, 650);
    g.addColorStop(0, '#111e2b');
    g.addColorStop(1, '#080d15');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, 1200, 650);

    ctx.strokeStyle = '#253547';
    ctx.lineWidth = 1;
    for (let x = -500; x < 1800; x += 100) {
        ctx.beginPath();
        ctx.moveTo(600, 230);
        ctx.lineTo(x, 650);
        ctx.stroke();
    }
    for (let y = 350; y < 650; y += 45) {
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(1200, y);
        ctx.stroke();
    }

    ctx.strokeStyle = 'rgba(99, 237, 210, 0.2)';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.ellipse(600, 470, 310, 100, 0, 0, Math.PI * 2);
    ctx.stroke();
}

// --- DRAW IRON WARDEN ---
function drawWarden() {
    const danger = s?.phase === 'windup';
    const swing = s?.phase === 'strike';
    const isStaggered = s?.enemyAnim?.stagger > 0;
    const isHitFlash = s?.enemyAnim?.hitFlash > 0;

    let ey = 300 + (danger ? Math.sin(now * 20) * 3 : Math.sin(now * 2) * 5);
    let ex = 600 + (isStaggered ? (Math.random() - 0.5) * 8 : 0);

    ctx.save();
    ctx.translate(ex, ey);

    // Shadow
    ctx.fillStyle = 'rgba(0, 0, 0, 0.45)';
    ctx.beginPath();
    ctx.ellipse(0, 95, 105, 22, 0, 0, Math.PI * 2);
    ctx.fill();

    if (isHitFlash) {
        ctx.shadowColor = '#63edd2';
        ctx.shadowBlur = 15;
    }

    // Torso / Heavy Armor
    ctx.fillStyle = isHitFlash ? '#ffffff' : (danger ? (s.move.parry ? '#8fe6ff' : '#ff785b') : '#455665');
    ctx.beginPath();
    ctx.moveTo(-65, -35);
    ctx.lineTo(-95, 65);
    ctx.lineTo(-35, 85);
    ctx.lineTo(0, 60);
    ctx.lineTo(35, 85);
    ctx.lineTo(95, 65);
    ctx.lineTo(65, -35);
    ctx.closePath();
    ctx.fill();

    // Helmet & Visor
    ctx.fillStyle = isHitFlash ? '#ffffff' : '#718795';
    ctx.fillRect(-34, -92, 68, 70);

    ctx.fillStyle = danger ? s.move.color : '#ffae77';
    ctx.shadowColor = danger ? s.move.color : '#ffae77';
    ctx.shadowBlur = danger ? 14 : 6;
    ctx.fillRect(-24, -66, 18, 6);
    ctx.fillRect(6, -66, 18, 6);
    ctx.shadowBlur = 0;

    // Shoulder Pauldrons
    ctx.fillStyle = danger ? '#b75642' : '#2d3b48';
    ctx.fillRect(-90, -45, 30, 24);
    ctx.fillRect(60, -45, 30, 24);

    // Weapon Arm & Great Cleaver / Lance
    ctx.save();
    ctx.strokeStyle = danger ? s.move.color : '#9ab2c0';
    ctx.lineWidth = 14;
    ctx.lineCap = 'round';
    ctx.shadowColor = danger ? s.move.color : 'transparent';
    ctx.shadowBlur = danger ? 18 : 0;

    let weaponEndX, weaponEndY;

    if (swing) {
        weaponEndX = s.move.parry ? -75 : -105;
        weaponEndY = s.move.parry ? 70 : 110;
    } else if (danger) {
        if (s.move.parry) {
            weaponEndX = 30 + Math.cos(now * 15) * 5;
            weaponEndY = -40 + Math.sin(now * 15) * 5;
        } else {
            weaponEndX = 120 + Math.cos(now * 15) * 6;
            weaponEndY = -130 + Math.sin(now * 15) * 6;
        }
    } else {
        weaponEndX = 115;
        weaponEndY = 65;
    }

    ctx.beginPath();
    ctx.moveTo(75, 25);
    ctx.lineTo(weaponEndX, weaponEndY);
    ctx.stroke();

    ctx.fillStyle = danger ? '#ffffff' : '#c3d5e0';
    ctx.fillRect(weaponEndX - 8, weaponEndY - 8, 20, 20);
    ctx.restore();

    ctx.restore();
}

// --- DRAW PLAYER WITH FULL COMMAND ANIMATIONS ---
function drawPlayer() {
    let px = 600;
    let py = 520;
    let rot = 0;
    let armX = 30, armY = -10;
    let bladeAngle = -0.7;
    let swordLen = 75;
    let glowColor = '#63edd2';

    const pAnim = s?.playerAnim?.current || 'idle';
    const progress = s?.playerAnim ? 1 - (s.playerAnim.timer / s.playerAnim.duration) : 0;

    // --- DASH (Shift) ---
    if (s?.dash > now) {
        const dashTime = (s.dash - now) / 0.45;
        px += Math.sin(dashTime * Math.PI) * 160;
        rot = (1 - dashTime) * 0.25;

        if (Math.random() > 0.4) {
            afterimages.push({ x: px, y: py, alpha: 0.5, color: '#63edd2' });
        }
    }

    // --- COMBO 1: LIGHT SLASH I (L) ---
    else if (pAnim === 'attack_light1') {
        const t = progress;
        px += Math.sin(t * Math.PI) * 35;
        py -= Math.sin(t * Math.PI) * 15;
        rot = (t - 0.5) * 0.4;
        bladeAngle = -2.2 + t * 2.8;
        glowColor = '#63edd2';
        drawSlashTrail(px, py - 40, 95, -2.4, -2.4 + t * 2.6, '#63edd2', 7);
    }

    // --- COMBO 2: LIGHT SLASH II (L-L) ---
    else if (pAnim === 'attack_light2') {
        const t = progress;
        px -= Math.sin(t * Math.PI) * 25;
        py -= Math.sin(t * Math.PI) * 18;
        rot = -(t - 0.5) * 0.4;
        bladeAngle = 0.8 - t * 2.6;
        glowColor = '#8fe6ff';
        drawSlashTrail(px, py - 40, 105, 0.9, 0.9 - t * 2.5, '#8fe6ff', 8);
    }

    // --- COMBO 3: TRIPLE BLADE WALTZ (L-L-L) ---
    else if (pAnim === 'attack_triple') {
        const t = progress;
        py -= Math.sin(t * Math.PI) * 35;
        px += Math.sin(t * Math.PI * 2) * 20;
        bladeAngle = -Math.PI / 2 + t * 3.5;
        glowColor = '#63edd2';
        drawSlashTrail(px, py - 50, 120, -Math.PI * 0.8, -Math.PI * 0.8 + t * 3.2, '#63edd2', 10);
        drawSlashTrail(px, py - 50, 100, Math.PI * 0.2, Math.PI * 0.2 - t * 2.8, '#8fe6ff', 7);
    }

    // --- COMBO 4: RISING FALCON CLEAVE (L-H) ---
    else if (pAnim === 'attack_rising') {
        const t = progress;
        const jumpY = Math.sin(t * Math.PI) * 55;
        py -= jumpY;
        px += Math.sin(t * Math.PI) * 30;
        bladeAngle = 0.6 - t * 3.2; // Upward rising cleave
        glowColor = '#f0c572';
        swordLen = 95;
        drawSlashTrail(px, py - 60, 130, 0.8, 0.8 - t * 3.0, '#f0c572', 12);
    }

    // --- COMBO 5: CYCLONE EXECUTION (L-L-H) ---
    else if (pAnim === 'attack_cyclone') {
        const t = progress;
        rot = t * Math.PI * 4; // 2 rapid spins
        bladeAngle = -1.0;
        glowColor = '#63edd2';
        swordLen = 100;
        ctx.save();
        ctx.strokeStyle = '#63edd2';
        ctx.lineWidth = 9;
        ctx.shadowColor = '#63edd2';
        ctx.shadowBlur = 22;
        ctx.beginPath();
        ctx.ellipse(px, py - 20, 110, 38, t * Math.PI * 2, 0, Math.PI * 2);
        ctx.stroke();
        ctx.restore();
    }

    // --- COMBO 6: STINGER THRUST (H) ---
    else if (pAnim === 'attack_stinger') {
        const t = progress;
        px += Math.sin(t * Math.PI) * 65; // Lunge forward
        py += 4;
        bladeAngle = 0.05; // Straight horizontal stab
        glowColor = '#f0c572';
        swordLen = 90;
    }

    // --- COMBO 7: VIPER RIPOSTE (H-L) ---
    else if (pAnim === 'attack_viper') {
        const t = progress;
        px -= Math.sin(t * Math.PI) * 30;
        bladeAngle = -1.8 + t * 3.4;
        glowColor = '#8fe6ff';
        drawSlashTrail(px, py - 40, 110, -1.9, -1.9 + t * 3.2, '#8fe6ff', 9);
    }

    // --- COMBO 8: CRAG BREAKER (H-H) ---
    else if (pAnim === 'attack_crag') {
        const t = progress;
        py -= Math.sin(t * Math.PI) * 60;
        px += Math.sin(t * Math.PI) * 45;
        bladeAngle = -Math.PI / 2 + (t > 0.35 ? (t - 0.35) * 4.8 : 0);
        glowColor = '#ffab62';
        swordLen = 110;
        drawSlashTrail(px, py - 70, 145, -Math.PI * 0.9, -Math.PI * 0.9 + t * 3.4, '#ffab62', 14);
    }

    // --- WEAPON SKILL (W) ---
    else if (pAnim === 'weapon') {
        const t = progress;
        px += Math.sin(t * Math.PI) * 45;
        py -= Math.sin(t * Math.PI) * 60;
        bladeAngle = -2.8 + t * 4.2;
        swordLen = 105;
        glowColor = '#8fe6ff';
        drawSlashTrail(px, py - 50, 140, -Math.PI * 0.9, -Math.PI * 0.9 + t * 3.4, '#8fe6ff', 12);
    }

    // --- CHARACTER SKILL (Q) ---
    else if (pAnim === 'character') {
        const t = progress;
        rot = t * Math.PI * 4;
        bladeAngle = -1.2;
        glowColor = '#63edd2';
        ctx.save();
        ctx.strokeStyle = '#63edd2';
        ctx.lineWidth = 6;
        ctx.shadowColor = '#63edd2';
        ctx.shadowBlur = 18;
        ctx.beginPath();
        ctx.ellipse(px, py - 20, 85, 30, t * Math.PI * 2, 0, Math.PI * 2);
        ctx.stroke();
        ctx.restore();
    }

    // --- PARRY (E) & RIPOSTE ---
    else if (pAnim === 'parry' || s?.parry > now) {
        py += 8;
        rot = -0.15;
        bladeAngle = 0;
        drawHexBarrier(px, py - 30);
    } else if (pAnim === 'riposte') {
        const t = progress;
        px += Math.sin(t * Math.PI) * 60;
        bladeAngle = 0.1;
        glowColor = '#8fe6ff';
        swordLen = 95;
    }

    // --- HURT ---
    else if (pAnim === 'hurt') {
        const t = progress;
        py += Math.sin(t * Math.PI) * 20;
        rot = -0.3;
        bladeAngle = 1.2;
    }

    // --- ULTIMATE (R) ---
    else if (s?.ultimatePhase === 1) {
        py += 18;
        rot = 0.2;
        bladeAngle = 1.4;
        glowColor = '#ffffff';
        burst(px, py, '#8fe6ff', 3, 100);
    } else if (s?.ultimatePhase === 3) {
        bladeAngle = -0.4;
        glowColor = '#ffffff';
    }

    // Draw Afterimages
    for (let ai of afterimages) {
        ctx.save();
        ctx.globalAlpha = ai.alpha * 0.45;
        ctx.fillStyle = ai.color;
        ctx.shadowColor = ai.color;
        ctx.shadowBlur = 10;
        ctx.beginPath();
        ctx.ellipse(ai.x, ai.y + 66, 68, 16, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(ai.x, ai.y - 64, 27, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    }

    // Draw Player Rig
    ctx.save();
    ctx.translate(px, py);
    ctx.rotate(rot);

    // Floor Shadow
    ctx.fillStyle = 'rgba(0, 0, 0, 0.45)';
    ctx.beginPath();
    ctx.ellipse(0, 66, 68, 16, 0, 0, Math.PI * 2);
    ctx.fill();

    // Robe
    ctx.fillStyle = s?.dash > now ? 'rgba(99, 237, 210, 0.4)' : '#1c7779';
    ctx.beginPath();
    ctx.moveTo(-30, -45);
    ctx.lineTo(-62, 56);
    ctx.lineTo(62, 56);
    ctx.lineTo(30, -45);
    ctx.closePath();
    ctx.fill();

    // Torso
    ctx.fillStyle = '#105658';
    ctx.fillRect(-22, -60, 44, 38);

    // Head
    ctx.fillStyle = '#87cbc8';
    ctx.beginPath();
    ctx.arc(0, -64, 27, 0, Math.PI * 2);
    ctx.fill();

    // Mask / Eyes
    ctx.fillStyle = '#091710';
    ctx.fillRect(-14, -68, 28, 7);
    ctx.fillStyle = '#63edd2';
    ctx.fillRect(-10, -66, 20, 3);

    // Katana Blade
    ctx.save();
    ctx.translate(armX, armY);
    ctx.rotate(bladeAngle);

    ctx.fillStyle = '#f0c572';
    ctx.fillRect(-4, -4, 8, 8);
    ctx.fillStyle = '#222';
    ctx.fillRect(-3, 4, 6, 16);

    ctx.shadowColor = glowColor;
    ctx.shadowBlur = 14;
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 4;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(0, 0);
    ctx.lineTo(0, -swordLen);
    ctx.stroke();

    ctx.strokeStyle = glowColor;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(1, 0);
    ctx.lineTo(1, -swordLen);
    ctx.stroke();

    ctx.restore();
    ctx.restore();
}

function drawSlashTrail(cx, cy, radius, startAngle, endAngle, color, width) {
    ctx.save();
    ctx.shadowColor = color;
    ctx.shadowBlur = 18;
    ctx.strokeStyle = color;
    ctx.lineWidth = width;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.arc(cx, cy, radius, startAngle, endAngle, false);
    ctx.stroke();

    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = width * 0.45;
    ctx.stroke();
    ctx.restore();
}

function drawHexBarrier(px, py) {
    ctx.save();
    ctx.translate(px, py);
    ctx.strokeStyle = '#8fe6ff';
    ctx.shadowColor = '#8fe6ff';
    ctx.shadowBlur = 16;
    ctx.lineWidth = 3;
    ctx.fillStyle = 'rgba(143, 230, 255, 0.15)';

    ctx.beginPath();
    const r = 65;
    for (let i = 0; i < 6; i++) {
        const a = (Math.PI / 3) * i;
        const hx = Math.cos(a) * r;
        const hy = Math.sin(a) * r;
        if (i === 0) ctx.moveTo(hx, hy);
        else ctx.lineTo(hx, hy);
    }
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.restore();
}

function drawCue(x, y, t, color) {
    const pop = Math.min(1, t / 0.12);
    const fade = Math.pow(1 - t, 1.5);
    const size = 18 + pop * 42;

    ctx.save();
    ctx.translate(x, y);
    ctx.globalCompositeOperation = 'lighter';
    ctx.globalAlpha = fade;
    ctx.shadowColor = color;
    ctx.shadowBlur = 24;
    ctx.fillStyle = color;

    ctx.beginPath();
    for (let i = 0; i < 8; i++) {
        const a = (i * Math.PI) / 4 - Math.PI / 2;
        const r = i % 2 ? size * 0.12 : size * (i === 2 || i === 6 ? 1.5 : 1);
        ctx.lineTo(Math.cos(a) * r, Math.sin(a) * r);
    }
    ctx.closePath();
    ctx.fill();

    ctx.shadowBlur = 10;
    ctx.fillStyle = '#fff';
    ctx.beginPath();
    ctx.moveTo(0, -size * 0.7);
    ctx.lineTo(4, -4);
    ctx.lineTo(size, 0);
    ctx.lineTo(4, 4);
    ctx.lineTo(0, size * 0.7);
    ctx.lineTo(-4, 4);
    ctx.lineTo(-size, 0);
    ctx.lineTo(-4, -4);
    ctx.closePath();
    ctx.fill();

    ctx.shadowBlur = 0;
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.globalAlpha = fade * 0.6;
    ctx.beginPath();
    ctx.arc(0, 0, 12 + t * 75, 0, Math.PI * 2);
    ctx.stroke();

    for (let i = 0; i < 4; i++) {
        const a = (i * Math.PI) / 2 + Math.PI / 4;
        const r = 20 + t * 95;
        ctx.beginPath();
        ctx.moveTo(Math.cos(a) * r, Math.sin(a) * r);
        ctx.lineTo(Math.cos(a) * (r + 9), Math.sin(a) * (r + 9));
        ctx.stroke();
    }
    ctx.restore();
}

function drawUltimateEffects() {
    for (let sl of s.ultSlashes) {
        ctx.save();
        ctx.shadowColor = '#8fe6ff';
        ctx.shadowBlur = 20;
        ctx.strokeStyle = '#8fe6ff';
        ctx.lineWidth = 4;
        ctx.beginPath();
        ctx.moveTo(sl.x1, sl.y1);
        ctx.lineTo(sl.x2, sl.y2);
        ctx.stroke();

        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 2;
        ctx.stroke();
        ctx.restore();
    }
}

function drawShockwaves() {
    for (let sw of shockwaves) {
        const alpha = Math.max(0, sw.life / sw.maxLife);
        ctx.save();
        ctx.globalAlpha = alpha;
        ctx.strokeStyle = sw.color;
        ctx.shadowColor = sw.color;
        ctx.shadowBlur = 12;
        ctx.lineWidth = 3 * alpha;
        ctx.beginPath();
        ctx.ellipse(sw.x, sw.y, sw.r * 1.6, sw.r * 0.7, 0, 0, Math.PI * 2);
        ctx.stroke();
        ctx.restore();
    }
}

function drawParticles() {
    for (let p of particles) {
        ctx.save();
        ctx.globalAlpha = Math.max(0, p.life / 0.55);
        ctx.fillStyle = p.color;
        ctx.shadowColor = p.color;
        ctx.shadowBlur = 6;
        ctx.fillRect(p.x, p.y, 4, 4);
        ctx.restore();
    }
}

function drawFloatingTexts() {
    for (let f of floats) {
        ctx.save();
        ctx.globalAlpha = Math.min(1, f.life * 2);
        ctx.fillStyle = f.color;
        ctx.shadowColor = f.color;
        ctx.shadowBlur = 10;
        ctx.font = 'bold 22px "Orbitron", sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText(f.text, f.x, f.y);
        ctx.restore();
    }
}

// --- MAIN LOOP ---
function frame(t) {
    now = t / 1000;
    let dt = Math.min(0.05, (t - last) / 1000);
    last = t;

    if (hitStopTimer > 0) {
        hitStopTimer -= dt;
        draw();
        requestAnimationFrame(frame);
        return;
    }

    tick(dt);
    draw();
    requestAnimationFrame(frame);
}

requestAnimationFrame(frame);
