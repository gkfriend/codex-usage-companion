using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using CodexUsageCompanion.Configuration;
using CodexUsageCompanion.Localization;
using CodexUsageCompanion.RateLimits;
using CodexUsageCompanion.Windows;

namespace CodexUsageCompanion.Ui;

public sealed class UsageOverlayWindow : Window
{
    private const double CellWidth = 52;
    private readonly UsageCardControls _primaryCard;
    private readonly UsageCardControls _secondaryCard;
    private readonly UiText _text;
    private readonly OverlayPosition _position;
    private readonly int _marginPixels;
    private nint _ownerHandle;
    private nint _windowHandle;
    private OverlayPlacementRequest? _lastPlacement;

    public UsageOverlayWindow(CompanionSettings? settings = null, UiText? text = null)
    {
        settings ??= new CompanionSettings();
        _text = text ?? UiText.For(UiLanguageResolver.Resolve(settings.Language, System.Globalization.CultureInfo.CurrentUICulture));
        _position = OverlayPlacement.ParsePosition(settings.Position);
        _marginPixels = settings.Margin;
        Width = 320;
        Height = settings.ShowFiveHourLimit ? 178 : 96;
        Opacity = settings.Opacity;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Focusable = false;
        Topmost = false;

        var root = new Border
        {
            Background = Brush("#F22B2D2B"),
            BorderBrush = Brush("#5B5E5A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10),
            Effect = new DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 4,
                Opacity = 0.34,
                Color = Colors.Black
            }
        };
        var stack = new StackPanel();
        _primaryCard = CreateCard(_text.FiveHourTitle);
        _secondaryCard = CreateCard(_text.WeeklyTitle);
        var spacer = new Border { Height = 8, Background = Brushes.Transparent };
        _primaryCard.Container.Visibility = settings.ShowFiveHourLimit ? Visibility.Visible : Visibility.Collapsed;
        spacer.Visibility = settings.ShowFiveHourLimit ? Visibility.Visible : Visibility.Collapsed;
        stack.Children.Add(_primaryCard.Container);
        stack.Children.Add(spacer);
        stack.Children.Add(_secondaryCard.Container);
        root.Child = stack;
        Content = root;
        SourceInitialized += HandleSourceInitialized;
        UpdateUsage(null);
    }

    public void UpdateUsage(RateLimitState? state)
    {
        UpdateCard(_primaryCard, state?.FiveHour, false, state is not null);
        UpdateCard(_secondaryCard, state?.Weekly, true, state is not null);
    }

    public void AttachAndPosition(CodexWindowInfo owner)
    {
        if (owner.IsMinimized || owner.IsCloaked)
        {
            Hide();
            return;
        }

        if (_ownerHandle != owner.Handle)
        {
            _ownerHandle = owner.Handle;
            new WindowInteropHelper(this).Owner = owner.Handle;
        }

        if (!IsVisible)
        {
            Show();
        }

        var dpi = NativeMethods.GetDpiForWindow(owner.Handle);
        var scale = dpi > 0 ? dpi / 96d : 1d;
        var width = (int)Math.Round(Width * scale);
        var height = (int)Math.Round(Height * scale);
        var margin = (int)Math.Round(_marginPixels * scale);
        var point = OverlayPlacement.Calculate(owner.ClientBounds, width, height, margin, _position);
        var placement = new OverlayPlacementRequest(owner.Handle, point.X, point.Y, width, height);
        if (!OverlayPlacement.ShouldApply(_lastPlacement, placement))
        {
            return;
        }

        if (NativeMethods.SetWindowPos(
            _windowHandle,
            0,
            point.X,
            point.Y,
            width,
            height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoZOrder))
        {
            _lastPlacement = placement;
        }
    }

    private static UsageCardControls CreateCard(string title)
    {
        var container = new Border
        {
            Height = 74,
            Background = Brush("#363836"),
            BorderBrush = Brush("#4B4E4A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 7, 10, 7)
        };
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = title,
            Foreground = Brush("#F4F5F3"),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        var remainingText = new TextBlock
        {
            Foreground = Brush("#9CA09C"),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(remainingText, 1);
        var resetText = new TextBlock
        {
            Foreground = Brush("#B7BAB6"),
            FontSize = 11.5,
            Margin = new Thickness(0, 1, 0, 3)
        };
        Grid.SetRow(resetText, 1);
        Grid.SetColumnSpan(resetText, 2);

        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var fills = new Border[5];
        for (var index = 0; index < fills.Length; index++)
        {
            var fill = new Border
            {
                Width = 0,
                Height = 9,
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(2.5)
            };
            var cell = new Border
            {
                Width = CellWidth,
                Height = 9,
                Background = Brush("#4A4D49"),
                CornerRadius = new CornerRadius(2.5),
                Margin = new Thickness(index == 0 ? 0 : 5, 0, 0, 0),
                Child = fill,
                ClipToBounds = true
            };
            fills[index] = fill;
            bar.Children.Add(cell);
        }

        Grid.SetRow(bar, 2);
        Grid.SetColumnSpan(bar, 2);
        grid.Children.Add(titleText);
        grid.Children.Add(remainingText);
        grid.Children.Add(resetText);
        grid.Children.Add(bar);
        container.Child = grid;
        return new UsageCardControls(container, remainingText, resetText, fills);
    }

    private void UpdateCard(
        UsageCardControls card,
        RateLimitWindowState? state,
        bool weekly,
        bool dataAvailable)
    {
        if (state is null)
        {
            card.Remaining.Text = _text.RemainingUnavailable;
            card.Reset.Text = dataAvailable ? _text.LimitUnavailable : _text.WaitingForData;
            ApplyBar(card, 0, UsageSignal.Gray);
            return;
        }

        card.Remaining.Text = _text.FormatRemaining(state.RemainingPercent);
        card.Reset.Text = state.ResetsAt is long unixSeconds
            ? weekly
                ? _text.FormatWeeklyReset(DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime())
                : _text.FormatFiveHourReset(DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime())
            : _text.ResetUnavailable;
        ApplyBar(card, state.RemainingPercent, UsagePresentation.GetSignal(state.RemainingPercent));
    }

    private static void ApplyBar(UsageCardControls card, int remainingPercent, UsageSignal signal)
    {
        var color = SignalBrush(signal);
        var ratios = UsagePresentation.GetCellFillRatios(remainingPercent);
        card.Remaining.Foreground = color;
        for (var index = 0; index < card.Fills.Length; index++)
        {
            card.Fills[index].Background = color;
            card.Fills[index].Width = CellWidth * ratios[index];
        }
    }

    private void HandleSourceInitialized(object? sender, EventArgs eventArgs)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(_windowHandle, NativeMethods.GwlExStyle).ToInt64();
        style |= NativeMethods.WsExTransparent |
                 NativeMethods.WsExToolWindow |
                 NativeMethods.WsExNoActivate;
        NativeMethods.SetWindowLongPtr(_windowHandle, NativeMethods.GwlExStyle, new nint(style));
    }

    private static SolidColorBrush SignalBrush(UsageSignal signal)
    {
        return signal switch
        {
            UsageSignal.Green => Brush("#4AD894"),
            UsageSignal.Yellow => Brush("#F2CF5B"),
            UsageSignal.Orange => Brush("#FF9847"),
            UsageSignal.Red => Brush("#FF6666"),
            _ => Brush("#777C78")
        };
    }

    private static SolidColorBrush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private sealed record UsageCardControls(
        Border Container,
        TextBlock Remaining,
        TextBlock Reset,
        Border[] Fills);
}
