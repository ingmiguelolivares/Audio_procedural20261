using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ResponsiveLandscapeUI : MonoBehaviour
{
    [Header("Canvas scaler")]
    public bool configureCanvasScaler = true;
    public CanvasScaler canvasScaler;
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;

    [Header("Landscape")]
    public bool forceLandscapeInPlayer = true;
    public ScreenOrientation preferredOrientation = ScreenOrientation.LandscapeLeft;

    [Header("Safe area")]
    public bool applySafeArea = true;
    public RectTransform safeAreaTarget;
    public Vector2 safeAreaPadding = Vector2.zero;

    [Header("Responsive text")]
    public bool applyAutoSizeToText = true;
    public bool includeInactiveText = true;
    public float textAutoSizeMin = 12f;
    public float textAutoSizeMax = 48f;
    public bool refreshTextAutoSizeInPlayMode = true;
    [Range(1, 60)] public int textRefreshFrameInterval = 10;
    public bool configureDropdownText = true;
    public bool applyLegacyTextBestFit = true;
    public int legacyTextMinSize = 12;
    public int legacyTextMaxSize = 48;

    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation;
    private int lastTextRefreshFrame = -9999;

    private void Awake()
    {
        ResolveReferences();
        ApplyResponsiveSettings();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyResponsiveSettings();
    }

    private void Update()
    {
        if (HasScreenChanged())
        {
            ApplyResponsiveSettings();
            return;
        }

        if (!Application.isPlaying || !refreshTextAutoSizeInPlayMode || !applyAutoSizeToText)
            return;

        if (Time.frameCount - lastTextRefreshFrame >= textRefreshFrameInterval)
        {
            ApplyTextAutoSize();
            lastTextRefreshFrame = Time.frameCount;
        }
    }

    [ContextMenu("Apply Responsive Settings")]
    public void ApplyResponsiveSettings()
    {
        ResolveReferences();
        ApplyLandscapeOrientation();
        ApplyCanvasScaler();
        ApplySafeArea();
        ApplyTextAutoSize();
        lastTextRefreshFrame = Time.frameCount;
        CacheScreenState();
    }

    private void ResolveReferences()
    {
        if (canvasScaler == null)
            canvasScaler = GetComponent<CanvasScaler>();

        if (safeAreaTarget == null)
            safeAreaTarget = GetComponent<RectTransform>();
    }

    private void ApplyLandscapeOrientation()
    {
        if (!Application.isPlaying || !forceLandscapeInPlayer)
            return;

        Screen.orientation = preferredOrientation;
    }

    private void ApplyCanvasScaler()
    {
        if (!configureCanvasScaler || canvasScaler == null)
            return;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
    }

    private void ApplySafeArea()
    {
        if (!applySafeArea || safeAreaTarget == null)
            return;

        Rect safeArea = Screen.safeArea;
        float screenWidth = Mathf.Max(1f, Screen.width);
        float screenHeight = Mathf.Max(1f, Screen.height);

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x = Mathf.Clamp01((anchorMin.x + safeAreaPadding.x) / screenWidth);
        anchorMin.y = Mathf.Clamp01((anchorMin.y + safeAreaPadding.y) / screenHeight);
        anchorMax.x = Mathf.Clamp01((anchorMax.x - safeAreaPadding.x) / screenWidth);
        anchorMax.y = Mathf.Clamp01((anchorMax.y - safeAreaPadding.y) / screenHeight);

        safeAreaTarget.anchorMin = anchorMin;
        safeAreaTarget.anchorMax = anchorMax;
        safeAreaTarget.offsetMin = Vector2.zero;
        safeAreaTarget.offsetMax = Vector2.zero;
    }

    private void ApplyTextAutoSize()
    {
        if (!applyAutoSizeToText)
            return;

        Transform searchRoot = GetTextSearchRoot();

        TextMeshProUGUI[] tmpTexts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(includeInactiveText);
        for (int i = 0; i < tmpTexts.Length; i++)
            ApplyTextMeshProAutoSize(tmpTexts[i]);

        if (configureDropdownText)
            ApplyDropdownTextAutoSize(searchRoot);

        if (!applyLegacyTextBestFit)
            return;

        Text[] legacyTexts = searchRoot.GetComponentsInChildren<Text>(includeInactiveText);
        for (int i = 0; i < legacyTexts.Length; i++)
            ApplyLegacyTextBestFit(legacyTexts[i]);
    }

    private Transform GetTextSearchRoot()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.rootCanvas != null)
            return parentCanvas.rootCanvas.transform;

        return transform;
    }

    private void ApplyDropdownTextAutoSize(Transform searchRoot)
    {
        TMP_Dropdown[] tmpDropdowns = searchRoot.GetComponentsInChildren<TMP_Dropdown>(includeInactiveText);
        for (int i = 0; i < tmpDropdowns.Length; i++)
            ApplyTextMeshProDropdownAutoSize(tmpDropdowns[i]);

        if (!applyLegacyTextBestFit)
            return;

        Dropdown[] dropdowns = searchRoot.GetComponentsInChildren<Dropdown>(includeInactiveText);
        for (int i = 0; i < dropdowns.Length; i++)
            ApplyLegacyDropdownBestFit(dropdowns[i]);
    }

    private void ApplyTextMeshProDropdownAutoSize(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        ApplyTextMeshProAutoSize(dropdown.captionText);
        ApplyTextMeshProAutoSize(dropdown.itemText);

        if (dropdown.template == null)
            return;

        TextMeshProUGUI[] templateTexts = dropdown.template.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < templateTexts.Length; i++)
            ApplyTextMeshProAutoSize(templateTexts[i]);
    }

    private void ApplyLegacyDropdownBestFit(Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        ApplyLegacyTextBestFit(dropdown.captionText);
        ApplyLegacyTextBestFit(dropdown.itemText);

        if (dropdown.template == null)
            return;

        Text[] templateTexts = dropdown.template.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < templateTexts.Length; i++)
            ApplyLegacyTextBestFit(templateTexts[i]);
    }

    private void ApplyTextMeshProAutoSize(TMP_Text text)
    {
        if (text == null)
            return;

        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(1f, textAutoSizeMin);
        text.fontSizeMax = Mathf.Max(text.fontSizeMin, textAutoSizeMax);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void ApplyLegacyTextBestFit(Text text)
    {
        if (text == null)
            return;

        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(1, legacyTextMinSize);
        text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize, legacyTextMaxSize);
    }

    private bool HasScreenChanged()
    {
        return lastScreenSize.x != Screen.width ||
               lastScreenSize.y != Screen.height ||
               lastSafeArea != Screen.safeArea ||
               lastOrientation != Screen.orientation;
    }

    private void CacheScreenState()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;
        lastOrientation = Screen.orientation;
    }
}

