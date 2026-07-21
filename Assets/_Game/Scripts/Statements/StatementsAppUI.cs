using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// The iPad "Statements" app: a scrollable list of recorded witness statements
// (from StatementLibrary, always sorted ascending by date) plus a compact
// player bar at the bottom with play/pause and a scrub slider.
//
// The whole interface is generated at runtime from the library data, so when
// the real recordings replace the filler entries the UI updates by itself.
// Playback uses ONE AudioSource: selecting a new recording automatically
// overrides whatever was playing.
//
// Sits on the StatementsScreen object of the Tablet_manager prefab (created by
// Tools > Crime Scene > Build iPad Statements App).
public class StatementsAppUI : MonoBehaviour
{
    [Tooltip("Statement data. Auto-found on the tablet root if left empty.")]
    [SerializeField] private StatementLibrary library;

    // UI is authored in this design space, then scaled to the tablet screen.
    private const float DesignWidth = 800f;
    private const float DesignHeight = 1150f;

    // All colours come from TabletTheme so this app matches the other apps.
    private static readonly Color RowColor = TabletTheme.Surface;
    private static readonly Color RowPlayingColor = TabletTheme.AccentSoft;
    private static readonly Color AccentColor = TabletTheme.Accent;
    private static readonly Color SubtleTextColor = TabletTheme.TextSecondary;

    private AudioSource _source;
    private VideoPlayer _videoPlayer;
    private RenderTexture _videoTexture;
    private RectTransform _designRoot;
    private RectTransform _scrollArea;
    private RectTransform _videoPanel;
    private TextMeshProUGUI _nowPlayingText;
    private TextMeshProUGUI _timeText;
    private TextMeshProUGUI _playPauseLabel;
    private Slider _scrubSlider;

    private readonly List<(StatementLibrary.StatementEntry entry, Image background)> _rows = new();
    private StatementLibrary.StatementEntry _current;
    private bool _paused;
    private bool _wasPlaying;

    private void Awake()
    {
        if (library == null)
            library = GetComponentInParent<StatementLibrary>();

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f; // always audible, like a phone speaker

        // Video statements render into a texture shown on the in-app panel;
        // their sound routes through the same AudioSource as vocal statements.
        _videoTexture = new RenderTexture(640, 360, 0) { name = "StatementVideo" };
        _videoPlayer = GetComponent<VideoPlayer>();
        if (_videoPlayer == null)
            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _videoTexture;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
            _videoPlayer.loopPointReached -= OnVideoFinished;
        if (_videoTexture != null)
        {
            _videoTexture.Release();
            Destroy(_videoTexture);
        }
    }

    private void OnVideoFinished(VideoPlayer _)
    {
        _wasPlaying = false;
        if (_playPauseLabel != null)
            _playPauseLabel.text = "Play";
    }

    private void OnEnable()
    {
        BuildUI();
    }

    private void OnDisable()
    {
        StopPlayback();
    }

    private bool CurrentIsVideo => _current != null && _current.IsVideo;

    private float CurrentPlaybackTime =>
        CurrentIsVideo ? (float)_videoPlayer.time : _source.time;

    private bool CurrentlyPlaying =>
        CurrentIsVideo ? _videoPlayer.isPlaying : _source.isPlaying;

    private void Update()
    {
        if (_current == null)
            return;

        if (CurrentlyPlaying)
        {
            _scrubSlider.SetValueWithoutNotify(CurrentPlaybackTime);
            UpdateTimeLabel(CurrentPlaybackTime);
            _wasPlaying = true;
        }
        else if (_wasPlaying && !_paused && !CurrentIsVideo)
        {
            // Audio clip finished naturally (videos use loopPointReached).
            _wasPlaying = false;
            _playPauseLabel.text = "Play";
        }
    }

    // ------------------------------------------------------------- playback

