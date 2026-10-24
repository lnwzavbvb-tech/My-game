using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CounterArena
{
    public sealed class ArenaGame : MonoBehaviour
    {
        public static readonly Vector3 PlayerArenaPosition = new Vector3(50, 0, -3.1f);
        public static readonly Vector3 EnemyArenaPosition = new Vector3(50, .7f, 2.1f);
        public Transform ExplorationRoot, BattleRoot, ExplorePlayer, BattlePlayer;
        public Transform[] Patrols, BattleEnemies, Pickups;
        public Camera GameCamera;
        public Material CyanMaterial, OrangeMaterial, GoldMaterial;
        public ArenaRules Rules { get; private set; }
        public Vector2 Position { get; private set; }
        public bool Paused { get; private set; }
        readonly bool[] collected = new bool[3];
        readonly Vector2[] patrolPositions = new Vector2[3];
        readonly float[] patrolDirections = { 1, 1, 0 };
        readonly Color cyan = new Color(.39f, .93f, .82f);
        readonly Color orange = new Color(1, .52f, .25f);
        readonly Color gold = new Color(.94f, .77f, .45f);
        GUIStyle label, small, big, title, button, right;
        Texture2D pixel;
        AudioSource audioSource;
        AudioClip[] tones;
        string animationName = "";
        float animationUntil, animationDuration, hitStop, shake, comboVisibleUntil, flashUntil;
        Transform playerSword, enemySword;
        GameMode previousMode = GameMode.Title;
        Vector3 desiredCamera, desiredLook;
        float desiredSize;
        bool smoke, smokeDriving;
        PixelActor[] allPixelActors;
        readonly List<ArenaRing> impactRings = new List<ArenaRing>();
        const float Width = 1600, Height = 900;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Rules = new ArenaRules(); Rules.Effect += OnEffect;
            audioSource = gameObject.AddComponent<AudioSource>();
            tones = new AudioClip[6];
            for (int i = 0; i < tones.Length; i++) tones[i] = CreateTone(i);
            playerSword = BattlePlayer.Find("SwordPivot");
            var actors = new List<PixelActor>();
            actors.AddRange(ExplorationRoot.GetComponentsInChildren<PixelActor>(true));
            actors.AddRange(BattleRoot.GetComponentsInChildren<PixelActor>(true));
            allPixelActors = actors.ToArray();
            Position = SectorMap.Spawn; ResetWorld();
            SetView(false, true);
            smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "--arena-smoke") >= 0;
            if (smoke) StartCoroutine(SmokeRun());
        }

        public void StartStage()
        {
            Rules.StartStage(); Position = SectorMap.Spawn; Paused = false;
            hitStop = shake = flashUntil = comboVisibleUntil = animationUntil = animationDuration = 0;
            animationName = "";
            audioSource.Stop();
            foreach (ArenaRing ring in impactRings) if (ring) Destroy(ring.gameObject);
            impactRings.Clear();
            foreach (PixelActor actor in GetAllPixelActors()) actor.ResetPresentation();
            ResetWorld(); SyncWorld(true);
        }

        PixelActor[] GetAllPixelActors()
        {
            return allPixelActors;
        }

        void ResetWorld()
        {
            for (int i = 0; i < 3; i++)
            {
                collected[i] = false; Pickups[i].gameObject.SetActive(true);
                patrolPositions[i] = ArenaRules.Enemies[i].Start; patrolDirections[i] = i == 2 ? 0 : 1;
                Patrols[i].gameObject.SetActive(true);
            }
            ExplorePlayer.position = V(Position);
            ExplorePlayer.rotation = Quaternion.identity;
            BattlePlayer.position = PlayerArenaPosition;
            BattlePlayer.rotation = Quaternion.identity;
            for (int i = 0; i < BattleEnemies.Length; i++) BattleEnemies[i].Find("Cue").gameObject.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && Rules.Mode != GameMode.Title) Paused = !Paused;
            if (Paused) return;
            if ((Rules.Mode == GameMode.Title || Rules.Mode == GameMode.Cleared || Rules.Mode == GameMode.Defeated) && Input.GetKeyDown(KeyCode.Return)) StartStage();
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (hitStop > 0) { hitStop -= dt; return; }
            if (!smokeDriving)
            {
                Rules.Tick(dt);
                if (Rules.Mode == GameMode.Explore) Explore(dt);
                if (Rules.Mode == GameMode.Battle) CombatInput();
            }
            SyncWorld(false);
            AnimateActors();
            shake = Mathf.MoveTowards(shake, 0, dt * 1.2f);
        }

        void Explore(float dt)
        {
            Vector2 move = new Vector2(
                (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.Space);
            Position = SectorMap.Move(Position, move, (sprint ? 6.6f : 4.2f) * dt);
            ExplorePlayer.position = V(Position);
            if (move.sqrMagnitude > 0) ExplorePlayer.rotation = Quaternion.Slerp(ExplorePlayer.rotation, Quaternion.LookRotation(V(move)), dt * 18);
            for (int i = 0; i < 3; i++)
            {
                if (Rules.Dead[i]) continue;
                if (i == 0)
                {
                    patrolPositions[i].x += patrolDirections[i] * 2.6f * dt;
                    if (patrolPositions[i].x > 9.6f || patrolPositions[i].x < 4.8f)
                    { patrolPositions[i].x = Mathf.Clamp(patrolPositions[i].x, 4.8f, 9.6f); patrolDirections[i] *= -1; }
                }
                if (i == 1)
                {
                    patrolPositions[i].y += patrolDirections[i] * 1.7f * dt;
                    if (patrolPositions[i].y > .3f || patrolPositions[i].y < -2.7f)
                    { patrolPositions[i].y = Mathf.Clamp(patrolPositions[i].y, -2.7f, .3f); patrolDirections[i] *= -1; }
                }
                Patrols[i].position = V(patrolPositions[i]);
                if (Vector2.Distance(Position, patrolPositions[i]) < SectorMap.PlayerRadius + ArenaRules.Enemies[i].Radius + .24f)
                { Rules.Encounter(i); return; }
            }
            for (int i = 0; i < 3; i++)
            {
                if (collected[i] || Vector2.Distance(Position, SectorMap.Pickups[i]) >= .72f) continue;
                collected[i] = true; Pickups[i].gameObject.SetActive(false);
                Rules.Pickup(i == 1, i == 0 ? 35 : 25);
                Ring(V(SectorMap.Pickups[i]), i == 1 ? GoldMaterial : CyanMaterial, 1.2f);
            }
        }

        void CombatInput()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            { if (!MouseOnHud() || Input.GetKeyDown(KeyCode.Space)) Rules.Act(CombatAction.Light); }
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.K)) Rules.Act(CombatAction.Heavy);
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) Rules.Act(CombatAction.Dash);
            if (Input.GetKeyDown(KeyCode.E)) Rules.Act(CombatAction.Parry);
            if (Input.GetKeyDown(KeyCode.Q)) Rules.Act(CombatAction.Character);
            if (Input.GetKeyDown(KeyCode.W)) Rules.Act(CombatAction.Weapon);
            if (Input.GetKeyDown(KeyCode.R)) Rules.Act(CombatAction.Ultimate);
        }

        bool MouseOnHud()
        {
            // Match the GUI matrix: controls consume the lower strip.
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            float offsetY = (Screen.height - Height * scale) * .5f;
            return (Screen.height - Input.mousePosition.y - offsetY) / scale >= 738;
        }

        void SyncWorld(bool instant)
        {
            bool battle = Rules.Mode == GameMode.Battle || Rules.Mode == GameMode.Return || Rules.Mode == GameMode.Defeated || Rules.Mode == GameMode.Cleared;
            ExplorationRoot.gameObject.SetActive(!battle); BattleRoot.gameObject.SetActive(battle);
            for (int i = 0; i < 3; i++)
            {
                Patrols[i].gameObject.SetActive(!Rules.Dead[i]);
                BattleEnemies[i].gameObject.SetActive(i == Rules.EnemyIndex);
            }
            if (Rules.Mode != previousMode)
            {
                SetView(battle, instant);
                enemySword = BattleEnemies[Rules.EnemyIndex].Find("SwordPivot");
                previousMode = Rules.Mode;
            }
            if (instant) SetView(battle, true);
        }

        void SetView(bool battle, bool instant)
        {
            // Fixed rear composition: foreground hero, elevated central enemy,
            // orthographic depth. Gameplay remains on a 2D ground plane.
            desiredCamera = battle ? new Vector3(50, 6.2f, -16) : new Vector3(0, 18, -15);
            desiredLook = battle ? new Vector3(50, 2, 0) : Vector3.zero;
            desiredSize = battle ? 4.7f : 8.1f;
            if (instant)
            { GameCamera.transform.position = desiredCamera; GameCamera.transform.LookAt(desiredLook); GameCamera.orthographicSize = desiredSize; }
        }

        void LateUpdate()
        {
            if (Paused) return;
            float t = 1 - Mathf.Exp(-Time.unscaledDeltaTime * 8);
            GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, desiredCamera, t);
            GameCamera.transform.rotation = Quaternion.Slerp(GameCamera.transform.rotation, Quaternion.LookRotation(desiredLook - desiredCamera), t);
            // Keep the map visible at narrower window sizes.
            float size = desiredSize * Mathf.Max(1, (16f / 9f) / GameCamera.aspect);
            GameCamera.orthographicSize = Mathf.Lerp(GameCamera.orthographicSize, size, t);
            if (shake > 0) GameCamera.transform.position += UnityEngine.Random.insideUnitSphere * shake * .1f;
        }

        void AnimateActors()
        {
            float clock = Rules.Time;
            for (int i = 0; i < 3; i++)
            {
                if (!collected[i])
                {
                    Transform core = Pickups[i].Find("Core");
                    core.localPosition = new Vector3(0, .65f + Mathf.Sin(clock * 3 + i) * .1f, 0);
                    core.localRotation = Quaternion.Euler(25, clock * 65, 25);
                }
            }
            if (!BattleRoot.gameObject.activeSelf) return;
            float p = animationUntil > clock ? 1 - (animationUntil - clock) / Mathf.Max(.01f, animationDuration) : 1;
            float wave = p < 1 ? Mathf.Sin(p * Mathf.PI) : 0;
            BattlePlayer.GetComponentInChildren<PixelActor>().Pose(animationName, p);
            Vector3 basePosition = PlayerArenaPosition;
            Vector3 lunge = new Vector3(0, 0, 2.1f) * wave;
            float angle = -135;
            if (p < 1)
            {
                if (animationName == "dash") { lunge = new Vector3(-1.4f, 0, -1.5f) * wave; angle = -35; }
                else if (animationName == "guard" || animationName == "parry") { lunge = Vector3.zero; angle = -55; }
                else if (animationName == "hurt") { lunge *= -.35f; angle = 30; }
                else angle = Mathf.Lerp(-160, 70, p);
            }
            BattlePlayer.position = basePosition + lunge;
            playerSword.localRotation = Quaternion.Euler(0, angle, -20);
            BattlePlayer.localRotation = Quaternion.Euler(0, animationName == "heal" && p < 1 ? p * 720 : 0, 0);
            Transform enemy = BattleEnemies[Rules.EnemyIndex];
            float windup = Rules.Phase == BattlePhase.Windup && Rules.Mode == GameMode.Battle ? 1 - Mathf.Clamp01((Rules.Until - clock) / Rules.Move.Duration) : 0;
            enemy.position = EnemyArenaPosition + (Rules.Phase == BattlePhase.Strike && Rules.Mode == GameMode.Battle ? new Vector3(0, 0, -1.25f) : Vector3.zero);
            enemy.localScale = Vector3.one * (Rules.EnemyIndex == 0 ? 1.15f : Rules.EnemyIndex == 1 ? 1.4f : 1.7f);
            if (Rules.Mode == GameMode.Return || Rules.Mode == GameMode.Cleared)
                enemy.localScale *= Mathf.Clamp01((Rules.Until - Rules.Time) / 1.2f);
            if (enemySword) enemySword.localRotation = Quaternion.Euler(0, Rules.Phase == BattlePhase.Strike ? 110 : -windup * 135, -windup * 20);
            Transform cue = enemy.Find("Cue");
            enemy.GetComponentInChildren<PixelActor>().Pose("windup", windup > 0 ? windup : 1);
            cue.gameObject.SetActive(windup > 0);
            if (windup > 0)
            {
                cue.GetComponent<Renderer>().sharedMaterial = Rules.Move.Parryable ? CyanMaterial : OrangeMaterial;
                cue.localScale = Vector3.one * (.18f + windup * .3f);
                cue.Rotate(0, 100 * Time.deltaTime, 0);
            }
        }

        void OnEffect(string name, float value)
        {
            if (name == "hit")
            {
                Ring(EnemyArenaPosition, CyanMaterial, value > 40 ? 3 : 1.6f);
                shake = value > 30 ? .5f : .2f; hitStop = value > 40 ? .06f : .025f; PlayTone(1); return;
            }
            if (name == "light" || name == "heavy") comboVisibleUntil = Rules.Time + 1.2f;
            if (value > 0 && name != "cue") { animationName = name; animationDuration = value; animationUntil = Rules.Time + value; }
            if (name == "parry" || name == "guard" || name == "heal") Ring(BattlePlayer.position, CyanMaterial, 1.8f);
            if (name == "ultimate") { Ring(new Vector3(50, 0, 0), GoldMaterial, 6); PlayTone(4); }
            else if (name == "encounter") { shake = .65f; PlayTone(4); }
            else if (name == "hurt") { flashUntil = Rules.Time + .15f; shake = .7f; PlayTone(1); }
            else if (name == "parry") { hitStop = .09f; shake = .45f; PlayTone(3); }
            else if (name == "cue") PlayTone(value > 0 ? 3 : 0);
            else if (name == "victory" || name == "pickup" || name == "heal") PlayTone(5);
            else PlayTone(2);
        }

        void Ring(Vector3 position, Material material, float radius)
        {
            GameObject go = new GameObject("Impact ring"); go.transform.position = position + Vector3.up * .08f;
            LineRenderer line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material;
            line.useWorldSpace = false; line.positionCount = 49; line.widthMultiplier = .045f;
            for (int i = 0; i < 49; i++)
            { float a = i / 48f * Mathf.PI * 2; line.SetPosition(i, new Vector3(Mathf.Cos(a), .02f, Mathf.Sin(a)) * radius); }
            impactRings.RemoveAll(ring => !ring);
            ArenaRing effect = go.AddComponent<ArenaRing>(); effect.Lifetime = .4f;
            impactRings.Add(effect);
        }

        void PlayTone(int index) { if (audioSource && tones != null) audioSource.PlayOneShot(tones[index], .18f); }
        AudioClip CreateTone(int index)
        {
            const int rate = 22050; int count = (int)(rate * .3f); float[] data = new float[count];
            float frequency = new[] { 260f, 100f, 650f, 1500f, 180f, 880f }[index];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate, fade = Mathf.Exp(-t * 18);
                float phase = frequency * t + (index == 4 ? -150 * t * t : 0);
                data[i] = Mathf.Sin(phase * Mathf.PI * 2) * fade * .7f;
            }
            AudioClip clip = AudioClip.Create("Prototype tone " + index, count, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        void Styles()
        {
            if (pixel) return;
            pixel = Texture2D.whiteTexture;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label = new GUIStyle { font = font, fontSize = 22, normal = { textColor = Color.white } };
            small = new GUIStyle(label) { fontSize = 16 };
            big = new GUIStyle(label) { fontSize = 36, fontStyle = FontStyle.Bold };
            title = new GUIStyle(big) { fontSize = 64 };
            right = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
            button = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 18 };
        }

        void OnGUI()
        {
            if (Rules == null) return;
            Styles();
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) * .5f, (Screen.height - Height * scale) * .5f, 0), Quaternion.identity, Vector3.one * scale);
            Panel(new Rect(0, 0, Width, 112), new Color(.025f, .045f, .075f, .98f));
            Text(42, 22, "COUNTER / ARENA", big, cyan);
            Text(44, 70, "SECTOR 07   /   RUINED CYBER GRID", small, new Color(.5f, .63f, .7f));
            GUI.Label(new Rect(1050, 25, 500, 38), "ACTION SCORE   " + Rules.Score.ToString("D5"), right);
            GUI.Label(new Rect(1050, 68, 500, 30), Rules.Remaining + " TARGETS REMAINING    /    ESC  PAUSE", right);

            bool battle = BattleRoot.gameObject.activeSelf;
            if (battle)
            {
                Panel(new Rect(42, 132, 430, 99), new Color(.03f, .055f, .08f, .95f));
                Text(64, 144, Rules.Enemy.Name, label, Rules.EnemyIndex == 2 ? new Color(1, .28f, .4f) : orange);
                Bar(new Rect(64, 182, 386, 12), Rules.EnemyHp / (float)Rules.Enemy.Health, orange);
                Text(64, 203, Rules.EnemyHp + " / " + Rules.Enemy.Health + " HP", small, Color.white);
                if (Rules.Mode == GameMode.Battle)
                {
                    bool windup = Rules.Phase == BattlePhase.Windup;
                    Color cue = windup ? (Rules.Move.Parryable ? cyan : orange) : gold;
                    string phase = windup ? Rules.Move.Name : Rules.Phase == BattlePhase.Strike ? "IMPACT" : Rules.Phase == BattlePhase.Recovery ? "PUNISH WINDOW" : "YOUR OPENING";
                    Panel(new Rect(1120, 132, 438, 100), new Color(.025f, .04f, .06f, .92f));
                    Center(1339, 150, phase, label, cue, 430);
                    Center(1339, 189, windup ? (Rules.Move.Parryable ? "E  /  PARRY" : "SHIFT  /  EVADE") + "    -    HIT " + (Rules.MoveIndex + 1) + "/" + Rules.Enemy.Chains[Rules.ChainIndex].Length : "LMB + RMB  /  MIX YOUR COMBOS", small, Color.white, 430);
                    if (windup) Bar(new Rect(1120, 229, 438, 5), 1 - Mathf.Clamp01((Rules.Until - Rules.Time) / Rules.Move.Duration), cue);
                }
                if (Rules.Time < comboVisibleUntil)
                {
                    Panel(new Rect(485, 632, 630, 67), new Color(.025f, .04f, .06f, .9f));
                    Center(800, 642, Rules.Command + "    /    " + Rules.Combo, label, gold, 620);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (!Rules.Dead[i])
                    {
                        SpriteRenderer sprite = Patrols[i].GetComponentInChildren<SpriteRenderer>();
                        float height = sprite.sprite.bounds.max.y * Patrols[i].localScale.y + .35f;
                        WorldTag(Patrols[i].position + GameCamera.transform.up * height, ArenaRules.Enemies[i].Name, i == 2 ? orange : cyan, scale);
                    }
                    if (!collected[i]) WorldTag(Pickups[i].position + Vector3.up * 1.25f, i == 1 ? "ULT" : "+HP", i == 1 ? gold : cyan, scale);
                }
                Panel(new Rect(42, 132, 315, 106), new Color(.025f, .04f, .06f, .92f));
                Text(60, 146, "EXPLORATION", label, cyan);
                Text(60, 184, "Approach a target to engage.", small, Color.white);
                Text(60, 209, "Gold core: ultimate   /   Cyan: HP", small, gold);
            }
            Panel(new Rect(0, 724, Width, 176), new Color(.025f, .045f, .075f, .98f));
            Text(42, 740, "PLAYER", label, cyan);
            Text(200, 744, Rules.Hp + " / 100 HP", small, Color.white);
            Bar(new Rect(42, 777, 310, 12), Rules.Hp / 100f, cyan);
            Text(42, 800, "STAMINA " + Mathf.FloorToInt(Rules.Stamina), small, new Color(.5f, .7f, 1));
            Bar(new Rect(42, 825, 310, 7), Rules.Stamina / 100, new Color(.4f, .62f, .9f));
            Text(42, 843, "ULTIMATE " + Rules.Energy + "%", small, gold);
            Bar(new Rect(42, 870, 310, 7), Rules.Energy / 100f, gold);
            if (battle)
            {
                string[] keys = { "LMB / SPACE", "RMB / F", "SHIFT", "E", "Q", "W", "R" };
                string[] names = { "LIGHT", "HEAVY", "DASH", "PARRY", "MEND", "SUNDER", "ULTIMATE" };
                string[] detail = { "Combo starter", "Combo finisher", "25 stamina", "20 stamina", "+18 HP / 15 DMG", "32 damage", "70 damage" };
                for (int i = 0; i < 7; i++)
                {
                    Rect r = new Rect(386 + i * 167, 741, 153, 98);
                    CombatAction action = (CombatAction)i;
                    bool enabled = !Paused && Rules.CanAct(action);
                    Color accent = i == 6 ? gold : i == 1 ? orange : cyan;
                    Panel(r, enabled ? new Color(.08f, .14f, .18f) : new Color(.055f, .075f, .095f));
                    Panel(new Rect(r.x, r.y, r.width, 3), enabled ? accent : new Color(.15f, .2f, .24f));
                    Center(r.center.x, r.y + 12, keys[i], small, enabled ? accent : Color.gray, 150);
                    Center(r.center.x, r.y + 38, names[i], button, enabled ? Color.white : Color.gray, 150);
                    float cd = Mathf.Max(0, Rules.ReadyAt[i] - Rules.Time);
                    Center(r.center.x, r.y + 71, cd > 0 ? cd.ToString("F1") + "s" : detail[i], small, enabled ? Color.white : Color.gray, 150);
                    GUI.enabled = enabled;
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none)) Rules.Act(action);
                    GUI.enabled = true;
                }
                Text(386, 856, Rules.Message, small, Color.white);
            }
            else
            {
                Text(410, 750, "W A S D / ARROWS    MOVE          SHIFT / SPACE    SPRINT", label, Color.white);
                Text(410, 796, "Reach all three targets. Clear the sector. Keep your health between battles.", small, new Color(.55f, .7f, .76f));
                Text(410, 850, Rules.Message, small, gold);
            }
            if (Rules.Mode == GameMode.Encounter)
            {
                Panel(new Rect(0, 0, Width, Height), new Color(.02f, .03f, .055f, .83f));
                Panel(new Rect(0, 370, Width, 160), new Color(.12f, .025f, .06f, .95f));
                Center(800, 383, "ENCOUNTER", title, orange, 1300);
                Center(800, 466, Rules.Enemy.Name, label, Color.white, 1300);
            }
            if (Rules.Time < flashUntil) Panel(new Rect(0, 112, Width, 612), new Color(1, .1f, .15f, .18f));
            if (Rules.UltimateUntil > Rules.Time)
            {
                Panel(new Rect(0, 341, Width, 34), Color.black);
                Center(800, 347, "OMNI SLASH / DIMENSION RIFT", label, gold, 1300);
                float p = Rules.UltimateProgress;
                for (int i = 0; i < 4; i++)
                    if (p > .22f + i * .08f) DrawSlash(new Vector2(620 + i * 100, 230), new Vector2(985 - i * 95, 470), i % 2 == 0 ? cyan : gold);
            }
            if (Paused || Rules.Mode == GameMode.Title || Rules.Mode == GameMode.Cleared || Rules.Mode == GameMode.Defeated)
                Overlay();
            GUI.matrix = Matrix4x4.identity;
        }

        void Overlay()
        {
            Panel(new Rect(0, 112, Width, 612), new Color(.02f, .035f, .06f, .88f));
            Panel(new Rect(360, 220, 880, 450), new Color(.035f, .06f, .09f, .98f));
            Panel(new Rect(360, 220, 880, 4), Rules.Mode == GameMode.Defeated ? orange : cyan);
            string heading = Paused ? "PAUSED" : Rules.Mode == GameMode.Cleared ? "SECTOR LIBERATED" : Rules.Mode == GameMode.Defeated ? "YOU FELL IN BATTLE" : "COUNTER / ARENA";
            Center(800, 265, heading, title, Rules.Mode == GameMode.Defeated ? orange : cyan, 850);
            string intro = Paused ? "Take a breath. Your timing matters." : Rules.Mode == GameMode.Title ? "Explore. Encounter. Counterattack." : "ACTION SCORE  /  " + Rules.Score.ToString("D5");
            Center(800, 355, intro, label, Color.white, 850);
            Center(800, 412, "CYAN ATTACK  /  E TO PARRY       ORANGE ATTACK  /  SHIFT TO EVADE", small, gold, 850);
            Center(800, 450, "COMBOS:  L L L   /   L H   /   L L H   /   H L   /   H H", small, Color.white, 850);
            Rect r = new Rect(560, 520, 480, 68);
            Panel(r, cyan);
            GUIStyle dark = new GUIStyle(button) { fontSize = 22, normal = { textColor = new Color(.025f, .06f, .08f) } };
            if (GUI.Button(r, Paused ? "RESUME  /  ESC" : Rules.Mode == GameMode.Title ? "ENTER SECTOR  /  ENTER" : "RESTART SECTOR  /  ENTER", dark))
            { if (Paused) Paused = false; else StartStage(); }
            Center(800, 618, "UNITY C# PROTOTYPE   /   PRIMITIVE MODELS + MOCKUP MATERIALS", small, new Color(.4f, .55f, .62f), 850);
        }

        void DrawSlash(Vector2 a, Vector2 b, Color color)
        {
            Matrix4x4 old = GUI.matrix; GUIUtility.RotateAroundPivot(Vector2.SignedAngle(Vector2.right, b - a), a);
            Panel(new Rect(a.x, a.y, Vector2.Distance(a, b), 5), color); GUI.matrix = old;
        }
        void WorldTag(Vector3 world, string caption, Color color, float scale)
        {
            Vector3 p = GameCamera.WorldToScreenPoint(world);
            if (p.z <= 0) return;
            float x = (p.x - (Screen.width - Width * scale) * .5f) / scale;
            float y = (Screen.height - p.y - (Screen.height - Height * scale) * .5f) / scale;
            y = Mathf.Clamp(y, 132, 690);
            float w = small.CalcSize(new GUIContent(caption)).x + 16;
            Panel(new Rect(x - w / 2, y - 2, w, 24), new Color(.025f, .04f, .06f, .87f));
            Center(x, y, caption, small, color, w);
        }
        void Panel(Rect rect, Color color) { GUI.color = color; GUI.DrawTexture(rect, pixel); GUI.color = Color.white; }
        void Bar(Rect rect, float value, Color color)
        { Panel(rect, new Color(.12f, .18f, .23f)); rect.width *= Mathf.Clamp01(value); Panel(rect, color); }
        void Text(float x, float y, string text, GUIStyle style, Color color)
        { Color old = style.normal.textColor; style.normal.textColor = color; GUI.Label(new Rect(x, y, 1180, style.fontSize + 8), text, style); style.normal.textColor = old; }
        void Center(float x, float y, string text, GUIStyle style, Color color, float width)
        {
            TextAnchor old = style.alignment; Color oldColor = style.normal.textColor;
            style.alignment = TextAnchor.UpperCenter; style.normal.textColor = color;
            GUI.Label(new Rect(x - width / 2, y, width, style.fontSize + 10), text, style);
            style.alignment = old; style.normal.textColor = oldColor;
        }
        static Vector3 V(Vector2 p) => new Vector3(p.x, 0, p.y);

        // Run the built player with --arena-smoke for live render + flow validation.
        IEnumerator SmokeRun()
        {
            smokeDriving = true;
            string folder = Path.Combine(Application.dataPath, "..", "SmokeResults"); Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);
            StartStage(); SyncWorld(true);
            yield return new WaitForSecondsRealtime(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "exploration.png"));
            yield return new WaitForSecondsRealtime(.3f);
            // Exercise actual world movement, pickups, and proximity encounter.
            Position = SectorMap.Pickups[1]; Explore(0);
            bool pickup = Rules.Energy == 100 && !Pickups[1].gameObject.activeSelf;
            Position = patrolPositions[2]; Explore(0);
            bool encounter = Rules.Mode == GameMode.Encounter;
            for (int i = 0; i < 90; i++) Rules.Tick(.01f);
            SyncWorld(true);
            for (int i = 0; i < 181; i++) Rules.Tick(.01f);
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "battle.png"));
            yield return new WaitForSecondsRealtime(.3f);
            bool battle = Rules.Mode == GameMode.Battle && BattleRoot.gameObject.activeSelf;
            bool ultimate = Rules.Act(CombatAction.Ultimate);
            for (int i = 0; i < 60; i++) Rules.Tick(.01f);
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "ultimate.png"));
            yield return new WaitForSecondsRealtime(.3f);
            for (int i = 0; i < 90; i++) Rules.Tick(.01f);
            bool ultimateDamage = Rules.EnemyHp == 250;
            // Reproduce the reported bug: a fatal hit leaves a future flash timer,
            // then restart resets the simulation clock to zero in the same frame.
            for (int i = 0; i < 10000 && Rules.Mode == GameMode.Battle; i++) Rules.Tick(.01f);
            bool fatalFlash = Rules.Mode == GameMode.Defeated && flashUntil > Rules.Time;
            StartStage();
            bool restart = fatalFlash && Rules.Mode == GameMode.Explore && Rules.Hp == 100 && Rules.Score == 0
                && flashUntil == 0 && hitStop == 0 && animationUntil == 0 && comboVisibleUntil == 0
                && ExplorationRoot.gameObject.activeSelf && !BattleRoot.gameObject.activeSelf;
            foreach (PixelActor actor in GetAllPixelActors()) restart &= actor.GetComponent<SpriteRenderer>().color == Color.white;
            yield return null;
            restart &= impactRings.Count == 0;
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "restart.png"));
            bool passed = pickup && encounter && battle && ultimate && ultimateDamage && restart;
            File.WriteAllText(Path.Combine(folder, "result.json"), "{\"passed\":" + passed.ToString().ToLowerInvariant() + ",\"pickup\":" + pickup.ToString().ToLowerInvariant() + ",\"encounter\":" + encounter.ToString().ToLowerInvariant() + ",\"battle\":" + battle.ToString().ToLowerInvariant() + ",\"ultimateDamage\":" + ultimateDamage.ToString().ToLowerInvariant() + ",\"restartAfterFatalHit\":" + restart.ToString().ToLowerInvariant() + "}");
            yield return new WaitForSecondsRealtime(.3f);
            Application.Quit(passed ? 0 : 1);
        }

        void OnDestroy()
        {
            if (Rules != null) Rules.Effect -= OnEffect;
            if (tones != null) foreach (AudioClip clip in tones) if (clip) Destroy(clip);
        }
    }

    public sealed class ArenaRing : MonoBehaviour
    {
        public float Lifetime = .4f;
        float age;
        void Update()
        {
            if (FindAnyObjectByType<ArenaGame>() is ArenaGame game && game.Paused) return;
            age += Time.deltaTime; transform.localScale = Vector3.one * (1 + age * 2);
            if (age >= Lifetime) Destroy(gameObject);
        }
    }
}
