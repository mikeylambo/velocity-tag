// VelocityTagSceneBuilder.cs
// Builds Assets/Scenes/TimeAttack.unity from code instead of hand-authored YAML.
//
// Rationale: scene and prefab YAML is merge-hostile, unreviewable in a diff, and
// silently corruptible if written by hand. Generating the scene makes it a build
// artifact — reproducible, inspectable as C#, and safe to regenerate whenever the
// layout changes. Run it from the menu; commit the resulting scene/prefab.
//
// Arena layout, launch pad power and target spawn coordinates are transcribed
// verbatim from the JS build's src/maps/trainingCylinder.js — the official
// balance map. No geometry values are invented here; anything not in that file
// (collider thicknesses, primitive scaling) is structural, not tuning.
//
// This is a gameplay scaffold, not an art pass: untextured primitives and flat
// materials, present only because TargetDummy swaps sharedMaterial at runtime.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ShooterCore;
using ShooterCore.Input;
using TMPro;

namespace VelocityTag.EditorTools
{
    public static class VelocityTagSceneBuilder
    {
        private const string ScenePath  = "Assets/Scenes/TimeAttack.unity";
        private const string ConfigPath = "Assets/VelocityTag/Config/VelocityTagConfig.asset";
        private const string PrefabPath = "Assets/VelocityTag/Prefabs/TargetDummy.prefab";
        private const string MaterialDir = "Assets/VelocityTag/Materials";
        private const string ZoneLayerName = "TargetZones";

        // --- src/maps/trainingCylinder.js, transcribed ---
        private static readonly Vector3 PlayerSpawn = new Vector3(0f, 0f, 12f);

        private static readonly (float x, float y, float z, float sx, float sz)[] Platforms =
        {
            (  0f,  4f, -8f, 8f, 4f),
            (-12f,  8f,  4f, 6f, 4f),
            ( 12f,  8f,  4f, 6f, 4f),
            (  0f, 12f,  8f, 5f, 5f),
        };

        private static readonly (float x, float z, float radius, float height)[] Pillars =
        {
            (0f, 0f, 3f, 18f),
        };

        private static readonly (float x, float y, float z, float power)[] LaunchPads =
        {
            (  0f, 0.05f, -16f, 22f),
            (  0f, 0.05f,  16f, 22f),
            (-16f, 0.05f,   0f, 22f),
            ( 16f, 0.05f,   0f, 22f),
        };

        private static readonly Vector3[] RechargePads =
        {
            new Vector3(-21f, 0.05f, -5f),
            new Vector3( 21f, 0.05f, -5f),
        };

        private static readonly Vector3[] TargetSpawns =
        {
            new Vector3(  0f,  0.2f, -10f), new Vector3(-12f,  4.2f, -8f), new Vector3(12f,  4.2f, -8f),
            new Vector3(-12f,  8.2f,   4f), new Vector3( 12f,  8.2f,  4f), new Vector3( 0f, 12.2f,  8f),
        };

        // Target zone boxes, transcribed from src/targets.js init(). Sizes AND
        // local positions both come from there — the port README describes the
        // head zone as sitting on "top", but targets.js puts it at y 1.6 / z -0.18,
        // i.e. a visor on the front of the head. Getting these wrong silently
        // changes which zone a shot scores.
        // Front of the dummy is -Z, matching TargetDummy's yaw convention.
        private static readonly Vector3 BodySize  = new Vector3(0.55f, 0.75f, 0.35f);
        private static readonly Vector3 ChestSize = new Vector3(0.45f, 0.30f, 0.10f);
        private static readonly Vector3 HeadSize  = new Vector3(0.35f, 0.10f, 0.10f);
        private static readonly Vector3 BackSize  = new Vector3(0.30f, 0.40f, 0.12f);

        private static readonly Vector3 BodyPos  = new Vector3(0f, 1.0f,  0f);
        private static readonly Vector3 ChestPos = new Vector3(0f, 1.1f, -0.18f);
        private static readonly Vector3 HeadPos  = new Vector3(0f, 1.6f, -0.18f);
        private static readonly Vector3 BackPos  = new Vector3(0f, 1.1f,  0.20f);

