using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExperimentInteractionLogger : MonoBehaviour
{
    [Header("Session")]
    public string participantId = "participant";
    public string sessionId;
    public bool startSessionOnAwake = true;

    [Header("Auto tracking")]
    public bool autoRegisterControlsOnStart = true;
    public bool includeInactiveObjects = true;
    public List<GameObject> scanRoots = new List<GameObject>();

    [Header("Export")]
    public bool exportOnApplicationQuit = true;
    public bool preferAccessibleDocumentsDirectory = true;
    public bool fallbackToPersistentDataPathIfPublicFolderFails = true;
    public string accessibleFolderName = "ProceduralMusicExperiment";
    public string customExportDirectory;
    public string filePrefix = "procedural_music_experiment";

    public string CurrentPhase { get; private set; }
    public int CurrentTrialIndex { get; private set; } = -1;
    public string CurrentImageName { get; private set; } = string.Empty;
    public string LastExportPath { get; private set; }

    private readonly List<ExperimentCsvRow> rows = new List<ExperimentCsvRow>();
    private readonly Dictionary<string, int> objectActionCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> objectTotalCounts = new Dictionary<string, int>();
    private readonly HashSet<int> registeredControlIds = new HashSet<int>();
    private DateTime sessionStartUtc;
    private bool hasExported;

    private void Awake()
    {
        if (startSessionOnAwake)
            BeginSession();
    }

    private void Start()
    {
        if (autoRegisterControlsOnStart)
            RegisterControls();
    }

    private void OnApplicationQuit()
    {
        if (exportOnApplicationQuit && !hasExported && rows.Count > 0)
            ExportCsv();
    }

    public void BeginSession()
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        sessionStartUtc = DateTime.UtcNow;
        hasExported = false;
        SetContext("Session", -1, string.Empty);
        RecordEvent("Session", "Experiment", "SessionStart", string.Empty, 0f);
    }

    public void ResetSessionData(bool createNewSessionId = true)
    {
        rows.Clear();
        objectActionCounts.Clear();
        objectTotalCounts.Clear();
        LastExportPath = string.Empty;

        if (createNewSessionId)
            sessionId = string.Empty;

        BeginSession();
    }

    public void SetContext(string phase, int trialIndex, string imageName)
    {
        CurrentPhase = string.IsNullOrEmpty(phase) ? string.Empty : phase;
        CurrentTrialIndex = trialIndex;
        CurrentImageName = string.IsNullOrEmpty(imageName) ? string.Empty : imageName;
    }

    public void RegisterControls()
    {
        if (scanRoots != null && scanRoots.Count > 0)
        {
            for (int i = 0; i < scanRoots.Count; i++)
                RegisterControlsUnder(scanRoots[i]);

            return;
        }

        Selectable[] selectables = FindObjectsByType<Selectable>(
            includeInactiveObjects ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < selectables.Length; i++)
            RegisterSelectable(selectables[i]);
    }

    public void RegisterControlsUnder(GameObject root)
    {
        if (root == null)
            return;

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(includeInactiveObjects);
        for (int i = 0; i < selectables.Length; i++)
            RegisterSelectable(selectables[i]);
    }

    public void RecordEvent(string objectName, string objectType, string action, string value = "", float durationSeconds = 0f)
    {
        AddRow("event", objectName, objectType, objectName, action, value, 0, durationSeconds);
    }

    public void RecordInteraction(Component control, string objectType, string action, string value = "")
    {
        if (control == null)
            return;

        string objectPath = GetHierarchyPath(control.transform);
        string objectName = control.gameObject.name;
        string actionKey = objectPath + "|" + action;
        string totalKey = objectPath;

        int actionCount = IncrementCount(objectActionCounts, actionKey);
        IncrementCount(objectTotalCounts, totalKey);
        AddRow("interaction", objectName, objectType, objectPath, action, value, actionCount, 0f);
    }

    public void RecordTrialStart(int trialIndex, string imageName)
    {
        SetContext("ImageTrial", trialIndex, imageName);
        RecordEvent("Trial " + trialIndex, "Experiment", "TrialStart", imageName, 0f);
    }

    public void RecordTrialEnd(int trialIndex, string imageName, float durationSeconds)
    {
        SetContext("ImageTrial", trialIndex, imageName);
        RecordEvent("Trial " + trialIndex, "Experiment", "TrialEnd", imageName, durationSeconds);
    }

    public string ExportCsv()
    {
        string directory = ResolveExportDirectory();
        directory = PrepareExportDirectory(directory);

        string safeParticipant = SanitizeFilePart(participantId);
        string safeSession = SanitizeFilePart(sessionId);
        string fileName = filePrefix + "_" + safeParticipant + "_" + safeSession + ".csv";
        LastExportPath = Path.Combine(directory, fileName);

        List<ExperimentCsvRow> exportRows = new List<ExperimentCsvRow>(rows);
        AppendSummaryRows(exportRows);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("rowType,sessionId,participantId,platform,timestampIso,elapsedSeconds,phase,trialIndex,imageName,objectType,objectName,objectPath,action,value,count,durationSeconds");

        for (int i = 0; i < exportRows.Count; i++)
            sb.AppendLine(exportRows[i].ToCsvLine());

        File.WriteAllText(LastExportPath, sb.ToString(), Encoding.UTF8);
        hasExported = true;
        Debug.Log("[ExperimentInteractionLogger] CSV exported: " + LastExportPath);
        return LastExportPath;
    }

    private void RegisterSelectable(Selectable selectable)
    {
        if (selectable == null)
            return;

        int id = selectable.GetInstanceID();
        if (registeredControlIds.Contains(id))
            return;

        registeredControlIds.Add(id);

        Button button = selectable as Button;
        if (button != null)
        {
            button.onClick.AddListener(() => RecordInteraction(button, "Button", "Click"));
            return;
        }

        Slider slider = selectable as Slider;
        if (slider != null)
        {
            slider.onValueChanged.AddListener(value =>
                RecordInteraction(slider, "Slider", "ValueChanged", value.ToString("0.###", CultureInfo.InvariantCulture)));

            AddPointerTracking(slider, "Slider");
            return;
        }

        TMP_Dropdown tmpDropdown = selectable as TMP_Dropdown;
        if (tmpDropdown != null)
        {
            tmpDropdown.onValueChanged.AddListener(value =>
                RecordInteraction(tmpDropdown, "TMP_Dropdown", "ValueChanged", value.ToString(CultureInfo.InvariantCulture)));

            AddPointerTracking(tmpDropdown, "TMP_Dropdown");
            return;
        }

        Dropdown dropdown = selectable as Dropdown;
        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(value =>
                RecordInteraction(dropdown, "Dropdown", "ValueChanged", value.ToString(CultureInfo.InvariantCulture)));

            AddPointerTracking(dropdown, "Dropdown");
            return;
        }

        Toggle toggle = selectable as Toggle;
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(value =>
                RecordInteraction(toggle, "Toggle", "ValueChanged", value ? "true" : "false"));

            AddPointerTracking(toggle, "Toggle");
        }
    }

    private void AddPointerTracking(Component control, string objectType)
    {
        EventTrigger trigger = control.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = control.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener(_ => RecordInteraction(control, objectType, "PointerDown"));
        trigger.triggers.Add(pointerDown);
    }

    private void AddRow(string rowType, string objectName, string objectType, string objectPath, string action, string value, int count, float durationSeconds)
    {
        rows.Add(new ExperimentCsvRow
        {
            rowType = rowType,
            sessionId = sessionId,
            participantId = participantId,
            platform = Application.platform.ToString(),
            timestampIso = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            elapsedSeconds = GetElapsedSeconds(),
            phase = CurrentPhase,
            trialIndex = CurrentTrialIndex,
            imageName = CurrentImageName,
            objectType = objectType,
            objectName = objectName,
            objectPath = objectPath,
            action = action,
            value = value,
            count = count,
            durationSeconds = durationSeconds
        });
    }

    private void AppendSummaryRows(List<ExperimentCsvRow> exportRows)
    {
        foreach (KeyValuePair<string, int> kv in objectActionCounts)
        {
            string[] parts = kv.Key.Split('|');
            string objectPath = parts.Length > 0 ? parts[0] : kv.Key;
            string action = parts.Length > 1 ? parts[1] : "Unknown";

            exportRows.Add(CreateSummaryRow(objectPath, action, kv.Value));
        }

        foreach (KeyValuePair<string, int> kv in objectTotalCounts)
            exportRows.Add(CreateSummaryRow(kv.Key, "TotalInteractions", kv.Value));
    }

    private ExperimentCsvRow CreateSummaryRow(string objectPath, string action, int count)
    {
        return new ExperimentCsvRow
        {
            rowType = "summary",
            sessionId = sessionId,
            participantId = participantId,
            platform = Application.platform.ToString(),
            timestampIso = DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
            elapsedSeconds = GetElapsedSeconds(),
            phase = "Summary",
            trialIndex = -1,
            imageName = string.Empty,
            objectType = "UI",
            objectName = GetLastPathPart(objectPath),
            objectPath = objectPath,
            action = action,
            value = string.Empty,
            count = count,
            durationSeconds = 0f
        };
    }

    private float GetElapsedSeconds()
    {
        if (sessionStartUtc == default(DateTime))
            sessionStartUtc = DateTime.UtcNow;

        return (float)(DateTime.UtcNow - sessionStartUtc).TotalSeconds;
    }

    private static int IncrementCount(Dictionary<string, int> dictionary, string key)
    {
        int count;
        dictionary.TryGetValue(key, out count);
        count++;
        dictionary[key] = count;
        return count;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        List<string> names = new List<string>();
        Transform current = transform;
        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private string ResolveExportDirectory()
    {
        if (!string.IsNullOrWhiteSpace(customExportDirectory))
            return customExportDirectory;

        if (!preferAccessibleDocumentsDirectory)
            return ResolvePersistentExportDirectory();

#if UNITY_ANDROID
        return Path.Combine("/storage/emulated/0/Documents", GetExportFolderName());
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        return Path.Combine(ResolveUserDocumentsDirectory(), GetExportFolderName());
#elif UNITY_IOS
        return ResolvePersistentExportDirectory();
#else
        return Path.Combine(ResolveUserDocumentsDirectory(), GetExportFolderName());
#endif
    }

    private string PrepareExportDirectory(string preferredDirectory)
    {
        if (TryCreateDirectory(preferredDirectory))
            return preferredDirectory;

        if (!fallbackToPersistentDataPathIfPublicFolderFails)
            return preferredDirectory;

        string fallbackDirectory = ResolvePersistentExportDirectory();
        if (TryCreateDirectory(fallbackDirectory))
        {
            Debug.LogWarning("[ExperimentInteractionLogger] Could not write to public export folder. Using fallback path: " + fallbackDirectory);
            return fallbackDirectory;
        }

        return preferredDirectory;
    }

    private static bool TryCreateDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return false;

        try
        {
            Directory.CreateDirectory(directory);
            return Directory.Exists(directory);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ExperimentInteractionLogger] Could not create export directory: " + directory + "\n" + ex.Message);
            return false;
        }
    }

    private string ResolvePersistentExportDirectory()
    {
        return Path.Combine(Application.persistentDataPath, GetExportFolderName());
    }

    private string ResolveUserDocumentsDirectory()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (!string.IsNullOrWhiteSpace(documents))
            return documents;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
            return Path.Combine(home, "Documents");

        return Application.persistentDataPath;
    }

    private string GetExportFolderName()
    {
        return string.IsNullOrWhiteSpace(accessibleFolderName) ? "ProceduralMusicExperiment" : accessibleFolderName;
    }

    private static string SanitizeFilePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "session";

        char[] invalid = Path.GetInvalidFileNameChars();
        string result = value;
        for (int i = 0; i < invalid.Length; i++)
            result = result.Replace(invalid[i], '_');

        return result.Replace(' ', '_');
    }

    private static string GetLastPathPart(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        int index = path.LastIndexOf('/');
        return index >= 0 && index + 1 < path.Length ? path.Substring(index + 1) : path;
    }

    private class ExperimentCsvRow
    {
        public string rowType;
        public string sessionId;
        public string participantId;
        public string platform;
        public string timestampIso;
        public float elapsedSeconds;
        public string phase;
        public int trialIndex;
        public string imageName;
        public string objectType;
        public string objectName;
        public string objectPath;
        public string action;
        public string value;
        public int count;
        public float durationSeconds;

        public string ToCsvLine()
        {
            return string.Join(",",
                Csv(rowType),
                Csv(sessionId),
                Csv(participantId),
                Csv(platform),
                Csv(timestampIso),
                elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                Csv(phase),
                trialIndex.ToString(CultureInfo.InvariantCulture),
                Csv(imageName),
                Csv(objectType),
                Csv(objectName),
                Csv(objectPath),
                Csv(action),
                Csv(value),
                count.ToString(CultureInfo.InvariantCulture),
                durationSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static string Csv(string value)
        {
            if (value == null)
                value = string.Empty;

            bool mustQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
            value = value.Replace("\"", "\"\"");
            return mustQuote ? "\"" + value + "\"" : value;
        }
    }
}
