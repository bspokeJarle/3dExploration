using CommonUtilities.CommonGlobalState;
using Domain;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace _3dTesting.MainWindowClasses
{
    /// <summary>
    /// HUD Overlay V2:
    /// - Uses a PNG frame as the base layer.
    /// - Places dynamic values using Canvas slots (pixel-precise).
    /// - Scales uniformly based on a fixed design resolution (2256 width).
    /// - Visible only when ScreenOverlayState.Type == ScreenOverlayType.Game AND ShowOverlay == false.
    /// - Bars only (no labels, no % text) because labels exist in the PNG.
    /// </summary>
    public sealed class HudOverlayHandlerV2
    {
        // ----------------------------
        // DESIGN BASELINE
        // ----------------------------
        private const double DesignWidth = 2256.0;
        private const double DesignHeight = 1504.0;

        // You said 0.40 looked correct in-game.
        private const double DesignHudHeightRatio = 0.25;

        private static readonly double DesignHudHeight = DesignHeight * DesignHudHeightRatio;

        // Asset pack URI
        public string FrameAssetUri { get; set; } = "GameGraphics\\HudOverlay.png";

        // ----------------------------
        // WPF ROOTS
        // ----------------------------
        private readonly Grid _root;
        private readonly Grid _hudRoot;
        private readonly Image _frameImage;
        private readonly Canvas _canvas;

        // ----------------------------
        // HUD ELEMENTS (SLOTS)
        // ----------------------------
        private readonly Image _minimap;
        private readonly Image _activePowerupIcon;
        private readonly Image _powerupLazerIcon;
        private readonly Image _powerupDecoyIcon;

        // Center FPS line (numbers only; the "FPS | TRI | P" labels are in the PNG)
        private readonly TextBlock _fpsCenter;

        // Bars only (labels exist in PNG)
        private readonly Rectangle _altBarFill;
        private readonly Rectangle _thrBarFill;
        private readonly Rectangle _bioBarFill;

        // ----------------------------
        // SLOT LAYOUT (DESIGN COORDS)
        // Tune these numbers to match your PNG precisely.
        // ----------------------------

        // Minimap window inside the frame
        // NOTE: 72x72 is what you want long-term, but your current PNG window is larger.
        // You can keep this large window for now, and later switch to 72x72 when the PNG window is updated.
        //
        // If you want it sharper WITHOUT changing GameHelpers right now:
        // - make the minimap VIEW a bit smaller than the frame-window (less stretch)
        // - and let it sit centered inside the window
        //
        // Current values match what you showed (map fits). We keep it stable.
        private const double MiniMapX = 160;
        private const double MiniMapY = 100;
        private const double MiniMapW = 500;
        private const double MiniMapH = 210;

        // FPS line — adjusted from your earlier values to sit inside the top middle label slot
        private const double FpsX = 870;
        private const double FpsY = 28;
        private const double ActivePowerupIconX = 1310;
        private const double ActivePowerupIconY = 18;
        private const double ActivePowerupIconSize = 48;

        private const double PowerupLazerX = 850;
        private const double PowerupDecoyX = 950;
        private const double PowerupRowY = 75;
        private const double PowerupIconSize = 48;

        // Right panel bar fills (positions adjusted down to align with “track” lines in PNG)
        private const double RightPanelX = 1665;

        private const double RightAltY = 127;
        private const double RightThrY = 208;
        private const double RightBioY = 287;

        private const double RightBarX = RightPanelX + 40;

        // Bar dimensions (must match the track width inside PNG)
        private const double BarW = 400;   // slightly wider than before; adjust if your PNG track is shorter/longer
        private const double BarH = 16;

        // Visual style for bar fill
        private static readonly Brush BarFillBrush = new SolidColorBrush(Color.FromArgb(220, 0, 255, 255));

        public HudOverlayHandlerV2(Grid root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));

            _hudRoot = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(_hudRoot, int.MaxValue - 250);

            _frameImage = new Image
            {
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 1.0
            };

            _canvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false
            };

            // ----- Minimap -----
            _minimap = new Image
            {
                Width = MiniMapW,
                Height = MiniMapH,
                Opacity = 0.90,
                Stretch = Stretch.UniformToFill // keeps it visually tighter / less “smeared”
            };
            Canvas.SetLeft(_minimap, MiniMapX);
            Canvas.SetTop(_minimap, MiniMapY);

            // ----- Center FPS / TRI / P -----
            _fpsCenter = new TextBlock
            {
                Text = "",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 22,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            };
            Canvas.SetLeft(_fpsCenter, FpsX);
            Canvas.SetTop(_fpsCenter, FpsY);

            _activePowerupIcon = CreatePowerupIcon(ActivePowerupIconX, ActivePowerupIconY, ActivePowerupIconSize);
            _powerupLazerIcon = CreatePowerupIcon(PowerupLazerX, PowerupRowY, PowerupIconSize);
            _powerupDecoyIcon = CreatePowerupIcon(PowerupDecoyX, PowerupRowY, PowerupIconSize);

            _powerupLazerIcon.Source = TryLoadBitmapImage("GameGraphics\\laser_icon_48.png");
            _powerupDecoyIcon.Source = TryLoadBitmapImage("GameGraphics\\decoy_icon_48.png");

            // ----- Bars only -----
            _altBarFill = CreateBarFill(RightBarX, RightAltY);
            _thrBarFill = CreateBarFill(RightBarX, RightThrY);
            _bioBarFill = CreateBarFill(RightBarX, RightBioY);

            // Compose
            _hudRoot.Children.Add(_frameImage);
            _hudRoot.Children.Add(_canvas);

            _canvas.Children.Add(_minimap);
            _canvas.Children.Add(_fpsCenter);
            _canvas.Children.Add(_activePowerupIcon);
            _canvas.Children.Add(_powerupLazerIcon);
            _canvas.Children.Add(_powerupDecoyIcon);

            _canvas.Children.Add(_altBarFill);
            _canvas.Children.Add(_thrBarFill);
            _canvas.Children.Add(_bioBarFill);

            _root.Children.Add(_hudRoot);

            TryLoadFrameImage();
        }

        /// <summary>
        /// For existing helpers: lets MainWindow keep using GameHelpers.UpdateMapOverlay(...)
        /// </summary>
        public Image GetMinimapImage() => _minimap;

        /// <summary>
        /// Update HUD values & scaling.
        /// - overlayState determines visibility (Game only, and hidden while other overlay is visible).
        /// - gameplay provides infection level.
        /// - altitude/thrust are passed in since they may live on Ship/Controls.
        /// </summary>
        public void Update(
            ScreenOverlayState overlayState,
            GamePlayState gameplay,
            double screenWidth,
            double screenHeight,
            int fps,
            int triangles)
        {
            if (overlayState == null || gameplay == null)
            {
                _hudRoot.Visibility = Visibility.Collapsed;
                return;
            }

            // Hide HUD when cinematic/modal overlay is visible
            if (GameState.ScreenOverlayState.Type != ScreenOverlayType.Game)
            {
                _hudRoot.Visibility = Visibility.Collapsed;
                return;
            }

            if (screenWidth <= 0) screenWidth = DesignWidth;
            if (screenHeight <= 0) screenHeight = DesignHeight;

            _hudRoot.Visibility = Visibility.Visible;

            // Uniform scale constrained by both width and desired HUD height
            double targetHudHeight = screenHeight * DesignHudHeightRatio;

            double scaleX = screenWidth / DesignWidth;
            double scaleY = targetHudHeight / DesignHudHeight;

            double scale = Math.Min(scaleX, scaleY);

            _hudRoot.LayoutTransform = new ScaleTransform(scale, scale);

            // Logical size (design-space)
            _hudRoot.Width = DesignWidth;
            _hudRoot.Height = DesignHudHeight;

            _frameImage.Width = DesignWidth;
            _frameImage.Height = DesignHudHeight;

            _canvas.Width = DesignWidth;
            _canvas.Height = DesignHudHeight;

            // Center line – numbers only (labels are in PNG)
            var activePowerup = string.IsNullOrWhiteSpace(gameplay.ActivePowerup) ? "LAZER" : gameplay.ActivePowerup.ToUpperInvariant();

            _fpsCenter.Text = $"{fps}                       {triangles}";

            _activePowerupIcon.Source = activePowerup == "DECOY"
                ? _powerupDecoyIcon.Source
                : _powerupLazerIcon.Source;

            _powerupLazerIcon.Opacity = activePowerup == "LAZER" ? 1.0 : 0.45;
            _powerupDecoyIcon.Opacity = activePowerup == "DECOY" ? 1.0 : 0.45;
            _activePowerupIcon.Opacity = 1.0;

            SetBarFill(_altBarFill, gameplay.Alt);

            SetBarFill(_thrBarFill, gameplay.Thrust/10);
           
            //TODO: Temporary fix
            SetBarFill(_bioBarFill, gameplay.InfectionLevel/10);
        }

        public void ReloadFrame() => TryLoadFrameImage();

        private static Image CreatePowerupIcon(double x, double y, double size)
        {
            var image = new Image
            {
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                Opacity = 1.0
            };

            Canvas.SetLeft(image, x);
            Canvas.SetTop(image, y);

            return image;
        }

        private static BitmapImage? TryLoadBitmapImage(string assetPath)
        {
            try
            {
                var uri = new Uri(assetPath, UriKind.RelativeOrAbsolute);
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        private void TryLoadFrameImage()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FrameAssetUri))
                {
                    _frameImage.Source = null;
                    return;
                }

                var uri = new Uri(FrameAssetUri, UriKind.RelativeOrAbsolute);

                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = uri;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();

                _frameImage.Source = img;
            }
            catch(Exception ex)
            {
                Logger.Log($"Exception loading map. {ex.Message} ");
                _frameImage.Source = null;
            }
        }

        private static Rectangle CreateBarFill(double x, double y)
        {
            var fill = new Rectangle
            {
                Width = 0,
                Height = BarH,
                Fill = BarFillBrush,
                Opacity = 0.85
            };

            Canvas.SetLeft(fill, x);
            Canvas.SetTop(fill, y);

            return fill;
        }

        private static void SetBarFill(Rectangle rect, float value01)
        {
            rect.Width = BarW * Clamp01(value01);
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}