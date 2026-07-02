# lhwm-wrapper

_LibreHardwareMonitor bridge DLL for native (C++) consumers — used by the NZXT Kraken OpenRGB plugins._

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20(x64)-0078D6)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4)
![LHM](https://img.shields.io/badge/LibreHardwareMonitor-0.9.4-blue)

Fork of [OpenRGBDevelopers/lhwm-wrapper](https://gitlab.com/OpenRGBDevelopers/lhwm-wrapper) exposing [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) sensors and fan controls to native C++ code, through two artifacts:

| Artifact | Role |
|----------|------|
| **`lhwm-wrapper.dll`** | Single self-contained .NET assembly (LibreHardwareMonitorLib and its dependencies merged with ILRepack). This is the file end users install. |
| **`lhwm-cpp-wrapper.lib` + `lhwm-cpp-wrapper.h`** | C++/CLI static library linked into the consuming plugin. Loads `lhwm-wrapper.dll` from the host application folder at first call. |

Used by:

- [NZXT Kraken LCD plugin](https://github.com/LeProta/NZXTKrakenLCDPlugin) — system sensors for the LCD infographics.
- [NZXT Kraken Pump plugin](https://github.com/LeProta/NZXTKrakenPumpPlugin) — motherboard fan control + temperature sources.

---

## Changes vs upstream

- **LibreHardwareMonitorLib pinned to 0.9.4** (stable). The 0.9.6 pre-releases fail to initialize the kernel sensor driver (no motherboard chip, CPU temperatures stuck at 0).
- **`SetControlDefault(identifier)`** — releases a fan control back to its default (BIOS/automatic) mode via `Control.SetDefault()`. Required by the Pump plugin to hand fans back on exit.
- **`GetReport()`** — full LibreHardwareMonitor diagnostic report, useful for debugging sensor enumeration.

## API (`lhwm-cpp-wrapper.h`)

```cpp
class LHWM
{
public:
    // hardware -> [(sensor name, sensor type, identifier), ...]
    static std::map<std::string, std::vector<std::tuple<std::string, std::string, std::string>>> GetHardwareSensorMap();
    static float       GetSensorValue(std::string identifier);
    static void        SetControlValue(std::string identifier, float value);   // duty %
    static void        SetControlDefault(std::string identifier);              // back to BIOS/auto
    static std::string GetReport();
};
```

Identifiers are LibreHardwareMonitor paths (`/lpc/nct6799d/0/control/0`, `/amdcpu/0/temperature/2`, …).

---

## Installation (end users)

Download **`lhwm-wrapper.dll`** from the [Releases](https://github.com/LeProta/lhwm-wrapper/releases) page and place it **next to `OpenRGB.exe`**. Both NZXT Kraken plugins share this single file.

Run OpenRGB **as Administrator** for motherboard/CPU sensors — the kernel driver needs it.

---

## Building from source

Two parts, built separately.

### Requirements

| Tool | Notes |
|------|-------|
| Visual Studio Build Tools 2022 | *Desktop development with C++* + *.NET Framework 4.7.2 SDK* (v143 toolset) |
| .NET SDK | for `dotnet build` of the C# project |
| ILRepack | `nuget install ILRepack` (used manually) |

### 1. Managed assembly (`lhwm-wrapper.dll`)

```bat
dotnet build lhwm-wrapper -c Release -p:Platform=x64
```

Then merge the output into a single assembly (primary first, output name must be `lhwm-wrapper.dll`):

```bat
ILRepack.exe /out:lhwm-wrapper.dll LhwmBindingsLib.dll LibreHardwareMonitorLib.dll <remaining bin\*.dll>
```

### 2. Native bridge (`lhwm-cpp-wrapper.lib`)

From an **x64 Native Tools Command Prompt for VS**:

```bat
msbuild lhwm-cpp-wrapper\lhwm-cpp-wrapper.sln /p:Configuration=Release /p:Platform=x64 ^
  /p:PlatformToolset=v143 /p:CLRSupport=true /p:TargetFrameworkVersion=v4.7.2
```

The static library is compiled with `/MD` — consuming plugins must build Release with the same runtime, otherwise `LNK2038`.

---

## Acknowledgements & licenses

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) — MPL-2.0.
- [OpenRGBDevelopers/lhwm-wrapper](https://gitlab.com/OpenRGBDevelopers/lhwm-wrapper) — upstream project (OpenRGB Hardware Sync plugin dependency).
- NvAPIWrapper.Net — MIT.
