﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace ImageGlass.UI;

[DefaultEvent(nameof(ColorChanged))]
public partial class ColorBox2D : Control
{
    private ColorMode _colorMode = ColorMode.Luminance;
    private HslColor _colorHsl = HslColor.FromAhsl(1.0, 1.0, 1.0);
    private Color _colorRgb = Color.Empty;
    private PointF _markerPoint = PointF.Empty;
    private bool _darkMode = false;
    private bool _isPressed = false;


    // Private properties
    #region Private properties

    private float MarkerSize => 10f * DpiScale;


    private float Gap => 4f * MarkerSize / 10f / 2;


    /// <summary>
    /// Gets the bounding rectangle of slider bar.
    /// </summary>
    private RectangleF ContentRect
    {
        get
        {
            var rect = new RectangleF(
                Gap / 2,
                Gap / 2,
                Width - Gap,
                Height - Gap);

            return rect;
        }
    }


    /// <summary>
    /// Gets the color according to the <see cref="DarkMode"/> value.
    /// </summary>
    private Color ThemeColor => DarkMode ? Color.White : Color.Black;

    #endregion // Private properties


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets, sets dark mode for <see cref="VSlider"/>.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            if (_darkMode != value)
            {
                Invalidate();
            }

            _darkMode = value;
        }
    }


    /// <summary>
    /// Gets DPI scale value.
    /// </summary>
    public float DpiScale => DeviceDpi / 96f;


    /// <summary>
    /// Gets, sets color mode.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ColorMode ColorMode
    {
        get => _colorMode;
        set
        {
            _colorMode = value;
            ResetMarker();
            Refresh();
        }
    }


    /// <summary>
    /// Gets, set HSL color value.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HslColor ColorHsl
    {
        get => _colorHsl;
        set
        {
            _colorHsl = value;
            _colorRgb = _colorHsl.RgbValue;
            ResetMarker();
            Refresh();
        }
    }


    /// <summary>
    /// Gets, sets RGB color value.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorRgb
    {
        get => _colorRgb;
        set
        {
            _colorRgb = value;
            _colorHsl = HslColor.FromColor(_colorRgb);
            ResetMarker();
            Refresh();
        }
    }


    /// <summary>
    /// Occurs when color is changed.
    /// </summary>
    public event EventHandler<ColorChangedEventArgs>? ColorChanged;

    #endregion // Public Properties


    public ColorBox2D()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint, true);

        _colorRgb = _colorHsl.RgbValue;
    }


    // Overriden Methods
    #region Overriden Methods

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _isPressed = true;
        SetMarker(e.X, e.Y);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isPressed)
        {
            SetMarker(e.X, e.Y);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isPressed = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var color = HslColor.FromAhsl(255);
        var color2 = HslColor.FromAhsl(255);
        var length = ColorMode == ColorMode.Saturation || ColorMode == ColorMode.Luminance
            ? ContentRect.Width
            : ContentRect.Height;

        switch (ColorMode)
        {
            case ColorMode.Hue:
                color.H = ColorHsl.H;
                color2.H = ColorHsl.H;
                color.S = 0.0;
                color2.S = 1.0;
                break;

            case ColorMode.Saturation:
                color.S = ColorHsl.S;
                color2.S = ColorHsl.S;
                color.L = 1.0;
                color2.L = 0.0;
                break;

            case ColorMode.Luminance:
                color.L = ColorHsl.L;
                color2.L = ColorHsl.L;
                color.S = 1.0;
                color2.S = 0.0;
                break;
        }


        for (int i = 0; i < length; i++)
        {
            var green = (int)Math.Round(255.0 - 255.0 * i / ContentRect.Height);
            var empty = Color.Empty;
            var rgbValue = Color.Empty;

            switch (ColorMode)
            {
                case ColorMode.Red:
                    empty = Color.FromArgb(ColorRgb.R, green, 0);
                    rgbValue = Color.FromArgb(ColorRgb.R, green, 255);
                    break;

                case ColorMode.Green:
                    empty = Color.FromArgb(green, ColorRgb.G, 0);
                    rgbValue = Color.FromArgb(green, ColorRgb.G, 255);
                    break;

                case ColorMode.Blue:
                    empty = Color.FromArgb(0, green, ColorRgb.B);
                    rgbValue = Color.FromArgb(255, green, ColorRgb.B);
                    break;

                case ColorMode.Hue:
                    color2.L = color.L = 1.0 - (i / ContentRect.Height);
                    empty = color.RgbValue;
                    rgbValue = color2.RgbValue;
                    break;

                case ColorMode.Saturation:
                case ColorMode.Luminance:
                    color2.H = color.H = i / ContentRect.Width;
                    empty = color.RgbValue;
                    rgbValue = color2.RgbValue;
                    break;
            }


            if (ColorMode == ColorMode.Saturation || ColorMode == ColorMode.Luminance)
            {
                // draw vertical line
                var gradientRect = new RectangleF(ContentRect.X, ContentRect.Y, 1, ContentRect.Height);
                var fillRect = new RectangleF(i + ContentRect.X, ContentRect.Y, 1, ContentRect.Height);

                using var gradientBrush = new LinearGradientBrush(gradientRect, empty, rgbValue, 90f, false);
                e.Graphics.FillRectangle(gradientBrush, fillRect);
            }
            else
            {
                // draw horizontal line
                var gradientRect = new RectangleF(ContentRect.X, ContentRect.Y, ContentRect.Width, 1);
                var fillRect = new RectangleF(ContentRect.X, i + ContentRect.Y, ContentRect.Width, 1);

                using var gradientBrush = new LinearGradientBrush(gradientRect, empty, rgbValue, 0f, false);
                e.Graphics.FillRectangle(gradientBrush, fillRect);
            }
        }


        // draw box border
        var opacity = DarkMode ? 50 : 50;
        using var borderPen = new Pen(Color.FromArgb(opacity, ThemeColor), DpiScale)
        {
            Alignment = PenAlignment.Inset,
        };
        e.Graphics.DrawRectangle(borderPen, ContentRect.X, ContentRect.Y, ContentRect.Width, ContentRect.Height);


        // draw marker
        DrawMarker(e.Graphics);
    }

    #endregion // Overriden Methods


    // Private Methods
    #region Private Methods

    private void DrawMarker(Graphics g)
    {
        var smoothMode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var circlePen = new Pen(Color.White, DpiScale)
        {
            LineJoin = LineJoin.Round,
            DashCap = DashCap.Round,
            Alignment = PenAlignment.Inset,
        };

        // draw white circle marker
        g.DrawEllipse(circlePen,
            _markerPoint.X - MarkerSize / 2,
            _markerPoint.Y - MarkerSize / 2,
            MarkerSize,
            MarkerSize);

        circlePen.Color = Color.Black;
        circlePen.Alignment = PenAlignment.Outset;

        // draw black circle marker
        g.DrawEllipse(circlePen,
            _markerPoint.X - MarkerSize / 2,
            _markerPoint.Y - MarkerSize / 2,
            MarkerSize,
            MarkerSize);

        g.SmoothingMode = smoothMode;
    }


    private HslColor GetColor(float x, float y)
    {
        int num1;
        int num2;
        int num3;
        var color = HslColor.FromAhsl(255);

        switch (ColorMode)
        {
            case ColorMode.Red:
                num2 = (int)Math.Round(255.0 * (1.0 - (y / (Height - Gap))));
                num3 = (int)Math.Round(255.0 * x / (Width - Gap));
                return HslColor.FromColor(Color.FromArgb(_colorRgb.R, num2, num3));

            case ColorMode.Green:
                num1 = (int)Math.Round(255.0 * (1.0 - y / (Height - Gap)));
                num3 = (int)Math.Round(255.0 * x / (Width - Gap));
                return HslColor.FromColor(Color.FromArgb(num1, _colorRgb.G, num3));

            case ColorMode.Blue:
                num1 = (int)Math.Round(255.0 * x / (Width - Gap));
                num2 = (int)Math.Round(255.0 * (1.0 - y / (Height - Gap)));
                return HslColor.FromColor(Color.FromArgb(num1, num2, _colorRgb.B));

            case ColorMode.Hue:
                color.H = _colorHsl.H;
                color.S = x / (Width - Gap);
                color.L = 1.0 - y / (Height - Gap);
                return color;

            case ColorMode.Saturation:
                color.S = _colorHsl.S;
                color.H = x / (Width - Gap);
                color.L = 1.0 - (y / (Height - Gap));
                return color;

            case ColorMode.Luminance:
                color.L = _colorHsl.L;
                color.H = x / (Width - Gap);
                color.S = 1.0 - (y / (Height - Gap));
                return color;
        }

        return color;
    }


    private void ResetMarker()
    {
        switch (_colorMode)
        {
            case ColorMode.Red:
                _markerPoint.X = (float)((Width - Gap) * _colorRgb.B / 255.0);
                _markerPoint.Y = (float)((Height - Gap) * (1.0 - _colorRgb.G / 255.0));
                return;

            case ColorMode.Green:
                _markerPoint.X = (float)((Width - Gap) * _colorRgb.B / 255.0);
                _markerPoint.Y = (float)((Height - Gap) * (1.0 - _colorRgb.R / 255.0));
                return;

            case ColorMode.Blue:
                _markerPoint.X = (float)((Width - Gap) * _colorRgb.R / 255.0);
                _markerPoint.Y = (float)((Height - Gap) * (1.0 - _colorRgb.G / 255.0));
                return;

            case ColorMode.Hue:
                _markerPoint.X = (float)((Width - Gap) * _colorHsl.S);
                _markerPoint.Y = (float)((Height - Gap) * (1.0 - _colorHsl.L));
                return;

            case ColorMode.Saturation:
                _markerPoint.X = (float)((Width - Gap) * _colorHsl.H);
                _markerPoint.Y = (float)((Height - Gap) * (1.0 - _colorHsl.L));
                return;

            case ColorMode.Luminance:
                _markerPoint.X = (float)((Width - Gap) * _colorHsl.H);
                _markerPoint.Y = (float)((Height - Gap) * (1.0 - _colorHsl.S));
                return;
        }
    }


    private void SetMarker(float x, float y)
    {
        x = Math.Clamp(x, 0, Width - Gap);
        y = Math.Clamp(y, 0, Height - Gap);

        if (_markerPoint.X != x || _markerPoint.Y != y)
        {
            _markerPoint = new PointF(x, y);
            _colorHsl = GetColor(x, y);
            _colorRgb = _colorHsl.RgbValue;
            Refresh();

            ColorChanged?.Invoke(this, new ColorChangedEventArgs(_colorRgb));
        }
    }

    #endregion // Private Methods

}
