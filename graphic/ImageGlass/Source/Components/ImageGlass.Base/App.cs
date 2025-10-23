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
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;

namespace ImageGlass.Base;

public class App
{
    /// <summary>
    /// Gets the application executable path
    /// </summary>
    public static string IGExePath => StartUpDir("ImageGlass.exe");


    /// <summary>
    /// Gets the application name
    /// </summary>
    public static string AppName => FileVersionInfo.GetVersionInfo(IGExePath).ProductName;


    /// <summary>
    /// Gets the product version
    /// </summary>
    public static string Version => FileVersionInfo.GetVersionInfo(IGExePath).FileVersion ?? "";


    /// <summary>
    /// Checks if the current user is administator
    /// </summary>
    public static bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent())
       .IsInRole(WindowsBuiltInRole.Administrator);


    /// <summary>
    /// Gets value of Portable mode if the startup dir is writable
    /// </summary>
    public static bool IsPortable => BHelper.CheckPathWritable(PathType.Dir, StartUpDir());


    /// <summary>
    /// Gets the path based on the startup folder of ImageGlass.
    /// </summary>
    public static string StartUpDir(params string[] paths)
    {
        var newPaths = paths.ToList();
        newPaths.Insert(0, Application.StartupPath);

        return Path.Combine([.. newPaths]);
    }


    /// <summary>
    /// Returns the path based on the configuration folder of ImageGlass.
    /// For portable mode, ConfigDir = InstalledDir, else <c>%LocalAppData%\ImageGlass</c>
    /// </summary>
    /// <param name="type">Indicates if the given path is either file or directory</param>
    public static string ConfigDir(PathType type, params string[] paths)
    {
        // use StartUp dir if it's writable
        var startUpPath = StartUpDir(paths);

        if (BHelper.CheckPathWritable(type, startUpPath))
        {
            return startUpPath;
        }

        // else, use AppData dir
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

        // create the directory if not exists
        Directory.CreateDirectory(appDataDir);

        var newPaths = paths.ToList();
        newPaths.Insert(0, appDataDir);
        appDataDir = Path.Combine([.. newPaths]);

        return appDataDir;
    }



    /// <summary>
    /// Checks if ImageGlass starts with OS
    /// </summary>
    public static bool CheckStartWithOs()
    {
        const string APP_NAME = "ImageGlass";
        var regAppPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";


        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(regAppPath);
            var keyValue = key?.GetValue(APP_NAME)?.ToString();

            var isEnabled = !string.IsNullOrWhiteSpace(keyValue);

            return isEnabled;
        }
        catch { }

        return false;
    }


    /// <summary>
    /// Sets or unsets ImageGlass to start with OS in <see cref="IgCommands.STARTUP_BOOST"/> mode.
    /// Returns <c>null</c> if successful.
    /// </summary>
    public static Exception? SetStartWithOs(bool enable)
    {
        Exception? error = null;
        const string APP_NAME = "ImageGlass";
        var regAppPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(regAppPath, true);

            if (enable)
            {
                key?.SetValue(APP_NAME, $"\"{App.IGExePath}\" {IgCommands.STARTUP_BOOST}");
            }
            else
            {
                key?.DeleteValue(APP_NAME);
            }
        }
        catch (Exception ex) { error = ex; }


        return error;
    }


}
