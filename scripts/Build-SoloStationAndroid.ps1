param(
    [ValidatePattern('^[A-Z]$')]
    [string]$DriveLetter = 'V',
    [string]$EditorPath = 'D:\Unity\Editors\6000.3.24f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$drive = "${DriveLetter}:"
$mappedRoot = "$drive\"
$logPathReal = Join-Path $project 'Logs\SoloStationAndroidBuild.log'

if (-not (Test-Path -LiteralPath $EditorPath)) {
    throw "Unity Editor not found: $EditorPath"
}
if (Test-Path -LiteralPath $mappedRoot) {
    throw "Drive $drive is already in use. Choose another letter with -DriveLetter."
}

& subst $drive $project
if ($LASTEXITCODE -ne 0) { throw "Could not map $drive to the VR project." }

try {
    $logPath = Join-Path $mappedRoot 'Logs\SoloStationAndroidBuild.log'
    $arguments = @(
        '-batchmode', '-nographics', '-quit', '-buildTarget', 'Android',
        '-projectPath', $mappedRoot,
        '-executeMethod', 'CourseSetup.SoloStationBuilder.BuildAndroidApk',
        '-logFile', $logPath
    )
    $process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "APK build failed (exit $($process.ExitCode)). Inspect $logPathReal."
    }
    $apk = Join-Path $project 'Builds\SoloSpaceStation.apk'
    if (-not (Test-Path -LiteralPath $apk)) {
        throw "Unity returned success, but the APK is missing: $apk"
    }
    Write-Host "APK: $apk"
}
finally {
    & subst $drive /D
    if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove temporary drive mapping $drive." }
}