    private void PlayEntry(StatementLibrary.StatementEntry entry)
    {
        if (entry == null || !entry.IsPlayable)
            return;

        // One shared player: starting any new statement overrides the old one,
        // whichever medium either of them uses.
        _source.Stop();
        _videoPlayer.Stop();
        _source.clip = null;

        if (entry.IsVideo)
        {
            _videoPlayer.clip = entry.video;
            if (entry.video.audioTrackCount > 0)
            {
                _videoPlayer.EnableAudioTrack(0, true);
                _videoPlayer.SetTargetAudioSource(0, _source);
            }
            _videoPlayer.time = 0.0;
            _videoPlayer.Play();
        }
        else
        {
            _source.clip = entry.clip;
            _source.time = 0f;
            _source.Play();
        }

        SetVideoPanelVisible(entry.IsVideo);

        _current = entry;
        _paused = false;
        _wasPlaying = true;

        _scrubSlider.maxValue = entry.DurationSeconds;
        _scrubSlider.SetValueWithoutNotify(0f);
        _nowPlayingText.text = $"{entry.personName}  ·  {entry.dateRecorded}";
        _playPauseLabel.text = "Pause";
        UpdateTimeLabel(0f);
        HighlightPlayingRow();
    }

    private void OnPlayPausePressed()
    {
        // Nothing selected yet: start the first (oldest) statement.
        if (_current == null)
        {
            List<StatementLibrary.StatementEntry> sorted = library != null ? library.GetSortedAscending() : null;
            if (sorted != null && sorted.Count > 0)
                PlayEntry(sorted[0]);
            return;
        }

        if (CurrentlyPlaying)
        {
            if (CurrentIsVideo) _videoPlayer.Pause();
            else _source.Pause();
            _paused = true;
            _playPauseLabel.text = "Play";
        }
        else if (_paused)
        {
            if (CurrentIsVideo) _videoPlayer.Play(); // resumes from pause
            else _source.UnPause();
            _paused = false;
            _wasPlaying = true;
            _playPauseLabel.text = "Pause";
        }
        else
        {
            // Finished earlier — restart from the top.
            PlayEntry(_current);
        }
    }

    // Skim: dragging the slider seeks inside the current recording.
    private void OnScrub(float seconds)
    {
        if (_current == null)
            return;

        seconds = Mathf.Clamp(seconds, 0f, Mathf.Max(0f, _current.DurationSeconds - 0.05f));
        if (CurrentIsVideo)
            _videoPlayer.time = seconds;
        else if (_source.clip != null)
            _source.time = seconds;
        UpdateTimeLabel(seconds);
    }

    private void StopPlayback()
    {
        _source.Stop();
        _videoPlayer.Stop();
        _current = null;
        _paused = false;
        _wasPlaying = false;
        SetVideoPanelVisible(false);
    }

    private void SetVideoPanelVisible(bool visible)
    {
        if (_videoPanel == null || _scrollArea == null)
            return;

        _videoPanel.gameObject.SetActive(visible);
        // Shrink the list while the video panel is on screen.
        _scrollArea.offsetMin = new Vector2(24f, visible ? 520f : 200f);
    }

    private void UpdateTimeLabel(float seconds)
    {
        if (_timeText == null || _current == null)
            return;
        _timeText.text = $"{Format(seconds)} / {Format(_current.DurationSeconds)}";
    }

