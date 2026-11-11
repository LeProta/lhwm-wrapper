# lhwm-wrapper

Gives native C++ code access to [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) sensors and fan controls. It is the sensor backend of the NZXT Kraken [LCD](https://github.com/LeProta/NZXTKrakenLCDPlugin) and [Pump](https://github.com/LeProta/NZXTKrakenPumpPlugin) plugins for OpenRGB.

[![Release](https://img.shields.io/github/v/release/LeProta/lhwm-wrapper)](https://github.com/LeProta/lhwm-wrapper/releases)
![LibreHardwareMonitor](https://img.shields.io/badge/LibreHardwareMonitor-0.9.6-blue)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-lightgrey)

Fork of [OpenRGBDevelopers/lhwm-wrapper](https://gitlab.com/OpenRGBDevelopers/lhwm-wrapper), the version used by the OpenRGB Hardware Sync plugin.

## Versions

| lhwm-wrapper | Used by |
|--------------|---------|
| 1.2.0 | LCD 1.1.0 and Pump 1.1.0 (OpenRGB 1.1), also works with the 1.0.x plugins |
| 1.1.0 | LCD 1.0.6 and Pump 1.0.8 (OpenRGB 1.0rc3) |

1.2.0 is recommended with both plugins: it is thread-safe and refreshes the hardware about 4 times per second instead of 25.

## What's in it

The repository builds two things:

- `lhwm-wrapper.dll`: a .NET assembly with LibreHardwareMonitorLib and its dependencies merged into one file. This is what users install.
- `lhwm-cpp-wrapper.lib` and `lhwm-cpp-wrapper.h`: a small C++/CLI static library that plugins link against. It loads `lhwm-wrapper.dll` from the application folder on the first call.

Changes compared to upstream:

- LibreHardwareMonitorLib 0.9.6, which talks to the hardware through [PawnIO](https://pawnio.eu). Older versions used WinRing0, which Windows 11 24H2 blocks, and then fell back to `inpoutx64.sys`, a driver with a certificate that expired in 2009, installed as an auto-start service. That service stops some games from launching (THE FINALS for example).
- `SetControlDefault()` hands a fan control back to the BIOS or automatic mode. The Pump plugin uses it when it unloads.
- `GetReport()` returns the full LibreHardwareMonitor report, handy to debug sensor detection.
- Thread-safe: one lock covers initialization, reads and fan controls, since both plugins call in from their own threads.
- A piece of hardware is refreshed at most once every 250 ms, instead of on every value read.

## Installation

1. Download `lhwm-wrapper.dll` from the [releases page](https://github.com/LeProta/lhwm-wrapper/releases) and put it next to `OpenRGB.exe`. The LCD and Pump plugins share this one file.
2. Install [PawnIO](https://pawnio.eu) 2.1.0 or newer. Without it, only GPU and RAM sensors show up.
3. Run OpenRGB as administrator to get the CPU and motherboard sensors.

The copy of `lhwm-wrapper.dll` that comes with the Hardware Sync plugin is the upstream one: it does not expose motherboard fan control, so replace it with this one.

## Troubleshooting

| Problem | What to try |
|---------|-------------|
| A plugin tab says "Cannot load the plugin" | `lhwm-wrapper.dll` is not next to `OpenRGB.exe`. |
| No CPU or motherboard sensors | Install PawnIO, run OpenRGB as administrator, and check that Windows is not blocking the driver (Memory Integrity, vulnerable driver blocklist). Close HWiNFO and Ryzen Master too. |
| No motherboard fans in the Pump plugin | You are using the upstream DLL from the Hardware Sync plugin. Use the one from this repository. |

## API

```cpp
class LHWM
{
public:
    // hardware -> [(sensor name, sensor type, identifier), ...]
    static std::map<std::string, std::vector<std::tuple<std::string, std::string, std::string>>> GetHardwareSensorMap();
    static float       GetSensorValue(std::string identifier);
    static void        SetControlValue(std::string identifier, float value);   // duty in %
    static void        SetControlDefault(std::string identifier);              // back to BIOS/auto
    static std::string GetReport();
};
```

Identifiers are LibreHardwareMonitor paths, for example `/lpc/nct6799d/0/control/0` or `/amdcpu/0/temperature/2`. A value can be up to 250 ms old.

## Building from source

You need:

- Visual Studio Build Tools 2022 with the C++ desktop workload and the .NET Framework 4.7.2 SDK (v143 toolset)
- the .NET SDK, for `dotnet build`
- [ILRepack](https://www.nuget.org/packages/ILRepack), for example `nuget install ILRepack`

### 1. The .NET assembly

```bat
dotnet build lhwm-wrapper\lhwm-wrapper.csproj -c Release
```

The csproj sets `RuntimeIdentifier=win-x64`. Without it the build succeeds but `LibreHardwareMonitorLib.dll` never lands in the output folder, `lhwm-wrapper\bin\Release\net472\win-x64\`.

Then merge everything into `lhwm-wrapper.dll`, with `LhwmBindingsLib.dll` first:

```bat
cd lhwm-wrapper\bin\Release\net472\win-x64
ILRepack.exe /target:library /targetplatform:v4 /out:lhwm-wrapper.dll ^
  LhwmBindingsLib.dll LibreHardwareMonitorLib.dll <every other .dll in the folder>
```

The merged file must keep the identity `lhwm-wrapper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null`. That is what `lhwm-cpp-wrapper.lib` binds to, so plugins built against an older wrapper keep working without a rebuild.

`smoke-test.ps1` checks the result: no embedded kernel driver, the right identity, and real CPU and motherboard readings through PawnIO. Run it from an elevated Windows PowerShell 5.1:

```bat
powershell -ExecutionPolicy Bypass -File smoke-test.ps1
```

### 2. The native library

From an x64 Native Tools Command Prompt:

```bat
msbuild lhwm-cpp-wrapper\lhwm-cpp-wrapper.sln /p:Configuration=Release /p:Platform=x64 ^
  /p:PlatformToolset=v143 /p:CLRSupport=true /p:TargetFrameworkVersion=v4.7.2
```

The library is built with `/MD`, so the plugins that use it must be Release builds with the same runtime, otherwise the link fails with `LNK2038`.

## Credits

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL-2.0), which does all the actual work
- [PawnIO](https://pawnio.eu) for the hardware access driver
- [OpenRGBDevelopers/lhwm-wrapper](https://gitlab.com/OpenRGBDevelopers/lhwm-wrapper), the upstream project
