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
using ImageGlass.Base;
using ImageGlass.Settings;
using ImageGlass.Viewer;
using System.ComponentModel;

namespace ImageGlass;

public partial class FrmColorPicker : ToolForm, IToolForm<ColorPickerConfig>
{
    private Color? _pickedColor;
    private Point _pickedLocation;
    private bool _isInitialized;


    public string ToolId => "ColorPicker";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ColorPickerConfig Settings { get; set; }


    public FrmColorPicker() : base()
    {
        InitializeComponent();
    }


    public FrmColorPicker(Form owner) : base()
    {
        InitializeComponent();
        if (DesignMode) return;
        _isInitialized = true;

        Owner = owner;
        Settings = new(ToolId);

        ApplyTheme(Config.Theme.Settings.IsDarkMode);
    }



    // Override methods
    #region Override methods

    protected override void ApplyTheme(bool darkMode, BackdropStyle? style = null)
    {
        if (!_isInitialized) return;

        SuspendLayout();


        // show backdrop effect for title and footer
        BackdropMargin = new Padding(0);

        if (!EnableTransparent)
        {
            BackColor = Config.Theme.ColorPalatte.AppBg;
        }

        TableLayout.BackColor = Config.Theme.ColorPalatte.AppBg;
        TooltipMain.DarkMode = darkMode;

        base.ApplyTheme(darkMode, style);

        TxtLocation.BackColor =
            TxtRgb.BackColor =
            TxtHex.BackColor =
            TxtCmyk.BackColor =
            TxtHsl.BackColor =
            TxtHsv.BackColor =
            TxtCIELAB.BackColor = Config.Theme.ColorPalatte.AppBg;

        ResumeLayout();
    }


    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);

        OnUpdateHeight();
        ApplyTheme(Config.Theme.Settings.IsDarkMode);
    }


    protected override void OnLoad(EventArgs e)
    {
        if (DesignMode)
        {
            base.OnLoad(e);
            return;
        }

        // load tool configs
        Settings.LoadFromAppConfig();
        ApplySettings();

        // add control events
        Local.FrmMain.PicMain.ImageMouseMove += PicMain_ImageMouseMove;
        Local.FrmMain.PicMain.ImageMouseClick += PicMain_ImageMouseClick;

        base.OnLoad(e);

        ApplyLanguage();
    }

    private void PicMain_ImageMouseMove(object? sender, Viewer.ImageMouseEventArgs e)
    {
        if (e.Button != MouseButtons.None) return;

        if (e.ImageX < 0 || e.ImageY < 0
            || e.ImageX > Local.FrmMain.PicMain.SourceWidth
            || e.ImageY > Local.FrmMain.PicMain.SourceHeight)
        {
            LblCursorLocation.Text = string.Empty;
        }
        else
        {
            LblCursorLocation.Text = $"{(int)e.ImageX}, {(int)e.ImageY}";
        }
    }

    private void PicMain_ImageMouseClick(object? sender, Viewer.ImageMouseEventArgs e)
    {
        if (sender is not ViewerCanvas PicMain
            || e.ImageX < 0 || e.ImageY < 0
            || e.ImageX > Local.FrmMain.PicMain.SourceWidth
            || e.ImageY > Local.FrmMain.PicMain.SourceHeight) return;

        var x = (int)e.ImageX;
        var y = (int)e.ImageY;
        _pickedColor = PicMain.GetColorAt(x, y);
        _pickedLocation = new Point(x, y);

        ShowPickedColor(_pickedColor, x, y);
    }

    protected override int OnUpdateHeight(bool performUpdate = true)
    {
        var baseHeight = base.OnUpdateHeight(false);
        if (!_isInitialized) return baseHeight;

        // calculate form height
        var contentHeight = TableLayout.Height + TableLayout.Padding.Vertical;
        var formHeight = contentHeight + baseHeight;

        if (performUpdate)
        {
            Height = formHeight;
        }

        return formHeight;
    }


    protected override void OnToolFormClosing(ToolFormClosingEventArgs e)
    {
        base.OnToolFormClosing(e);

        // remove events
        Local.FrmMain.PicMain.ImageMouseMove -= PicMain_ImageMouseMove;
        Local.FrmMain.PicMain.ImageMouseClick -= PicMain_ImageMouseClick;


        // save settings
        Settings.SaveToAppConfig();
        Local.FrmMain.Activate();
    }


    protected override void OnRequestUpdatingLanguage()
    {
        ApplyLanguage();
    }


    #endregion // Override methods


    // Private methods
    #region Private methods

    private void ApplyLanguage()
    {
        Text = Config.Language[$"{nameof(Local.FrmMain)}.{nameof(Local.FrmMain.MnuColorPicker)}"];

        TooltipMain.SetToolTip(BtnSettings, Config.Language[$"{Name}.{nameof(BtnSettings)}._Tooltip"]);
        TooltipMain.SetToolTip(BtnCopyLocation, Config.Language[$"_._Copy"]);
        TooltipMain.SetToolTip(BtnCopyRgb, Config.Language[$"_._Copy"]);
        TooltipMain.SetToolTip(BtnCopyHex, Config.Language[$"_._Copy"]);
        TooltipMain.SetToolTip(BtnCopyCmyk, Config.Language[$"_._Copy"]);
        TooltipMain.SetToolTip(BtnCopyHsl, Config.Language[$"_._Copy"]);
        TooltipMain.SetToolTip(BtnCopyHsv, Config.Language[$"_._Copy"]);
        TooltipMain.SetToolTip(BtnCopyCIELab, Config.Language[$"_._Copy"]);
    }


    private void ApplySettings()
    {
        LblRgb.Text = Settings.ShowRgbWithAlpha ? "RGBA:" : "RGB:";
        LblHex.Text = Settings.ShowHexWithAlpha ? "HEXA:" : "HEX:";
        LblHsl.Text = Settings.ShowHslWithAlpha ? "HSLA:" : "HSL:";
        LblHsv.Text = Settings.ShowHsvWithAlpha ? "HSVA:" : "HSV:";
        LblCIELAB.Text = "CIELAB:";

        LblCmyk.Text = "CMYK:";
        LblLocation.Text = "X, Y:";

        ShowPickedColor(_pickedColor, _pickedLocation.X, _pickedLocation.Y);
    }


    private void ShowPickedColor(Color? pickedColor, int x, int y)
    {
        if (pickedColor == null) return;
        var color = pickedColor.Value;


        TxtLocation.Text = $"{x}, {y}";
        PanColor.BackColor = color;
        LblCursorLocation.ForeColor = color.InvertBlackOrWhite();

        var alpha = Math.Round(color.A / 255f, 3);

        // RGBA color -----------------------------------------------
        var alphaText = Settings.ShowRgbWithAlpha ? $", {alpha}" : "";
        TxtRgb.Text = $"{color.R}, {color.G}, {color.B}{alphaText}";

        // HEXA color -----------------------------------------------
        TxtHex.Text = color.ToHex(!Settings.ShowHexWithAlpha);

        // CMYK color -----------------------------------------------
        var cmyk = color.ToCmyk();
        TxtCmyk.Text = $"{cmyk[0]}%, {cmyk[1]}%, {cmyk[2]}%, {cmyk[3]}%";

        // HSLA color -----------------------------------------------
        var hsla = color.ToHsla();
        alphaText = Settings.ShowHslWithAlpha ? $", {hsla[3]}" : "";
        TxtHsl.Text = $"{hsla[0]}, {hsla[1]}%, {hsla[2]}%{alphaText}";

        // HSVA color -----------------------------------------------
        var hsva = color.ToHsva();
        alphaText = Settings.ShowHsvWithAlpha ? $", {hsva[3]}" : "";
        TxtHsv.Text = $"{hsva[0]}, {hsva[1]}%, {hsva[2]}%{alphaText}";

        // CIELAB color -----------------------------------------------
        var cielab = color.ToCIELAB();
        alphaText = Settings.ShowCIELabWithAlpha ? $", {cielab.Alpha}" : "";
        TxtCIELAB.Text = $"{cielab.L}, {cielab.A}, {cielab.B}{alphaText}";
    }


    private void BtnCopyLocation_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(TxtLocation.Text);
    }

    private void BtnCopyRgb_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(TxtRgb.Text);
    }

    private void BtnCopyHex_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(TxtHex.Text);
    }

    private void BtnCopyCmyk_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(TxtCmyk.Text);
    }

    private void BtnCopyHsl_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(TxtHsl.Text);
    }

    private void BtnCopyHsv_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(TxtHsv.Text);
    }

    private void BtnCopyCIELab_Click(object sender, EventArgs e)
    {
        Clipboard.SetText(TxtCIELAB.Text);
    }

    private void BtnSettings_Click(object sender, EventArgs e)
    {
        using var frm = new FrmColorPickerSettings(Settings)
        {
            StartPosition = FormStartPosition.Manual,
            Left = Left,
            Top = Bottom,
        };

        if (frm.ShowDialog(this) == DialogResult.OK)
        {
            Settings = frm.Settings;

            ApplySettings();
        }
    }

    #endregion // Private methods


}
