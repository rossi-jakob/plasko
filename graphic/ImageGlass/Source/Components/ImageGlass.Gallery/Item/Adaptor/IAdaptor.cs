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

---------------------
ImageGlass.Gallery is based on ImageListView v13.8.2:
Url: https://github.com/oozcitak/imagelistview
License: Apache License Version 2.0, http://www.apache.org/licenses/
---------------------
*/
using ImageGlass.Base.Photoing.Codecs;

namespace ImageGlass.Gallery;


/// <summary>
/// Represents the abstract case class for adaptors.
/// </summary>
public abstract class IAdaptor : IDisposable
{
    #region Abstract Methods

    /// <summary>
    /// Returns the thumbnail image for the given item.
    /// </summary>
    /// <param name="filePath">Item file path.</param>
    /// <param name="size">Requested image size.</param>
    /// <param name="useEmbeddedThumbnails">Embedded thumbnail usage.</param>
    /// <param name="useExifOrientation">true to automatically rotate images based on Exif orientation; otherwise false.</param>
    /// <returns>The thumbnail image from the given item or null if an error occurs.</returns>
    public abstract Image? GetThumbnail(string filePath, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool useExifOrientation);

    /// <summary>
    /// Returns a unique identifier for this thumbnail to be used in persistent
    /// caching.
    /// </summary>
    /// <param name="filePath">Item file path.</param>
    /// <param name="size">Requested image size.</param>
    /// <param name="useEmbeddedThumbnails">Embedded thumbnail usage.</param>
    /// <param name="useExifOrientation">true to automatically rotate images based on Exif orientation; otherwise false.</param>
    /// <returns>A unique identifier string for the thumnail.</returns>
    public abstract string GetUniqueIdentifier(string filePath, Size size, UseEmbeddedThumbnails useEmbeddedThumbnails, bool useExifOrientation);

    /// <summary>
    /// Returns the path to the source image for use in drag operations.
    /// </summary>
    /// <param name="filePath">Item file path.</param>
    /// <returns>The path to the source image.</returns>
    public abstract string GetSourceImage(string filePath);

    /// <summary>
    /// Returns the details for the given item.
    /// </summary>
    /// <param name="filePath">Item file path.</param>
    public abstract IgMetadata GetDetails(string filePath);

    /// <summary>
    /// Performs application-defined tasks associated with freeing,
    /// releasing, or resetting unmanaged resources.
    /// </summary>
    public abstract void Dispose();

    #endregion
}