        [MenuItem("Tools/Velocity Tag/Rebuild Time Attack Scene")]
        public static void Rebuild()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Time Attack Scene",
                    $"This regenerates:\n\n{ScenePath}\n{PrefabPath}\n\n" +
                    "Any hand-edits to those two assets will be lost. Continue?",
                    "Rebuild", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int zoneLayer = EnsureLayer(ZoneLayerName);
            var config = EnsureConfig();
            var mats = EnsureMaterials();
            var prefab = BuildTargetDummyPrefab(config, mats, zoneLayer);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            var arena = BuildArena(config);
            var match = BuildMatch(config, out var matchState, out var round);
            BuildPlayer(config, arena, matchState, round, zoneLayer, out var locomotion);
            WirePads(locomotion);
            BuildSpawns(config, arena, prefab, locomotion, matchState);

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Velocity Tag] Rebuilt {ScenePath}.\n" +
                      $"Arena radius {config.arenaRadius}, {Platforms.Length} platforms, " +
                      $"{LaunchPads.Length} launch pads (power 22), {TargetSpawns.Length} target spawns.\n" +
                      "Camera rig is third-person in both modes; CameraModeSwitch swaps " +
                      "camera and aim origin when a headset connects.");

            Selection.activeObject = match;
        }

        // ---------------------------------------------------------------- scene

        private static void BuildLighting()
        {
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static ArenaBounds BuildArena(GameConfig config)
        {
            var root = new GameObject("Arena");
            var bounds = root.AddComponent<ArenaBounds>();

            var slabs = new List<ArenaBounds.PlatformSlab>();
            var columns = new List<ArenaBounds.PillarColumn>();

            // Floor: a disc of radius CONFIG.ARENA_RADIUS. Unity's cylinder
            // primitive is diameter 1, height 2, hence the halved scale.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(config.arenaRadius * 2f, 0.05f, config.arenaRadius * 2f);

            foreach (var (x, y, z, sx, sz) in Platforms)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = $"Platform_{x}_{y}_{z}";
                p.transform.SetParent(root.transform);
                // JS slabs are 0.4 thick with the walkable surface at y.
                p.transform.localPosition = new Vector3(x, y - 0.2f, z);
                p.transform.localScale = new Vector3(sx, 0.4f, sz);

                slabs.Add(new ArenaBounds.PlatformSlab
                {
                    minX = x - sx * 0.5f, maxX = x + sx * 0.5f,
                    minZ = z - sz * 0.5f, maxZ = z + sz * 0.5f,
                    y = y,
                });
            }

            foreach (var (x, z, radius, height) in Pillars)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{x}_{z}";
                pillar.transform.SetParent(root.transform);
                pillar.transform.localPosition = new Vector3(x, height * 0.5f, z);
                pillar.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

                columns.Add(new ArenaBounds.PillarColumn { x = x, z = z, radius = radius, height = height });
            }

            // Collision queries read the slab/column data above, exactly as arena.js
            // reads mapData — never these colliders. The meshes keep them anyway so
            // the reticle can land on world geometry, as main.js's aim ray does.
            bounds.SetGeometry(slabs, columns);

            var padRoot = new GameObject("LaunchPads");
            padRoot.transform.SetParent(root.transform);
            foreach (var (x, y, z, power) in LaunchPads)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = $"LaunchPad_{x}_{z}";
                pad.transform.SetParent(padRoot.transform);
                pad.transform.localPosition = new Vector3(x, y, z);
                pad.transform.localScale = new Vector3(3f, 0.05f, 3f);   // 1.5 trigger radius

                var launch = pad.AddComponent<LaunchPad>();
                SetField(launch, "_config", config);
                SetField(launch, "_arena", bounds);
                SetField(launch, "_launchVelocity", power);
            }

            var gateRoot = new GameObject("RechargeGates");
            gateRoot.transform.SetParent(root.transform);
            foreach (var pos in RechargePads)
            {
                var gate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                gate.name = $"RechargeGate_{pos.x}_{pos.z}";
                gate.transform.SetParent(gateRoot.transform);
                gate.transform.localPosition = pos;
                gate.transform.localScale = new Vector3(3.6f, 0.05f, 3.6f);   // 1.8 trigger radius

                var sync = gate.AddComponent<SyncGate>();
                SetField(sync, "_config", config);
                SetField(sync, "_arena", bounds);
            }

            return bounds;
        }

        /// Pads need the player, which is built after the arena.
        private static void WirePads(JetpackLocomotion player)
        {
            foreach (var pad in Object.FindObjectsByType<LaunchPad>(FindObjectsSortMode.None))
                SetField(pad, "_player", player);
            foreach (var gate in Object.FindObjectsByType<SyncGate>(FindObjectsSortMode.None))
                SetField(gate, "_player", player);
        }

        private static GameObject BuildMatch(GameConfig config, out MatchStateMachine state, out TimeAttackRound round)
        {
            var go = new GameObject("Match");
            state = go.AddComponent<MatchStateMachine>();
            SetField(state, "_mode", "TIME_ATTACK");
            SetField(state, "_map", "TRAINING_CYLINDER");

            round = go.AddComponent<TimeAttackRound>();
            SetField(round, "_config", config);
            SetField(round, "_matchState", state);

            // Start/retry entry point — Enter on desktop, A on the right controller.
            var harness = go.AddComponent<RoundStartHarness>();
            SetField(harness, "_round", round);
            SetField(harness, "_matchState", state);
            return go;
        }

        private static GameObject BuildPlayer(GameConfig config, ArenaBounds arena, MatchStateMachine state,
                                              TimeAttackRound round, int zoneLayer, out JetpackLocomotion locomotion)
        {
            var player = new GameObject("Player");
            player.transform.position = PlayerSpawn;

            // No Rigidbody and no collider: the pads poll distance like arena.js, so
            // nothing depends on trigger callbacks, and the chase camera's aim ray
            // starts behind the avatar and must not stop on it. The 1.6m standing
            // profile that matters for collision lives in ArenaBounds.
            locomotion = player.AddComponent<JetpackLocomotion>();
            var input = player.AddComponent<CrossPlatformInput>();
            player.AddComponent<XRInputDriver>();
            var router = player.AddComponent<PlayerInputRouter>();
            var blaster = player.AddComponent<TagBlaster>();

            SetField(locomotion, "_config", config);
            SetField(locomotion, "_input", input);
            SetField(locomotion, "_arena", arena);
            SetField(locomotion, "_matchState", state);

            // --- Camera rig ---
            // A SCENE ROOT, not a child of Player. camera.js adds `this.rig` to the
            // scene and moves it in world space each frame; parenting it under the
            // avatar would compose the avatar's own transform on top of that.
            // Both cameras hang off this one rig, exactly as the JS build nests its
            // camera and controllers under the same group.
            var rigRoot = new GameObject("CameraRig");
            rigRoot.transform.position = PlayerSpawn;

            var rig = rigRoot.AddComponent<ShooterCore.CameraRig.ChaseCameraRig>();
            SetField(rig, "_config", config);
            SetField(rig, "_target", player.transform);

            // One listener for the whole rig, so swapping cameras cannot leave the
            // scene with zero or two of them.
            rigRoot.AddComponent<AudioListener>();

            var chase = new GameObject("ChaseCamera");
            chase.transform.SetParent(rigRoot.transform, false);
            var chaseCam = chase.AddComponent<Camera>();
            chaseCam.fieldOfView = 75f;          // camera.js PerspectiveCamera(75, ...)
            chaseCam.nearClipPlane = 0.05f;
            SetField(rig, "_camera", chaseCam);

            // --- XR subtree: headset drives LOCAL pose under the rig ---
            var xrRoot = new GameObject("XR");
            xrRoot.transform.SetParent(rigRoot.transform, false);

            var head = new GameObject("Head");
            head.transform.SetParent(xrRoot.transform, false);
            var xrCam = head.AddComponent<Camera>();
            xrCam.fieldOfView = 75f;
            xrCam.nearClipPlane = 0.05f;
            SetField(head.AddComponent<XRNodePoseDriver>(), "_node", (int)UnityEngine.XR.XRNode.Head);

            var leftHand = new GameObject("LeftHand Controller");
            leftHand.transform.SetParent(xrRoot.transform, false);
            SetField(leftHand.AddComponent<XRNodePoseDriver>(), "_node", (int)UnityEngine.XR.XRNode.LeftHand);

            var rightHand = new GameObject("RightHand Controller");
            rightHand.transform.SetParent(xrRoot.transform, false);
            SetField(rightHand.AddComponent<XRNodePoseDriver>(), "_node", (int)UnityEngine.XR.XRNode.RightHand);

            // CameraModeSwitch picks the live camera at runtime and starts on
            // desktop, so the scene is playable in the Editor with no headset.
            xrRoot.SetActive(false);

            // --- Reticle (camera.js aimReticle) ---
            var reticleGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleGo.name = "Reticle";
            reticleGo.transform.localScale = Vector3.one * 0.2f;   // 0.1 radius, as in camera.js
            Object.DestroyImmediate(reticleGo.GetComponent<Collider>());
            var reticle = reticleGo.AddComponent<ReticleView>();
            SetField(reticle, "_renderer", reticleGo.GetComponent<Renderer>());
            SetField(reticle, "_aimMask", ~0);
            reticleGo.GetComponent<Renderer>().sharedMaterial =
                EnsureMaterial("Reticle", new Color32(0x00, 0xFF, 0x66, 0xFF), false);

            // --- HUD (hud.js VRHUD panel) ---
            // TextMeshPro's mesh renderer, not a Canvas: world-space text with no
            // UI stack, which is what a head-mounted panel wants.
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            var hudText = hudGo.AddComponent<TextMeshPro>();
            hudText.rectTransform.sizeDelta = new Vector2(2.0f, 0.5f);   // hud.js plane size
            hudText.alignment = TextAlignmentOptions.Center;
            hudText.fontSize = 0.11f;
            hudText.color = new Color32(0x00, 0xEA, 0xFF, 0xFF);

            var hud = hudGo.AddComponent<RunHud>();
            SetField(hud, "_label", hudText);
            SetField(hud, "_round", round);
            SetField(hud, "_matchState", state);

            var modeSwitch = rigRoot.AddComponent<CameraModeSwitch>();
            SetField(modeSwitch, "_rig", rig);
            SetField(modeSwitch, "_blaster", blaster);
            SetField(modeSwitch, "_desktopCamera", chase);
            SetField(modeSwitch, "_desktopAimOrigin", chase.transform);
            SetField(modeSwitch, "_desktopCameraComponent", chaseCam);
            SetField(modeSwitch, "_xrRoot", xrRoot);
            SetField(modeSwitch, "_xrAimOrigin", rightHand.transform);
            SetField(modeSwitch, "_xrCameraComponent", xrCam);
            SetField(modeSwitch, "_reticle", reticle);
            SetField(modeSwitch, "_hud", hudGo.transform);

            SetField(blaster, "_config", config);
            SetField(blaster, "_aimOrigin", chase.transform);   // switched on headset connect
            SetField(blaster, "_matchState", state);
            SetField(blaster, "_targetZoneMask", 1 << zoneLayer);

            SetField(router, "_blaster", blaster);
            SetField(router, "_matchState", state);

            return player;
        }

        private static void BuildSpawns(GameConfig config, ArenaBounds arena, TargetDummy prefab,
                                        JetpackLocomotion player, MatchStateMachine state)
        {
            var root = new GameObject("Spawns");
            var network = root.AddComponent<TargetSpawnNetwork>();

            var points = new List<Transform>();
            for (int i = 0; i < TargetSpawns.Length; i++)
            {
                var point = new GameObject($"TargetSpawn_{i}");
                point.transform.SetParent(root.transform);
                point.transform.localPosition = TargetSpawns[i];
                points.Add(point.transform);
            }

            SetField(network, "_config", config);
            SetField(network, "_arena", arena);
            SetField(network, "_dummyPrefab", prefab);
            SetField(network, "_player", player);
            SetField(network, "_matchState", state);

            var so = new SerializedObject(network);
            var list = so.FindProperty("_spawnPoints");
            list.arraySize = points.Count;
            for (int i = 0; i < points.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // --------------------------------------------------------------- assets

        private static TargetDummy BuildTargetDummyPrefab(GameConfig config, Materials mats, int zoneLayer)
        {
            EnsureFolder("Assets/VelocityTag/Prefabs");

            var root = new GameObject("TargetDummy");
            var dummy = root.AddComponent<TargetDummy>();

            var body  = AddZoneBox(root, "Body",  BodySize,  BodyPos,  mats.Body,  zoneLayer, null);
            var chest = AddZoneBox(root, "Chest", ChestSize, ChestPos, mats.Chest, zoneLayer, TagType.Chest);
            var head  = AddZoneBox(root, "Head",  HeadSize,  HeadPos,  mats.Head,  zoneLayer, TagType.Helmet);
            var back  = AddZoneBox(root, "Back",  BackSize,  BackPos,  mats.Back,  zoneLayer, TagType.FlankPack);

            SetField(dummy, "_config", config);
            SetField(dummy, "_body",  body.GetComponent<Renderer>());
            SetField(dummy, "_chest", chest.GetComponent<Renderer>());
            SetField(dummy, "_head",  head.GetComponent<Renderer>());
            SetField(dummy, "_back",  back.GetComponent<Renderer>());
            SetField(dummy, "_matBody", mats.Body);
            SetField(dummy, "_matChestNormal", mats.Chest);
            SetField(dummy, "_matHeadNormal", mats.Head);
            SetField(dummy, "_matBackNormal", mats.Back);
            SetField(dummy, "_matHitChest", mats.HitChest);
            SetField(dummy, "_matHitHead", mats.HitHead);
            SetField(dummy, "_matHitBack", mats.HitBack);
            SetField(dummy, "_matGhost", mats.Ghost);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<TargetDummy>();
        }

        private static GameObject AddZoneBox(GameObject parent, string name, Vector3 size, Vector3 localPos,
                                             Material material, int zoneLayer, TagType? zone)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;

            if (zone.HasValue)
            {
                go.layer = zoneLayer;
                go.AddComponent<TargetZone>().zone = zone.Value;
            }
            else
            {
                // The body is visual only — it must not block a zone raycast.
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            return go;
        }

        private struct Materials
        {
            public Material Body, Chest, Head, Back, HitChest, HitHead, HitBack, Ghost;
        }

        private static Materials EnsureMaterials()
        {
            EnsureFolder(MaterialDir);
            // Reference palette from targets.js, kept only so hit states are
            // distinguishable during gameplay validation. Not an art pass.
            return new Materials
            {
                Body     = EnsureMaterial("TargetBody",     new Color32(0x11, 0x11, 0x15, 0xFF), false),
                Chest    = EnsureMaterial("TargetChest",    new Color32(0xFF, 0x24, 0x5F, 0xFF), false),
                Head     = EnsureMaterial("TargetHead",     new Color32(0xFF, 0xD7, 0x6A, 0xFF), false),
                Back     = EnsureMaterial("TargetBack",     new Color32(0x00, 0xEA, 0xFF, 0xFF), false),
                HitChest = EnsureMaterial("TargetHitChest", new Color32(0xFF, 0x66, 0xCC, 0xFF), false),
                HitHead  = EnsureMaterial("TargetHitHead",  Color.white, false),
                HitBack  = EnsureMaterial("TargetHitBack",  new Color32(0xBF, 0x55, 0xEC, 0xFF), false),
                Ghost    = EnsureMaterial("TargetGhost",    new Color(0f, 0.92f, 1f, 0.4f), true),
            };
        }

        private static Material EnsureMaterial(string name, Color color, bool transparent)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (transparent)
            {
                // URP Lit surface-type switch; harmless no-ops on Standard.
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetOverrideTag("RenderType", "Transparent");
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static GameConfig EnsureConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/VelocityTag/Config");
            var config = ScriptableObject.CreateInstance<GameConfig>();   // defaults == config.js
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        // ---------------------------------------------------------------- utils

        /// Assigns a private [SerializeField] without widening its access modifier.
        private static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Velocity Tag] {target.GetType().Name} has no serialized field '{field}'.");
                return;
            }

            switch (value)
            {
                case Object o:  prop.objectReferenceValue = o; break;
                case float f:   prop.floatValue = f; break;
                case int i:     prop.intValue = i; break;   // also enums and LayerMasks
                case string s:  prop.stringValue = s; break;
                case bool b:    prop.boolValue = b; break;
                default:
                    Debug.LogError($"[Velocity Tag] Unsupported field type for '{field}'.");
                    return;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)   // 0-7 are Unity's built-ins
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }

            Debug.LogError($"[Velocity Tag] No free layer slot for '{name}'. Free one and re-run.");
            return 0;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Drop entries whose scene asset is gone — e.g. the URP template's
            // SampleScene — so a build cannot fail on a dangling reference.
            scenes.RemoveAll(s => string.IsNullOrEmpty(s.path) ||
                                  AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) == null);

            if (!scenes.Exists(s => s.path == path))
                scenes.Insert(0, new EditorBuildSettingsScene(path, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
