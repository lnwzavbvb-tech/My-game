using System;
using System.Collections.Generic;
using UnityEngine;

namespace CounterArena
{
    public enum GameMode { Title, Explore, Encounter, Battle, Return, Cleared, Defeated }
    public enum BattlePhase { Opening, Windup, Strike, Recovery }
    public enum CombatAction { Light, Heavy, Dash, Parry, Character, Weapon, Ultimate }

    [Serializable]
    public struct EnemyMove
    {
        public string Name;
        public bool Parryable;
        public float Duration;
        public int Damage;
        public EnemyMove(string name, bool parryable, float duration, int damage)
        { Name = name; Parryable = parryable; Duration = duration; Damage = damage; }
    }

    public sealed class EnemyDefinition
    {
        public string Name;
        public int Health;
        public Vector2 Start;
        public float Radius;
        public EnemyMove[][] Chains;
        public EnemyDefinition(string name, int health, Vector2 start, float radius, params EnemyMove[][] chains)
        { Name = name; Health = health; Start = start; Radius = radius; Chains = chains; }
    }

    // Pure C# combat state. Time is advanced explicitly, so cooldowns and defenses
    // use the same clock during play, pause, hit stop, and automated validation.
    public sealed class ArenaRules
    {
        public static readonly EnemyDefinition[] Enemies = {
            new EnemyDefinition("CYBER SENTINEL", 140, new Vector2(7, -3.5f), .4f,
                new[] { new EnemyMove("QUICK THRUST", true, .65f, 14), new EnemyMove("HEAVY CLEAVE", false, .78f, 22) }),
            new EnemyDefinition("HEAVY ENFORCER", 220, new Vector2(0, -1.1f), .52f,
                new[] { new EnemyMove("HEAVY CLEAVE", false, .85f, 25), new EnemyMove("EARTH CRUSHER", false, .75f, 30) }),
            new EnemyDefinition("IRON WARDEN", 320, new Vector2(0, 3.9f), .72f,
                new[] { new EnemyMove("QUICK THRUST", true, .65f, 14), new EnemyMove("RAPID JAB", true, .55f, 16), new EnemyMove("EARTHSHAKER CLEAVE", false, .88f, 32) },
                new[] { new EnemyMove("HEAVY SWEEP", false, .78f, 20), new EnemyMove("COUNTER THRUST", true, .60f, 18), new EnemyMove("OVERHEAD IMPACT", false, .85f, 30) })
        };
        public static readonly float[] Cooldowns = { .28f, .42f, .45f, .3f, 8.5f, 4.5f, 1.4f };
        public readonly bool[] Dead = new bool[3];
        public readonly float[] ReadyAt = new float[7];
        public GameMode Mode { get; private set; } = GameMode.Title;
        public BattlePhase Phase { get; private set; }
        public int Hp { get; private set; } = 100;
        public int EnemyHp { get; private set; }
        public float Stamina { get; private set; } = 100;
        public int Energy { get; private set; }
        public int Score { get; private set; }
        public int EnemyIndex { get; private set; }
        public int ChainIndex { get; private set; }
        public int MoveIndex { get; private set; }
        public float Time { get; private set; }
        public float Until { get; private set; }
        public float DashUntil { get; private set; }
        public float ParryUntil { get; private set; }
        public float UltimateUntil { get; private set; }
        public string Message { get; private set; } = "Explore Sector 07. Defeat all three targets.";
        public string Combo { get; private set; } = "";
        public string Command { get; private set; } = "";
        public EnemyDefinition Enemy => Enemies[EnemyIndex];
        public EnemyMove Move => Enemy.Chains[ChainIndex][MoveIndex];
        public int Remaining { get { int n = 0; foreach (bool d in Dead) if (!d) n++; return n; } }
        public float UltimateProgress => UltimateUntil > Time ? 1 - (UltimateUntil - Time) / 1.4f : 0;
        public event Action<string, float> Effect;
        string queue = "";
        float queueUntil, regenAt, pendingAt;
        int pendingDamage, round;
        bool cuePlayed;

        public void StartStage()
        {
            Array.Clear(Dead, 0, Dead.Length);
            Array.Clear(ReadyAt, 0, ReadyAt.Length);
            Hp = 100; Stamina = 100; Energy = 0; Score = 0; Time = 0;
            DashUntil = ParryUntil = UltimateUntil = pendingAt = regenAt = 0;
            pendingDamage = 0; queue = Command = Combo = "";
            Until = queueUntil = 0; EnemyIndex = EnemyHp = ChainIndex = MoveIndex = round = 0;
            Phase = BattlePhase.Opening; cuePlayed = false;
            Mode = GameMode.Explore;
            Message = "SECTOR 07 / Find the targets. Cyan = health. Gold = ultimate.";
        }

        public void Encounter(int index)
        {
            if (Mode != GameMode.Explore || index < 0 || index >= Dead.Length || Dead[index]) return;
            EnemyIndex = index; Mode = GameMode.Encounter; Until = Time + .85f;
            Message = "ENCOUNTER / " + Enemy.Name;
            Effect?.Invoke("encounter", 0);
        }

