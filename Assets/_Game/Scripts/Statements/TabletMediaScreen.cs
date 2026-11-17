using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// The "Ring Footage" and "Audio Recordings" tablet apps (Figma frames 1:157 and
// 1:174): a grid of tiles generated from StatementLibrary, three per row.
//
//   Video: every entry with a video clip, shown as its thumbnail (the entry's
//          thumbnail texture, or the clip's first frame). Tapping one plays it
//          inline in the "Footage Player" under the grid; tapping the playing
//          tile or the player pauses/resumes.
//   Audio: every audio-only entry, shown as the gold waveform tile. Tapping
//          plays/pauses it; the playing tile is tinted darker.
//
// The look comes from a styled, inactive tile template authored by
// TabletMediaScreensBuilder in Figma pixel space; this component clones it per
// entry and steps it by tilePitch. Inline playback is an interim choice: the
// Figma board has no player design yet.
public class TabletMediaScreen : MonoBehaviour
{
    public enum MediaKind { Video, Audio }

    [SerializeField] private MediaKind kind;

    [Tooltip("Recording data. Auto-found on the tablet root if left empty.")]
    [SerializeField] private StatementLibrary library;

    [Header("Grid")]
    [Tooltip("Inactive, fully styled first tile; its position is the grid origin.")]
    [SerializeField] private RectTransform tileTemplate;
    [SerializeField] private Vector2 tilePitch = new Vector2(231.25f, 220.67f);
    [SerializeField] private int columns = 3;

    [Tooltip("Tile names follow the Figma layers: {0} is the 1-based index plus nameOffset.")]
    [SerializeField] private string tileNameFormat = "Footage {0}";
    [SerializeField] private int nameOffset;

    [Header("Video")]
    [SerializeField] private RectTransform player;
    [SerializeField] private RawImage playerImage;
    [SerializeField] private float playerGap = 50f;
    [SerializeField] private float playerBottomLimit = 1000f;

    [Header("Audio")]
    [SerializeField] private Color playingTint = new Color(0.75f, 0.75f, 0.75f);

    private readonly List<(StatementLibrary.StatementEntry entry, RectTransform tile)> _tiles = new();
    private readonly List<RenderTexture> _thumbnailTextures = new();

    private AudioSource _source;
    private VideoPlayer _videoPlayer;
    private RenderTexture _videoTexture;
    private StatementLibrary.StatementEntry _current;
    private bool _paused;

    private void Awake()
    {
        if (library == null)
            library = GetComponentInParent<StatementLibrary>();

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f; // always audible, like a phone speaker

        if (kind == MediaKind.Video)
        {
            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.loopPointReached += _ => StopPlayback();
        }

        if (player != null && player.TryGetComponent(out Button playerButton))
            playerButton.onClick.AddListener(TogglePause);
    }

    private void OnEnable()
    {
        BuildTiles();
    }

    private void OnDisable()
    {
        StopPlayback();
        ClearTiles();
    }

    private void OnDestroy()
    {
        ReleaseTexture(ref _videoTexture);
    }

    private void Update()
    {
        // Audio clips end on their own; videos use loopPointReached.
        if (kind == MediaKind.Audio && _current != null && !_paused && !_source.isPlaying)
            StopPlayback();
    }

    // ------------------------------------------------------------------ grid

    private void BuildTiles()
    {
        ClearTiles();
        if (tileTemplate == null || library == null)
            return;

        tileTemplate.gameObject.SetActive(false);

        int index = 0;
        foreach (StatementLibrary.StatementEntry entry in library.GetSortedAscending())
        {
            bool matches = kind == MediaKind.Video ? entry.IsVideo : (!entry.IsVideo && entry.clip != null);
            if (!matches)
                continue;

            RectTransform tile = Instantiate(tileTemplate, tileTemplate.parent);
            tile.name = string.Format(tileNameFormat, index + 1 + nameOffset);
            tile.anchoredPosition = tileTemplate.anchoredPosition +
                new Vector2(tilePitch.x * (index % columns), -tilePitch.y * (index / columns));
            tile.gameObject.SetActive(true);

            if (kind == MediaKind.Video)
                ShowThumbnail(entry, tile.GetComponentInChildren<RawImage>(true));

            StatementLibrary.StatementEntry captured = entry;
            tile.GetComponent<Button>().onClick.AddListener(() => OnTileClicked(captured));

            _tiles.Add((entry, tile));
            index++;
        }

        PlacePlayer(index);
        SetPlayerVisible(false);
    }

    private void ClearTiles()
    {
        foreach ((_, RectTransform tile) in _tiles)
            if (tile != null)
                Destroy(tile.gameObject);
        _tiles.Clear();

        for (int i = 0; i < _thumbnailTextures.Count; i++)
        {
            RenderTexture rt = _thumbnailTextures[i];
            ReleaseTexture(ref rt);
        }
        _thumbnailTextures.Clear();
    }

