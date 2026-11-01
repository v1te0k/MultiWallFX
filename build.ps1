$frameworkDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$csc = Join-Path $frameworkDir "csc.exe"
$wpfDir = Join-Path $frameworkDir "WPF"

$currentDir = $PSScriptRoot
$outputExe = Join-Path $currentDir "MultiWallFX.exe"
$sourceFiles = @(
    (Join-Path $currentDir "Config.cs"),
    (Join-Path $currentDir "WallpaperManager.cs"),
    (Join-Path $currentDir "LockScreenManager.cs"),
    (Join-Path $currentDir "FullscreenDetector.cs"),
    (Join-Path $currentDir "StartupHelper.cs"),
    (Join-Path $currentDir "TransitionEngine.cs"),
    (Join-Path $currentDir "MainWindow.cs"),
    (Join-Path $currentDir "App.cs")
)

$references = @(
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Web.Extensions.dll",
    "/r:PresentationCore.dll",
    "/r:PresentationFramework.dll",
    "/r:WindowsBase.dll",
    "/r:System.Xaml.dll"
)

$iconPath = Join-Path $currentDir "app.ico"
$argsList = @(
    "/target:winexe",
    "/optimize+",
    "/platform:anycpu",
    "/win32icon:$iconPath",
    "/lib:$wpfDir",
    "/out:$outputExe"
) + $references + $sourceFiles

Write-Host "Compiling MultiWall FX in $currentDir..." -ForegroundColor Cyan
& $csc $argsList

if (Test-Path $outputExe) {
    Write-Host "Build SUCCESS: $outputExe" -ForegroundColor Green
} else {
    Write-Host "Build FAILED!" -ForegroundColor Red
}
