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
using ImageGlass.Base.WinApi;
using System.ComponentModel;

namespace ImageGlass.UI;

/// <summary>
/// Modern form with dark mode and backdrop support.
/// </summary>
public partial class ModernForm : Form
{
    private bool _darkMode = true;
    private bool _enableTransparent = true;
    private BackdropStyle _backdropStyle = BackdropStyle.MicaAlt;
    private Padding _backdropMargin = new(-1);
    private int _dpi = DpiApi.DPI_DEFAULT;
    private CancellationTokenSource _systemAccentColorChangedCancelToken = new();


    #region Public properties

    /// <summary>
    /// Enable transparent background.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual bool EnableTransparent
    {
        get
        {
            if (!WinColorsApi.IsTransparencyEnabled
                || !BHelper.IsOS(WindowsOS.Win11_22H2_OrLater))
            {
                _enableTransparent = false;
            }

            return _enableTransparent;
        }
        set
        {
            _enableTransparent = value;
        }
    }


    /// <summary>
    /// Enables or disables form's dark mode.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual bool DarkMode
    {
        get => _darkMode;
        set
        {
            _darkMode = value;
            SetDarkMode(value);
        }
    }


    /// <summary>
    /// Gets, sets window backdrop.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual BackdropStyle BackdropStyle
    {
        get => _backdropStyle;
        set
        {
            _backdropStyle = value;
            SetBackdrop(value);
        }
    }


    /// <summary>
    /// Gets, sets the backdrop margin.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual Padding BackdropMargin
    {
        get => _backdropMargin;
        set
        {
            _backdropMargin = value;

            if (IsDisposed) return;
            _ = WindowApi.SetWindowFrame(Handle, _backdropMargin);
        }
    }


    /// <summary>
    /// Gets, sets the keys to close the <see cref="ModernForm"/>.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual Keys CloseFormHotkey { get; set; } = Keys.None;


    /// <summary>
    /// Enables or disables shortcut key handling in parent form.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual bool EnableParentShortcut { get; set; } = false;


    /// <summary>
    /// Gets the current DPI. Default value is <c>96</c>.
    /// </summary>
    public virtual int Dpi => _dpi;


    /// <summary>
    /// Gets the current DPI scaling. Default value is <c>1.0f</c>.
    /// </summary>
    public virtual float DpiScale => _dpi / 96f;


    /// <summary>
    /// Gets, sets the value indicates that <see cref="DpiApi.CurrentDpi"/> should be updated when form DPI is changed.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public virtual bool EnableDpiApiUpdate { get; set; } = false;


    /// <summary>
    /// Occurs when the window state is changed.
    /// </summary>
    public event EventHandler<WindowStateChangedEventArgs>? WindowStateChanged;


    /// <summary>
    /// Occurs when the system accent color is changed.
    /// </summary>
    public event SystemAccentColorChangedHandler? SystemAccentColorChanged;
    public delegate void SystemAccentColorChangedHandler(SystemAccentColorChangedEventArgs e);

    #endregion // Public properties


    /// <summary>
    /// Initializes the new instance of <see cref="ModernForm"/>.
    /// </summary>
    public ModernForm()
    {
        InitializeComponent();
        SizeGripStyle = SizeGripStyle.Hide;

        _dpi = DeviceDpi;
    }


    // Protected / virtual methods
    #region Protected / virtual methods

    protected override CreateParams CreateParams
    {
        get
        {
            // ensure that when in full-screen or frameless mode we can still use the 
            // Windows shortcut keys to minimize, and allow the taskbar icon to minimize
            // the window.
            // ref https://www.fluxbytes.com/csharp/minimize-a-form-without-border-using-the-taskbar/

            const int WS_MINIMIZEBOX = 0x20000;
            const int CS_DBLCLKS = 0x8;

            var cp = base.CreateParams;
            cp.Style |= WS_MINIMIZEBOX;
            cp.ClassStyle |= CS_DBLCLKS;

            return cp;
        }
    }


