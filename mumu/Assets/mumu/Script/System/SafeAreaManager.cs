using UnityEngine;

public class SafeAreaManager : MonoBehaviour
{
    [SerializeField]                    
    private RectTransform safeAreaTransform;

    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()    
    {
        if (safeAreaTransform == null)
            safeAreaTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }


    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastOrientation = Screen.orientation;

        // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeAreaTransform.anchorMin = anchorMin;
        safeAreaTransform.anchorMax = anchorMax;

        // Set base resolution to 1280x720 and center it in the safe area
        float baseWidth = 1280f;
        float baseHeight = 720f;
        float safeAreaWidth = safeArea.width;
        float safeAreaHeight = safeArea.height;

        // Calculate scale to fit base resolution inside safe area
        float scale = Mathf.Min(safeAreaWidth / baseWidth, safeAreaHeight / baseHeight);
        float width = baseWidth * scale;
        float height = baseHeight * scale;

        safeAreaTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        safeAreaTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        // Center the rect in the safe area
        Vector2 offset = new Vector2(
            (safeAreaWidth - width) / 2f + safeArea.x,
            (safeAreaHeight - height) / 2f + safeArea.y
        );
        safeAreaTransform.anchoredPosition = offset;
    }
}
