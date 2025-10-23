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
using D2Phap.DXControl;
using DirectN;
using ImageGlass.Base;
using System.Runtime.InteropServices;
using WicNet;

namespace ImageGlass.Viewer;


/// <summary>
/// Provides helper functions for <see cref="ViewerCanvas"/>.
/// </summary>
public static class VHelper
{

    /// <summary>
    /// Creates checkerboard tile brush (Direct2D)
    /// </summary>
    public static ComObject<ID2D1BitmapBrush1> CreateCheckerBoxTileD2D(IComObject<ID2D1DeviceContext6> dc, float cellSize, Color cellColor1, Color cellColor2)
    {
        // create tile: [X,O]
        //              [O,X]
        var width = (int)cellSize * 2;
        var height = (int)cellSize * 2;

        var tileImg = new WicBitmapSource(width, height, WicPixelFormat.GUID_WICPixelFormat32bppPBGRA);

        using var tileImgDc = tileImg.CreateRenderTarget();
        tileImgDc.Object.SetAntialiasMode(D2D1_ANTIALIAS_MODE.D2D1_ANTIALIAS_MODE_ALIASED);
        tileImgDc.BeginDraw();


        // draw X cells -------------------------------
        var color1 = DXHelper.FromColor(cellColor1);
        tileImgDc.Object.CreateSolidColorBrush(color1, IntPtr.Zero, out var brush1);

        // draw cell: [X, ]
        //            [ ,X]
        tileImgDc.Object.FillRectangle(DXHelper.ToD2DRectF(0, 0, cellSize, cellSize), brush1);
        tileImgDc.Object.FillRectangle(DXHelper.ToD2DRectF(cellSize, cellSize, cellSize, cellSize), brush1);


        // draw O cells -------------------------------
        var color2 = DXHelper.FromColor(cellColor2);
        tileImgDc.Object.CreateSolidColorBrush(color2, IntPtr.Zero, out var brush2);

        // draw cell: [X,O]
        //            [O,X]
        tileImgDc.Object.FillRectangle(DXHelper.ToD2DRectF(cellSize, 0, cellSize, cellSize), brush2);
        tileImgDc.Object.FillRectangle(DXHelper.ToD2DRectF(0, cellSize, cellSize, cellSize), brush2);


        tileImgDc.EndDraw();


        // create D2DBitmap from WICBitmapSource
        using var bmp = DXHelper.ToD2D1Bitmap(dc, tileImg);
        var bmpPropsPtr = new D2D1_BITMAP_BRUSH_PROPERTIES1()
        {
            extendModeX = D2D1_EXTEND_MODE.D2D1_EXTEND_MODE_WRAP,
            extendModeY = D2D1_EXTEND_MODE.D2D1_EXTEND_MODE_WRAP,
        }.StructureToPtr();

        // create bitmap brush
        dc.Object.CreateBitmapBrush(bmp.Object, bmpPropsPtr, IntPtr.Zero, out ID2D1BitmapBrush1 bmpBrush).ThrowOnError();


        Marshal.FreeHGlobal(bmpPropsPtr);

        return new ComObject<ID2D1BitmapBrush1>(bmpBrush);
    }


    /// <summary>
    /// Draws button.
    /// </summary>
    public static void DrawDXButton(DXGraphics g, RectangleF bound, float radius, Color baseColor, Color stateColor, float dpiScale, IComObject<ID2D1Bitmap1>? icon, DXButtonStates state)
    {
        var iconOpacity = 1f;
        var iconY = 0;
        var borderAlpha = 0;

        if (state.HasFlag(DXButtonStates.Pressed))
        {
            baseColor = baseColor.WithAlpha(240);
            borderAlpha = baseColor.A;
            iconOpacity = 0.6f;
            iconY = (int)dpiScale;
        }
        else if (state.HasFlag(DXButtonStates.Hover))
        {
            baseColor = baseColor.WithAlpha(200);
            borderAlpha = baseColor.A;
        }
        else
        {
            stateColor = Color.Transparent;
            borderAlpha = baseColor.A / 2;
        }


        var borderColor = baseColor.Blend(stateColor, 0.35f, borderAlpha);
        var fillColor = baseColor.Blend(stateColor, 0.5f, baseColor.A);


        // draw fill and border color
        g.DrawRectangle(bound, radius, borderColor, fillColor, dpiScale * 1f);


        // draw icon
        if (icon == null) return;

        icon.Object.GetSize(out var size);
        var srcIconSize = DXHelper.ToSize(size);
        var iconSize = Math.Min(bound.Width, bound.Height) / 2;
        var iconBound = new RectangleF()
        {
            X = bound.X + iconSize / 2,
            Y = bound.Y + iconSize / 2 + iconY,
            Width = iconSize,
            Height = iconSize,
        };

        g.DrawBitmap(icon, iconBound, null, InterpolationMode.Linear, iconOpacity);
    }
}

