using System;
using System.Collections.Generic;
using UnityEngine;

// Backend grading for the crime scene.
//
// Every KeyEvidenceItem in the scene is registered here. When the player
// photographs one with the iPad camera, MarkFound() records it. The number of
// distinct key items found is compared against a required threshold to produce
// a simple pass/fail grade that the rest of the game can read.
//
// State is intentionally NOT persisted to disk: a fresh play session always
// starts with zero evidence found (matching the "no photos from previous
// sessions" requirement).
public class EvidenceGradingManager : MonoBehaviour
{
    public static EvidenceGradingManager Instance { get; private set; }

    [Header("Grading Rules")]
    [Tooltip("How many distinct key items must be photographed to pass. " +
             "Set to 0 to require ALL registered key items.")]
    [SerializeField] private int requiredToPass = 0;

    [Tooltip("Log every grade change to the console (useful while testing).")]
    [SerializeField] private bool verboseLogging = true;

    // Raised whenever an item is found (or the grade is reset).
    public event Action OnGradeChanged;

    private readonly Dictionary<string, KeyEvidenceItem> _registered = new();
    private readonly HashSet<string> _found = new();

    public IEnumerable<KeyEvidenceItem> RegisteredItems => _registered.Values;
    public int FoundCount => _found.Count;
    public int TotalCount => _registered.Count;

    public int RequiredToPass =>
        requiredToPass <= 0 ? Mathf.Max(1, _registered.Count) : requiredToPass;

    public bool HasPassed => FoundCount >= RequiredToPass;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Order-independent registration: pick up any key items already in the
        // scene that may have enabled before this manager woke up.
        KeyEvidenceItem[] items =
#if UNITY_2023_1_OR_NEWER
            FindObjectsByType<KeyEvidenceItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            FindObjectsOfType<KeyEvidenceItem>(true);
#endif
        foreach (KeyEvidenceItem item in items)
            Register(item);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Adds a key item to the master list. Safe to call multiple times.
    public void Register(KeyEvidenceItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.evidenceId))
            return;

        _registered[item.evidenceId] = item;
    }

    // Records a piece of evidence as found. Returns true only the FIRST time a
    // given id is marked, so callers can react to genuinely new discoveries.
    public bool MarkFound(string evidenceId)
    {
        if (string.IsNullOrEmpty(evidenceId))
            return false;

        if (!_found.Add(evidenceId))
            return false; // already found

        if (verboseLogging)
        {
            string label = _registered.TryGetValue(evidenceId, out KeyEvidenceItem item)
                ? item.displayName
                : evidenceId;
            Debug.Log($"[Grading] Evidence found: {label} ({evidenceId}). " +
                      $"Progress {FoundCount}/{RequiredToPass} (pass={HasPassed}).");
        }

        OnGradeChanged?.Invoke();
        return true;
    }

    public bool IsFound(string evidenceId) => _found.Contains(evidenceId);

    // Clears all found evidence (e.g. when restarting an investigation).
    public void ResetGrade()
    {
        _found.Clear();
        if (verboseLogging)
            Debug.Log("[Grading] Grade reset.");
        OnGradeChanged?.Invoke();
    }
}
