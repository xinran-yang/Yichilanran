param([switch]$IncludeEditor)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
$unityData='E:/Unity/Editor/6000.3.28f1/Editor/Data'
$baseline=Get-ChildItem Library/Bee/artifacts -Filter 'Assembly-CSharp.rsp' -Recurse | Select-Object -First 1
if(!$baseline){ throw 'Open the project once to generate Unity compiler response files.' }
New-Item -ItemType Directory -Force DevReports/Compile | Out-Null
$base=Get-Content $baseline.FullName | Where-Object { $_ -notmatch '^(-out:|-refout:)' -and $_ -notmatch '^"?Assets[/\\].*\.cs"?$' }
$lines=@($base)+@('-out:"DevReports/Compile/Assembly-CSharp.dll"','-refout:"DevReports/Compile/Assembly-CSharp.ref.dll"','"Assets/TestConnection.cs"')
$lines+=Get-ChildItem Assets/_Game/Scripts -Filter '*.cs' -Recurse | ForEach-Object { '"'+[IO.Path]::GetRelativePath($projectRoot,$_.FullName).Replace('\','/')+'"' }
$lines | Set-Content DevReports/Compile/check.rsp -Encoding utf8
Set-Content DevReports/Compile/compiler.log 'Runtime assembly'
& ($unityData+'/NetCoreRuntime/dotnet.exe') ($unityData+'/DotNetSdkRoslyn/csc.dll') '@DevReports/Compile/check.rsp' 2>&1 | Tee-Object -FilePath DevReports/Compile/compiler.log -Append
$code=$LASTEXITCODE
'Runtime ExitCode: '+$code | Add-Content DevReports/Compile/compiler.log
if($code -ne 0){ exit $code }
if($IncludeEditor){
    $editor=@($base)+@('-out:"DevReports/Compile/Assembly-CSharp-Editor.dll"','-r:"DevReports/Compile/Assembly-CSharp.dll"')
    $editor+=Get-ChildItem Assets/_Game/Editor -Filter '*.cs' -Recurse | ForEach-Object { '"'+[IO.Path]::GetRelativePath($projectRoot,$_.FullName).Replace('\','/')+'"' }
    $editor | Set-Content DevReports/Compile/editor-check.rsp -Encoding utf8
    & ($unityData+'/NetCoreRuntime/dotnet.exe') ($unityData+'/DotNetSdkRoslyn/csc.dll') '@DevReports/Compile/editor-check.rsp' 2>&1 | Tee-Object -FilePath DevReports/Compile/compiler.log -Append
    $code=$LASTEXITCODE
    'Editor ExitCode: '+$code | Add-Content DevReports/Compile/compiler.log
}
exit $code
