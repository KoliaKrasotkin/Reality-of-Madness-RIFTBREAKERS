param([string]$ProjectRoot=(Get-Location).Path)
$ErrorActionPreference='Stop'
$backup=Join-Path $ProjectRoot 'Backups/CiudadDelSol/Interior/Etapa2_20261117'
$report=Get-Content (Join-Path $backup 'Recepcion.json') -Raw|ConvertFrom-Json
$baseline=Get-Content (Join-Path $backup 'Reanudacion/Recepcion_inicial_3_de_6.json') -Raw|ConvertFrom-Json
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$ok,[string]$label){if(!$ok){throw ('FALLO: '+$label)};$checks.Add($label)}
Check ([string]::IsNullOrEmpty($report.error)) 'Recepción real sin error global'
Check (!$report.playMode -and !$report.simulation) 'Consultas reales en Edit Mode; sin simulación de movimiento'
Check ($report.filesUnchanged -and !$report.sceneDirty) 'La recepción no guardó ni ensució la escena'
Check ($report.connections.Count -eq 6) 'Se conservan seis conexiones'
$ids=@($report.connections|ForEach-Object {$_.plan.id}|Sort-Object)
Check (($ids -join ',') -eq 'H06,H12,H13,H14,VC01,VC02') 'IDs originales sin conexiones adicionales'
Check ($report.negativeControlsRejectObstaclesAndOtherLevels) 'Controles negativos de muro, cota incorrecta y vacío rechazados'
Check (!$report.activeNavMeshLinks) 'Sin enlaces activos usados para resolver raycasts ambiguos'
Check ([Math]::Abs($report.agentRadius-.57) -lt .0001 -and [Math]::Abs($report.agentClimb-.36) -lt .0001) 'Humanoid 0 conserva radio y escalón'
Check ([Math]::Abs($report.warriorRadius-.32) -lt .0001 -and [Math]::Abs($report.warriorHeight-2) -lt .0001 -and [Math]::Abs($report.warriorStep-.30) -lt .0001) 'Dimensiones y stepOffset originales del Warrior'
foreach($id in @('H06','H13','H14')){
  $old=$baseline.connections|Where-Object {$_.plan.id -eq $id}
  $current=$report.connections|Where-Object {$_.plan.id -eq $id}
  Check ($old.technicalIndividual -and $current.technicalIndividual) ('Sin regresión del enlace '+$id)
}
foreach($id in @('H12','H06','H13','H14','VC01')){
  $c=$report.connections|Where-Object {$_.plan.id -eq $id}
  Check ($c.technicalIndividual -and $c.forward.complete -and $c.reverse.complete -and $c.forward.physical -and $c.reverse.physical) ('Prueba física y NavMesh bidireccional '+$id)
  Check ($c.forward.blockedRaycasts -eq 0 -and $c.reverse.blockedRaycasts -eq 0 -and $c.edgeProtectionClear) ('Continuidad y protecciones sin omitir impactos '+$id)
  Check (!$c.realPlayersTested) ('No se confunden consultas con cuatro jugadores '+$id)
}
$h12=$report.connections|Where-Object {$_.plan.id -eq 'H12'}
Check ($h12.reverse.rawBlockedRaycasts -gt 0 -and @($h12.reverse.rayEvidence|Where-Object {$_.rawBlocked -and $_.resolved -and $_.localPathsStraight}).Count -gt 0) 'H12 conserva el impacto bruto y su prueba recíproca/local'
$vc=$report.connections|Where-Object {$_.plan.id -eq 'VC02'}
if($vc.technicalIndividual){
  Check ($vc.forward.physical -and $vc.reverse.physical -and $vc.edgeProtectionClear -and $vc.forward.blockedRaycasts -eq 0 -and $vc.reverse.blockedRaycasts -eq 0) 'VC02 solo se aprueba con la recepción nativa completa'
}else{
  $issues=@($vc.forward.trace.problems)+@($vc.reverse.trace.problems)
  Check (@($issues|Where-Object {$_.kind -ne 'CAMINO_FUERA_CONEXION_O_NIVEL'}).Count -eq 0) 'VC02 pendiente únicamente por alcance del camino global'
  Check ($vc.guide.clear -and $vc.forward.trace.failures -eq 0 -and $vc.forward.trace.sweepFailures -eq 0 -and $vc.forward.trace.missingNav -eq 0 -and $vc.forward.trace.disconnectedSurfaceSteps -eq 0) 'Se conserva la evidencia física favorable de VC02 sin forzar aprobación'
}
$manifest=Get-Content (Join-Path $backup 'Antes_SHA256.json') -Raw|ConvertFrom-Json
foreach($entry in $manifest){
  Check ((Get-FileHash -LiteralPath (Join-Path $ProjectRoot $entry.path) -Algorithm SHA256).Hash -eq $entry.sha256) ('Protegido intacto: '+$entry.path)
}
$result=@{utc=[DateTime]::UtcNow.ToString('o');testsPassed=$checks.Count;checks=$checks;nativeStarted=$report.started;approved=@($report.connections|Where-Object {$_.technicalIndividual}|ForEach-Object {$_.plan.id});pending=@($report.connections|Where-Object {!$_.technicalIndividual}|ForEach-Object {$_.plan.id});realPlayersTested=$false}
$result|ConvertTo-Json -Depth 4|Set-Content (Join-Path $backup 'Reanudacion/Regresiones.json') -Encoding utf8
Write-Output ('Comprobaciones aprobadas: '+$checks.Count+'; conexiones recibidas: '+$result.approved.Count+'/6')
