# Real-Time Map Pin Detection Guide

## Overview
Your app now supports **real-time pin creation** on the map when detections occur. When the camera detects a person, it automatically:
1. Gets the current GPS location
2. Creates a sighting record
3. Adds a pin to the map in real-time
4. Centers the map on the new detection

## Features Implemented

### 1. Event-Based Architecture
- `ISightingRepository.SightingAdded` event fires when new detections are saved
- `MapPage` subscribes to this event and adds pins dynamically
- No need to refresh the page manually

### 2. Real-Time Pin Creation
- **Red pins**: Human detections
- **Blue pins**: Other detections (future vehicle/animal support)
- Pin labels include: `{Type} ({Confidence}) - {Time}`
- Auto-centers map on new detections

### 3. Camera Detection Flow
When a person is detected:
1. Camera captures frame
2. YOLO detector identifies "person"
3. GPS location is retrieved
4. Sighting is saved to repository
5. Event fires → Map updates instantly

## Testing on Android Emulator

### Set Emulator GPS Location
```powershell
# Connect to emulator
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 emu geo fix -122.084 37.4219

# Or use Extended Controls in emulator UI:
# ... (3 dots) → Location → Set custom location
```

### Common Test Locations
```powershell
# San Francisco
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 emu geo fix -122.4194 37.7749

# New York
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 emu geo fix -74.0060 40.7128

# London
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 emu geo fix -0.1276 51.5074
```

## Usage Instructions

### 1. Start the App
```powershell
# Build
dotnet build d:\GeolocMauiUi\ClientApp.csproj -f net9.0-android

# Install
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 install -r "d:\GeolocMauiUi\bin\Debug\net9.0-android\com.geoloc.clientapp-Signed.apk"

# Launch
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 shell am start -n com.geoloc.clientapp/crc641440c5aeb5906f31.MainActivity
```

### 2. Test Detection → Pin Flow
1. **Set GPS location** (use command above or emulator UI)
2. Navigate to **"Live Detection"** page
3. Point camera at a person (or use image with people)
4. Wait for detection (you'll see bounding box)
5. Navigate to **"Map"** page
6. **See the pin appear automatically!**

### 3. Verify Real-Time Updates
- Keep Map page open
- Switch to Camera Detection page (app stays in background)
- Detection occurs → **Pin appears on map instantly**
- No need to refresh or reload

## Code Changes Summary

### Files Modified
1. **`Core/Data/ISightingRepository.cs`**
   - Added `SightingAdded` event

2. **`Core/Data/InMemorySightingRepository.cs`**
   - Raises `SightingAdded` event when sighting is saved

3. **`Views/MapPage.xaml.cs`**
   - Subscribes to `SightingAdded` event
   - `OnSightingAdded()` adds pins in real-time
   - Auto-centers map on new detections
   - Color-codes pins (Red=Human, Blue=Other)
   - Unsubscribes on page close (prevents memory leaks)

### Existing Detection Logic
- **`Views/CameraDetectionPage.xaml.cs`** already saves detections
- No changes needed to detection flow
- Already requests location and saves to repository

## Troubleshooting

### GPS Not Working
```powershell
# Check location permission
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 shell pm grant com.geoloc.clientapp android.permission.ACCESS_FINE_LOCATION

# Verify GPS is enabled in emulator settings
# Settings → Location → Use location
```

### Pins Not Appearing
1. Check logcat for location errors:
```powershell
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -s emulator-5554 logcat -s "ClientApp"
```

2. Ensure person detection is working (bounding box appears)
3. Verify GPS location is set in emulator
4. Check that Map page is loaded (subscribes to events on appear)

### Detection Not Triggering
- Camera must detect "person" label
- Confidence threshold: any detection triggers save
- Location must be available (check GPS settings)
- Repository event must fire (check console logs)

## Next Steps

### Enhancements You Can Add
1. **Custom pin icons** for different detection types
2. **Pin clustering** for many detections
3. **Pin info window** with timestamp, image thumbnail
4. **Filter pins** by type or time range
5. **Export detections** to KML/GeoJSON
6. **Backend sync** to persist across devices

### Windows Testing
Same flow works on Windows with:
- Mock location service or GPS dongle
- Webcam for live detection
- Same real-time pin updates

## Architecture Benefits
- **Decoupled**: Detection logic doesn't know about map
- **Reactive**: Map updates automatically via events
- **Scalable**: Easy to add more subscribers (notifications, stats, etc.)
- **Testable**: Can inject mock repository with events

---

**Status**: ✅ Implemented and deployed to Android emulator
**Last Updated**: October 20, 2025
