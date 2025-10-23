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

using ImageGlass.Base.Photoing.Codecs;
using System.ComponentModel;

namespace ImageGlass.Base.Services;


/// <summary>
/// Image booster service.
/// </summary>
public class ImageBooster : IDisposable
{
    #region IDisposable Disposing

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // stop the worker
            IsRunWorker = false;

            // Free any other managed objects here.
            // clear list and release resources
            Reset();

            Worker?.Dispose();
        }

        // Free any unmanaged objects here.
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~ImageBooster()
    {
        Dispose(false);
    }

    #endregion


    #region PRIVATE PROPERTIES

    /// <summary>
    /// Image booster background service
    /// </summary>
    private readonly BackgroundWorker Worker = new();

    /// <summary>
    /// Controls worker state
    /// </summary>
    private bool IsRunWorker { get; set; } = false;

    /// <summary>
    /// The list of Imgs
    /// </summary>
    private List<IgPhoto> ImgList { get; } = [];

    /// <summary>
    /// The list of image index that waiting for loading
    /// </summary>
    private List<int> QueuedList { get; } = [];

    /// <summary>
    /// The list of image index that waiting for releasing resource
    /// </summary>
    private List<int> FreeList { get; } = [];


    #endregion


    #region PUBLIC PROPERTIES

    /// <summary>
    /// Gets, sets codec read options
    /// </summary>
    public CodecReadOptions ReadOptions { get; set; } = new();

    /// <summary>
    /// Gets, sets the distinct directories list
    /// </summary>
    public List<string> DistinctDirs { get; set; } = [];

    /// <summary>
    /// Gets length of Img list
    /// </summary>
    public int Length => ImgList.Count;

    /// <summary>
    /// Get file paths list
    /// </summary>
    public List<string> FilePaths => ImgList.Select(i => i.FilePath).ToList();

    /// <summary>
    /// Gets, sets the list of formats that only load the first page forcefully.
    /// </summary>
    public HashSet<string> SingleFrameFormats { get; set; } = [];

    /// <summary>
    /// Gets, sets the number of maximum items in queue list for 1 direction (Next or Back navigation).
    /// The maximum number of items in queue list is 2x + 1.
    /// </summary>
    public int MaxQueue { get; set; } = 1;

    /// <summary>
    /// Gets, sets the maximum image dimension to cache.
    /// If this value is <c>less than or equals 0</c>, the option will be ignored.
    /// </summary>
    public int MaxImageDimensionToCache { get; set; } = 0;

    /// <summary>
    /// Gets, sets the maximum image file size (in MB) to cache.
    /// If this value is <c>less than or equals 0</c>, the option will be ignored.
    /// </summary>
    public float MaxFileSizeInMbToCache { get; set; } = 0f;


    /// <summary>
    /// Occurs when the image is loaded.
    /// </summary>
    public event EventHandler<EventArgs>? OnFinishLoadingImage;

    #endregion


    /// <summary>
    /// Initializes <see cref="ImageBooster"/> instance.
    /// </summary>
    /// <param name="codec"></param>
    public ImageBooster(IEnumerable<string>? list = null)
    {
        if (list != null)
        {
            Add(list);
        }

        // background worker
        IsRunWorker = true;
        Worker.RunWorkerAsync(RunBackgroundWorker());
    }


    #region PRIVATE FUNCTIONS

    /// <summary>
    /// Preloads the images in <see cref="QueuedList"/>.
    /// </summary>
    private async Task RunBackgroundWorker()
    {
        while (IsRunWorker)
        {
            if (QueuedList.Count > 0)
            {
                // pop out the first item
                var index = QueuedList[0];
                var img = ImgList[index];
                QueuedList.RemoveAt(0);


                if (!img.IsDone)
                {
                    // start loading image file
                    await img.LoadAsync(ReadOptions with
                    {
                        FirstFrameOnly = SingleFrameFormats.Contains(img.Extension),
                    }).ConfigureAwait(false);
                }
            }

            await Task.Delay(10).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// Add index of the image to queue list
    /// </summary>
    /// <param name="index">Current index of image list</param>
    /// <param name="includeCurrentIndex">Include current index in the queue list</param>
    private List<int> GetQueueList(int index, bool includeCurrentIndex)
    {
        // check valid index
        if (index < 0 || index >= ImgList.Count) return [];

        var list = new HashSet<int>();
        if (includeCurrentIndex)
        {
            list.Add(index);
        }

        var maxCachedItems = (MaxQueue * 2) + 1;
        var iRight = index;
        var iLeft = index;

        // add index in the range in order: index -> right -> left -> ...
        for (var i = 0; list.Count < maxCachedItems && list.Count < ImgList.Count; i++)
        {
            // if i is even number
            if ((i & 1) == 0)
            {
                // add right item: [index + 1; ...; to]
                iRight++;

                if (iRight < ImgList.Count)
                {
                    list.Add(iRight);
                }
                else
                {
                    list.Add(iRight - ImgList.Count);
                }
            }
            // if i is odd number
            else
            {
                // add left item: [index - 1; ...; from]
                iLeft--;

                if (iLeft >= 0)
                {
                    list.Add(iLeft);
                }
                else
                {
                    list.Add(ImgList.Count + iLeft);
                }
            }
        }

        // release the resources
        var freeListCloned = new List<int>(FreeList);
        foreach (var itemIndex in freeListCloned)
        {
            if (!list.Contains(itemIndex) && itemIndex >= 0 && itemIndex < ImgList.Count)
            {
                ImgList[itemIndex].Dispose();
                FreeList.Remove(itemIndex);
            }
        }

        // update new index of free list
        FreeList.AddRange(list);

        // get new queue list
        var newQueueList = new List<int>();

        foreach (var itemIndex in list)
        {
            try
            {
                // use cache metadata
                var metadata = ImgList[itemIndex].Metadata;
                metadata ??= PhotoCodec.LoadMetadata(ImgList[itemIndex].FilePath);

                // check image dimension
                var notExceedDimension = MaxImageDimensionToCache <= 0
                    || (metadata.RenderedWidth <= MaxImageDimensionToCache
                        && metadata.RenderedHeight <= MaxImageDimensionToCache);

                // check file size
                var notExceedFileSize = MaxFileSizeInMbToCache <= 0
                    || (metadata.FileSize / 1024f / 1024f <= MaxFileSizeInMbToCache);

                // only put the index to the queue if it does not exceed the size limit
                if (ImgList[itemIndex].IsDone || (notExceedDimension && notExceedFileSize))
                {
                    newQueueList.Add(itemIndex);
                }
            }
            catch { }
        }

        return newQueueList;
    }

    #endregion


    #region PUBLIC FUNCTIONS


    /// <summary>
    /// Cancels loading process of a <see cref="IgPhoto"/>.
    /// </summary>
    /// <param name="index">Item index</param>
    public void CancelLoading(int index)
    {
        if (0 <= index && index < ImgList.Count)
        {
            ImgList[index].CancelLoading();
        }
    }


    /// <summary>
    /// Gets image metadata.
    /// </summary>
    /// <param name="index">Image index.</param>
    /// <param name="frameIndex">Frame index, if value is <c>null</c>, it will load the first frame.</param>
    public IgMetadata? GetMetadata(int index, int? frameIndex)
    {
        try
        {
            if (ImgList[index].Metadata == null
                || ImgList[index].Metadata.FrameIndex != frameIndex)
            {
                ImgList[index].Metadata = PhotoCodec.LoadMetadata(
                    ImgList[index].FilePath,
                    ReadOptions with
                    {
                        FrameIndex = frameIndex,
                    });
            }

            return ImgList[index].Metadata;
        }
        catch (ArgumentOutOfRangeException) { }

        return null;
    }


    /// <summary>
    /// Get Img data
    /// </summary>
    /// <param name="index">image index</param>
    /// <param name="useCache"></param>
    public async Task<IgPhoto?> GetAsync(
        int index,
        bool useCache = true,
        CancellationTokenSource? tokenSrc = null)
    {
        // reload fresh new image data
        if (!useCache)
        {
            await ImgList[index].LoadAsync(ReadOptions with
            {
                FirstFrameOnly = SingleFrameFormats.Contains(ImgList[index].Extension),
            }, tokenSrc).ConfigureAwait(false);
        }

        // get image data from cache
        else
        {
            // get queue list according to index
            var queueItems = GetQueueList(index, true);

            if (!queueItems.Contains(index))
            {
                await ImgList[index].LoadAsync(ReadOptions with
                {
                    FirstFrameOnly = SingleFrameFormats.Contains(ImgList[index].Extension),
                }, tokenSrc).ConfigureAwait(false);
            }
            else
            {
                QueuedList.Clear();
                QueuedList.AddRange(queueItems);
            }
        }

        // wait until the image loading is done
        if (ImgList.Count > 0)
        {
            while (!ImgList[index].IsDone)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
        }

        // Trigger event OnFinishLoadingImage
        OnFinishLoadingImage?.Invoke(this, EventArgs.Empty);

        // if there is no error
        if (ImgList.Count > 0)
        {
            return ImgList[index];
        }

        return null;
    }


    /// <summary>
    /// Start caching images.
    /// </summary>
    /// <param name="index">Current index of image list</param>
    /// <param name="includeCurrentIndex">Include current index in the queue list</param>
    public void StartCaching(int index, bool includeCurrentIndex)
    {
        // get queue list according to index
        var queueItems = GetQueueList(index, includeCurrentIndex);

        QueuedList.Clear();
        QueuedList.AddRange(queueItems);
    }


    /// <summary>
    /// Adds a file path
    /// </summary>
    public void Add(string filePath, int index = -1)
    {
        if (index < 0)
        {
            ImgList.Add(new IgPhoto(filePath));
        }
        else
        {
            ImgList.Insert(index, new IgPhoto(filePath));
        }
    }


    /// <summary>
    /// Adds multiple file paths.
    /// </summary>
    public void Add(IEnumerable<string> filePaths, int index = -1)
    {
        var items = filePaths.Select(i => new IgPhoto(i));

        if (index < 0)
        {
            ImgList.AddRange(items);
        }
        else
        {
            ImgList.InsertRange(index, items);
        }
    }


    /// <summary>
    /// Checks if the image is cached.
    /// </summary>
    public bool IsCached(int index)
    {
        try
        {
            if (ImgList.Count > 0 && ImgList[index] != null)
            {
                return ImgList[index].ImgData.IsImageNull == false;
            }
        }
        catch (ArgumentOutOfRangeException) // force reload of empty list
        { }

        return false;
    }


    /// <summary>
    /// Get filename with the given index
    /// </summary>
    /// <param name="index"></param>
    /// <returns>Returns filename or empty string</returns>
    public string GetFilePath(int index)
    {
        try
        {
            if (ImgList.Count > 0 && ImgList[index] != null)
            {
                return ImgList[index].FilePath;
            }
        }
        catch (ArgumentOutOfRangeException)
        { }

        return string.Empty;
    }


    /// <summary>
    /// Set filename
    /// </summary>
    /// <param name="index"></param>
    /// <param name="filename">Image filename</param>
    public void SetFileName(int index, string filename)
    {
        if (ImgList[index] != null)
        {
            ImgList[index].FilePath = filename;
        }
    }


    /// <summary>
    /// Gets file extension. Ex: <c>.jpg</c>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public string GetFileExtension(int index)
    {
        var filename = GetFilePath(index);

        return Path.GetExtension(filename);
    }


    /// <summary>
    /// Find index with the given filename
    /// </summary>
    /// <param name="filename">Image filename</param>
    /// <returns></returns>
    public int IndexOf(string filename)
    {
        if (string.IsNullOrEmpty(filename.Trim()))
        {
            return -1;
        }

        // case sensitivity, esp. if filename passed on command line
        return ImgList.FindIndex(item => string.Equals(item.FilePath, filename, StringComparison.InvariantCultureIgnoreCase));
    }


    /// <summary>
    /// Unload and release resources of item with the given index
    /// </summary>
    public void Unload(int index)
    {
        try
        {
            ImgList[index]?.Dispose();
        }
        catch (ArgumentOutOfRangeException) { }
    }


    /// <summary>
    /// Remove an item in the list with the given index
    /// </summary>
    /// <param name="index">Item index</param>
    public void Remove(int index)
    {
        Unload(index);
        ImgList.RemoveAt(index);
    }


    /// <summary>
    /// Clears and resets all resources 
    /// </summary>
    public void Reset()
    {
        // clear list and release resources
        Clear();

        // Clear lists
        FilePaths.Clear();
        QueuedList.Clear();
        FreeList.Clear();
    }


    /// <summary>
    /// Empty and release resource of the list
    /// </summary>
    public void Clear()
    {
        // release the resources of the img list
        ClearCache();
        ImgList.Clear();
    }


    /// <summary>
    /// Clear all cached images and release resource of the list
    /// </summary>
    public void ClearCache()
    {
        // release the resources of the img list
        foreach (var item in ImgList)
        {
            item.Dispose();
        }

        QueuedList.Clear();
    }


    /// <summary>
    /// Update cached images
    /// </summary>
    public void UpdateCache()
    {
        // clear current queue list
        QueuedList.Clear();

        var cachedIndexList = ImgList
            .Select((item, index) => new { ImgItem = item, Index = index })
            .Where(item => item.ImgItem.IsDone)
            .Select(item => item.Index)
            .ToList();

        // release the cached images
        foreach (var index in cachedIndexList)
        {
            ImgList[index].Dispose();
        }

        // add to queue list
        QueuedList.AddRange(cachedIndexList);
    }


    /// <summary>
    /// Check if the folder path of input filename exists in the list
    /// </summary>
    public bool ContainsDirPathOf(string filename)
    {
        var target = Path.GetDirectoryName(filename)?.ToUpperInvariant();

        var index = ImgList.FindIndex(item => Path.GetDirectoryName(item.FilePath)?.ToUpperInvariant() == target);

        return index != -1;
    }

    #endregion


}