        void BeginBattle()
        {
            EnemyHp = Enemy.Health; Stamina = 100;
            Array.Clear(ReadyAt, 0, ReadyAt.Length);
            DashUntil = ParryUntil = UltimateUntil = regenAt = 0;
            pendingDamage = 0; queue = Command = Combo = "";
            round = ChainIndex = MoveIndex = 0;
            Mode = GameMode.Battle; Phase = BattlePhase.Opening; Until = Time + 1.8f;
            Message = "YOUR OPENING / Mix light and heavy attacks.";
        }

        public void Tick(float dt)
        {
            if (dt <= 0) return;
            Time += dt;
            if (Mode == GameMode.Encounter && Time >= Until) { BeginBattle(); return; }
            if (Mode == GameMode.Return && Time >= Until)
            { Mode = Remaining == 0 ? GameMode.Cleared : GameMode.Explore; return; }
            if (Mode != GameMode.Battle) return;
            if (Time >= regenAt) Stamina = Mathf.Min(100, Stamina + 22 * dt);
            if (queue.Length > 0 && Time > queueUntil) queue = "";
            if (pendingDamage > 0 && Time >= pendingAt)
            {
                int n = pendingDamage; pendingDamage = 0; Damage(n);
                if (Mode != GameMode.Battle) return;
            }
            // Freeze enemy windup while the cinematic plays, retaining its timing.
            if (UltimateUntil > Time) { Until += dt; return; }
            if (Phase == BattlePhase.Windup && !cuePlayed && Time >= Until - .35f)
            { cuePlayed = true; Effect?.Invoke("cue", Move.Parryable ? 1 : 0); }
            if (Time < Until) return;
            switch (Phase)
            {
                case BattlePhase.Opening:
                    ChainIndex = round++ % Enemy.Chains.Length; MoveIndex = 0; BeginMove(); break;
                case BattlePhase.Windup: Impact(); break;
                case BattlePhase.Strike:
                    if (++MoveIndex < Enemy.Chains[ChainIndex].Length) BeginMove();
                    else { MoveIndex--; Phase = BattlePhase.Recovery; Until = Time + 1.1f; Message = "RECOVERY / Unleash your combo!"; }
                    break;
                default: Phase = BattlePhase.Opening; Until = Time + 1.8f; Message = "YOUR OPENING / Build your ultimate gauge."; break;
            }
        }

        void BeginMove()
        {
            Phase = BattlePhase.Windup; Until = Time + Move.Duration; cuePlayed = false;
            Message = Move.Parryable ? "CYAN / Press E just before impact to parry." : "ORANGE / Unblockable. Press SHIFT to evade.";
        }

        public bool CanAct(CombatAction action)
        {
            if (Mode != GameMode.Battle || Time < ReadyAt[(int)action] || Time < UltimateUntil) return false;
            if (action == CombatAction.Ultimate && (Energy < 100 || pendingDamage > 0)) return false;
            if (action == CombatAction.Weapon || action == CombatAction.Character) return pendingDamage == 0;
            if (action == CombatAction.Dash || action == CombatAction.Parry)
                return Stamina >= (action == CombatAction.Dash ? 25 : 20) && Time >= DashUntil && Time >= ParryUntil;
            return true;
        }

        public bool Act(CombatAction action)
        {
            if (!CanAct(action)) return false;
            ReadyAt[(int)action] = Time + Cooldowns[(int)action];
            switch (action)
            {
                case CombatAction.Light: Attack("L"); break;
                case CombatAction.Heavy: Attack("H"); break;
                case CombatAction.Dash:
                    Stamina -= 25; regenAt = Time + .8f; DashUntil = Time + .45f;
                    Message = "DASH / Invulnerable for 0.45s."; Effect?.Invoke("dash", .45f); break;
                case CombatAction.Parry:
                    Stamina -= 20; regenAt = Time + .8f; ParryUntil = Time + .3f;
                    Message = "PARRY / Guard window 0.30s."; Effect?.Invoke("guard", .3f); break;
                case CombatAction.Character:
                    Hp = Mathf.Min(100, Hp + 18); pendingDamage = 15; pendingAt = Time + .18f;
                    Message = "WIND MEND / +18 HP"; Effect?.Invoke("heal", .55f); break;
                case CombatAction.Weapon:
                    pendingDamage = 32; pendingAt = Time + .22f;
                    Message = "ARMOR SUNDER / 32 DAMAGE"; Effect?.Invoke("weapon", .6f); break;
                case CombatAction.Ultimate:
                    Energy = 0; UltimateUntil = Time + 1.4f; pendingDamage = 70; pendingAt = Time + 1.05f;
                    Message = "OMNI SLASH / DIMENSION RIFT"; Effect?.Invoke("ultimate", 1.4f); break;
            }
            return true;
        }

