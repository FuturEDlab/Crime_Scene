using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

// Holds every recorded statement (homeowner, neighbours, responders...) for the
// investigation. Lives on the Tablet_manager prefab root next to the other
// managers; the Statements app (StatementsAppUI) reads it to build its list.
//
// Currently populated with FILLER entries and generated sweep-tone audio for
// testing. When the real recordings arrive, replace the clips / names / dates
// in this component on the Tablet_manager prefab — nothing else needs to change.
public class StatementLibrary : MonoBehaviour
{
    [Serializable]
    public class StatementEntry
    {
        [Tooltip("Who gave this statement, e.g. \"Margaret Holloway — Homeowner\".")]
        public string personName = "Unknown";

        [Tooltip("When it was recorded. Any parseable format works; " +
                 "recommended: yyyy-MM-dd HH:mm (e.g. 2026-03-14 09:30).")]
        public string dateRecorded = "";

        [Tooltip("Audio-only (vocal) statement. Leave empty for video statements.")]
        public AudioClip clip;

        [Tooltip("Video statement (e.g. security footage). Takes priority over " +
                 "the audio clip if both are set.")]
        public VideoClip video;

        public bool IsVideo => video != null;
        public bool IsPlayable => video != null || clip != null;

        public float DurationSeconds =>
            video != null ? (float)video.length : (clip != null ? clip.length : 0f);

        // Unparseable dates sort last so a typo is visible instead of hidden.
        public DateTime ParsedDate =>
            DateTime.TryParse(dateRecorded, out DateTime parsed) ? parsed : DateTime.MaxValue;
    }

    [SerializeField] private List<StatementEntry> statements = new();

    public IReadOnlyList<StatementEntry> Statements => statements;

    // The app always shows statements in ascending date order, regardless of
    // how they are arranged in the Inspector.
    public List<StatementEntry> GetSortedAscending() =>
        statements.Where(s => s != null)
                  .OrderBy(s => s.ParsedDate)
                  .ToList();

    // Used by the editor builder to install the filler test data.
    public void SetStatements(List<StatementEntry> entries) => statements = entries;
}
