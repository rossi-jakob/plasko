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

using System.Collections.ObjectModel;

namespace ImageGlass.Base;

public class Web2MessageReceivedEventArgs(string? name, string? data, IReadOnlyList<object>? additionalObjects) : EventArgs
{
    /// <summary>
    /// Gets the name of the message.
    /// </summary>
    public string Name { get; init; } = name ?? string.Empty;

    /// <summary>
    /// Gets the data of the message.
    /// </summary>
    public string Data { get; init; } = data ?? string.Empty;

    /// <summary>
    /// Gets the additional received WebMessage objects.
    /// </summary>
    public IReadOnlyList<object> AdditionalObjects { get; init; } = additionalObjects ?? [];
}
