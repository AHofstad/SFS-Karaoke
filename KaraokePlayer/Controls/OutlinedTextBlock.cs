using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using FontStyle = System.Windows.FontStyle;

namespace KaraokePlayer.Controls;

public sealed class OutlinedTextBlock : FrameworkElement
{
  public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
    nameof(Text),
    typeof(string),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
    nameof(Fill),
    typeof(System.Windows.Media.Brush),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(System.Windows.Media.Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
    nameof(Stroke),
    typeof(System.Windows.Media.Brush),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(System.Windows.Media.Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
    nameof(StrokeThickness),
    typeof(double),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
    nameof(FontSize),
    typeof(double),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
    nameof(FontFamily),
    typeof(System.Windows.Media.FontFamily),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(System.Windows.SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
    nameof(FontWeight),
    typeof(FontWeight),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(
    nameof(FontStyle),
    typeof(System.Windows.FontStyle),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register(
    nameof(FontStretch),
    typeof(FontStretch),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
    nameof(TextAlignment),
    typeof(TextAlignment),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(TextAlignment.Left, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
    nameof(TextWrapping),
    typeof(TextWrapping),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty UnderlineProperty = DependencyProperty.Register(
    nameof(Underline),
    typeof(bool),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

  public static readonly DependencyProperty UnderlineThicknessProperty = DependencyProperty.Register(
    nameof(UnderlineThickness),
    typeof(double),
    typeof(OutlinedTextBlock),
    new FrameworkPropertyMetadata(2d, FrameworkPropertyMetadataOptions.AffectsRender));

  public string Text
  {
    get => (string)GetValue(TextProperty);
    set => SetValue(TextProperty, value);
  }

  public Brush Fill
  {
    get => (Brush)GetValue(FillProperty);
    set => SetValue(FillProperty, value);
  }

  public Brush Stroke
  {
    get => (Brush)GetValue(StrokeProperty);
    set => SetValue(StrokeProperty, value);
  }

  public double StrokeThickness
  {
    get => (double)GetValue(StrokeThicknessProperty);
    set => SetValue(StrokeThicknessProperty, value);
  }

  public double FontSize
  {
    get => (double)GetValue(FontSizeProperty);
    set => SetValue(FontSizeProperty, value);
  }

  public FontFamily FontFamily
  {
    get => (FontFamily)GetValue(FontFamilyProperty);
    set => SetValue(FontFamilyProperty, value);
  }

  public FontWeight FontWeight
  {
    get => (FontWeight)GetValue(FontWeightProperty);
    set => SetValue(FontWeightProperty, value);
  }

  public FontStyle FontStyle
  {
    get => (FontStyle)GetValue(FontStyleProperty);
    set => SetValue(FontStyleProperty, value);
  }

  public FontStretch FontStretch
  {
    get => (FontStretch)GetValue(FontStretchProperty);
    set => SetValue(FontStretchProperty, value);
  }

  public TextAlignment TextAlignment
  {
    get => (TextAlignment)GetValue(TextAlignmentProperty);
    set => SetValue(TextAlignmentProperty, value);
  }

  public TextWrapping TextWrapping
  {
    get => (TextWrapping)GetValue(TextWrappingProperty);
    set => SetValue(TextWrappingProperty, value);
  }

  public bool Underline
  {
    get => (bool)GetValue(UnderlineProperty);
    set => SetValue(UnderlineProperty, value);
  }

  public double UnderlineThickness
  {
    get => (double)GetValue(UnderlineThicknessProperty);
    set => SetValue(UnderlineThicknessProperty, value);
  }

  protected override Size MeasureOverride(Size availableSize)
  {
    var formattedText = CreateFormattedText(availableSize);
    return new Size(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
  }

  protected override Size ArrangeOverride(Size finalSize)
  {
    return finalSize;
  }

  protected override void OnRender(DrawingContext drawingContext)
  {
    var formattedText = CreateFormattedText(RenderSize);
    var geometry = formattedText.BuildGeometry(new Point(0, 0));
    drawingContext.DrawGeometry(Fill, new Pen(Stroke, StrokeThickness), geometry);

    if (Underline)
    {
      var underlineY = formattedText.Height - (UnderlineThickness * 0.5);
      var pen = new Pen(Fill, UnderlineThickness);
      drawingContext.DrawLine(pen, new Point(0, underlineY), new Point(formattedText.Width, underlineY));
    }
  }

  private FormattedText CreateFormattedText(System.Windows.Size availableSize)
  {
    var formattedText = new FormattedText(
      Text ?? string.Empty,
      CultureInfo.CurrentUICulture,
      System.Windows.FlowDirection.LeftToRight,
      new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
      FontSize,
      Fill,
      VisualTreeHelper.GetDpi(this).PixelsPerDip)
    {
      TextAlignment = TextAlignment
    };

    if (!double.IsInfinity(availableSize.Width))
    {
      formattedText.MaxTextWidth = Math.Max(0, availableSize.Width);
    }

    return formattedText;
  }
}
