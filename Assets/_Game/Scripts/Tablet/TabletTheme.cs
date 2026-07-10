using UnityEngine;

// Single source of truth for the tablet's UI look. Every generated app UI
// (Settings, Statements, home icons...) pulls its colours from here so all
// apps read as parts of the same operating system.
//
// Deliberately subtle: a dark neutral scheme with ONE accent colour for
// active/selected states, plus amber reserved for evidence/warnings.
public static class TabletTheme
{
    // Surfaces, dark to light.
    public static readonly Color Background    = new Color(0.117f, 0.117f, 0.125f); // app screens
    public static readonly Color Surface       = new Color(0.173f, 0.173f, 0.184f); // rows, bars, panels
    public static readonly Color SurfaceRaised = new Color(0.282f, 0.282f, 0.295f); // buttons, controls

    // The one accent. Used for: selected buttons, slider fills, active rows.
    public static readonly Color Accent     = new Color(0.04f, 0.52f, 1f);
    public static readonly Color AccentSoft = new Color(0.10f, 0.32f, 0.56f); // playing/selected rows

    // Text.
    public static readonly Color TextPrimary   = new Color(0.95f, 0.95f, 0.96f);
    public static readonly Color TextSecondary = new Color(0.62f, 0.62f, 0.64f);

    // Semantic: evidence markers / warnings only.
    public static readonly Color Amber = new Color(1f, 0.72f, 0.25f);
}
