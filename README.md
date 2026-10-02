# Nkraft.Mopups

[![NuGet](https://img.shields.io/nuget/v/Nkraft.Mopups.svg)](https://www.nuget.org/packages/Nkraft.Mopups/)

Popups for .NET MAUI on Android and iOS.

A fork of [Mopups](https://github.com/LuckyDucko/Mopups), maintained for [Nkraft.MvvmEssentials](https://github.com/mr5z/MvvmEssentials).

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