        void Attack(string type)
        {
            if (Time > queueUntil) queue = "";
            string key = queue + type;
            int damage;
            switch (key)
            {
                case "L": Combo = "LIGHT SLASH I"; damage = 12; break;
                case "LL": Combo = "LIGHT SLASH II"; damage = 16; break;
                case "LLL": Combo = "TRIPLE BLADE WALTZ"; damage = 26; break;
                case "LH": Combo = "RISING FALCON CLEAVE"; damage = 34; break;
                case "LLH": Combo = "CYCLONE EXECUTION"; damage = 45; break;
                case "H": Combo = "STINGER THRUST"; damage = 25; break;
                case "HL": Combo = "VIPER RIPOSTE"; damage = 30; break;
                case "HH": Combo = "CRAG BREAKER"; damage = 44; break;
                default: queue = ""; Attack(type); return;
            }
            Command = string.Join("  >  ", key.ToCharArray());
            queue = key.Length >= 3 || key == "LH" || key == "HL" || key == "HH" ? "" : key;
            queueUntil = Time + 1.1f;
            Message = Combo + " / " + damage + " DAMAGE";
            Effect?.Invoke(type == "L" ? "light" : "heavy", .35f);
            Damage(damage);
        }

        void Impact()
        {
            Phase = BattlePhase.Strike; Until = Time + .22f;
            if (DashUntil > Time)
            { Gain(12); Message = "EVADE / +12"; Effect?.Invoke("evade", 0); }
            else if (ParryUntil > Time && Move.Parryable)
            { Gain(20); Message = "PERFECT PARRY / RIPOSTE"; Effect?.Invoke("parry", .4f); Damage(18); }
            else
            {
                Hp = Mathf.Max(0, Hp - Move.Damage);
                Message = "HIT / -" + Move.Damage + " HP";
                Effect?.Invoke("hurt", .35f);
                if (Hp == 0) { Mode = GameMode.Defeated; ClearPending(); }
            }
        }

        void Damage(int amount)
        {
            if (Mode != GameMode.Battle) return;
            EnemyHp = Mathf.Max(0, EnemyHp - amount);
            Gain(Mathf.RoundToInt(amount / 2f)); Effect?.Invoke("hit", amount);
            if (EnemyHp > 0) return;
            Dead[EnemyIndex] = true; Gain(300); Hp = Mathf.Min(100, Hp + 25);
            Mode = GameMode.Return; Until = Time + 1.2f; ClearPending();
            Message = Enemy.Name + " DEFEATED / +25 HP / +300 SCORE"; Effect?.Invoke("victory", 0);
        }

        void ClearPending() { pendingDamage = 0; UltimateUntil = DashUntil = ParryUntil = 0; queue = ""; }
        void Gain(int n) { Score += n; Energy = Mathf.Min(100, Energy + n); }
        public void Pickup(bool ultimate, int health)
        {
            if (Mode != GameMode.Explore) return;
            if (ultimate) Energy = 100; else Hp = Mathf.Min(100, Hp + health);
            Gain(100); Message = ultimate ? "ADRENALINE CORE / ULTIMATE READY" : "NANITE MEDKIT / +" + health + " HP";
            Effect?.Invoke("pickup", 0);
        }
    }

    public static class SectorMap
    {
        public static readonly Vector2 Spawn = new Vector2(-8.4f, -3.7f);
        public const float PlayerRadius = .36f;
        public static readonly Rect[] Walls = {
            new Rect(-11.2f, 5.3f, 22.4f, .4f), new Rect(-11.2f, -5.7f, 22.4f, .4f),
            new Rect(-11.2f, -5.7f, .4f, 11.4f), new Rect(10.8f, -5.7f, .4f, 11.4f),
            new Rect(-4.4f, -5.7f, .5f, 5f), new Rect(4f, -5.7f, .5f, 5f),
            new Rect(-8.8f, .8f, 5.2f, .5f), new Rect(3.6f, .8f, 5.2f, .5f),
            new Rect(-2.4f, 1.9f, .6f, .8f), new Rect(1.8f, 1.9f, .6f, .8f)
        };
        public static readonly Vector2[] Pickups = { new Vector2(8.8f, -3.5f), new Vector2(-8.8f, 3.5f), new Vector2(9, 3.5f) };
        public static bool Blocked(Vector2 p)
        {
            foreach (Rect w in Walls)
            {
                Vector2 nearest = new Vector2(Mathf.Clamp(p.x, w.xMin, w.xMax), Mathf.Clamp(p.y, w.yMin, w.yMax));
                if ((p - nearest).sqrMagnitude < PlayerRadius * PlayerRadius) return true;
            }
            return false;
        }
        public static Vector2 Move(Vector2 position, Vector2 direction, float distance)
        {
            Vector2 d = Vector2.ClampMagnitude(direction, 1) * distance;
            // Substeps keep collision reliable during low frame rates and sprints.
            int steps = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / .15f));
            d /= steps;
            for (int i = 0; i < steps; i++)
            {
                Vector2 x = position + new Vector2(d.x, 0); if (!Blocked(x)) position = x;
                Vector2 y = position + new Vector2(0, d.y); if (!Blocked(y)) position = y;
            }
            return position;
        }
    }
}
