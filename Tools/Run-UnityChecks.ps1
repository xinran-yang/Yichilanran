param(
    [string]$UnityPath='E:/Unity/Editor/6000.3.28f1/Editor/Unity.exe',
    [switch]$PublishGenerated
)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$stamp=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6)
$checkRoot=Join-Path $projectRoot ('DevReports/UnityCheck-'+$stamp)
New-Item -ItemType Directory -Path $checkRoot -Force | Out-Null
foreach($name in @('Assets','Packages','ProjectSettings')){
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $checkRoot -Recurse
}
$logPath=Join-Path $checkRoot 'ReferenceDemo-Final.log'
$unityArgs=@('-batchmode','-nographics','-projectPath',('"'+$checkRoot+'"'),'-executeMethod','YiChiLanRan.Editor.RealmCorrectionChecks.FinalizeDemo','-logFile',('"'+$logPath+'"'))
$process=Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
foreach($file in Get-ChildItem (Join-Path $checkRoot 'DevReports') -File -ErrorAction SilentlyContinue){
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $projectRoot 'DevReports') -Force
}
Copy-Item -LiteralPath $logPath -Destination (Join-Path $projectRoot 'DevReports/ReferenceDemo-Final.log') -Force
Set-Content (Join-Path $projectRoot 'DevReports/check-project-path.txt') $checkRoot
if($process.ExitCode -ne 0){
    Write-Output ('Unity checks failed. Inspect '+$logPath)
    exit $process.ExitCode
}
if($PublishGenerated){
    foreach($name in @('Data','Prefabs','Scenes')){
        Copy-Item -LiteralPath (Join-Path $checkRoot ('Assets/_Game/'+$name)) -Destination (Join-Path $projectRoot 'Assets/_Game') -Recurse -Force
    }
    foreach($meta in Get-ChildItem (Join-Path $checkRoot 'Assets') -Filter '*.meta' -Recurse){
        $relative=[IO.Path]::GetRelativePath($checkRoot,$meta.FullName)
        $target=Join-Path $projectRoot $relative
        if(!(Test-Path -LiteralPath $target)){ Copy-Item -LiteralPath $meta.FullName -Destination $target }
    }
    Write-Output 'Generated scenes/data/prefabs published. In Unity run Tools > YiChiLanRan > 4 Configure demo player to configure build scenes and window settings.'
}
Write-Output ('All Unity checks passed. Reports: '+(Join-Path $projectRoot 'DevReports'))
Write-Output ('Retained isolated project: '+$checkRoot)




