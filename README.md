# Nkraft.Mopups

[![NuGet](https://img.shields.io/nuget/v/Nkraft.Mopups.svg)](https://www.nuget.org/packages/Nkraft.Mopups/)

A minimal popup page for .NET MAUI on Android and iOS.

A fork of [Mopups](https://github.com/LuckyDucko/Mopups), trimmed down to just the popup page and its navigation. The goal is to keep it small and keep fixing issues on iOS and Android.

## Requirements

- .NET 10
- Android 7.0 (API 24) or later
- iOS 12.2 or later

## Install

```sh
dotnet add package Nkraft.Mopups
```

## Setup

```csharp
builder
    .UseMauiApp<App>()
    .ConfigureMopups();
```

## Usage

Inherit your page from `PopupPage`, then push and pop it:

```csharp
await MopupService.Instance.PushAsync(new MyPopup());
await MopupService.Instance.PopAsync();
```
