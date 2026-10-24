using System;
using System.IO;
using CounterArena;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ArenaProjectBuilder
{
    const string Root = "Assets/CounterArena";
    const string ScenePath = Root + "/Scenes/Sector07.unity";
    static Material floor, wall, trim, cyan, orange, gold, red, steel, dark, white, violet, blue, rock, cloth;

    [InitializeOnLoadMethod]
    static void FirstOpen()
    {
        if (Application.isBatchMode || File.Exists(ScenePath)) return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                CreateScene();
        };
    }

    [MenuItem("Counter Arena/Create or rebuild mockup scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Prefabs");
        PixelArtAssets.Generate();
        floor = Mat("Floor", new Color(.025f, .055f, .08f));
        wall = Mat("Wall", new Color(.10f, .17f, .22f));
        trim = Mat("Grid", new Color(.07f, .22f, .26f), true);
        cyan = Mat("Player_Cyan", new Color(.25f, .93f, .82f), true);
        orange = Mat("Warning_Orange", new Color(1, .4f, .12f), true);
        gold = Mat("Core_Gold", new Color(1, .75f, .24f), true);
        red = Mat("Boss_Crimson", new Color(.95f, .08f, .24f), true);
        steel = Mat("Armor_Steel", new Color(.31f, .43f, .53f));
        dark = Mat("Player_Armor", new Color(.035f, .28f, .29f));
        white = Mat("Blade", new Color(.8f, .94f, 1), true);
        violet = Mat("Distant_Violet", new Color(.19f, .09f, .30f));
        blue = Mat("Distant_Blue", new Color(.08f, .16f, .32f));
        rock = Mat("Arena_Obsidian", new Color(.12f, .09f, .20f));
        cloth = Mat("Hunter_Cloth", new Color(.055f, .035f, .105f));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.5f, .6f, .7f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(.095f, .105f, .24f); RenderSettings.fogDensity = .018f;
        GameObject lightGo = new GameObject("Key light"); Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.3f; light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(52, -35, 0);
        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        Camera camera = camGo.AddComponent<Camera>(); camera.orthographic = true;
        camera.allowMSAA = false; camera.allowHDR = false;
        QualitySettings.antiAliasing = 0;
        string pixelMaterialPath = Root + "/Materials/PixelPalette.mat";
        Material pixelMaterial = AssetDatabase.LoadAssetAtPath<Material>(pixelMaterialPath);
        if (!pixelMaterial)
        {
            pixelMaterial = new Material(Shader.Find("CounterArena/PixelPalette"));
            AssetDatabase.CreateAsset(pixelMaterial, pixelMaterialPath);
        }
        camGo.AddComponent<PixelPresentation>().PaletteMaterial = pixelMaterial;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f, .085f, .19f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 120;
        camGo.AddComponent<AudioListener>();
        camera.transform.position = new Vector3(0, 18, -15); camera.transform.LookAt(Vector3.zero); camera.orthographicSize = 8.1f;
        Transform explore = new GameObject("Sector 07 - Exploration").transform;
        Part("Sector foundation", PrimitiveType.Cube, explore, new Vector3(0, -.3f, 0), new Vector3(23, .5f, 12), floor);
        Grid(explore, Vector3.zero, 22, 11);
        for (int i = 0; i < SectorMap.Walls.Length; i++)
        {
            Rect r = SectorMap.Walls[i];
            Part("Barrier " + i, PrimitiveType.Cube, explore, new Vector3(r.center.x, .55f, r.center.y), new Vector3(r.width, 1.1f, r.height), wall);
            Part("Barrier light " + i, PrimitiveType.Cube, explore, new Vector3(r.center.x, 1.11f, r.center.y), new Vector3(r.width, .035f, r.height), trim);
        }
        // A clear goal beyond the boss, with floor markings that guide exploration.
        Part("Exit gate", PrimitiveType.Cube, explore, new Vector3(0, 1.2f, 5.4f), new Vector3(2.7f, 2.4f, .16f), steel);
        for (int i = -1; i <= 1; i++) Part("Gate energy", PrimitiveType.Cube, explore, new Vector3(i * .72f, 1.2f, 5.29f), new Vector3(.08f, 2, .06f), red);
        FloorRing("Boss zone", explore, new Vector3(0, .02f, 3.9f), 1.4f, red);
        Transform hero = Fighter("Explorer", explore, dark, cyan, false, .64f);
        hero.position = new Vector3(SectorMap.Spawn.x, 0, SectorMap.Spawn.y);
        PixelArtAssets.Attach(hero, true);
        Transform[] patrols = new Transform[3]; Transform[] pickups = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            patrols[i] = Fighter(ArenaRules.Enemies[i].Name, explore, steel, i == 0 ? cyan : i == 1 ? orange : red, i == 2, i == 0 ? .56f : i == 1 ? .7f : .9f);
            patrols[i].position = new Vector3(ArenaRules.Enemies[i].Start.x, 0, ArenaRules.Enemies[i].Start.y);
            WorldLabel(patrols[i], ArenaRules.Enemies[i].Name, new Vector3(0, 2.65f, 0), .085f);
            PixelArtAssets.Attach(patrols[i], false, i);
            pickups[i] = new GameObject(i == 1 ? "Adrenaline core" : "Nanite medkit").transform; pickups[i].SetParent(explore, false);
            pickups[i].position = new Vector3(SectorMap.Pickups[i].x, 0, SectorMap.Pickups[i].y);
            Part("Pedestal", PrimitiveType.Cylinder, pickups[i], new Vector3(0, .08f, 0), new Vector3(.8f, .08f, .8f), steel);
            Part("Core", PrimitiveType.Cube, pickups[i], new Vector3(0, .65f, 0), Vector3.one * .4f, i == 1 ? gold : cyan);
            FloorRing("Pickup light", pickups[i], new Vector3(0, .02f, 0), .55f, i == 1 ? gold : cyan);
            WorldLabel(pickups[i], i == 1 ? "ULT" : "+HP", new Vector3(0, 1.25f, 0), .08f);
        }
        Transform battle = new GameObject("Counter Arena - Battle").transform;
        BuildBattleEnvironment(battle);
        Transform battleHero = Fighter("Player", battle, dark, gold, false, 1.2f);
        battleHero.position = ArenaGame.PlayerArenaPosition; battleHero.rotation = Quaternion.identity;
        DressHunter(battleHero);
        PixelArtAssets.Attach(battleHero, true);
        Transform[] enemies = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            enemies[i] = Fighter(ArenaRules.Enemies[i].Name + " battle", battle, steel, i == 0 ? cyan : i == 1 ? orange : red, i == 2, i == 0 ? 1.15f : i == 1 ? 1.4f : 1.7f);
            enemies[i].position = ArenaGame.EnemyArenaPosition; enemies[i].rotation = Quaternion.Euler(0, 180, 0);
            if (i == 2)
            {
                Part("Warden robe", PrimitiveType.Cube, enemies[i], new Vector3(0, .75f, -.15f), new Vector3(.82f, 1.3f, .18f), violet);
                Part("Royal mantle", PrimitiveType.Cube, enemies[i], new Vector3(0, 1.45f, -.17f), new Vector3(1.2f, .28f, .32f), red);
            }
            enemies[i].gameObject.SetActive(false);
            PixelArtAssets.Attach(enemies[i], false, i);
        }
        battle.gameObject.SetActive(false);
        ArenaGame game = new GameObject("Game - Counter Arena").AddComponent<ArenaGame>();
        game.ExplorationRoot = explore; game.BattleRoot = battle; game.ExplorePlayer = hero; game.BattlePlayer = battleHero;
        game.Patrols = patrols; game.BattleEnemies = enemies; game.Pickups = pickups; game.GameCamera = camera;
        game.CyanMaterial = cyan; game.OrangeMaterial = orange; game.GoldMaterial = gold;
        PrefabUtility.SaveAsPrefabAsset(battleHero.gameObject, Root + "/Prefabs/PlayerMockup.prefab");
        PrefabUtility.SaveAsPrefabAsset(enemies[2].gameObject, Root + "/Prefabs/WardenMockup.prefab");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "Counter Arena"; PlayerSettings.productName = "Counter Arena";
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        AssetDatabase.SaveAssets();
        Debug.Log("ARENA_SCENE_READY " + ScenePath);
    }

    static Material Mat(string name, Color color, bool emissive = false)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
        material.color = color; material.SetFloat("_Glossiness", .32f); material.SetFloat("_Metallic", emissive ? .1f : .3f);
        if (emissive) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .65f); }
        EditorUtility.SetDirty(material); return material;
    }

    static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go.transform;
    }

    static Transform Fighter(string name, Transform parent, Material armor, Material accent, bool boss, float size)
    {
        Transform root = new GameObject(name).transform; root.SetParent(parent, false); root.localScale = Vector3.one * size;
        Part("Torso", PrimitiveType.Cube, root, new Vector3(0, 1.1f, 0), new Vector3(.72f, .8f, .45f), armor);
        Part("Helmet", PrimitiveType.Cube, root, new Vector3(0, 1.82f, 0), new Vector3(.5f, .48f, .48f), steel);
        Part("Visor", PrimitiveType.Cube, root, new Vector3(0, 1.84f, .25f), new Vector3(.39f, .09f, .05f), accent);
        Part("Chest core", PrimitiveType.Cube, root, new Vector3(0, 1.18f, .24f), new Vector3(.21f, .24f, .04f), accent);
        for (int s = -1; s <= 1; s += 2)
        {
            Part("Leg", PrimitiveType.Cube, root, new Vector3(s * .22f, .34f, 0), new Vector3(.25f, .65f, .3f), armor);
            Part("Shoulder", PrimitiveType.Cube, root, new Vector3(s * .51f, 1.38f, 0), new Vector3(.32f, .31f, .52f), armor);
            Part("Arm", PrimitiveType.Cube, root, new Vector3(s * .5f, .99f, .05f), new Vector3(.2f, .56f, .25f), steel);
            Part("Shoulder trim", PrimitiveType.Cube, root, new Vector3(s * .51f, 1.55f, 0), new Vector3(.3f, .035f, .5f), accent);
        }
        Transform sword = new GameObject("SwordPivot").transform; sword.SetParent(root, false); sword.localPosition = new Vector3(.58f, 1, .2f);
        Part("Grip", PrimitiveType.Cube, sword, new Vector3(0, 0, .18f), new Vector3(.12f, .12f, .4f), dark);
        Part("Guard", PrimitiveType.Cube, sword, new Vector3(0, 0, .4f), new Vector3(.45f, .12f, .1f), gold);
        Part("Blade", PrimitiveType.Cube, sword, new Vector3(0, 0, 1.05f), new Vector3(boss ? .23f : .12f, .08f, 1.3f), white);
        Part("Blade edge", PrimitiveType.Cube, sword, new Vector3(.08f, 0, 1.05f), new Vector3(.035f, .1f, 1.3f), accent);
        Transform cue = Part("Cue", PrimitiveType.Sphere, root, new Vector3(0, 2.6f, 0), Vector3.one * .3f, accent); cue.gameObject.SetActive(false);
        if (boss)
            for (int i = -1; i <= 1; i++) Part("Crown", PrimitiveType.Cube, root, new Vector3(i * .24f, 2.15f, 0), new Vector3(.13f, .28f, .18f), gold);
        FloorRing("Foot light", root, new Vector3(0, .025f, 0), .68f, accent);
        return root;
    }

    static void Grid(Transform parent, Vector3 center, int width, int depth)
    {
        for (int i = -width / 2; i <= width / 2; i++) Part("Grid x", PrimitiveType.Cube, parent, center + new Vector3(i, -.035f, 0), new Vector3(.018f, .02f, depth), trim);
        for (int i = -depth / 2; i <= depth / 2; i++) Part("Grid z", PrimitiveType.Cube, parent, center + new Vector3(0, -.035f, i), new Vector3(width, .02f, .018f), trim);
    }

    static void FloorRing(string name, Transform parent, Vector3 position, float radius, Material material)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        LineRenderer line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = false;
        line.positionCount = 65; line.widthMultiplier = .035f;
        for (int i = 0; i < 65; i++) { float a = i / 64f * Mathf.PI * 2; line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius)); }
    }

    static void WorldLabel(Transform parent, string text, Vector3 position, float size)
    {
        GameObject go = new GameObject("Label"); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(58, 0, 0);
        TextMesh mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 48; mesh.characterSize = size;
        mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = Color.white;
        // Labels are composited by the HUD so downsampling cannot destroy text.
        go.SetActive(false);
    }

    static void DressHunter(Transform hero)
    {
        foreach (Transform child in hero)
        {
            if (child.name != "Leg") continue;
            float side = Mathf.Sign(child.localPosition.x);
            child.localPosition = new Vector3(side * .32f, .34f, 0);
            child.localRotation = Quaternion.Euler(0, 0, side * 12);
        }
        Part("Hunter hat brim", PrimitiveType.Cylinder, hero, new Vector3(0, 2.02f, 0), new Vector3(1.1f, .045f, .85f), cloth);
        Transform hat = Part("Hunter hat crown", PrimitiveType.Cube, hero, new Vector3(0, 2.17f, -.08f), new Vector3(.6f, .28f, .48f), cloth);
        hat.localRotation = Quaternion.Euler(0, 0, -12);
        Part("Back cape", PrimitiveType.Cube, hero, new Vector3(0, 1.13f, -.31f), new Vector3(.72f, .75f, .09f), cloth);
        Transform scarf = Part("Trailing scarf", PrimitiveType.Cube, hero, new Vector3(.65f, 1.55f, -.27f), new Vector3(1.15f, .15f, .08f), cloth);
        scarf.localRotation = Quaternion.Euler(0, -12, -12);
        Transform sword = hero.Find("SwordPivot");
        sword.localPosition = new Vector3(-.6f, .75f, -.15f);
        sword.localRotation = Quaternion.Euler(0, -135, -20);
        Transform blade = sword.Find("Blade"); blade.localScale = new Vector3(.32f, .1f, 1.75f); blade.localPosition = new Vector3(0, 0, 1.25f);
        blade.GetComponent<Renderer>().sharedMaterial = cloth;
        Transform edge = sword.Find("Blade edge"); edge.localPosition = new Vector3(.18f, 0, 1.25f); edge.localScale = new Vector3(.045f, .12f, 1.75f);
        edge.GetComponent<Renderer>().sharedMaterial = gold;
        Part("Greatsword second edge", PrimitiveType.Cube, sword, new Vector3(-.18f, 0, 1.25f), new Vector3(.045f, .12f, 1.75f), gold);
        Part("Greatsword tip", PrimitiveType.Cube, sword, new Vector3(0, 0, 2.12f), new Vector3(.4f, .12f, .045f), gold);
    }

    static void BuildBattleEnvironment(Transform battle)
    {
        // Orthographic 3D layers give the fixed 2.5D shot depth without free camera movement.
        Part("Abyss floor", PrimitiveType.Cube, battle, new Vector3(50, -1.15f, 1), new Vector3(36, .4f, 34), blue);
        Part("Duel causeway", PrimitiveType.Cube, battle, new Vector3(50, -.25f, -.2f), new Vector3(4.8f, .45f, 12), rock);
        for (int i = 0; i < 9; i++)
        {
            float z = -5.6f + i * 1.05f;
            float y = i < 5 ? -.08f : (i - 4) * .17f;
            Part("Processional step " + i, PrimitiveType.Cube, battle, new Vector3(50, y - .12f, z), new Vector3(4.4f - i * .08f, .24f, 1.0f), i % 2 == 0 ? rock : violet);
            Part("Step trim " + i, PrimitiveType.Cube, battle, new Vector3(50, y + .01f, z - .46f), new Vector3(3.9f - i * .08f, .025f, .035f), violet);
        }
        Part("Warden dais", PrimitiveType.Cylinder, battle, new Vector3(50, .35f, 2.1f), new Vector3(4, .35f, 3.3f), rock);
        FloorRing("Dais seal", battle, new Vector3(50, .71f, 2.1f), 1.7f, red);
        var random = new System.Random(707);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 13; i++)
            {
                float z = -4 + i * 1.3f;
                float x = 50 + side * (3.3f + (float)random.NextDouble() * 4);
                float h = 1.2f + i * .23f + (float)random.NextDouble() * 2;
                Transform spire = Part("Obsidian spire", PrimitiveType.Cube, battle, new Vector3(x, h * .5f - .8f, z), new Vector3(.65f + (float)random.NextDouble(), h, .7f), i % 2 == 0 ? violet : blue);
                spire.localRotation = Quaternion.Euler(side * 8, i * 21, -side * (10 + i));
                if (i % 3 == 0) Part("Spire ember", PrimitiveType.Cube, spire, new Vector3(0, .2f, -.51f), new Vector3(.07f, .12f, .03f), orange);
            }
            for (int i = 0; i < 15; i++)
            {
                float x = 50 + side * (2.1f + (float)random.NextDouble() * 5.2f);
                float y = .6f + (float)random.NextDouble() * 4.2f;
                float z = -2 + (float)random.NextDouble() * 10;
                Transform shard = Part("Suspended fragment", PrimitiveType.Cube, battle, new Vector3(x, y, z), Vector3.one * (.06f + (float)random.NextDouble() * .13f), i % 4 == 0 ? orange : rock);
                shard.localRotation = Quaternion.Euler(i * 22, i * 35, i * 11);
            }
        }
        GameObject halo = new GameObject("Golden crescent - backdrop"); halo.transform.SetParent(battle, false);
        halo.transform.localPosition = new Vector3(50, 3.2f, 4.3f);
        LineRenderer line = halo.AddComponent<LineRenderer>(); line.sharedMaterial = gold; line.useWorldSpace = false;
        line.positionCount = 81; line.widthMultiplier = .10f;
        for (int i = 0; i < 81; i++)
        {
            float a = Mathf.Lerp(135, 405, i / 80f) * Mathf.Deg2Rad;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * 1.5f, Mathf.Sin(a) * 1.5f, 0));
        }
        GameObject glow = new GameObject("Golden halo light"); glow.transform.SetParent(battle, false); glow.transform.localPosition = new Vector3(50, 3, 2);
        Light light = glow.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .22f, .09f); light.intensity = 5; light.range = 9;
        glow = new GameObject("Blue foreground light"); glow.transform.SetParent(battle, false); glow.transform.localPosition = new Vector3(48, 3, -5);
        light = glow.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(.16f, .35f, 1); light.intensity = 4; light.range = 10;
    }

    public static void PrepareAndBuild()
    {
        CreateScene();
        ArenaValidation.Run();
        string buildFolder = "Builds/Windows";
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
            if (arguments[i] == "-arenaBuildFolder") buildFolder = arguments[i + 1];
        Directory.CreateDirectory(buildFolder);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { ScenePath }, locationPathName = Path.Combine(buildFolder, "CounterArena.exe"),
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Player build failed: " + report.summary.result);
        Debug.Log("ARENA_BUILD_PASSED " + report.summary.totalSize + " bytes");
    }
}
