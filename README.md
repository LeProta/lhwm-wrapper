# lhwm-wrapper

_LibreHardwareMonitor bridge DLL for native (C++) consumers — used by the NZXT Kraken OpenRGB plugins._

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20(x64)-0078D6)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4)
![LHM](https://img.shields.io/badge/LibreHardwareMonitor-0.9.6-blue)
![Driver](https://img.shields.io/badge/kernel%20driver-PawnIO-2ea44f)

Fork of [OpenRGBDevelopers/lhwm-wrapper](https://gitlab.com/OpenRGBDevelopers/lhwm-wrapper) exposing [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) sensors and fan controls to native C++ code, through two artifacts:

| Artifact | Role |
|----------|------|
| **`lhwm-wrapper.dll`** | Single self-contained .NET assembly (LibreHardwareMonitorLib and its dependencies merged with ILRepack). This is the file end users install. |
| **`lhwm-cpp-wrapper.lib` + `lhwm-cpp-wrapper.h`** | C++/CLI static library linked into the consuming plugin. Loads `lhwm-wrapper.dll` from the host application folder at first call. |

Used by:

- [NZXT Kraken LCD plugin](https://github.com/LeProta/NZXTKrakenLCDPlugin) — system sensors for the LCD infographics.
- [NZXT Kraken Pump plugin](https://github.com/LeProta/NZXTKrakenPumpPlugin) — motherboard fan control + temperature sources.

Both plugins (1.1.0 for OpenRGB 1.1, and the older 1.0.x for OpenRGB 1.0rc3) work with v1.1.0 or later. v1.2.0 is recommended: it is thread-safe and cuts the hardware refreshes from about 25 to 4 per second when both plugins run.

---

## Changes vs upstream

- **LibreHardwareMonitorLib pinned to 0.9.6** (stable). 0.9.5 dropped the WinRing0 / inpoutx64 kernel drivers in favour of [PawnIO](https://pawnio.eu) — Microsoft-signed and accepted by anti-cheats. On 0.9.4 the WinRing0 driver is refused by Windows 11 24H2+ (blocklisted), LHM falls back to `inpoutx64.sys` (certificate expired in 2009) and installs it as an **auto-start** service, which blocks games such as THE FINALS from launching.
- **`SetControlDefault(identifier)`** — releases a fan control back to its default (BIOS/automatic) mode via `Control.SetDefault()`. Required by the Pump plugin to hand fans back on exit.
- **Thread-safe, rate-limited updates** — one lock around init/reads/controls (both plugins call in from their own threads), and `GetSensorValue` refreshes a hardware at most once per 250 ms instead of on every read.
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

Install **[PawnIO](https://pawnio.eu)** (2.1.0 or later) — it provides the kernel driver LHM reads the hardware through. Without it, only GPU and RAM sensors show up.

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
dotnet build lhwm-wrapper\lhwm-wrapper.csproj -c Release
```

`RuntimeIdentifier=win-x64` is set in the csproj so that NuGet drops the RID-specific
`LibreHardwareMonitorLib.dll` next to the output — without it the build succeeds but the
LHM assembly never lands in `bin\`. Output goes to `bin\Release\net472\win-x64\`.

Then merge the output into a single assembly (primary first, output name must be `lhwm-wrapper.dll`):

```bat
cd lhwm-wrapper\bin\Release\net472\win-x64
ILRepack.exe /target:library /targetplatform:v4 /out:lhwm-wrapper.dll ^
  LhwmBindingsLib.dll LibreHardwareMonitorLib.dll <remaining *.dll>
```

The merged assembly must keep the identity `lhwm-wrapper, Version=1.0.0.0, Culture=neutral,
PublicKeyToken=null` — that is what `lhwm-cpp-wrapper.lib` binds to, so plugins built against
an older build keep working without a rebuild.

Sanity check on the result — it must contain no embedded kernel driver:

```bat
findstr /m "inpoutx64 WinRing0.gz" lhwm-wrapper.dll
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
- [PawnIO](https://pawnio.eu) — kernel driver used by LHM 0.9.5+.
- [OpenRGBDevelopers/lhwm-wrapper](https://gitlab.com/OpenRGBDevelopers/lhwm-wrapper) — upstream project (OpenRGB Hardware Sync plugin dependency).
