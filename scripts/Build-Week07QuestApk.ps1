param(
    [ValidatePattern('^[A-Z]$')]
    [string]$DriveLetter = 'V',
    [string]$EditorPath = 'D:\Unity\Editors\6000.3.24f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$drive = "${DriveLetter}:"
$mappedRoot = "$drive\"
$logPathReal = Join-Path $project 'Logs\Week07_QuestBuild.log'

if (-not (Test-Path -LiteralPath $EditorPath)) { throw "Unity Editor not found: $EditorPath" }
if (Test-Path -LiteralPath $mappedRoot) {
    throw "Drive $drive is already in use. Choose another letter with -DriveLetter."
}

& subst $drive $project
if ($LASTEXITCODE -ne 0) { throw "Could not map $drive to the VR project." }

try {
    $logPath = Join-Path $mappedRoot 'Logs\Week07_QuestBuild.log'
    $arguments = @(
        '-batchmode', '-nographics', '-quit', '-buildTarget', 'Android',
        '-projectPath', $mappedRoot,
        '-executeMethod', 'CourseSetup.Week06TeamUIBuilder.BuildAndroidApk',
        '-logFile', $logPath
    )
    $process = Start-Process -FilePath $EditorPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
    # Wait for the Editor itself; Android/Hub background helpers may outlive it.
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Week 07 APK build failed (exit $($process.ExitCode)). Inspect $logPathReal."
    }
    $apk = Join-Path $project 'Builds\Wang_VR_Week07.apk'
    if (-not (Test-Path -LiteralPath $apk)) {
        throw "Unity returned success, but the APK is missing: $apk"
    }
    $deliverables = Join-Path $project 'Deliverables\Week07'
    New-Item -ItemType Directory -Force -Path $deliverables | Out-Null
    $copy = Join-Path $deliverables 'Wang_VR_Week07.apk'
    Copy-Item -LiteralPath $apk -Destination $copy -Force
    if ((Get-FileHash -LiteralPath $apk).Hash -ne (Get-FileHash -LiteralPath $copy).Hash) {
        throw 'Week 07 deliverable APK differs from the Unity build output.'
    }
    Write-Host "APK: $apk"
    Write-Host "Course deliverable: $copy"
}
finally {
    & subst $drive /D
    if ($LASTEXITCODE -ne 0) { Write-Warning "Could not remove temporary drive mapping $drive." }
}
