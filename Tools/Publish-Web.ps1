param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe'
$buildFolder = Join-Path $projectRoot 'Builds\WebGL'
$webAssets = Join-Path $projectRoot 'Build'

function Invoke-Checked {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}

Push-Location $projectRoot
try {
    if (-not $SkipBuild) {
        $log = Join-Path $projectRoot 'Builds\web-build.log'
        New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Builds') -Force | Out-Null
        $arguments = '-batchmode -quit -buildTarget WebGL -projectPath "{0}" -executeMethod ArenaWebBuild.Build -logFile "{1}"' -f $projectRoot, $log
        $process = Start-Process -FilePath $unityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "Unity build failed. Read $log" }
    }
    if (-not (Test-Path (Join-Path $buildFolder 'build-info.json'))) { throw 'Build metadata is missing.' }
    $info = Get-Content (Join-Path $buildFolder 'build-info.json') -Raw | ConvertFrom-Json
    Invoke-Checked python @('Tools/Verify-WebBuild.py', $buildFolder)
    # Only the generated site asset folder may be replaced. Unity source is in Assets/.
    if (Test-Path -LiteralPath $webAssets) {
        $resolvedAssets = (Resolve-Path -LiteralPath $webAssets).Path
        if ($resolvedAssets -ne (Join-Path $projectRoot 'Build')) { throw 'Unexpected web asset directory.' }
        Remove-Item -LiteralPath $resolvedAssets -Recurse -Force
    }
    Copy-Item -LiteralPath (Join-Path $buildFolder 'Build') -Destination $webAssets -Recurse
    foreach ($file in @('index.html', 'build-info.json', '.nojekyll')) {
        Copy-Item -LiteralPath (Join-Path $buildFolder $file) -Destination (Join-Path $projectRoot $file) -Force
    }
    if (Test-Path (Join-Path $buildFolder 'StreamingAssets')) {
        Copy-Item -LiteralPath (Join-Path $buildFolder 'StreamingAssets') -Destination $projectRoot -Recurse -Force
    }
    Invoke-Checked git @('add','--','Assets','Packages','ProjectSettings','Tools','Docs','README.md','.gitignore','LegacyWeb','Build','index.html','build-info.json','.nojekyll')
    & git diff --cached --quiet
    if ($LASTEXITCODE -eq 1) { Invoke-Checked git @('commit','-m',"Publish Unity pixel-art playtest $($info.version)") }
    elseif ($LASTEXITCODE -ne 0) { throw 'Could not inspect staged changes.' }
    Invoke-Checked git @('push','origin','main')
    Write-Host "Published $($info.version): https://lnwzavbvb-tech.github.io/My-game/"
    Write-Host 'GitHub Pages may take a few minutes to finish deployment.'
}
finally { Pop-Location }
