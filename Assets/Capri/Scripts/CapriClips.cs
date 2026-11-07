// Loads Capri's animations from Resources/CapriSprites (sheets + clips.json, made from the desktop app's strips).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CapriRoom
{
    public sealed class CapriClip
    {
        public string Name;
        public Sprite[] Frames;
        public float Fps;
        public bool Perched;          // drawn sitting on a ledge, pivot at her seat
        public float Seconds { get { return Frames.Length / Fps; } }
    }

    public static class CapriClips
    {
        [Serializable] private class ClipInfo { public string name; public int count; public float fps; public int cellWidth, cellHeight, columns, rows; public float pivotX, pivotY, unitsHigh; }
        [Serializable] private class ClipList { public ClipInfo[] clips; }

        private static Dictionary<string, CapriClip> loaded;
        public static readonly List<string> Problems = new List<string>();   // what went wrong loading, shown in her bubble

        // A clip that failed to load falls back to one that's always there, so a bad sheet shows the wrong pose instead of
        // breaking her: sitting poses fall back to her original sit, everything else to standing. Mouths just don't show.
        public static CapriClip Get(string name)
        {
            if (loaded == null) Load();
            CapriClip clip;
            if (loaded.TryGetValue(name, out clip)) return clip;
            if (name.StartsWith("mouth")) return null;
            string fallback = name.StartsWith("perch") || name == "lay" ? "perch" : "idle";
            if (name != fallback && loaded.TryGetValue(fallback, out clip)) return clip;
            throw new Exception("Capri's '" + name + "' animation is missing from Resources/CapriSprites.");
        }

        public static bool Has(string name)
        {
            if (loaded == null) Load();
            return loaded.ContainsKey(name);
        }

        private static void Load()
        {
            loaded = new Dictionary<string, CapriClip>();
            TextAsset json = Resources.Load<TextAsset>("CapriSprites/clips");
            if (json == null) throw new Exception("Resources/CapriSprites/clips.json is missing.");
            foreach (ClipInfo info in JsonUtility.FromJson<ClipList>(json.text).clips)
            {
                try { loaded[info.name] = Make(info); }
                catch (Exception problem)
                {
                    Problems.Add(info.name + ": " + problem.Message);
                    Debug.LogError("[Capri] Couldn't load the '" + info.name + "' animation: " + problem.Message);
                }
            }
        }

        private static CapriClip Make(ClipInfo info)
        {
            Texture2D sheet = Resources.Load<Texture2D>("CapriSprites/" + info.name);
            if (sheet == null) throw new Exception("Resources/CapriSprites/" + info.name + ".png is missing.");
            // If the sheet was imported smaller than drawn (a max texture size on the build), cut the frames to match.
            float sx = sheet.width / (float)(info.columns * info.cellWidth), sy = sheet.height / (float)(info.rows * info.cellHeight);
            float cellW = info.cellWidth * sx, cellH = info.cellHeight * sy;
            if (sx < 0.999f || sy < 0.999f) Problems.Add(info.name + " sheet came in at " + sheet.width + "x" + sheet.height);
            if (sx < 0.999f || sy < 0.999f) Debug.LogWarning("[Capri] '" + info.name + "' sheet is " + sheet.width + "x" + sheet.height + ", smaller than drawn; using it scaled.");
            // One world unit = her size (about her standing height), so the root's scale is her size in metres.
            float pixelsPerUnit = cellH / info.unitsHigh;
            Sprite[] frames = new Sprite[info.count];
            for (int i = 0; i < info.count; i++)
            {
                int col = i % info.columns, row = i / info.columns;
                // Texture rows count up from the bottom. Kept inside the sheet so rounding can't push a frame past its edge.
                float x = Mathf.Floor(col * cellW), y = Mathf.Floor(sheet.height - (row + 1) * cellH);
                float w = Mathf.Min(Mathf.Floor(cellW), sheet.width - x), h = Mathf.Min(Mathf.Floor(cellH), sheet.height - Mathf.Max(0, y));
                Rect rect = new Rect(x, Mathf.Max(0, y), w, h);
                frames[i] = Sprite.Create(sheet, rect, new Vector2(info.pivotX, info.pivotY), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                frames[i].name = info.name + "_" + i;
            }
            return new CapriClip { Name = info.name, Frames = frames, Fps = info.fps, Perched = info.name.StartsWith("perch") };
        }
    }
}
