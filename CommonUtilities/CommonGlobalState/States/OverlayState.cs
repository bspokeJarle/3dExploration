using System;
using System.Windows.Input;

namespace Domain
{
    public enum ScreenOverlayType
    {
        None = 0,
        Intro = 1,
        Outro = 2,
        Game = 3,
        NameEntry = 4
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

        public bool ShowVideoOverlay { get; set; } = false;
        public string VideoClipPath { get; set; } = "";

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
        // Name entry state
        // -----------------------------
        public const int MaxCallsignLength = 16;
        public string NameEntryBuffer { get; set; } = "";
        public string NameEntryValidationMessage { get; set; } = "";
        public bool IsNameConfirmed { get; set; } = false;
        private float _cursorBlinkTimer = 0f;

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

        /// <summary>
        /// When true, text blocks are center-aligned within the panel.
        /// </summary>
        public bool CenterText { get; set; } = false;

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
            ShowVideoOverlay = false;
            VideoClipPath = "";
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

            ShowVideoOverlay = false;
            VideoClipPath = "";

            Anchor = ScreenOverlayAnchor.Top;
            DimStrength = 0.55f;
            PanelFillStrength = 0.70f;
            BorderStrength = 0.85f;

            PanelWidthRatio = 0.70f;
            PanelHeightRatio = 0.28f;
            PanelYOffsetRatio = 0.18f;
            CornerRadius = 12f;
            CenterText = false;

            FadeInSpeed = 2.5f;
            FadeOutSpeed = 3.0f;

            AutoHide = false;
            AutoHideSeconds = 0f;

            NameEntryBuffer = "";
            NameEntryValidationMessage = "";
            IsNameConfirmed = false;
            _cursorBlinkTimer = 0f;

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

            // Name entry: update Body with buffer + blinking cursor
            if (Type == ScreenOverlayType.NameEntry && ShowOverlay)
            {
                _cursorBlinkTimer += dtSeconds;
                bool showCursor = ((int)(_cursorBlinkTimer / 0.5f)) % 2 == 0;
                string cursor = showCursor ? "█" : " ";
                string display = NameEntryBuffer + cursor;
                string validation = string.IsNullOrEmpty(NameEntryValidationMessage)
                    ? "" : $"\n{NameEntryValidationMessage}";
                Body = $"CALLSIGN: {display}{validation}";
            }
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

        public void SetNameEntryPreset(string defaultName = "")
        {
            Type = ScreenOverlayType.NameEntry;
            Anchor = ScreenOverlayAnchor.Center;
            IsModal = true;

            Header = "RETROMESH // PILOT REGISTRY";
            Title = "IDENTIFY YOURSELF";
            NameEntryBuffer = defaultName;
            NameEntryValidationMessage = "";
            IsNameConfirmed = false;
            _cursorBlinkTimer = 0f;
            Body = "";
            Footer = "ENTER TO CONFIRM  //  ESC TO GO BACK";

            DimStrength = 0.65f;
            PanelWidthRatio = 0.68f;
            PanelHeightRatio = 0.26f;
            PanelYOffsetRatio = 0.00f;
            CenterText = true;

            AutoHide = false;
            AutoHideSeconds = 0f;
            ShowOverlay = true;
        }

        /// <summary>
        /// Processes a key press during name entry. Returns true if the key was consumed.
        /// </summary>
        public bool ProcessNameEntryKey(Key key)
        {
            if (Type != ScreenOverlayType.NameEntry || !ShowOverlay) return false;

            // Letters A-Z
            if (key >= Key.A && key <= Key.Z)
            {
                if (NameEntryBuffer.Length < MaxCallsignLength)
                    NameEntryBuffer += (char)('A' + (key - Key.A));
                return true;
            }

            // Digits 0-9 (top row)
            if (key >= Key.D0 && key <= Key.D9)
            {
                if (NameEntryBuffer.Length < MaxCallsignLength)
                    NameEntryBuffer += (char)('0' + (key - Key.D0));
                return true;
            }

            // NumPad digits
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                if (NameEntryBuffer.Length < MaxCallsignLength)
                    NameEntryBuffer += (char)('0' + (key - Key.NumPad0));
                return true;
            }

            // Backspace
            if (key == Key.Back && NameEntryBuffer.Length > 0)
            {
                NameEntryBuffer = NameEntryBuffer[..^1];
                NameEntryValidationMessage = "";
                return true;
            }

            // Space
            if (key == Key.Space && NameEntryBuffer.Length < MaxCallsignLength)
            {
                NameEntryBuffer += ' ';
                return true;
            }

            // Minus / underscore
            if ((key == Key.OemMinus || key == Key.Subtract) && NameEntryBuffer.Length < MaxCallsignLength)
            {
                NameEntryBuffer += '-';
                return true;
            }

            return false;
        }
    }
}