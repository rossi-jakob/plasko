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

namespace ImageGlass.Base;


/// <summary>
/// Define the flags to tell FrmMain update the UI
/// </summary>
[Flags]
public enum UpdateRequests
{
    #pragma warning disable format

    None                = 0,
    Language            = 1 << 1,

    MouseActions        = 1 << 3,
    RealTimeFileUpdate  = 1 << 4,
    MenuHotkeys         = 1 << 5,

    ReloadImage         = 1 << 6,
    ReloadImageList     = 1 << 7,
    Slideshow           = 1 << 8,

    ToolbarAlignment    = 1 << 9,
    ToolbarIcons        = 1 << 10,
    ToolbarButtons      = 1 << 11,
    Gallery             = 1 << 12,
    Layout              = 1 << 13,

    Appearance          = 1 << 14,
    Theme               = 1 << 15,

    #pragma warning restore format
}


/// <summary>
/// Color profile options.
/// </summary>
public enum ColorProfileOption
{
    None,
    Custom,
    CurrentMonitorProfile,

    // ImageMagick's profiles
    AdobeRGB1998,
    AppleRGB,
    CoatedFOGRA39,
    ColorMatchRGB,
    sRGB,
    USWebCoatedSWOP,
}


/// <summary>
/// Types of path
/// </summary>
public enum PathType
{
    File,
    Dir,
    Unknown,
}


/// <summary>
/// Determines Windows OS requirement
/// </summary>
public enum WindowsOS
{
    /// <summary>
    /// Build 22621
    /// </summary>
    Win11_22H2_OrLater,

    /// <summary>
    /// Build 22000
    /// </summary>
    Win11OrLater,
    Win10,
    Win10OrLater,
}


/// <summary>
/// Exit codes of ImageGlass ultilities
/// </summary>
public enum IgExitCode : int
{
    Done = 0,
    AdminRequired = 1,
    Error = 2,
    Error_FileNotFound = 3,
}


/// <summary>
/// Flip options.
/// </summary>
[Flags]
public enum FlipOptions
{
    None = 0,
    Horizontal = 1 << 1,
    Vertical = 1 << 2,
}


/// <summary>
/// Rotate option.
/// </summary>
public enum RotateOption
{
    Left = 0,
    Right = 1,
}


/// <summary>
/// Color channels
/// </summary>
[Flags]
public enum ColorChannels
{
    R = 1 << 1,
    G = 1 << 2,
    B = 1 << 3,
    A = 1 << 4,

    RGB = R | G | B,
    RGBA = RGB | A,
}


/// <summary>
/// Selection aspect ratio.
/// </summary>
public enum SelectionAspectRatio
{
    FreeRatio = 0,
    Custom = 1,
    Original = 2,
    Ratio1_1 = 3,
    Ratio1_2 = 4,
    Ratio2_1 = 5,
    Ratio2_3 = 6,
    Ratio3_2 = 7,
    Ratio3_4 = 8,
    Ratio4_3 = 9,
    Ratio9_16 = 10,
    Ratio16_9 = 11,
}


/// <summary>
/// Window backdrop effect.
/// </summary>
public enum BackdropStyle
{
    /// <summary>
    /// Use default setting of Windows.
    /// </summary>
    None = 0,

    /// <summary>
    /// Mica effect.
    /// </summary>
    Mica = 2,

    /// <summary>
    /// Acrylic effect.
    /// </summary>
    Acrylic = 3,

    /// <summary>
    /// Draw the backdrop material effect corresponding to a window with a tabbed title bar.
    /// </summary>
    MicaAlt = 4,
}


/// <summary>
/// Options indicate what source of image is saved.
/// </summary>
public enum ImageSaveSource
{
    Undefined,
    SelectedArea,
    Clipboard,
    CurrentFile,
}


/// <summary>
/// Options for resampling methods.
/// </summary>
public enum ImageResamplingMethod : int
{
    Auto = 0,
    Average,
    CatmullRom,
    Cubic,
    CubicSmoother,
    Hermite,
    Lanczos,
    Linear,
    Mitchell,
    NearestNeighbor,
    Quadratic,
    Spline36,
}


/// <summary>
/// The loading order list.
/// **If we need to rename, we MUST update the language string too.
/// Because the name is also language keyword!
/// </summary>
public enum ImageOrderBy
{
    Name = 0,
    Random,
    FileSize,
    Extension,
    DateCreated,
    DateAccessed,
    DateModified,
    ExifDateTaken,
    ExifRating,
}


/// <summary>
/// The loading order types list
/// **If we need to rename, we MUST update the language string too.
/// Because the name is also language keyword!
/// </summary>
public enum ImageOrderType
{
    Asc = 0,
    Desc = 1,
}