[ExecuteAlways]
[DisallowMultipleComponent]
public class ResponsiveLandscapeElement : MonoBehaviour
{
    [Header("Target")]
    public RectTransform target;

    [Header("Width")]
    [Range(0f, 1f)] public float widthPercentOfParent = 0.9f;
    public float minWidth = 320f;
    public float maxWidth = 1400f;

    [Header("Height")]
    [Range(0f, 1f)] public float heightPercentOfParent = 0.85f;
    public float minHeight = 220f;
    public float maxHeight = 900f;

    [Header("Behavior")]
    public bool centerInParent = true;
    public bool applyEveryFrameInEditor = true;

    private void Awake()
    {
        ResolveTarget();
        ApplySize();
    }

    private void OnEnable()
    {
        ResolveTarget();
        ApplySize();
    }

    private void Update()
    {
        if (!Application.isPlaying && !applyEveryFrameInEditor)
            return;

        ApplySize();
    }

    [ContextMenu("Apply Size")]
    public void ApplySize()
    {
        ResolveTarget();

        if (target == null || target.parent == null)
            return;

        RectTransform parent = target.parent as RectTransform;
        if (parent == null)
            return;

        float width = Mathf.Clamp(parent.rect.width * widthPercentOfParent, minWidth, maxWidth);
        float height = Mathf.Clamp(parent.rect.height * heightPercentOfParent, minHeight, maxHeight);

        if (centerInParent)
        {
            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = Vector2.zero;
        }

        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private void ResolveTarget()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
    }
}
