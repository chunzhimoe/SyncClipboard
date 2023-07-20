﻿using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using SyncClipboard.Core.AbstractClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncClipboard.WinUI3.Views;

internal class TrayIconImpl : TrayIconBase<BitmapImage>
{
    public override event Action? MainWindowWakedUp;

    private readonly TrayIcon _trayIcon;

    protected override BitmapImage DefaultIcon => new BitmapImage(new Uri("ms-appx:///Assets/default.ico"));
    protected override BitmapImage ErrorIcon => new BitmapImage(new Uri("ms-appx:///Assets/erro.ico"));
    protected override int MaxToolTipLenth => 60;

    public TrayIconImpl(TrayIcon trayIcon)
    {
        _trayIcon = trayIcon;
        _trayIcon.DoubleClickCommand = new RelayCommand(() => MainWindowWakedUp?.Invoke());
    }

    public override void Create()
    {
        _trayIcon.ForceCreate();
    }

    protected override void SetIcon(BitmapImage icon)
    {
        App.Current.MainThreadContext.Post((_) => _trayIcon.IconSource = icon, null);
    }

    protected override BitmapImage[] UploadIcons()
    {
        return Enumerable.Range(1, 17)
            .Select(x => $"ms-appx:///Assets/upload{x:d3}.ico")
            .Select(x => new BitmapImage(new Uri(x)))
            .ToArray();
    }

    protected override BitmapImage[] DownloadIcons()
    {
        return Enumerable.Range(1, 17)
           .Select(x => $"ms-appx:///Assets/download{x:d3}.ico")
           .Select(x => new BitmapImage(new Uri(x)))
           .ToArray();
    }

    protected override void SetToolTip(string text)
    {
        // Not Support, do nothing;
    }
}