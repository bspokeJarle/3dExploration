using System;

namespace Domain
{
    public enum ScreenOverlayType
    {
        None = 0,
        Intro = 1,
        Outro = 2,
        Game = 3
    }

    public enum ScreenOverlayAnchor
    {
        Center = 0,
        Top = 1,
        Bottom = 2
    }

    /// <summary>
    /// ScreenOverlayState is a lightweight, global UI state for modal/cinematic overlays.
    /// - Objects are allowed to toggle ShowOverlay and set the text fields.
    /// - Rendering is handled elsewhere (renderer reads this state only).
    /// - Update() provides deterministic fade-in/out based on ShowOverlay.
    /// </summary>
    public sealed class ScreenOverlayState
    {
        // -----------------------------
        // Identity / usage
        // -----------------------------
        public ScreenOverlayType Type { get; set; } = ScreenOverlayType.None;

        // -----------------------------
        // Hide or show Debug overlays
        // -----------------------------
        public bool ShowDebugOverlay { get; set; } = false;

        /// <summary>
        /// Main on/off switch. When false, overlay fades out to Opacity=0.
        /// When true, overlay fades in to Opacity=1.
        /// </summary>
        public bool ShowOverlay { get; set; } = false;

        /// <summary>
        /// Optional: blocks gameplay input (scene/game can use this if desired).
        /// Renderer does not care.
        /// </summary>
        public bool IsModal { get; set; } = false;

        // -----------------------------
        // Content
        // -----------------------------
        public string Header { get; set; } = "";   // ingress/label (small)
        public string Title { get; set; } = "";    // main heading (big)
        public string Body { get; set; } = "";     // multi-line
        public string Footer { get; set; } = "";   // CTA (press any key)

        // -----------------------------
        // Presentation knobs (kept simple)
        // -----------------------------
        public ScreenOverlayAnchor Anchor { get; set; } = ScreenOverlayAnchor.Top;

        /// <summary>
        /// 0..1. How much to dim the whole screen behind the panel.
        /// Renderer can interpret as (alpha = DimStrength * Opacity).
        /// </summary>
        public float DimStrength { get; set; } = 0.55f;

        /// <summary>
        /// 0..1. Background strength for the panel itself (glass fill).
        /// Renderer can interpret as (alpha = PanelFillStrength * Opacity).
        /// </summary>
        public float PanelFillStrength { get; set; } = 0.70f;

        /// <summary>
        /// 0..1. Border strength for the panel.
        /// </summary>
        public float BorderStrength { get; set; } = 0.85f;

        /// <summary>
        /// Panel width relative to screen width (0..1).
        /// </summary>
        public float PanelWidthRatio { get; set; } = 0.70f;

        /// <summary>
        /// Panel height relative to screen height (0..1).
        /// </summary>
        public float PanelHeightRatio { get; set; } = 0.28f;

        /// <summary>
        /// Panel vertical offset as ratio of screen height (0..1).
        /// For Anchor.Top, this is the "top margin"; for Center/Bottom it's used as additional offset.
        /// </summary>
        public float PanelYOffsetRatio { get; set; } = 0.18f;

        /// <summary>
        /// Panel corner radius in DIP. Renderer can use this directly.
        /// </summary>
        public float CornerRadius { get; set; } = 12f;

        // -----------------------------
        // Fade (deterministic)
        // -----------------------------
        /// <summary>
        /// Current opacity 0..1 (owned by this state).
        /// </summary>
        public float Opacity { get; private set; } = 0f;

        /// <summary>
        /// Fade speed in opacity units per second.
        /// Example: 2.5 means ~0.4s to fade from 0->1.
        /// </summary>
        public float FadeInSpeed { get; set; } = 2.5f;

        /// <summary>
        /// Fade out speed in opacity units per second.
        /// </summary>
        public float FadeOutSpeed { get; set; } = 3.0f;

        /// <summary>
        /// Optional: if true, ShowOverlay will be turned off after AutoHideSeconds.
        /// Useful for short "mission start" cards.
        /// </summary>
        public bool AutoHide { get; set; } = false;

        /// <summary>
        /// If AutoHide is enabled, overlay will start fading out after this duration (seconds)
        /// counted from the moment ShowOverlay first became true.
        /// </summary>
        public float AutoHideSeconds { get; set; } = 0f;

        private float _shownTimeSeconds = 0f;
        private bool _wasShowingLastUpdate = false;

