# Verifies a freshly merged lhwm-wrapper.dll:
#   1. embeds no kernel driver (the inpoutx64 / WinRing0 regression that gets games banned)
#   2. keeps the assembly identity lhwm-cpp-wrapper.lib binds to
#   3. actually reads CPU / motherboard sensors through PawnIO
# Run from Windows PowerShell 5.1 (net472 host), elevated.
#   powershell -ExecutionPolicy Bypass -File smoke-test.ps1 [-Dll <path>]
param(
    [string]$Dll = "$PSScriptRoot\lhwm-wrapper\bin\Release\net472\win-x64\lhwm-wrapper.dll"
)
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Dll)) { throw "not found: $Dll" }

# 1. no embedded driver
$raw = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($Dll))
foreach ($needle in 'inpoutx64', 'WinRing0.gz', 'WinRing0x64.gz', 'Ring0x64.gz') {
    if ($raw.Contains($needle)) { throw "FAIL: '$needle' embedded -- LHM fell back to a blocklisted kernel driver" }
}
Write-Host "OK  no embedded kernel driver"

# 2. assembly identity (lhwm-cpp-wrapper.lib has this baked in)
$expected = 'lhwm-wrapper, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
$actual = [System.Reflection.AssemblyName]::GetAssemblyName($Dll).FullName
if ($actual -ne $expected) { throw "FAIL: identity is '$actual', expected '$expected' -- plugins would need a rebuild" }
Write-Host "OK  identity $actual"

# 3. sensors
[void][System.Reflection.Assembly]::LoadFrom($Dll)
$w = [LHWMWrapperNameSpace.LHWMWrapper]::GetInstance()
$map = $w.GetHardwareSensorMap()
$all = $map.Values | ForEach-Object { $_ }

$cpuTemp = $all | Where-Object { $_.Item2 -eq 'Temperature' -and $_.Item3 -like '*cpu*' } | Select-Object -First 1
if (-not $cpuTemp) { throw "FAIL: no CPU temperature sensor -- PawnIO missing, or not running elevated" }
$v = $w.GetSensorValue($cpuTemp.Item3)
if ($v -le 0) { throw "FAIL: CPU temperature reads $v" }
Write-Host ("OK  CPU {0} = {1} C" -f $cpuTemp.Item1, [math]::Round($v, 1))

$controls = $all | Where-Object { $_.Item2 -eq 'Control' -and $_.Item3 -like '/lpc/*' }
if (-not $controls) { throw "FAIL: no motherboard fan control -- the Pump plugin would show no channel" }
Write-Host ("OK  {0} motherboard fan control(s)" -f @($controls).Count)

Write-Host "`nPASS"