    private static string Format(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60}:{total % 60:00}";
    }

    private void HighlightPlayingRow()
    {
        foreach ((StatementLibrary.StatementEntry entry, Image background) in _rows)
            background.color = entry == _current ? RowPlayingColor : RowColor;
    }

    // ------------------------------------------------------------ UI build

    private void BuildUI()
    {
        if (_designRoot != null)
            Destroy(_designRoot.gameObject);
        _rows.Clear();
        _current = null;

        // Scaled design-space container (the tablet canvas uses tiny world units).
        RectTransform screen = (RectTransform)transform;
        _designRoot = CreateRect("DesignRoot", screen);
        _designRoot.sizeDelta = new Vector2(DesignWidth, DesignHeight);
        Vector2 available = screen.rect.size;
        float scale = (available.x > 0.001f && available.y > 0.001f)
            ? Mathf.Min(available.x / DesignWidth, available.y / DesignHeight)
            : 0.004f;
        _designRoot.localScale = new Vector3(scale, scale, 1f);

        // Title.
        TextMeshProUGUI title = CreateText(_designRoot, "Title", "Recorded Statements", 46f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 80f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);

        // Back to the Notebook app this section lives in (same header pattern
        // as the Settings screen's "< Home" button).
        RectTransform back = CreateRect("BackButton", _designRoot);
        back.anchorMin = new Vector2(0f, 1f);
        back.anchorMax = new Vector2(0f, 1f);
        back.pivot = new Vector2(0f, 1f);
        back.sizeDelta = new Vector2(210f, 60f);
        back.anchoredPosition = new Vector2(24f, -24f);
        Image backBg = back.gameObject.AddComponent<Image>();
        backBg.color = TabletTheme.SurfaceRaised;
        Button backButton = back.gameObject.AddComponent<Button>();
        backButton.targetGraphic = backBg;
        backButton.onClick.AddListener(() =>
        {
            ScreenManager screens = GetComponentInParent<ScreenManager>();
            if (screens != null)
                screens.ShowNotebook();
        });
        TextMeshProUGUI backLabel = CreateText(back, "Label", "< Notebook", 26f, TextAlignmentOptions.Center);
        Stretch(backLabel.rectTransform);

        BuildPlayerBar();
        BuildVideoPanel();
        BuildList();
        PopulateRows();
        SetVideoPanelVisible(false);
    }

    // In-app video display, sitting between the list and the player bar. Only
    // visible while a video statement is playing.
    private void BuildVideoPanel()
    {
        _videoPanel = CreateRect("VideoPanel", _designRoot);
        _videoPanel.anchorMin = new Vector2(0f, 0f);
        _videoPanel.anchorMax = new Vector2(1f, 0f);
        _videoPanel.pivot = new Vector2(0.5f, 0f);
        _videoPanel.sizeDelta = new Vector2(-48f, 300f);
        _videoPanel.anchoredPosition = new Vector2(0f, 200f);

        Image frame = _videoPanel.gameObject.AddComponent<Image>();
        frame.color = Color.black;

        RectTransform display = CreateRect("VideoDisplay", _videoPanel);
        Stretch(display);
        display.offsetMin = new Vector2(6f, 6f);
        display.offsetMax = new Vector2(-6f, -6f);
        RawImage raw = display.gameObject.AddComponent<RawImage>();
        raw.texture = _videoTexture;
        raw.color = Color.white;

        // Keep 16:9 inside the panel.
        var fitter = display.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
    }

    private void BuildList()
    {
        // Scroll area between the title (top ~110) and the player bar (bottom 190).
        RectTransform scrollArea = CreateRect("ScrollArea", _designRoot);
        scrollArea.anchorMin = Vector2.zero;
        scrollArea.anchorMax = Vector2.one;
        scrollArea.offsetMin = new Vector2(24f, 200f);
        scrollArea.offsetMax = new Vector2(-24f, -110f);
        _scrollArea = scrollArea;

        ScrollRect scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = CreateRect("Viewport", scrollArea);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        viewport.gameObject.AddComponent<RectMask2D>();
        // An (invisible) graphic is required for the viewport to catch drags.
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, 0f);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        _listContent = content;
    }

    private RectTransform _listContent;

    private void PopulateRows()
    {
        if (library == null)
        {
            CreateText(_listContent, "NoLibrary", "No StatementLibrary found on the tablet.", 30f, TextAlignmentOptions.Center);
            Debug.LogWarning("[StatementsApp] No StatementLibrary found — list is empty.");
            return;
        }

        List<StatementLibrary.StatementEntry> sorted = library.GetSortedAscending();
        Debug.Log($"[StatementsApp] Library found with {sorted.Count} statement(s).");
        if (sorted.Count == 0)
        {
            CreateText(_listContent, "Empty",
                "No statements recorded yet.\n(Library is empty — re-run Tools > Crime Scene > Build iPad Statements App.)",
                30f, TextAlignmentOptions.Center);
            return;
        }

        foreach (StatementLibrary.StatementEntry entry in sorted)
        {
            RectTransform row = CreateRect($"Row_{entry.personName}", _listContent);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 118f;

            Image background = row.gameObject.AddComponent<Image>();
            background.color = RowColor;

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            StatementLibrary.StatementEntry captured = entry;
            button.onClick.AddListener(() => PlayEntry(captured));
            button.interactable = entry.IsPlayable;

            TextMeshProUGUI name = CreateText(row, "Name", entry.personName, 32f, TextAlignmentOptions.Left);
            name.fontStyle = FontStyles.Bold;
            RectTransform nameRect = name.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(24f, 0f);
            nameRect.offsetMax = new Vector2(-24f, -8f);

            string mediaTag = entry.IsVideo ? "VIDEO" : "AUDIO";
            string duration = entry.IsPlayable ? Format(entry.DurationSeconds) : "no media";
            TextMeshProUGUI details = CreateText(row, "Details",
                $"{entry.dateRecorded}   ·   {mediaTag}   ·   {duration}", 26f, TextAlignmentOptions.Left);
            details.color = SubtleTextColor;
            RectTransform detailsRect = details.rectTransform;
            detailsRect.anchorMin = new Vector2(0f, 0f);
            detailsRect.anchorMax = new Vector2(1f, 0.5f);
            detailsRect.offsetMin = new Vector2(24f, 8f);
            detailsRect.offsetMax = new Vector2(-24f, 0f);

            _rows.Add((entry, background));
        }
    }

    private void BuildPlayerBar()
    {
        RectTransform bar = CreateRect("PlayerBar", _designRoot);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.sizeDelta = new Vector2(-48f, 170f);
        bar.anchoredPosition = new Vector2(0f, 16f);

        Image barBg = bar.gameObject.AddComponent<Image>();
        barBg.color = TabletTheme.Surface;

        _nowPlayingText = CreateText(bar, "NowPlaying", "Select a statement to play it.", 26f, TextAlignmentOptions.Left);
        RectTransform nowRect = _nowPlayingText.rectTransform;
        nowRect.anchorMin = new Vector2(0f, 1f);
        nowRect.anchorMax = new Vector2(1f, 1f);
        nowRect.pivot = new Vector2(0.5f, 1f);
        nowRect.sizeDelta = new Vector2(-40f, 44f);
        nowRect.anchoredPosition = new Vector2(0f, -8f);

        // Play/Pause button (bottom-left of the bar).
        RectTransform buttonRect = CreateRect("PlayPauseButton", bar);
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(0f, 0f);
        buttonRect.pivot = new Vector2(0f, 0f);
        buttonRect.sizeDelta = new Vector2(150f, 64f);
        buttonRect.anchoredPosition = new Vector2(20f, 16f);
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = AccentColor;
        Button playPause = buttonRect.gameObject.AddComponent<Button>();
        playPause.targetGraphic = buttonImage;
        playPause.onClick.AddListener(OnPlayPausePressed);
        _playPauseLabel = CreateText(buttonRect, "Label", "Play", 30f, TextAlignmentOptions.Center);
        _playPauseLabel.fontStyle = FontStyles.Bold;
        Stretch(_playPauseLabel.rectTransform);

        // Time readout (bottom-right).
        _timeText = CreateText(bar, "Time", "0:00 / 0:00", 26f, TextAlignmentOptions.Right);
        RectTransform timeRect = _timeText.rectTransform;
        timeRect.anchorMin = new Vector2(1f, 0f);
        timeRect.anchorMax = new Vector2(1f, 0f);
        timeRect.pivot = new Vector2(1f, 0f);
        timeRect.sizeDelta = new Vector2(220f, 64f);
        timeRect.anchoredPosition = new Vector2(-20f, 16f);

        // Scrub slider (middle band of the bar).
        _scrubSlider = BuildSlider(bar);
        _scrubSlider.onValueChanged.AddListener(OnScrub);
    }

    private Slider BuildSlider(RectTransform parent)
    {
        RectTransform root = CreateRect("ScrubSlider", parent);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = new Vector2(-40f, 36f);
        root.anchoredPosition = new Vector2(0f, 92f);

        RectTransform bg = CreateRect("Background", root);
        bg.anchorMin = new Vector2(0f, 0.35f);
        bg.anchorMax = new Vector2(1f, 0.65f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = TabletTheme.SurfaceRaised;

        RectTransform fillArea = CreateRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = new Vector2(8f, 0f);
        fillArea.offsetMax = new Vector2(-8f, 0f);

        RectTransform fill = CreateRect("Fill", fillArea);
        fill.sizeDelta = new Vector2(8f, 0f);
        fill.gameObject.AddComponent<Image>().color = AccentColor;

        RectTransform handleArea = CreateRect("Handle Slide Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(12f, 0f);
        handleArea.offsetMax = new Vector2(-12f, 0f);

        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(28f, 0f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    // ------------------------------------------------------------- helpers

    private RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions align)
    {
        RectTransform rect = CreateRect(name, parent);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = TabletTheme.TextPrimary;
        tmp.alignment = align;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
