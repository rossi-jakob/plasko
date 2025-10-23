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

public partial class FrmCrop : ToolForm, IToolForm<CropToolConfig>
{
    private readonly Keys _squareRatioSelectionKey = Keys.Shift | Keys.ShiftKey;
    private bool _isSquareRatioSelectionKeyPressed;
    private bool _isDefaultSelectionLoaded;
    private bool _isNewFileSaved;
    private bool _isInitialized;
    private Rectangle _lastSelectionArea;


    public string ToolId => "CropTool";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CropToolConfig Settings { get; set; }


    public FrmCrop() : base()
    {
        InitializeComponent();
    }

    public FrmCrop(Form owner) : base()
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
        BackdropMargin = new Padding(0, 0, 0, TableBottom.Height);

        if (EnableTransparent)
        {
            TableBottom.BackColor = darkMode
                ? Color.White.WithAlpha(10)
                : Color.White.WithAlpha(200);
        }
        else
        {
            BackColor = Config.Theme.ColorPalatte.AppBg;

            TableBottom.BackColor = darkMode
                ? Color.White.WithAlpha(15)
                : Color.Black.WithAlpha(15);
        }

        TableTop.BackColor = Config.Theme.ColorPalatte.AppBg;
        TooltipMain.DarkMode = darkMode;


        base.ApplyTheme(darkMode, style);
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

        // load crop tool configs
        Settings.LoadFromAppConfig();
        NumRatioFrom.Value = Settings.AspectRatioValues[0];
        NumRatioTo.Value = Settings.AspectRatioValues[1];

        // load form data
        LoadAspectRatioItems();
        UpdateAspectRatioValues();

        // add control events
        Local.FrmMain.KeyDown += FrmMain_KeyDown;
        Local.FrmMain.KeyUp += FrmMain_KeyUp;
        Local.ImageSaved += Local_ImageSaved;
        Local.ImageLoading += Local_ImageLoading;
        Local.FrmMain.PicMain.SelectionChanged += PicMain_OnImageSelecting;
        Local.FrmMain.PicMain.ImageLoading += PicMain_ImageLoading;
        Local.FrmMain.PicMain.ImageDrawn += PicMain_ImageDrawn;

        CmbAspectRatio.SelectedIndexChanged += CmbAspectRatio_SelectedIndexChanged;
        NumRatioFrom.LostFocus += NumRatio_LostFocus;
        NumRatioTo.LostFocus += NumRatio_LostFocus;
        NumX.LostFocus += NumSelections_LostFocus;
        NumY.LostFocus += NumSelections_LostFocus;
        NumWidth.LostFocus += NumSelections_LostFocus;
        NumHeight.LostFocus += NumSelections_LostFocus;

        TableTop.Enabled =
            TableBottom.Enabled = Local.FrmMain.PicMain.Source != ImageSource.Null
                && !Local.FrmMain.PicMain.CanImageAnimate
                && !Local.FrmMain.PicMain.UseWebview2;

        base.OnLoad(e);


        // load default selection
        LoadDefaultSelectionSetting(true);

