using System;
using System.Collections.Generic;
using System.IO;
using CounterArena;
using UnityEngine;

public static class ArenaValidation
{
    static readonly List<string> checks = new List<string>();
    static void Check(bool condition, string name)
    { if (!condition) throw new Exception("ARENA_VALIDATION_FAILED: " + name); checks.Add(name); }
    static void Advance(ArenaRules r, float seconds)
    { int steps = Mathf.CeilToInt(seconds / .005f); for (int i = 0; i < steps; i++) r.Tick(seconds / steps); }
    static ArenaRules Battle(int enemy = 2)
    { var r = new ArenaRules(); r.StartStage(); r.Encounter(enemy); Advance(r, .86f); return r; }
    static void BeforeImpact(ArenaRules r)
    {
        int safety = 0;
        while (r.Phase != BattlePhase.Windup && safety++ < 1000) r.Tick(.005f);
        while (r.Until - r.Time > .1f && safety++ < 2000) r.Tick(.005f);
    }

    [UnityEditor.MenuItem("Counter Arena/Validate gameplay rules")]
    public static void Run()
    {
        checks.Clear();
        var r = Battle();
        Check(r.Mode == GameMode.Battle && r.EnemyHp == 320, "Encounter transitions into boss battle");
        r.Act(CombatAction.Light); Advance(r, .29f); r.Act(CombatAction.Light); Advance(r, .29f); r.Act(CombatAction.Heavy);
        Check(r.EnemyHp == 247 && r.Combo == "CYCLONE EXECUTION", "LLH deals 12 + 16 + 45 damage");
        var combos = new[] { "LLL", "LH", "HL", "HH" };
        int[] damages = { 54, 46, 55, 69 };
        for (int i = 0; i < combos.Length; i++)
        {
            r = Battle(); foreach (char c in combos[i]) { r.Act(c == 'L' ? CombatAction.Light : CombatAction.Heavy); Advance(r, .43f); }
            Check(r.EnemyHp == 320 - damages[i], combos[i] + " combo damage");
        }
        r = Battle(); r.Act(CombatAction.Light); int hp = r.EnemyHp;
        Check(!r.Act(CombatAction.Light) && r.EnemyHp == hp, "Attack cooldown blocks spam");
        Advance(r, 1.2f); r.Act(CombatAction.Heavy);
        Check(r.Combo == "STINGER THRUST", "Combo queue expires");
        r = Battle(); BeforeImpact(r); r.Act(CombatAction.Parry); Advance(r, .13f);
        Check(r.Hp == 100 && r.EnemyHp == 302 && r.Energy >= 29, "Cyan parry prevents damage and ripostes");
        r = Battle(1); BeforeImpact(r); r.Act(CombatAction.Parry); Advance(r, .13f);
        Check(r.Hp == 75, "Orange attacks cannot be parried");
        r = Battle(1); BeforeImpact(r); r.Act(CombatAction.Dash); Advance(r, .13f);
        Check(r.Hp == 100 && r.Energy == 12, "Dash invulnerability avoids impact");
        Check(!r.Act(CombatAction.Parry), "Cannot overlap dash and parry");
        Advance(r, .9f); Check(r.Stamina > 75, "Stamina regenerates after delay");
        r = Battle(); Check(!r.Act(CombatAction.Ultimate), "Ultimate requires 100 energy");
        r = new ArenaRules(); r.StartStage(); r.Pickup(true, 0); r.Encounter(2); Advance(r, .86f);
        r.Act(CombatAction.Ultimate); Advance(r, 1.1f);
        Check(r.EnemyHp == 250 && r.Energy == 35 && r.Hp == 100, "Ultimate consumes meter and deals 70 with safe cinematic");
        r = new ArenaRules(); r.StartStage(); r.Pickup(true, 0); r.Encounter(2); Advance(r, .86f);
        r.Act(CombatAction.Weapon);
        Check(!r.Act(CombatAction.Ultimate), "Ultimate cannot overwrite a pending skill hit");
        r = Battle(); r.Act(CombatAction.Weapon); Advance(r, .23f);
        Check(r.EnemyHp == 288 && !r.Act(CombatAction.Weapon), "Weapon skill delayed damage and cooldown");
        r.Act(CombatAction.Character); Advance(r, .19f);
        Check(r.EnemyHp == 273, "Mend skill deals 15 damage");
        r = Battle(); Advance(r, 4);
        Check(r.Hp < 100, "Unanswered enemy chains damage player");
        int beforeHeal = r.Hp; r.Act(CombatAction.Character);
        Check(r.Hp == Mathf.Min(100, beforeHeal + 18), "Mend restores 18 HP");
        r = Battle(); Advance(r, 60);
        Check(r.Mode == GameMode.Defeated && r.Hp == 0, "Defeat flow stops battle");
        r.StartStage();
        Check(r.Mode == GameMode.Explore && r.Hp == 100 && r.Energy == 0 && r.Score == 0 && r.Remaining == 3, "Restart fully resets state");
        // Complete a sector using only normal heavy attacks within their cooldown.
        r = new ArenaRules(); r.StartStage();
        for (int i = 0; i < 3; i++)
        {
            r.Encounter(i); Advance(r, .86f);
            int safety = 0;
            while (r.Mode == GameMode.Battle && safety++ < 1000)
            { r.Act(CombatAction.Heavy); Advance(r, .43f); }
            Check(r.Dead[i] && r.Mode != GameMode.Defeated, "Enemy " + i + " can be defeated with normal attacks");
            Check(r.Hp <= 100, "Victory reward clamps health " + i);
            Advance(r, 1.3f);
        }
        Check(r.Mode == GameMode.Cleared && r.Remaining == 0 && r.Score >= 900, "All three victories clear the sector");
        r.StartStage(); r.Encounter(0); Advance(r, .86f); r.Act(CombatAction.Weapon); r.StartStage(); Advance(r, 2);
        Check(r.Mode == GameMode.Explore && r.Hp == 100 && r.Score == 0, "Restart cancels delayed skill damage");
        Check(!SectorMap.Blocked(SectorMap.Spawn), "Spawn is walkable");
        foreach (Vector2 p in SectorMap.Pickups) Check(!SectorMap.Blocked(p), "Pickup is accessible " + p);
        Vector2 movement = SectorMap.Move(SectorMap.Spawn, Vector2.right, 30);
        Check(movement.x < -4.7f && !SectorMap.Blocked(movement), "Sprint substeps cannot tunnel through wall");
        Vector2 diag = SectorMap.Move(Vector2.zero, Vector2.one, 1);
        Check(Mathf.Abs(diag.magnitude - 1) < .01f, "Diagonal movement is normalized");
        // Flood-fill navigation proves all enemy/pickup locations share the spawn region.
        var visited = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>();
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(SectorMap.Spawn.x * 5), Mathf.RoundToInt(SectorMap.Spawn.y * 5));
        queue.Enqueue(start); visited.Add(start);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            foreach (Vector2Int d in directions)
            {
                Vector2Int n = p + d;
                if (Mathf.Abs(n.x) > 54 || Mathf.Abs(n.y) > 27 || visited.Contains(n) || SectorMap.Blocked((Vector2)n / 5)) continue;
                visited.Add(n); queue.Enqueue(n);
            }
        }
        foreach (EnemyDefinition e in ArenaRules.Enemies)
            Check(visited.Contains(new Vector2Int(Mathf.RoundToInt(e.Start.x * 5), Mathf.RoundToInt(e.Start.y * 5))), "Route to " + e.Name);
        foreach (Vector2 p in SectorMap.Pickups)
            Check(visited.Contains(new Vector2Int(Mathf.RoundToInt(p.x * 5), Mathf.RoundToInt(p.y * 5))), "Route to pickup " + p);
        Directory.CreateDirectory("Validation");
        File.WriteAllLines("Validation/gameplay-checks.txt", checks);
#if !ARENA_STANDALONE_VALIDATION
        Debug.Log("ARENA_VALIDATION_PASSED " + checks.Count + " checks");
#endif
    }
}