        // -----------------------------
        // Convenience
        // -----------------------------
        public bool ShouldRender => Opacity > 0.001f;

        /// <summary>
        /// Fully hides without fade (hard reset).
        /// </summary>
        public void HardHide()
        {
            ShowOverlay = false;
            Opacity = 0f;
            _shownTimeSeconds = 0f;
            _wasShowingLastUpdate = false;
        }

        /// <summary>
        /// Clears content and resets presentation knobs to defaults.
        /// </summary>
        public void ResetToDefaults()
        {
            Type = ScreenOverlayType.None;
            ShowOverlay = false;
            IsModal = false;

            Header = "";
            Title = "";
            Body = "";
            Footer = "";

            Anchor = ScreenOverlayAnchor.Top;
            DimStrength = 0.55f;
            PanelFillStrength = 0.70f;
            BorderStrength = 0.85f;

            PanelWidthRatio = 0.70f;
            PanelHeightRatio = 0.28f;
            PanelYOffsetRatio = 0.18f;
            CornerRadius = 12f;

            FadeInSpeed = 2.5f;
            FadeOutSpeed = 3.0f;

            AutoHide = false;
            AutoHideSeconds = 0f;

            HardHide();
        }

        /// <summary>
        /// Preferred update call once per frame (before render pass).
        /// dtSeconds should be stable (e.g., 1/60f).
        /// </summary>
        public void Update(float dtSeconds)
        {
            if (dtSeconds <= 0f) return;

            // Track how long we've been "requested visible"
            if (ShowOverlay)
            {
                if (!_wasShowingLastUpdate)
                {
                    // Rising edge: overlay just turned on
                    _shownTimeSeconds = 0f;
                }
                else
                {
                    _shownTimeSeconds += dtSeconds;
                }
            }

            _wasShowingLastUpdate = ShowOverlay;

            // Auto-hide behavior (optional)
            if (ShowOverlay && AutoHide && AutoHideSeconds > 0f && _shownTimeSeconds >= AutoHideSeconds)
            {
                ShowOverlay = false;
            }

            // Fade to target
            float target = ShowOverlay ? 1f : 0f;

            if (Opacity < target)
            {
                Opacity += FadeInSpeed * dtSeconds;
                if (Opacity > 1f) Opacity = 1f;
            }
            else if (Opacity > target)
            {
                Opacity -= FadeOutSpeed * dtSeconds;
                if (Opacity < 0f) Opacity = 0f;
            }

            // If fully invisible, Type can be reset automatically if you want.
            // Keeping it as-is is often useful for debugging/replay.
        }

        // -----------------------------
        // Quick presets (optional, but handy)
        // -----------------------------
        public void SetIntroPreset(string title, string footer = "PRESS ANY KEY")
        {
            Type = ScreenOverlayType.Intro;
            Anchor = ScreenOverlayAnchor.Top;
            IsModal = true;

            Header = "RETROMESH BOOT SEQUENCE";
            Title = title;
            Body = "";
            Footer = footer;

            DimStrength = 0.55f;
            PanelWidthRatio = 0.72f;
            PanelHeightRatio = 0.26f;
            PanelYOffsetRatio = 0.16f;

            AutoHide = false;
            AutoHideSeconds = 0f;
        }

        public void SetOutroPreset(string title, string body, string footer = "PRESS ANY KEY")
        {
            Type = ScreenOverlayType.Outro;
            Anchor = ScreenOverlayAnchor.Center;
            IsModal = true;

            Header = "TRANSMISSION ENDS";
            Title = title;
            Body = body ?? "";
            Footer = footer;

            DimStrength = 0.65f;
            PanelWidthRatio = 0.75f;
            PanelHeightRatio = 0.32f;
            PanelYOffsetRatio = 0.00f;

            AutoHide = false;
            AutoHideSeconds = 0f;
        }

        public void SetGameOverlayPreset(string header, string title, string body, string footer = "")
        {
            Type = ScreenOverlayType.Game;
            Anchor = ScreenOverlayAnchor.Top;
            IsModal = false;

            Header = header ?? "";
            Title = title ?? "";
            Body = body ?? "";
            Footer = footer ?? "";

            DimStrength = 0.25f; // lighter in-game
            PanelWidthRatio = 0.62f;
            PanelHeightRatio = 0.22f;
            PanelYOffsetRatio = 0.06f;

            AutoHide = false;
            AutoHideSeconds = 0f;
        }
    }
}