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
using System.ComponentModel;

namespace ImageGlass.UI;

public class ModernTextBox : TextBox
{
    private bool _darkMode = false;

    /// <summary>
    /// Toggles dark mode for this <see cref="ModernButton"/> control.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DarkMode
    {
        get => _darkMode;
        set
        {
            _darkMode = value;

            if (_darkMode)
            {
                var colors = BHelper.GetThemeColorPalatte(_darkMode);

                Padding = new Padding(2, 2, 2, 2);
                BackColor = colors.ControlBg;
                ForeColor = colors.AppText;
                BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                BackColor = SystemColors.Window;
                ForeColor = SystemColors.WindowText;
                BorderStyle = BorderStyle.Fixed3D;
            }
        }
    }


    /// <summary>
    /// Gets, sets value indicates that the text should be selected if the control is focused or clicked.
    /// </summary>
    [DefaultValue(false)]
    public bool SelectAllTextOnFocus { get; set; } = false;


    public ModernTextBox()
    {

    }


    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);

        if (SelectAllTextOnFocus)
        {
            Select(0, Text.Length);
        }
    }


    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();

        if (SelectAllTextOnFocus)
        {
            Select(0, Text.Length);
        }
    }

}
