﻿using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using SyncClipboard.Core.Clipboard;
using SyncClipboard.Core.Interfaces;
using SyncClipboard.Core.Models;
using SyncClipboard.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT;

namespace SyncClipboard.WinUI3.ClipboardWinUI;

internal class ClipboardFactory : ClipboardFactoryBase
{
    protected override ILogger Logger { get; set; }
    protected override IServiceProvider ServiceProvider { get; set; }
    protected override IWebDav WebDav { get; set; }

    private const string LOG_TAG = nameof(ClipboardFactory);

    private delegate void FormatHandler(DataPackageView ClipboardData, ClipboardMetaInfomation meta);
    private static List<KeyValuePair<string, FormatHandler>> FormatHandlerlist => new Dictionary<string, FormatHandler>
    {
        [StandardDataFormats.Text] = HanleText,
        ["DeviceIndependentBitmap"] = HanleDib,
        [StandardDataFormats.Bitmap] = HanleBitmap,
        [StandardDataFormats.Html] = HanleHtml,
        [StandardDataFormats.StorageItems] = HanleFiles,
        ["Preferred DropEffect"] = HanleDropEffect,
    }.ToList();

    private static void HanleBitmap(DataPackageView ClipboardData, ClipboardMetaInfomation meta)
    {
        if (meta.Image is not null) return;
        using var stream = ClipboardData.GetBitmapAsync().AsTask().Result.OpenReadAsync().AsTask().Result.AsStream();
        using MagickImage image = new(stream);
        meta.Image = WinBitmap.FromImage(image.ToBitmap());
    }

    private static void HanleDib(DataPackageView ClipboardData, ClipboardMetaInfomation meta)
    {
        var res = ClipboardData.GetDataAsync("DeviceIndependentBitmap").AsTask().Result;
        using var stream = res.As<IRandomAccessStream>().AsStream();
        using MagickImage image = new(stream);
        meta.Image = WinBitmap.FromImage(image.ToBitmap());
    }

    private static void HanleDropEffect(DataPackageView ClipboardData, ClipboardMetaInfomation meta)
    {
        var res = ClipboardData.GetDataAsync("Preferred DropEffect").AsTask().Result;
        using IRandomAccessStream randomAccessStream = res.As<IRandomAccessStream>();
        meta.Effects = (DragDropEffects?)randomAccessStream.AsStreamForRead().ReadByte();
    }

    private static void HanleFiles(DataPackageView ClipboardData, ClipboardMetaInfomation meta)
    {
        IReadOnlyList<IStorageItem> list = ClipboardData.GetStorageItemsAsync().AsTask().Result;
        meta.Files = list.Select(storageItem => storageItem.Path).ToArray();
    }

    private static void HanleHtml(DataPackageView ClipboardData, ClipboardMetaInfomation meta)
        => meta.Html = ClipboardData.GetHtmlFormatAsync().AsTask().Result;

    private static void HanleText(DataPackageView ClipboardData, ClipboardMetaInfomation meta)
        => meta.Text = ClipboardData.GetTextAsync().AsTask().Result;

    public ClipboardFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        Logger = ServiceProvider.GetRequiredService<ILogger>();
        WebDav = ServiceProvider.GetRequiredService<IWebDav>();
    }

    public override ClipboardMetaInfomation GetMetaInfomation()
    {
        ClipboardMetaInfomation meta = new();
        DataPackageView ClipboardData = Clipboard.GetContent();
        if (ClipboardData is null)
        {
            return meta;
        }

        for (int i = 0; ClipboardData.AvailableFormats.Count == 0 && i < 10; i++)
        {
            Thread.Sleep(200);
            ClipboardData = Clipboard.GetContent();
            Logger.Write(LOG_TAG, "retry times: " + (i + 1));
        }

        if (ClipboardData.AvailableFormats.Count == 0)
        {
            Logger.Write(LOG_TAG, "ClipboardData.AvailableFormats.Count is 0");
            meta.Text = "";
            return meta;
        }

        int errortimes = 0;
        foreach (var handler in FormatHandlerlist)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (ClipboardData.AvailableFormats.Contains(handler.Key))
                        handler.Value(ClipboardData, meta);
                }
                catch (Exception ex)
                {
                    errortimes += 1;
                    Logger.Write(LOG_TAG, ex.ToString());
                    Thread.Sleep(200);
                }
            }
        }

        Logger.Write(LOG_TAG, "Text: " + meta.Text ?? "");
        return meta;
    }
}