        ApplyLanguage();
    }


    protected override int OnUpdateHeight(bool performUpdate = true)
    {
        var baseHeight = base.OnUpdateHeight(false);
        if (!_isInitialized) return baseHeight;

        // calculate form height
        var formHeight = baseHeight;

        try
        {
            var contentHeight = TableTop.Height + TableTop.Padding.Vertical
                + TableBottom.Height;
            formHeight = contentHeight + baseHeight;
        }
        catch { }

        if (performUpdate)
        {
            Height = formHeight;
        }

        return formHeight;
    }


    protected override void OnToolFormClosing(ToolFormClosingEventArgs e)
    {
        base.OnToolFormClosing(e);

        // reset selection
        BtnReset.PerformClick();

        // remove events
        Local.FrmMain.KeyDown -= FrmMain_KeyDown;
        Local.FrmMain.KeyUp -= FrmMain_KeyUp;
        Local.ImageSaved -= Local_ImageSaved;
        Local.ImageLoading -= Local_ImageLoading;
        Local.FrmMain.PicMain.SelectionChanged -= PicMain_OnImageSelecting;
        Local.FrmMain.PicMain.ImageLoading -= PicMain_ImageLoading;
        Local.FrmMain.PicMain.ImageDrawn -= PicMain_ImageDrawn;

        CmbAspectRatio.SelectedIndexChanged -= CmbAspectRatio_SelectedIndexChanged;
        NumRatioFrom.LostFocus -= NumRatio_LostFocus;
        NumRatioTo.LostFocus -= NumRatio_LostFocus;
        NumX.LostFocus -= NumSelections_LostFocus;
        NumY.LostFocus -= NumSelections_LostFocus;
        NumWidth.LostFocus -= NumSelections_LostFocus;
        NumHeight.LostFocus -= NumSelections_LostFocus;


        // save settings
        Settings.AspectRatioValues = [
            (int)NumRatioFrom.Value,
            (int)NumRatioTo.Value,
        ];

        Settings.SaveToAppConfig();
        Local.FrmMain.Activate();
    }


    protected override void OnRequestUpdatingLanguage()
    {
        ApplyLanguage();
        LoadAspectRatioItems();
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // get hotkey list
        bool CheckHotkey(string action)
        {
            var menuHotkeyList = Config.GetHotkey(FrmMain.CurrentMenuHotkeys, action);
            var menuHotkey = menuHotkeyList.SingleOrDefault(k => k.KeyData == e.KeyData);

            return menuHotkey != null;
        }

        if (CheckHotkey(nameof(Local.FrmMain.MnuSave)))
        {
            BtnSave.PerformClick();
            return;
        }

        if (CheckHotkey(nameof(Local.FrmMain.MnuSaveAs)))
        {
            BtnSaveAs.PerformClick();
            return;
        }

        if (CheckHotkey(nameof(Local.FrmMain.MnuCopyImageData)))
        {
            BtnCopy.PerformClick();
            return;
        }
    }

    #endregion // Override methods


    // Private methods
    #region Private methods

    private void ApplyLanguage()
    {
        Text = Config.Language[$"{nameof(Local.FrmMain)}.{nameof(Local.FrmMain.MnuCropTool)}"];

        LblAspectRatio.Text = Config.Language[$"{Name}.{nameof(LblAspectRatio)}"];
        LblLocation.Text = Config.Language[$"{Name}.{nameof(LblLocation)}"];
        LblSize.Text = Config.Language[$"{Name}.{nameof(LblSize)}"];

        BtnSave.Text = Config.Language[$"{Name}.{nameof(BtnSave)}"];
        BtnSaveAs.Text = Config.Language[$"{Name}.{nameof(BtnSaveAs)}"];
        BtnCrop.Text = Config.Language[$"{Name}.{nameof(BtnCrop)}"];
        BtnCopy.Text = Config.Language[$"{Name}.{nameof(BtnCopy)}"];


        // get hotkey string
        var saveHotkey = Config.GetHotkeyString(FrmMain.CurrentMenuHotkeys, nameof(Local.FrmMain.MnuSave));
        var saveAsHotkey = Config.GetHotkeyString(FrmMain.CurrentMenuHotkeys, nameof(Local.FrmMain.MnuSaveAs));
        var copyHotkey = Config.GetHotkeyString(FrmMain.CurrentMenuHotkeys, nameof(Local.FrmMain.MnuCopyImageData));


        // set tooltip
        TooltipMain.SetToolTip(BtnQuickSelect, Config.Language[$"{Name}.{nameof(BtnQuickSelect)}._Tooltip"]);
        TooltipMain.SetToolTip(BtnReset, Config.Language[$"{Name}.{nameof(BtnReset)}._Tooltip"]);
        TooltipMain.SetToolTip(BtnSettings, Config.Language[$"{Name}.{nameof(BtnSettings)}._Tooltip"]);

        TooltipMain.SetToolTip(BtnSave, string.Concat(Config.Language[$"{Name}.{nameof(BtnSave)}._Tooltip"], $" ({saveHotkey})"));
        TooltipMain.SetToolTip(BtnSaveAs, string.Concat(Config.Language[$"{Name}.{nameof(BtnSaveAs)}._Tooltip"], $" ({saveAsHotkey})"));
        TooltipMain.SetToolTip(BtnCrop, Config.Language[$"{Name}.{nameof(BtnCrop)}._Tooltip"]);
        TooltipMain.SetToolTip(BtnCopy, string.Concat(Config.Language[$"{Name}.{nameof(BtnCopy)}._Tooltip"], $" ({copyHotkey})"));
    }


    private void LoadAspectRatioItems()
    {
        CmbAspectRatio.Items.Clear();
        var i = 0;
        var cmbIndex = 0;

        foreach (SelectionAspectRatio arValue in Enum.GetValues<SelectionAspectRatio>())
        {
            if (arValue == Settings.AspectRatio)
            {
                cmbIndex = i;
            }


            var displayName = string.Empty;
            if (Const.AspectRatioValue.TryGetValue(arValue, out var enumValue))
            {
                displayName = string.Join(":", enumValue);
            }
            else
            {
                var arName = Enum.GetName(arValue);
                var langPath = $"{Name}.{nameof(SelectionAspectRatio)}._{arName}";

                if (!Config.Language.TryGetValue(langPath, out displayName))
                {
                    displayName = arName;
                }
            }

            CmbAspectRatio.Items.Add(displayName);
            i++;
        }

        // select item
        CmbAspectRatio.SelectedIndex = cmbIndex;
    }


    private void UpdateAspectRatioValues()
    {
        var ratio = (SelectionAspectRatio)CmbAspectRatio.SelectedIndex;
        var ratioFrom = NumRatioFrom.Value;
        var ratioTo = NumRatioTo.Value;

        if (ratio == SelectionAspectRatio.Original)
        {
            var srcW = (int)Local.FrmMain.PicMain.SourceWidth;
            var srcH = (int)Local.FrmMain.PicMain.SourceHeight;

            if (srcW > 0 && srcH > 0)
            {
                var results = BHelper.SimplifyFractions(srcW, srcH);
                ratioFrom = results[0];
                ratioTo = results[1];
            }
        }

        // fill selected item to the text boxes
        else if (Const.AspectRatioValue.TryGetValue((SelectionAspectRatio)CmbAspectRatio.SelectedIndex, out var value))
        {
            ratioFrom = value[0];
            ratioTo = value[1];
        }

        // load custom aspect ratio
        NumRatioFrom.Value = ratioFrom;
        NumRatioTo.Value = ratioTo;

        NumRatioFrom.Visible = NumRatioTo.Visible =
            ratio is SelectionAspectRatio.Original or SelectionAspectRatio.Custom;
        NumRatioFrom.Enabled = NumRatioTo.Enabled = ratio == SelectionAspectRatio.Custom;

        // adjust form size
        OnUpdateHeight();


        // load selection area data
        NumX.Value = (decimal)Local.FrmMain.PicMain.SourceSelection.X;
        NumY.Value = (decimal)Local.FrmMain.PicMain.SourceSelection.Y;
        NumWidth.Value = (decimal)Local.FrmMain.PicMain.SourceSelection.Width;
        NumHeight.Value = (decimal)Local.FrmMain.PicMain.SourceSelection.Height;


        Settings.AspectRatio = ratio;
        Settings.AspectRatioValues = [(int)ratioFrom, (int)ratioTo];
        if (ratio == SelectionAspectRatio.FreeRatio)
        {
            Local.FrmMain.PicMain.SelectionAspectRatio = new SizeF();
        }
        else
        {
            Local.FrmMain.PicMain.SelectionAspectRatio = new SizeF((int)ratioFrom, (int)ratioTo);
        }

        // set buttons state
        BtnSave.Enabled =
            BtnSaveAs.Enabled =
            BtnCrop.Enabled =
            BtnCopy.Enabled = !Local.FrmMain.PicMain.SourceSelection.IsEmpty;
    }


    private void LoadSelectionFromFormInputs(bool drawSelection)
    {
        var newRect = new Rectangle(
                (int)NumX.Value,
                (int)NumY.Value,
                (int)NumWidth.Value,
                (int)NumHeight.Value);

        if (newRect != Local.FrmMain.PicMain.SourceSelection)
        {
            Local.FrmMain.PicMain.SourceSelection = newRect;
        }

        if (drawSelection)
        {
            Local.FrmMain.PicMain.Refresh(false);
        }
    }


    private void LoadDefaultSelectionSetting(bool drawSelection)
    {
        var useLastSelection = _isNewFileSaved
            || Settings.InitSelectionType == DefaultSelectionType.UseTheLastSelection;

        var srcW = (int)Local.FrmMain.PicMain.SourceWidth;
        var srcH = (int)Local.FrmMain.PicMain.SourceHeight;

        var x = 0;
        var y = 0;
        var w = 0;
        var h = 0;

        if (useLastSelection)
        {
            x = _lastSelectionArea.X;
            y = _lastSelectionArea.Y;
            w = _lastSelectionArea.Width;
            h = _lastSelectionArea.Height;
        }
        else if (Settings.InitSelectionType == DefaultSelectionType.CustomArea)
        {
            x = Settings.InitSelectedArea.X;
            y = Settings.InitSelectedArea.Y;
            w = Settings.InitSelectedArea.Width;
            h = Settings.InitSelectedArea.Height;
        }
        else
        {
            var selectPercent = Settings.InitSelectionType switch
            {
                DefaultSelectionType.SelectNone => 0f,
                DefaultSelectionType.Select10Percent => 0.1f,
                DefaultSelectionType.Select20Percent => 0.2f,
                DefaultSelectionType.Select25Percent => 0.25f,
                DefaultSelectionType.Select30Percent => 0.3f,
                DefaultSelectionType.SelectOneThird => 1 / 3f,
                DefaultSelectionType.Select40Percent => 0.4f,
                DefaultSelectionType.Select50Percent => 0.5f,
                DefaultSelectionType.Select60Percent => 0.6f,
                DefaultSelectionType.SelectTwoThirds => 2 / 3f,
                DefaultSelectionType.Select70Percent => 0.7f,
                DefaultSelectionType.Select75Percent => 0.75f,
                DefaultSelectionType.Select80Percent => 0.8f,
                DefaultSelectionType.Select90Percent => 0.9f,
                DefaultSelectionType.SelectAll => 1f,
                _ => 1.0f,
            };

            w = (int)(srcW * selectPercent);
            h = (int)(srcH * selectPercent);
        }


        // update selection size according to the ratio
        if (Settings.AspectRatio != SelectionAspectRatio.FreeRatio
            && Settings.AspectRatioValues[0] > 0
            && Settings.AspectRatioValues[1] > 0)
        {
            var ratioSize = GetSizeWithAspectRatio(new Size(w, h));

            w = ratioSize.Width;
            h = ratioSize.Height;
        }

        // auto-center the selection
        if (Settings.AutoCenterSelection && !useLastSelection)
        {
            x = srcW / 2 - w / 2;
            y = srcH / 2 - h / 2;
        }

        // validate selection bounds
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        w = Math.Max(0, w);
        h = Math.Max(0, h);

        Local.FrmMain.PicMain.SourceSelection = new(x, y, w, h);

        _isDefaultSelectionLoaded = true;
        if (drawSelection)
        {
            Local.FrmMain.PicMain.Refresh(false);
        }
    }


    private Size GetSizeWithAspectRatio(Size size)
    {
        // get aspect ratio
        var ratioW = Settings.AspectRatioValues[0];
        var ratioH = Settings.AspectRatioValues[1];

        var wRatio = 1f * ratioW / ratioH;
        var hRatio = 1f * ratioH / ratioW;


        // get source size
        var srcW = (int)Local.FrmMain.PicMain.SourceWidth;
        var srcH = (int)Local.FrmMain.PicMain.SourceHeight;

        var w = size.Width;
        var h = size.Height;


        // scale the new size to the aspect ratio
        if (w > h)
        {
            w = (int)(h * wRatio);
        }
        else if (w < h)
        {
            h = (int)(w * hRatio);
        }

        // if new size is larger than source size
        if (w >= srcW || h >= srcH)
        {
            var srcWRatio = 1f * srcW / srcH;
            var srcHRatio = 1f * srcH / srcW;

            if (srcWRatio >= wRatio)
            {
                w = (int)(wRatio * srcH);
            }
            else if (srcHRatio >= hRatio)
            {
                h = (int)(hRatio * srcW);
            }
        }

        return new Size(w, h);
    }


    #endregion // Private methods


    // Form events
    #region Form events

    private void FrmMain_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_isSquareRatioSelectionKeyPressed) return;
        _isSquareRatioSelectionKeyPressed = e.KeyData == _squareRatioSelectionKey;

        if (_isSquareRatioSelectionKeyPressed)
        {
            Local.FrmMain.PicMain.SelectionAspectRatio = new SizeF(1, 1);

            if (Local.FrmMain.PicMain.CurrentSelectionAction == SelectionAction.Drawing)
            {
                Local.FrmMain.PicMain.UpdateSelectionByMousePosition();
            }


            Local.FrmMain.PicMain.Invalidate();
        }
    }


    private void FrmMain_KeyUp(object? sender, KeyEventArgs e)
    {
        if (_isSquareRatioSelectionKeyPressed)
        {
            _isSquareRatioSelectionKeyPressed = false;
            UpdateAspectRatioValues();

            if (Local.FrmMain.PicMain.CurrentSelectionAction == SelectionAction.Drawing)
            {
                Local.FrmMain.PicMain.UpdateSelectionByMousePosition();
            }

            Local.FrmMain.PicMain.Invalidate();
        }
    }


    private void Local_ImageSaved(ImageSaveEventArgs e)
    {
        _isNewFileSaved = e.IsSaveAsNewFile;

        if (Settings.CloseToolAfterSaving
            && e.SaveSource == ImageSaveSource.SelectedArea)
        {
            Close();
        }
    }


    private void PicMain_OnImageSelecting(object? sender, Viewer.SelectionEventArgs e)
    {
        NumX.Value = (decimal)e.SourceSelection.X;
        NumY.Value = (decimal)e.SourceSelection.Y;
        NumWidth.Value = (decimal)e.SourceSelection.Width;
        NumHeight.Value = (decimal)e.SourceSelection.Height;

        BtnSave.Enabled =
            BtnSaveAs.Enabled =
            BtnCrop.Enabled =
            BtnCopy.Enabled = !e.SourceSelection.IsEmpty;
    }

    private void Local_ImageLoading(ImageLoadingEventArgs e)
    {
        TableTop.Enabled = TableBottom.Enabled = !e.UseWebview2;
    }

    private void PicMain_ImageLoading(object? sender, EventArgs e)
    {
        _isDefaultSelectionLoaded = false;
        _lastSelectionArea = new Rectangle(
                (int)NumX.Value,
                (int)NumY.Value,
                (int)NumWidth.Value,
                (int)NumHeight.Value);
    }


    private void PicMain_ImageDrawn(object? sender, EventArgs e)
    {
        TableTop.Enabled =
            TableBottom.Enabled = Local.FrmMain.PicMain.Source != ImageSource.Null
                && !Local.FrmMain.PicMain.CanImageAnimate;

        UpdateAspectRatioValues();

        // calculate default selection
        if (!_isDefaultSelectionLoaded)
        {
            LoadDefaultSelectionSetting(false);
        }

        _isNewFileSaved = false;
    }


    private void CmbAspectRatio_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateAspectRatioValues();
        LoadDefaultSelectionSetting(true);
    }


    private void NumRatio_LostFocus(object? sender, EventArgs e)
    {
        Settings.AspectRatioValues = [(int)NumRatioFrom.Value, (int)NumRatioTo.Value];
        Local.FrmMain.PicMain.SelectionAspectRatio = new SizeF((float)NumRatioFrom.Value, (float)NumRatioTo.Value);

        LoadDefaultSelectionSetting(true);
    }


    private void NumSelections_LostFocus(object? sender, EventArgs e)
    {
        LoadSelectionFromFormInputs(true);
    }


    private void BtnSave_Click(object sender, EventArgs e)
    {
        Local.FrmMain.IG_Save();
    }


    private void BtnSaveAs_Click(object sender, EventArgs e)
    {
        Local.FrmMain.IG_SaveAs();
    }


    private void BtnCopy_Click(object sender, EventArgs e)
    {
        Local.FrmMain.IG_CopyImageData();
    }


    private void BtnCrop_Click(object sender, EventArgs e)
    {
        Local.FrmMain.IG_Crop();
    }

    private void BtnReset_Click(object sender, EventArgs e)
    {
        Local.FrmMain.PicMain.SourceSelection = default;
        Local.FrmMain.PicMain.Invalidate();
    }

    private void BtnQuickSelect_Click(object sender, EventArgs e)
    {

    }

    private void BtnSettings_Click(object sender, EventArgs e)
    {
        using var frm = new FrmCropSettings(Settings)
        {
            StartPosition = FormStartPosition.Manual,
            Left = Left,
            Top = Bottom,
        };

        if (frm.ShowDialog(this) == DialogResult.OK)
        {
            Settings = frm.Settings;
        }
    }

    #endregion // Form events


}
