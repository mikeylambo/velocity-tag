// FxSceneSetup.cs (Editor)
// One-click wiring for the Feel Pass 01 FX layer:
// VelocityTag > Setup FX in TimeAttack Scene. Adds TargetFX to the TargetDummy
// prefab, and builds an FX root in the scene carrying WeaponBeamFX (with its
// LineRenderer + material asset) and ScorePopupFX (font wired for builds).
// Idempotent — safe to rerun. Saves prefab and scene.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VelocityTag.EditorTools
{
    public static class FxSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/TimeAttack.unity";
        private const string PrefabPath = "Assets/VelocityTag/TargetDummy.prefab";
        private const string BeamMatPath = "Assets/VelocityTag/FX_Beam.mat";

        [MenuItem("VelocityTag/Setup FX in TimeAttack Scene")]
        public static void Setup()
        {
            // ---- prefab: ensure TargetFX on the dummy root ----
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError("FxSceneSetup: TargetDummy prefab not found at " + PrefabPath);
                return;
            }
            if (prefabRoot.GetComponent<TargetFX>() == null)
                prefabRoot.AddComponent<TargetFX>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            // ---- scene ----
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var old = GameObject.Find("FX");
            if (old != null) Object.DestroyImmediate(old);

            var beamMat = AssetDatabase.LoadAssetAtPath<Material>(BeamMatPath);
            if (beamMat == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    Debug.LogError("FxSceneSetup: Sprites/Default shader not found.");
                    return;
                }
                beamMat = new Material(shader);
                AssetDatabase.CreateAsset(beamMat, BeamMatPath);
            }

            var fxRoot = new GameObject("FX");

            var beamGO = new GameObject("Beam");
            beamGO.transform.SetParent(fxRoot.transform, false);
            var line = beamGO.AddComponent<LineRenderer>();
            line.material = beamMat;
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.numCapVertices = 2;
            line.useWorldSpace = true;
            line.enabled = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            var beamFX = fxRoot.AddComponent<WeaponBeamFX>();
            var soBeam = new SerializedObject(beamFX);
            soBeam.FindProperty("_line").objectReferenceValue = line;
            soBeam.ApplyModifiedPropertiesWithoutUndo();

            var popupFX = fxRoot.AddComponent<ScorePopupFX>();
            var soPopup = new SerializedObject(popupFX);
            soPopup.FindProperty("_font").objectReferenceValue =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            soPopup.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("FxSceneSetup: TargetFX on prefab + FX root (beam, score popups) wired and saved.");
        }
    }
}
