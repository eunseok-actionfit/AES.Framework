#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class TouchButtonMenu
{
    // 프로젝트에서 Btn_Base.prefab 위치에 맞게 경로 수정
    private const string BtnBasePrefabPath = "Assets/_Project/Prefabs/UI/Common/Buttons/Btn_Base.prefab";

    [MenuItem("GameObject/UI/Touch Button", false, 2010)]
    public static void CreateTouchButton(MenuCommand menuCommand)
    {
        // 1. Prefab 로드
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BtnBasePrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"TouchButtonMenu: Btn_Base prefab not found at path: {BtnBasePrefabPath}");
            return;
        }

        // 2. 부모 / Canvas 찾기
        GameObject parentGO = menuCommand.context as GameObject;
        Canvas canvas = null;

        if (parentGO != null)
        {
            canvas = parentGO.GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            // Canvas 없으면 새로 생성
            var canvasGO = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // EventSystem 없으면 같이 생성
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            parentGO = canvas.gameObject;
        }

        // 3. Btn_Base 프리팹 인스턴스 생성 및 부모 정렬
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parentGO.transform);
        if (instance == null)
        {
            Debug.LogError("TouchButtonMenu: Failed to instantiate Btn_Base prefab.");
            return;
        }

        instance.name = "TouchButton";

        // 4. RectTransform 기본 사이즈 설정
        var rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(350, 150);
        }

        // 5. Undo / 선택 처리
        Undo.RegisterCreatedObjectUndo(instance, "Create Touch Button");
        Selection.activeGameObject = instance;
    }
}
#endif
