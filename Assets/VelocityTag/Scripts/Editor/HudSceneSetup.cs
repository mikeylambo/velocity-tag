// HudSceneSetup.cs (Editor)
// One-click wiring for the minimal HUD + results screen in TimeAttack.unity:
// VelocityTag > Setup HUD in TimeAttack Scene. Idempotent — reruns tear down
// and rebuild the HUD/Results objects. Saves the scene when done.

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace VelocityTag.EditorTools
{
    public static class HudSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/TimeAttack.unity";

        [MenuItem("VelocityTag/Setup HUD in TimeAttack Scene")]
        public static void Setup()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var canvasGO = GameObject.Find("Canvas");
            var round = Object.FindFirstObjectByType<TimeAttackRound>();
            var startButton = GameObject.Find("StartButton");
            if (canvasGO == null || round == null || startButton == null)
            {
                Debug.LogError("HudSceneSetup: Canvas / TimeAttackRound / StartButton not found — is TimeAttack.unity assembled?");
                return;
            }

            // idempotent: clear previous runs
            DestroyIfExists(canvasGO.transform, "HUD");
            DestroyIfExists(canvasGO.transform, "ResultsPanel");
            foreach (var old in canvasGO.GetComponents<TimeAttackHUD>()) Object.DestroyImmediate(old);
            foreach (var old in canvasGO.GetComponents<ResultsScreen>()) Object.DestroyImmediate(old);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ---------- HUD ----------
            var hudRoot = NewUI("HUD", canvasGO.transform);
            Stretch(hudRoot);

            var score = MakeText("ScoreText", hudRoot.transform, font, 34, TextAnchor.UpperLeft);
            Place(score, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(320f, 44f), new Vector2(0f, 1f));
            var breakdown = MakeText("BreakdownText", hudRoot.transform, font, 18, TextAnchor.UpperLeft);
            Place(breakdown, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -66f), new Vector2(520f, 26f), new Vector2(0f, 1f));
            breakdown.color = new Color(1f, 0.85f, 0.2f);

            var timer = MakeText("TimerText", hudRoot.transform, font, 34, TextAnchor.UpperCenter);
            Place(timer, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(200f, 44f), new Vector2(0.5f, 1f));

            var combo = MakeText("ComboText", hudRoot.transform, font, 28, TextAnchor.UpperRight);
            Place(combo, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(160f, 40f), new Vector2(1f, 1f));
            combo.color = new Color(1f, 0.85f, 0.2f);

            var center = MakeText("CenterText", hudRoot.transform, font, 72, TextAnchor.MiddleCenter);
            Place(center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(400f, 90f), new Vector2(0.5f, 0.5f));

            var crossGO = NewUI("Crosshair", hudRoot.transform);
            var crossImg = crossGO.AddComponent<Image>();
            crossImg.raycastTarget = false;
            var crossRT = crossGO.GetComponent<RectTransform>();
            crossRT.anchorMin = crossRT.anchorMax = crossRT.pivot = new Vector2(0.5f, 0.5f);
            crossRT.anchoredPosition = Vector2.zero;
            crossRT.sizeDelta = new Vector2(8f, 8f);

            var hud = canvasGO.AddComponent<TimeAttackHUD>();
            var soH = new SerializedObject(hud);
            soH.FindProperty("_round").objectReferenceValue = round;
            soH.FindProperty("_scoreText").objectReferenceValue = score;
            soH.FindProperty("_timerText").objectReferenceValue = timer;
            soH.FindProperty("_comboText").objectReferenceValue = combo;
            soH.FindProperty("_centerText").objectReferenceValue = center;
            soH.FindProperty("_breakdownText").objectReferenceValue = breakdown;
            soH.FindProperty("_crosshair").objectReferenceValue = crossImg;
            soH.ApplyModifiedPropertiesWithoutUndo();

            // ---------- Results panel ----------
            var panelGO = NewUI("ResultsPanel", canvasGO.transform);
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.05f, 0.1f, 0.88f);
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(640f, 360f);

            var title = MakeText("Title", panelGO.transform, font, 40, TextAnchor.MiddleCenter);
            Place(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 50f), new Vector2(0.5f, 1f));
            title.text = "RUN COMPLETE";

            var rScore = MakeText("FinalScoreText", panelGO.transform, font, 32, TextAnchor.MiddleCenter);
            Place(rScore, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(600f, 42f), new Vector2(0.5f, 1f));

            var rMedal = MakeText("MedalText", panelGO.transform, font, 28, TextAnchor.MiddleCenter);
            Place(rMedal, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(600f, 36f), new Vector2(0.5f, 1f));
            rMedal.color = new Color(1f, 0.85f, 0.2f);

            var rStats = MakeText("StatsText", panelGO.transform, font, 20, TextAnchor.MiddleCenter);
            Place(rStats, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(600f, 60f), new Vector2(0.5f, 1f));

            var againGO = NewUI("PlayAgainButton", panelGO.transform);
            var againImg = againGO.AddComponent<Image>();
            againImg.color = new Color(0.15f, 0.6f, 0.25f, 0.95f);
            var againBtn = againGO.AddComponent<Button>();
            var againRT = againGO.GetComponent<RectTransform>();
            againRT.anchorMin = againRT.anchorMax = againRT.pivot = new Vector2(0.5f, 0f);
            againRT.anchoredPosition = new Vector2(0f, 32f);
            againRT.sizeDelta = new Vector2(200f, 52f);
            var againTxt = MakeText("Text", againGO.transform, font, 24, TextAnchor.MiddleCenter);
            Stretch(againTxt.gameObject);
            againTxt.text = "PLAY AGAIN";
            UnityEventTools.AddPersistentListener(againBtn.onClick,
                new UnityEngine.Events.UnityAction(round.StartTimeAttack));

            var results = canvasGO.AddComponent<ResultsScreen>();
            var soR = new SerializedObject(results);
            soR.FindProperty("_panel").objectReferenceValue = panelGO;
            soR.FindProperty("_startButton").objectReferenceValue = startButton;
            soR.FindProperty("_scoreText").objectReferenceValue = rScore;
            soR.FindProperty("_medalText").objectReferenceValue = rMedal;
            soR.FindProperty("_statsText").objectReferenceValue = rStats;
            soR.ApplyModifiedPropertiesWithoutUndo();

            panelGO.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("HudSceneSetup: HUD + results screen wired into TimeAttack.unity and scene saved.");
        }

        private static void DestroyIfExists(Transform parent, string childName)
        {
            var t = parent.Find(childName);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor anchor)
        {
            var go = NewUI(name, parent);
            var txt = go.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = size;
            txt.alignment = anchor;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private static void Place(Text t, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
