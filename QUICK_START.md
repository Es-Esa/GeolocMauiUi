# High-Performance Camera Pipeline - Quick Start

## 🚀 Pika-asennus (5 min)

### 1. Restore packages
```powershell
cd D:\GeolocMauiUi
dotnet restore
```

### 2. Vaihda uuteen sivuun (2 vaihtoehtoa)

#### Vaihtoehto A: Korvaa vanha (SUOSITUS)
Avaa `AppShell.xaml` ja muuta:
```xml
<!-- VANHA -->
ContentTemplate="{DataTemplate views:CameraDetectionPage}"

<!-- UUSI -->
ContentTemplate="{DataTemplate views:CameraDetectionPageV2}"
```

#### Vaihtoehto B: Lisää testivälilehti
Avaa `AppShell.xaml` ja lisää:
```xml
<ShellContent
    Title="30 FPS Test"
    Route="DetectV2"
    Icon="detect.png"
    ContentTemplate="{DataTemplate views:CameraDetectionPageV2}" />
```

### 3. Build Android
```powershell
dotnet build -f net9.0-android
```

### 4. Deploy to emulator/device
```powershell
# Find device
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" devices

# Install
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" install -r .\bin\Debug\net9.0-android\com.geoloc.clientapp-Signed.apk

# Launch
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" shell am start -n com.geoloc.clientapp/crc641440c5aeb5906f31.MainActivity
```

### 5. Testaa FPS
1. Avaa "Live detection" tai "30 FPS Test" välilehti
2. Katso statuslabelista: `Render: XX FPS | YOLO: XX FPS | Queue: X`
3. **Tavoite**: Render 30 FPS, YOLO 25+ FPS, Queue 0-2

## ⚙️ Jos FPS < 25

### Nopeat säädöt (ei tarvitse rebuildia)
Avaa `Views/CameraDetectionPageV2.xaml.cs` ja muuta:

**1. Pienennä resoluutio** (rivi ~72):
```csharp
// ENNEN
await _videoFrameService.SetResolutionAsync(VideoResolution.Medium); // 640x480

// JÄLKEEN
await _videoFrameService.SetResolutionAsync(VideoResolution.Low); // 320x240 = 35+ FPS
```

**2. Frame skipping** (rivi ~55):
```csharp
_yoloProcessor = new AsyncYoloProcessor(_objectDetector)
{
    MaxQueueSize = 2,
    FrameSkipFactor = 2  // MUUTA 1 → 2 (process every other frame)
};
```

Rebuild ja deploy uudelleen.

## 🎯 Optimaalinen Setup

### Pixel/Galaxy (high-end)
```csharp
Resolution: VideoResolution.Medium (640x480)
FrameSkipFactor: 1
MaxQueueSize: 2
→ Expected: 28-30 FPS
```

### Budget devices (low-end)
```csharp
Resolution: VideoResolution.Low (320x240)
FrameSkipFactor: 2
MaxQueueSize: 3
→ Expected: 20-25 FPS
```

## 📊 Debugging

### Logcat (real-time debugging)
```powershell
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" logcat | Select-String "Sighting|YOLO|AsyncYolo"
```

### Performance profiler
```powershell
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" shell dumpsys cpuinfo | Select-String clientapp
```

## 🐛 Yleisimmät ongelmat

### 1. "No IVideoFrameService registered"
**Syy**: MauiProgram.cs conditional compilation ei toimi
**Ratkaisu**: 
```csharp
// MauiProgram.cs - Korvaa conditional block:
#if ANDROID
    builder.Services.AddSingleton<IVideoFrameService, 
        ClientApp.Platforms.Android.Services.AndroidVideoFrameService>();
#endif
```

### 2. CameraX crash: "No lifecycle owner"
**Syy**: MainActivity ei implement ILifecycleOwner
**Ratkaisu**: MAUI 9 hoitaa automaattisesti, mutta varmista:
```xml
<!-- AndroidManifest.xml -->
<application android:allowBackup="true" 
             android:theme="@style/Maui.SplashTheme"
             android:hardwareAccelerated="true">
```

### 3. Musta näyttö (ei previewia)
**Tämä on NORMAALIA versio 2.0:ssa!**
- CameraX framet menevät suoraan YOLO:lle
- Preview rendering TODO (katso HIGH_PERFORMANCE_CAMERA_GUIDE.md)
- Detections näkyvät overlay-boxeina

### 4. GPS location null
**Syy**: Location permissions puuttuu
**Ratkaisu**:
```xml
<!-- AndroidManifest.xml -->
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

## 📱 Test Checklist

- [ ] App aukeaa ilman crashia
- [ ] Camera permission granted
- [ ] Status label shows FPS stats
- [ ] Detection boxes appear when pointing at person
- [ ] Map shows pins when person detected (check MapPage)
- [ ] Render FPS ≥ 25
- [ ] YOLO FPS ≥ 20
- [ ] Queue size ≤ 3

## 🎉 Valmis!

Jos FPS ≥ 25, olet valmis! 

**Seuraavat askeleet:**
1. Testaa eri valaistuksissa
2. Testaa liikkuvilla kohteilla
3. Säädä `SightingCooldownSeconds` tarpeen mukaan
4. Harkitse kevyempää YOLO-mallia lisänopeutta varten

---
**Tukea tarvittaessa**: Katso `HIGH_PERFORMANCE_CAMERA_GUIDE.md`