    protected override void WndProc(ref Message m)
    {
        // WM_SYSCOMMAND
        if (m.Msg == 0x0112)
        {
            if (m.WParam == new IntPtr(0xF030)) // SC_MAXIMIZE
            {
                // The window is being maximized
                OnWindowStateChanging(new(FormWindowState.Maximized));
            }
            else if (m.WParam == new IntPtr(0xF120)) // SC_RESTORE
            {
                // The window is being restored
                OnWindowStateChanging(new(FormWindowState.Normal));
            }
            else if (m.WParam == new IntPtr(0xF020)) // SC_MINIMIZE
            {
                // The window is being minimized
                OnWindowStateChanging(new(FormWindowState.Minimized));
            }
        }
        //else if (m.Msg == DpiApi.WM_DPICHANGED)
        //{
        //    // get new dpi value
        //    _dpi = (short)m.WParam;

        //    OnDpiChanged();
        //}
        // WM_DWMCOLORIZATIONCOLORCHANGED: accent color changed
        else if (m.Msg == 0x0320)
        {
            DelayTriggerSystemAccentColorChangedEvent();
        }


        base.WndProc(ref m);
    }


    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);

        // get new dpi value
        _dpi = e.DeviceDpiNew;

        OnDpiChanged();
    }


    /// <summary>
    /// Occurs when window's DPI is changed.
    /// </summary>
    protected virtual void OnDpiChanged()
    {
        if (EnableDpiApiUpdate)
        {
            DpiApi.CurrentDpi = _dpi;
        }
    }


    /// <summary>
    /// Triggers <see cref="WindowStateChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnWindowStateChanging(WindowStateChangedEventArgs e)
    {
        if (DesignMode) return;

        WindowStateChanged?.Invoke(this, e);
    }


    /// <summary>
    /// Triggers <see cref="SystemAccentColorChanged"/> event.
    /// </summary>
    protected virtual void OnSystemAccentColorChanged(SystemAccentColorChangedEventArgs e)
    {
        if (DesignMode) return;

        // emits the event
        SystemAccentColorChanged?.Invoke(e);

        // the event is not handled
        if (!e.Handled)
        {
            Invalidate(true);
        }
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!DesignMode
            && EnableTransparent
            && BackdropStyle != BackdropStyle.None
            && BackdropMargin.Vertical == 0 && BackdropMargin.Horizontal == 0)
        {
            WindowApi.SetTransparentBlackBackground(e.Graphics, Bounds);
        }
    }


    /// <summary>
    /// Apply theme of the window.
    /// </summary>
    protected virtual void ApplyTheme(bool darkMode, BackdropStyle? style = null)
    {
        if (DesignMode) return;

        Application.SetColorMode(darkMode ? SystemColorMode.Dark : SystemColorMode.Classic);
        DarkMode = darkMode;
        BackdropStyle = style ?? _backdropStyle;
    }


    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // disable parent form shotcuts
        if (!EnableParentShortcut)
        {
            return false;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }


    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Issue #555: Prevent the beep sound caused by alt key pressed
        if (e.Alt && e.KeyCode != Keys.F4) e.SuppressKeyPress = true;

        base.OnKeyDown(e);

        if (CloseFormHotkey != Keys.None && e.KeyData.Equals(CloseFormHotkey))
        {
            CloseFormByKeys();
        }
    }


    /// <summary>
    /// Closes the window when <see cref="CloseFormHotkey"/> is pressed.
    /// </summary>
    protected virtual void CloseFormByKeys()
    {
        Close();
    }


    /// <summary>
    /// Sets dark mode setting to the given control and its child controls.
    /// </summary>
    protected virtual void SetDarkModeToChildControls(bool darkMode, Control? c)
    {
        if (c == null || !c.HasChildren) return;

        foreach (Control? child in c.Controls)
        {
            SetDarkModePropertyOfControl(darkMode, child);
            SetDarkModeToChildControls(darkMode, child);
        }
    }


    #endregion // Protected / virtual methods


    // Private methods
    #region Private methods

    /// <summary>
    /// Sets window backdrop.
    /// </summary>
    private void SetBackdrop(BackdropStyle style)
    {
        if (DesignMode || IsDisposed) return;

        var backupBgColor = BackColor;
        if (style != BackdropStyle.None && EnableTransparent)
        {
            // back color must be black
            BackColor = Color.Black;
        }

        // set backdrop style
        var succeeded = WindowApi.SetWindowBackdrop(Handle, (DWM_SYSTEMBACKDROP_TYPE)style);
        var margin = (succeeded && style != BackdropStyle.None && EnableTransparent)
            ? BackdropMargin
            : new Padding(0);

        if (!succeeded)
        {
            BackColor = backupBgColor;
        }

        // set window frame
        _ = WindowApi.SetWindowFrame(Handle, margin);
    }


    /// <summary>
    /// Sets window dark mode.
    /// </summary>
    private void SetDarkMode(bool enable)
    {
        if (DesignMode || IsDisposed) return;

        // set dark/light mode for controls
        SetDarkModeToChildControls(enable, this);

        // apply dark/light mode for title bar
        WindowApi.SetImmersiveDarkMode(Handle, enable);
    }


    /// <summary>
    /// Sets dark mode setting to the given control.
    /// </summary>
    private void SetDarkModePropertyOfControl(bool darkMode, Control? c)
    {
        if (c == null) return;

        var darkModeProp = c.GetType()?.GetProperty(nameof(DarkMode));
        if (darkModeProp == null) return;

        darkModeProp.SetValue(c, darkMode);
    }


    /// <summary>
    /// Delays triggering <see cref="SystemAccentColorChanged"/> event.
    /// </summary>
    private void DelayTriggerSystemAccentColorChangedEvent()
    {
        _systemAccentColorChangedCancelToken.Cancel();
        _systemAccentColorChangedCancelToken = new();

        _ = TriggerSystemAccentColorChangedEventAsync(_systemAccentColorChangedCancelToken.Token);
    }


    /// <summary>
    /// Triggers <see cref="SystemAccentColorChanged"/> event.
    /// </summary>
    private async Task TriggerSystemAccentColorChangedEventAsync(CancellationToken token = default)
    {
        try
        {
            // since the message WM_DWMCOLORIZATIONCOLORCHANGED is triggered
            // multiple times (3 - 5 times)
            await Task.Delay(200, token);
            token.ThrowIfCancellationRequested();

            // emit event here
            OnSystemAccentColorChanged(new SystemAccentColorChangedEventArgs());
        }
        catch (OperationCanceledException) { }
    }


    #endregion // Private methods


    // Free moving
    #region Free moving

    private bool _isMouseDown; // moving windows is taking place
    private Point _lastLocation; // initial mouse position


    /// <summary>
    /// Enables borderless form moving by registering mouse events to the form and its child controls.
    /// </summary>
    protected virtual void EnableFormFreeMoving(Control c)
    {
        DisableFormFreeMoving(c);

        if (ModernForm.CanControlMoveForm(c))
        {
            c.MouseDown += Form_MouseDown;
            c.MouseUp += Form_MouseUp;
            c.MouseMove += Form_MouseMove;
        }

        foreach (Control child in c.Controls)
        {
            EnableFormFreeMoving(child);
        }
    }


    /// <summary>
    /// Disables borderless form moving by removing mouse events from the form and its child controls.
    /// </summary>
    protected virtual void DisableFormFreeMoving(Control c)
    {
        if (ModernForm.CanControlMoveForm(c))
        {
            c.MouseDown -= Form_MouseDown;
            c.MouseUp -= Form_MouseUp;
            c.MouseMove -= Form_MouseMove;
        }


        foreach (Control child in c.Controls)
        {
            DisableFormFreeMoving(child);
        }
    }


    private static bool CanControlMoveForm(Control c)
    {
        return c is Form
            || (c is Label && c is not LinkLabel)
            || c is PictureBox
            || c is TableLayoutPanel
            || c is ProgressBar
            || c.HasChildren;
    }


    private void Form_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Clicks == 1)
        {
            _isMouseDown = true;
        }

        _lastLocation = e.Location;
    }

    private void Form_MouseMove(object? sender, MouseEventArgs e)
    {
        // not moving windows, ignore
        if (!_isMouseDown) return;

        Location = new Point(
            Location.X - _lastLocation.X + e.X,
            Location.Y - _lastLocation.Y + e.Y);

        Update();
    }

    private void Form_MouseUp(object? sender, MouseEventArgs e)
    {
        _isMouseDown = false;
    }

    #endregion // Free moving


}

