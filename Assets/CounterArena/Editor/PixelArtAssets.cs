using System.IO;
using CounterArena;
using UnityEditor;
using UnityEngine;

// Reproducible, authored pixel silhouettes: no rescaled screenshots of the old models.
public static class PixelArtAssets
{
    const string Folder = "Assets/CounterArena/Art/Pixel";
    static Color32 C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
    static readonly Color32 Ink = C("151025"), Cloth = C("252039"), Mid = C("3e3555"), Edge = C("655778"),
        Steel = C("53627e"), Lit = C("8396ad"), DarkSteel = C("303751"),
        Pink = C("f52d83"), Rose = C("a72062"), Burgundy = C("541b49"),
        Gold = C("ffcb71"), White = C("fff2bf"), Teal = C("67dacc");
    sealed class Canvas
    {
        public const int W = 96, H = 128;
        public Color32[] Pixels = new Color32[W * H];
        public void Rect(int x, int y, int w, int h, Color32 color)
        { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Dot(xx, yy, color); }
        public void Dot(int x, int y, Color32 color) { if (x >= 0 && x < W && y >= 0 && y < H) Pixels[y * W + x] = color; }
        public void Line(int x0, int y0, int x1, int y1, int thickness, Color32 color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            { float t = steps == 0 ? 0 : i / (float)steps; Rect(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)) - thickness / 2, Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)) - thickness / 2, thickness, thickness, color); }
        }
        public void Poly(Color32 color, params int[] points)
        {
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                bool inside = false;
                for (int i = 0, j = points.Length - 2; i < points.Length; j = i, i += 2)
                {
                    int xi = points[i], yi = points[i+1], xj = points[j], yj = points[j+1];
                    if ((yi > y) != (yj > y) && x < (xj-xi) * (y-yi) / (float)(yj-yi) + xi) inside = !inside;
                }
                if (inside) Dot(x, y, color);
            }
        }
        public void Outline()
        {
            Color32[] original = (Color32[])Pixels.Clone();
            for (int y = 1; y < H-1; y++) for (int x = 1; x < W-1; x++)
                if (original[y*W+x].a == 0 && (original[y*W+x-1].a > 0 || original[y*W+x+1].a > 0 || original[(y-1)*W+x].a > 0 || original[(y+1)*W+x].a > 0)) Dot(x,y,Ink);
        }
        public Sprite Save(string name, int ppu)
        {
            Outline(); Directory.CreateDirectory(Folder);
            Texture2D texture = new Texture2D(W,H,TextureFormat.RGBA32,false);
            texture.SetPixels32(Pixels); texture.Apply();
            string path = Folder + "/" + name + ".png"; File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu; importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
            TextureImporterSettings settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f,0);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }

    static Sprite Hunter(int pose)
    {
        var p = new Canvas();
        // Wide grounded stance, rear silhouette, layered boots and coat tails.
        p.Poly(Ink, 36,40, 48,37, 40,12, 34,4, 22,4, 28,15);
        p.Poly(Mid, 38,35, 44,33, 35,10, 27,9);
        p.Line(27,7,36,7,2,Edge);
        p.Poly(Ink, 49,38, 59,40, 65,22, 74,5, 61,4, 51,20);
        p.Poly(Mid, 54,36, 59,35, 61,21, 68,10, 61,9, 53,22);
        p.Line(63,6,73,6,2,Edge);
        p.Poly(Cloth, 31,73, 58,74, 64,38, 51,29, 46,37, 31,31);
        p.Poly(Mid, 34,68, 44,67, 43,39, 33,35);
        p.Poly(DarkSteel, 30,73, 42,78, 57,75, 63,61, 54,48, 33,51);
        p.Rect(34,50,25,4,Ink); p.Rect(38,53,18,2,Edge);
        for (int y=57; y<75; y+=5) p.Line(35,y,54,y-2,2,Steel);
        p.Poly(Steel, 30,73, 24,65, 20,53, 25,49, 33,61);
        p.Poly(Lit, 27,68, 23,59, 26,57, 31,67);
        p.Poly(DarkSteel, 57,74, 65,68, 68,56, 61,53, 56,66);
        p.Line(64,65,65,58,3,Steel);
        // Hood/head and a bent brim recognizable from behind.
        p.Poly(Cloth, 33,81, 38,94, 52,96, 61,80, 54,75, 38,76);
        p.Poly(Mid, 38,87, 42,94, 53,92, 57,82, 40,82);
        p.Poly(Ink, 20,83, 34,87, 57,85, 67,79, 51,78, 34,80);
        p.Line(23,83,44,85,1,Edge);
        // Long scarf sweeps right, with tapered, stepped tip.
        p.Poly(Ink, 49,77, 65,77, 77,72, 85,61, 91,45, 90,31, 84,23, 85,42, 81,56, 73,66, 61,70, 50,71);
        p.Line(61,74,76,70,2,Mid); p.Line(78,66,86,48,2,Mid);
        p.Rect(32,70,26,5,Cloth); p.Rect(37,72,12,2,Edge);
        // Golden greatsword is drawn in discrete poses, not a rotated texture.
        if (pose == 1)
        {
            p.Line(28,58,75,111,8,Ink); p.Line(29,60,76,113,5,Gold); p.Line(31,62,77,114,2,White);
            p.Line(25,67,35,59,3,Gold); p.Rect(25,55,5,7,Cloth);
        }
        else if (pose == 2)
        {
            p.Line(23,52,30,97,7,Ink); p.Line(23,54,30,98,4,Gold); p.Line(24,55,31,98,1,White);
            p.Line(17,59,33,57,3,Gold);
        }
        else
        {
            p.Poly(Ink, 26,53, 34,49, 11,14, 3,15, 5,23);
            p.Line(28,49,7,18,5,Gold); p.Line(28,49,8,20,2,White);
            p.Line(24,54,34,46,3,Gold); p.Line(32,52,37,60,3,Cloth);
        }
        if (pose == 3) p.Rect(34,67,15,3,Rose);
        return p.Save("Hunter_" + pose, 40);
    }

    static Sprite Warden(bool attack, int kind)
    {
        if (kind == 0) return Sentinel(attack);
        if (kind == 1) return Enforcer(attack);
        var p = new Canvas();
        Color32 accent = kind == 0 ? Teal : kind == 1 ? Gold : Pink;
        Color32 shade = kind == 2 ? Burgundy : DarkSteel;
        // Long robe silhouette with sharp separated folds.
        p.Poly(Ink, 32,77, 63,77, 76,14, 62,8, 49,16, 35,8, 21,14);
        p.Poly(shade, 34,74, 61,75, 71,17, 59,14, 49,20, 36,14, 26,17);
        p.Poly(Rose, 45,72, 53,72, 61,17, 50,23, 40,17);
        p.Poly(Mid, 32,63, 38,68, 35,20, 28,17);
        p.Line(57,67,66,21,2,kind == 2 ? Rose : Steel);
        p.Line(47,61,46,24,2,kind == 2 ? Pink : Lit);
        p.Poly(shade, 28,93, 39,98, 58,98, 68,91, 62,68, 49,62, 32,68);
        p.Poly(Rose, 39,91, 58,91, 60,74, 49,67, 36,75);
        p.Rect(44,77,10,15,accent); p.Rect(47,81,4,8,White);
        p.Line(35,72,62,72,3,Gold); p.Rect(45,68,8,7,Gold); p.Rect(47,70,4,3,White);
        // Broad segmented pauldrons, highlights and hanging gauntlets.
        p.Poly(shade, 24,96, 35,98, 39,90, 35,80, 19,80, 15,86);
        p.Poly(accent, 22,94, 32,96, 36,90, 32,86, 18,86);
        p.Line(19,84,35,84,2,Rose);
        p.Poly(shade, 60,97, 71,95, 81,85, 76,79, 60,81, 57,90);
        p.Poly(accent, 63,95, 72,92, 78,85, 61,85);
        p.Line(61,82,77,82,2,Rose);
        int raise = attack ? 13 : 0;
        p.Poly(shade, 21,81, 31,79, 26,57+raise, 17,50+raise, 12,57+raise);
        p.Line(21,75,18,59+raise,3,accent);
        p.Poly(shade, 67,80, 75,81, 82,60, 77,50, 69,55);
        p.Line(74,75,78,61,3,accent);
        p.Poly(Ink, 36,100, 38,114, 47,120, 57,113, 61,100, 53,92, 42,93);
        p.Poly(Mid, 40,102, 42,113, 48,117, 56,110, 57,101, 51,95, 43,97);
        p.Poly(shade, 45,117, 48,125, 51,117, 54,119, 55,110, 59,112, 58,103, 39,103, 37,113, 42,110, 42,119);
        p.Line(40,102,56,102,3,accent); p.Rect(46,99,6,4,White);
        p.Line(48,116,48,106,2,kind == 2 ? Rose : Lit);
        // Weapon held to one side so the robe and face remain readable.
        if (attack) { p.Line(18,69,8,114,6,Gold); p.Line(18,71,8,115,3,White); }
        else { p.Line(18,56,7,21,6,Gold); p.Line(18,55,8,23,3,White); }
        p.Line(12,59+raise,24,55+raise,3,accent);
        return p.Save((kind == 0 ? "Sentinel" : kind == 1 ? "Enforcer" : "Warden") + (attack ? "_Attack" : "_Idle"), 64);
    }

    static Sprite Sentinel(bool attack)
    {
        var p = new Canvas();
        // A hovering mechanical core, swept wings and cyan sensor.
        p.Poly(Ink, 34,89, 59,89, 70,69, 59,45, 36,45, 25,67);
        p.Poly(DarkSteel, 36,85, 57,85, 65,69, 56,50, 38,50, 30,67);
        p.Poly(Steel, 35,79, 55,82, 61,70, 54,62, 35,64);
        p.Rect(37,67,20,7,Ink); p.Rect(40,69,14,3,Teal); p.Rect(45,69,4,3,White);
        p.Poly(DarkSteel, 27,78, 11,91, 4,83, 13,65, 28,59);
        p.Poly(Steel, 24,75, 12,85, 8,81, 17,69, 26,65);
        p.Line(10,79,20,68,2,Teal);
        p.Poly(DarkSteel, 67,79, 83,90, 91,81, 80,63, 66,59);
        p.Poly(Steel, 69,75, 81,85, 87,80, 77,67, 68,64);
        p.Line(76,69,86,79,2,Teal);
        p.Poly(Mid, 38,49, 55,49, 51,38, 43,35);
        p.Line(43,40,47,29,3,Teal); p.Line(51,40,48,26,2,White);
        p.Line(46,86,46,101,3,Steel); p.Rect(43,100,7,4,Teal);
        if (attack)
        {
            p.Line(8,61,22,52,3,Gold); p.Line(85,59,71,49,3,Gold);
            p.Rect(33,68,30,2,White);
        }
        return p.Save("Sentinel" + (attack ? "_Attack" : "_Idle"), 40);
    }

    static Sprite Enforcer(bool attack)
    {
        var p = new Canvas();
        p.Poly(Ink, 30,51, 45,49, 42,18, 24,14, 22,20);
        p.Poly(DarkSteel, 32,46, 41,45, 37,24, 26,22); p.Rect(23,15,20,6,Steel);
        p.Poly(Ink, 51,49, 64,51, 73,19, 70,14, 52,15);
        p.Poly(DarkSteel, 54,46, 62,46, 66,24, 54,22); p.Rect(52,15,20,6,Steel);
        p.Poly(DarkSteel, 25,86, 68,86, 72,58, 62,44, 33,44, 22,58);
        p.Poly(Steel, 30,83, 65,83, 65,64, 57,51, 38,51, 29,64);
        p.Line(32,80,63,80,3,Lit); p.Rect(40,64,17,10,Ink); p.Rect(43,66,11,6,Gold);
        p.Line(34,54,59,54,3,Gold); p.Line(34,60,60,60,2,DarkSteel);
        p.Poly(Ink, 12,91, 29,93, 36,81, 29,71, 9,73, 5,82);
        p.Poly(Steel, 12,88, 26,90, 31,82, 26,77, 9,78); p.Line(12,86,25,87,2,Gold);
        p.Poly(Ink, 65,93, 82,91, 90,81, 83,73, 65,73, 59,82);
        p.Poly(Steel, 67,90, 80,88, 86,81, 81,78, 65,78); p.Line(68,87,81,85,2,Gold);
        p.Rect(12,51,15,24,DarkSteel); p.Rect(14,56,8,17,Steel);
        p.Rect(69,49,16,25,DarkSteel); p.Rect(72,53,8,17,Steel);
        p.Poly(Ink, 33,90, 35,108, 47,114, 62,106, 63,90, 55,82, 41,82);
        p.Poly(DarkSteel, 37,93, 39,105, 48,110, 58,103, 59,92, 53,86, 42,86);
        p.Rect(38,94,21,5,Gold); p.Rect(46,94,6,4,White);
        p.Line(47,108,47,101,2,Steel);
        int y = attack ? 93 : 50;
        p.Line(16,62,9,y,6,DarkSteel); p.Rect(2,y-3,21,12,Steel); p.Rect(3,y+6,19,3,Gold);
        return p.Save("Enforcer" + (attack ? "_Attack" : "_Idle"), 48);
    }

    static Sprite[] hunter;
    static Sprite[,] foes;
    public static void Generate()
    {
        hunter = new[] { Hunter(0), Hunter(1), Hunter(2), Hunter(3) };
        foes = new Sprite[3,2];
        for(int i=0;i<3;i++) { foes[i,0]=Warden(false,i); foes[i,1]=Warden(true,i); }
    }
    public static void Attach(Transform root, bool isHunter, int enemy = 2)
    {
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            if (r.name != "Cue" && !r.GetComponent<TextMesh>()) r.enabled = false;
        GameObject go = new GameObject("Pixel sprite"); go.transform.SetParent(root,false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = isHunter ? hunter[0] : foes[enemy,0];
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        PixelActor actor = go.AddComponent<PixelActor>(); actor.Hunter = isHunter;
        actor.Idle = renderer.sprite; actor.Attack = isHunter ? hunter[1] : foes[enemy,1];
        actor.Guard = isHunter ? hunter[2] : foes[enemy,0]; actor.Hurt = isHunter ? hunter[3] : foes[enemy,0];
    }
}
