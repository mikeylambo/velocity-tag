// ArenaLayoutSetup.cs (Editor)
// VelocityTag > Setup Arena Layout (Golden Core reference).
// Replaces the placeholder arena in TimeAttack.unity with the Training Cylinder
// layout from jetpack_laser_tag_golden_core_v02 arena.js / targets.js: 8
// platforms, 4 pillars, 4 launch pads, and the 8 real target spawn points
// (3 ground + 5 elevated) — the vertical routing the sport is built around.
// Platforms go on a "Platforms" layer wired into ArenaBounds._platformMask so
// JetpackLocomotion can land on them. Idempotent; saves the scene.
//
// NOTE: coordinates are golden_core_v02 (ARENA_RADIUS 24 era). The 2.0 Polish
// trainingCylinder.js may differ — reconcile when that source surfaces.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VelocityTag.EditorTools
{
    public static class ArenaLayoutSetup
    {
        private const string ScenePath = "Assets/Scenes/TimeAttack.unity";
        private const string PlatformLayerName = "Platforms";

        // arena.js addPlatform(x, y, z, sx, sz) — y is box center, thickness 0.42
        private static readonly float[][] Platforms = new float[][]
        {
            new float[] { 0f, 4.5f, -6f, 8f, 4f },
            new float[] { -9f, 8.0f, 3f, 5f, 4f },
            new float[] { 8f, 11.0f, 4f, 6f, 4f },
            new float[] { 0f, 12.5f, 0f, 9f, 3f },
            new float[] { -15f, 6.4f, -13.5f, 6f, 3f },
            new float[] { 15f, 6.4f, -13.5f, 6f, 3f },
            new float[] { -15f, 6.4f, 13.5f, 6f, 3f },
            new float[] { 15f, 6.4f, 13.5f, 6f, 3f },
        };

        // arena.js addPillar(x, z, h, r)
        private static readonly float[][] Pillars = new float[][]
        {
            new float[] { -10f, -7f, 10f, 1.1f },
            new float[] { 10f, -8f, 7f, 1.3f },
            new float[] { 7f, 7f, 12f, 0.95f },
            new float[] { -7f, 9f, 8.5f, 1.05f },
        };

        // arena.js buildPads launch positions (radius 1.75, ground pads)
        private static readonly Vector2[] LaunchPads = new Vector2[]
        {
            new Vector2(0f, 12f),
            new Vector2(0f, -12f),
            new Vector2(-17f, 13f),
            new Vector2(17f, 13f),
        };

        // targets.js spawnPads (base y) — dummy root rides +1.0 like the JS body center
        private static readonly Vector3[] Spawns = new Vector3[]
        {
            new Vector3(-12f, 0f, -10f),
            new Vector3(12f, 0f, -10f),
            new Vector3(0f, 0f, -14f),
            new Vector3(-6f, 4.9f, -6f),
            new Vector3(6f, 4.9f, -6f),
            new Vector3(-13.5f, 6.8f, 13.5f),
            new Vector3(13.5f, 6.8f, 13.5f),
            new Vector3(0f, 12.8f, 0f),
        };

        [MenuItem("VelocityTag/Setup Arena Layout (Golden Core reference)")]
        public static void Setup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var arenaBounds = Object.FindFirstObjectByType<ArenaBounds>();
            var spawnsGO = GameObject.Find("Spawns");
            var net = spawnsGO != null ? spawnsGO.GetComponent<TargetSpawnNetwork>() : null;
            if (arenaBounds == null || net == null)
            {
                Debug.LogError("ArenaLayoutSetup: ArenaBounds or TargetSpawnNetwork missing — run the base scene assembly first.");
                return;
            }

            int platformLayer = EnsureLayer(PlatformLayerName);
            if (platformLayer < 0)
            {
                Debug.LogError("ArenaLayoutSetup: no free layer slot for '" + PlatformLayerName + "'.");
                return;
            }

            // ---- clear placeholder geometry ----
            DestroyAll("Pillar_1", "Pillar_2", "Pillar_3", "CoverBox_1",
                       "LaunchPad_A", "LaunchPad_B", "Platforms");
            for (int i = spawnsGO.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(spawnsGO.transform.GetChild(i).gameObject);

            var obstacles = new List<Collider>();

            // ---- platforms ----
            var platRoot = new GameObject("Platforms");
            for (int i = 0; i < Platforms.Length; i++)
            {
                float[] p = Platforms[i];
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Platform_" + (i + 1);
                box.transform.SetParent(platRoot.transform, false);
                box.transform.position = new Vector3(p[0], p[1], p[2]);
                box.transform.localScale = new Vector3(p[3], 0.42f, p[4]);
                box.layer = platformLayer;
                obstacles.Add(box.GetComponent<Collider>()); // side push-out like arena.js obstacles
            }

            // ---- pillars ----
            var pillarRoot = new GameObject("Pillars");
            for (int i = 0; i < Pillars.Length; i++)
            {
                float[] p = Pillars[i];
                var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "Pillar_" + (i + 1);
                cyl.transform.SetParent(pillarRoot.transform, false);
                cyl.transform.position = new Vector3(p[0], p[2] / 2f, p[1]);
                // cylinder primitive: radius 0.5, height 2 at unit scale
                cyl.transform.localScale = new Vector3(p[3] * 2f, p[2] / 2f, p[3] * 2f);
                obstacles.Add(cyl.GetComponent<Collider>());
            }

            // ---- launch pads (positions from arena.js; velocity stays the
            //      component default pending the 2.0 Polish pad value) ----
            for (int i = 0; i < LaunchPads.Length; i++)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "LaunchPad_" + (char)('A' + i);
                pad.transform.position = new Vector3(LaunchPads[i].x, 0.08f, LaunchPads[i].y);
                pad.transform.localScale = new Vector3(3.5f, 0.08f, 3.5f); // r 1.75 like arena.js
                Object.DestroyImmediate(pad.GetComponent<CapsuleCollider>());
                var trig = pad.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                trig.size = new Vector3(1f, 20f, 1f);
                trig.center = new Vector3(0f, 10f, 0f); // world ~1.6u of trigger above the pad
                pad.AddComponent<LaunchPad>();
            }

            // ---- spawn points (dummy root = base + 1.0, the JS body-center height) ----
            var spawnList = new List<Transform>();
            for (int i = 0; i < Spawns.Length; i++)
            {
                var sp = new GameObject("Spawn_" + i);
                sp.transform.SetParent(spawnsGO.transform, false);
                sp.transform.position = Spawns[i] + Vector3.up * 1.0f;
                spawnList.Add(sp.transform);
            }

            // ---- rewire ArenaBounds ----
            var soB = new SerializedObject(arenaBounds);
            soB.FindProperty("_platformMask").intValue = 1 << platformLayer;
            var obsProp = soB.FindProperty("_obstacles");
            obsProp.arraySize = obstacles.Count;
            for (int i = 0; i < obstacles.Count; i++)
                obsProp.GetArrayElementAtIndex(i).objectReferenceValue = obstacles[i];
            soB.ApplyModifiedPropertiesWithoutUndo();

            // ---- rewire spawn list ----
            var soN = new SerializedObject(net);
            var spProp = soN.FindProperty("_spawnPoints");
            spProp.arraySize = spawnList.Count;
            for (int i = 0; i < spawnList.Count; i++)
                spProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnList[i];
            soN.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("ArenaLayoutSetup: golden-core Training Cylinder layout applied — "
                + Platforms.Length + " platforms, " + Pillars.Length + " pillars, "
                + LaunchPads.Length + " pads, " + Spawns.Length + " spawns. Scene saved.");
        }

        private static void DestroyAll(params string[] names)
        {
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }
            return -1;
        }
    }
}
