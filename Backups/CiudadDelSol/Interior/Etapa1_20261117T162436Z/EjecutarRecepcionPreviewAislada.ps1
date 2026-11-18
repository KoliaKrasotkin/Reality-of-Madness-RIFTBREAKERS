$ErrorActionPreference = 'Stop'
$stageRoot = 'C:\Users\juang\Desktop\Git\Reality-of-Madness-RIFTBREAKERS'
$stageUnity = 'C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe'
$stageMethod = 'ROM.EditorTools.CiudadDelSolInteriorStageOne.BatchInspect'
$stageBackup = 'C:\Users\juang\Desktop\Git\Reality-of-Madness-RIFTBREAKERS\Backups\CiudadDelSol\Interior\Etapa1_20261117T162436Z'
$stageOut = Join-Path $stageBackup 'RecepcionPreviewAislada'
New-Item -ItemType Directory -Path $stageOut -Force | Out-Null
$active = @(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -like ('*' + $stageRoot + '*') })
if ($active.Count -gt 0) { throw 'Existe una ejecución Unity del proyecto. No se inicia otra.' }
$env:ROM_CITY_STAGE1_OUT = $stageOut
$stageArgs = @('-batchmode','-nographics','-projectPath',('"'+$stageRoot+'"'),'-executeMethod',$stageMethod,'-logFile',('"'+(Join-Path $stageOut 'Unity.log')+'"'))
$stageProcess = Start-Process -FilePath $stageUnity -ArgumentList $stageArgs -PassThru -WindowStyle Hidden
$stageStarted = $stageProcess.StartTime.ToUniversalTime()
$stageDeadline = $stageStarted.AddSeconds(180)
@{pid=$stageProcess.Id;startedUTC=$stageStarted.ToString('o');deadlineUTC=$stageDeadline.ToString('o');method=$stageMethod;project=$stageRoot;unity=$stageUnity} | ConvertTo-Json | Set-Content (Join-Path $stageOut 'Proceso.json') -Encoding utf8
Write-Output ('Unity PID=' + $stageProcess.Id + '; inicio=' + $stageStarted.ToString('o') + '; límite=180s')
$stageTimedOut = $false
while (-not $stageProcess.HasExited) {
  if ([DateTime]::UtcNow -ge $stageDeadline) {
    $record = Get-CimInstance Win32_Process -Filter ('ProcessId=' + $stageProcess.Id)
    if ($null -ne $record) {
      if ($record.ExecutablePath -ne $stageUnity -or $record.CommandLine -notlike ('*' + $stageMethod + '*') -or $record.CommandLine -notlike ('*' + $stageRoot + '*')) { throw 'PID no coincide con el ensayo: no se detiene.' }
      Stop-Process -Id $stageProcess.Id -Force
      $stageTimedOut = $true
      Write-Output ('TIMEOUT: detenido exclusivamente PID ' + $stageProcess.Id + ' por superar 180 s desde su inicio.')
    }
    break
  }
  Start-Sleep -Seconds 1
  $stageProcess.Refresh()
}
$stageProcess.Refresh()
@{pid=$stageProcess.Id;timedOut=$stageTimedOut;finishedUTC=[DateTime]::UtcNow.ToString('o');exitCode=if($stageProcess.HasExited){$stageProcess.ExitCode}else{$null}} | ConvertTo-Json | Set-Content (Join-Path $stageOut 'Finalizacion.json') -Encoding utf8
Get-Content (Join-Path $stageOut 'Finalizacion.json')
if (Test-Path (Join-Path $stageOut 'Recepcion.json')) { Write-Output ('Informe: ' + (Join-Path $stageOut 'Recepcion.json')) }