    // The player sits one gap under the last row, as wide as the grid.
    private void PlacePlayer(int tileCount)
    {
        if (player == null || tileTemplate == null)
            return;

        int rows = Mathf.Max(1, Mathf.CeilToInt(tileCount / (float)columns));
        float top = -tileTemplate.anchoredPosition.y + rows * tilePitch.y - tilePitch.y + tileTemplate.sizeDelta.y + playerGap;
        float width = tilePitch.x * (columns - 1) + tileTemplate.sizeDelta.x;
        float height = Mathf.Min(width * 9f / 16f, playerBottomLimit - top);

        player.anchoredPosition = new Vector2(tileTemplate.anchoredPosition.x, -top);
        player.sizeDelta = new Vector2(width, Mathf.Max(0f, height));
    }

    // Thumbnail: the entry's own texture, otherwise the clip's first frame
    // rendered once by a muted, paused VideoPlayer on the tile.
    private void ShowThumbnail(StatementLibrary.StatementEntry entry, RawImage target)
    {
        if (target == null)
            return;

        if (entry.thumbnail != null)
        {
            target.texture = entry.thumbnail;
            target.uvRect = CoverUv(entry.thumbnail.width, entry.thumbnail.height, target.rectTransform.rect.size);
            return;
        }

        VideoClip clip = entry.video;
        int width = 320;
        int height = Mathf.Max(1, Mathf.RoundToInt(width * clip.height / Mathf.Max(1f, clip.width)));
        var rt = new RenderTexture(width, height, 0) { name = "FootageThumbnail" };
        _thumbnailTextures.Add(rt);

        target.texture = rt;
        target.uvRect = CoverUv(width, height, target.rectTransform.rect.size);

        VideoPlayer thumb = target.gameObject.AddComponent<VideoPlayer>();
        thumb.playOnAwake = false;
        thumb.renderMode = VideoRenderMode.RenderTexture;
        thumb.targetTexture = rt;
        thumb.audioOutputMode = VideoAudioOutputMode.None;
        thumb.clip = clip;
        thumb.sendFrameReadyEvents = true;
        thumb.frameReady += (p, _) =>
        {
            p.sendFrameReadyEvents = false;
            p.Pause();
        };
        thumb.Play();
    }

    // UV rect that crops a texture to fill a box (like CSS object-fit: cover).
    private static Rect CoverUv(float texWidth, float texHeight, Vector2 box)
    {
        if (texHeight <= 0f || box.y <= 0f)
            return new Rect(0f, 0f, 1f, 1f);

        float texAspect = texWidth / texHeight;
        float boxAspect = box.x / box.y;
        if (texAspect > boxAspect)
        {
            float w = boxAspect / texAspect;
            return new Rect((1f - w) * 0.5f, 0f, w, 1f);
        }
        float h = texAspect / boxAspect;
        return new Rect(0f, (1f - h) * 0.5f, 1f, h);
    }

    // -------------------------------------------------------------- playback

    private void OnTileClicked(StatementLibrary.StatementEntry entry)
    {
        if (entry == _current)
            TogglePause();
        else
            Play(entry);
    }

    private void Play(StatementLibrary.StatementEntry entry)
    {
        StopPlayback();
        _current = entry;
        _paused = false;

        if (kind == MediaKind.Video)
        {
            ReleaseTexture(ref _videoTexture);
            VideoClip clip = entry.video;
            _videoTexture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = "FootagePlayer" };
            _videoPlayer.targetTexture = _videoTexture;
            _videoPlayer.clip = clip;
            if (clip.audioTrackCount > 0)
            {
                _videoPlayer.EnableAudioTrack(0, true);
                _videoPlayer.SetTargetAudioSource(0, _source);
            }
            _videoPlayer.Play();

            if (playerImage != null)
            {
                playerImage.texture = _videoTexture;
                playerImage.uvRect = CoverUv(clip.width, clip.height, playerImage.rectTransform.rect.size);
            }
            SetPlayerVisible(true);
        }
        else
        {
            _source.clip = entry.clip;
            _source.Play();
        }

        RefreshTiles();
    }

    private void TogglePause()
    {
        if (_current == null)
            return;

        _paused = !_paused;
        if (kind == MediaKind.Video)
        {
            if (_paused) _videoPlayer.Pause();
            else _videoPlayer.Play();
        }
        else
        {
            if (_paused) _source.Pause();
            else _source.UnPause();
        }
        RefreshTiles();
    }

    private void StopPlayback()
    {
        _source.Stop();
        if (_videoPlayer != null)
            _videoPlayer.Stop();

        _current = null;
        _paused = false;
        SetPlayerVisible(false);
        RefreshTiles();
    }

    private void SetPlayerVisible(bool visible)
    {
        if (player != null && player.gameObject.activeSelf != visible)
            player.gameObject.SetActive(visible);
    }

    // Audio tiles show which recording is loaded (tinted while it is current).
    private void RefreshTiles()
    {
        if (kind != MediaKind.Audio)
            return;

        foreach ((StatementLibrary.StatementEntry entry, RectTransform tile) in _tiles)
            if (tile != null && tile.TryGetComponent(out Image image))
                image.color = entry == _current ? playingTint : Color.white;
    }

    private static void ReleaseTexture(ref RenderTexture rt)
    {
        if (rt == null)
            return;
        rt.Release();
        Destroy(rt);
        rt = null;
    }
}
