param([string]$ProjectRoot=(Get-Location).Path)
$ErrorActionPreference='Stop'
$folder=Join-Path $ProjectRoot 'Backups/CiudadDelSol/Interior/Etapa4_20261117'
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$ok,[string]$message){if(!$ok){throw ('FALLO: '+$message)};$checks.Add($message)}
$manifest=Get-Content (Join-Path $folder 'Antes_SHA256.json') -Raw|ConvertFrom-Json
foreach($p in $manifest){Check ((Get-FileHash -LiteralPath (Join-Path $ProjectRoot $p.path) -Algorithm SHA256).Hash -eq $p.sha256) ('Protegido intacto: '+$p.path)}
$receipt=Get-Content (Join-Path $folder 'Recepcion.json') -Raw|ConvertFrom-Json
Check ([string]::IsNullOrEmpty($receipt.error) -and $receipt.filesUnchanged -and !$receipt.sceneDirty) 'Recepción real sin cambios de escena o errores globales'
Check (!$receipt.playMode -and !$receipt.simulation) 'Edit Mode real; sin simulación de jugadores'
Check ($receipt.connections.Count -eq 2 -and (($receipt.connections.plan.id|Sort-Object)-join ',') -eq 'VS01,VS02') 'Dos conexiones autorizadas; cámara final fuera de la selección'
Check ($receipt.negativeControlsRejectObstaclesAndOtherLevels) 'Controles negativos de obstáculo, planta incorrecta y vacío rechazados'
Check ([Math]::Abs($receipt.agentRadius-.57) -lt .0001 -and [Math]::Abs($receipt.agentClimb-.36) -lt .0001) 'Humanoid 0 conserva radio y escalón'
Check ([Math]::Abs($receipt.warriorRadius-.32) -lt .0001 -and [Math]::Abs($receipt.warriorHeight-2) -lt .0001 -and [Math]::Abs($receipt.warriorStep-.30) -lt .0001) 'Dimensiones originales del Warrior conservadas'
foreach($stage in 1,2,3){
    $r=Get-Content (Join-Path $folder ('RegresionEtapa'+$stage+'/Recepcion.json')) -Raw|ConvertFrom-Json
    $count=@(1,6,3)[$stage-1]
    Check ($r.connections.Count -eq $count -and @($r.connections|Where-Object {!$_.technicalIndividual}).Count -eq 0 -and $r.filesUnchanged -and [string]::IsNullOrEmpty($r.error)) ('Regresión nativa Etapa '+$stage+': '+$count+'/'+$count)
}
$north=Get-Content (Join-Path $folder 'RodeoNorteVS01/Recepcion.json') -Raw|ConvertFrom-Json
$route=$north.connections[0]
Check ($route.guide.failures -eq 0 -and $route.guide.sweepFailures -eq 0 -and $route.guide.missingNav -eq 0) 'Rodeo norte: apoyo, huella, cápsula, barridos y NavMesh de la guía satisfactorios'
Check ($route.forward.complete -and $route.reverse.complete -and $route.forward.physical -and $route.reverse.physical -and $route.forward.blockedRaycasts -eq 0 -and $route.reverse.blockedRaycasts -eq 0) 'Rodeo norte: camino nativo físico completo en ambos sentidos'
$original=Get-Content (Join-Path $folder 'EjesOriginales/Recepcion.json') -Raw|ConvertFrom-Json
$second=$original.connections|Where-Object {$_.plan.id -eq 'VS02'}
Check ($second.forward.complete -and $second.reverse.complete -and $second.forward.physical -and $second.reverse.physical) 'Se conserva el resultado favorable del camino central de VS02'
$edges=Get-Content (Join-Path $folder 'BordesEscalerasEtapa4.json') -Raw|ConvertFrom-Json
Check ($edges.probes.Count -eq 14 -and @($edges.probes|Where-Object {$_.overflow}).Count -eq 0) 'Catorce sondas laterales nativas, sin saturación'
Check (@($edges.probes|Where-Object {$_.connection -eq 'VS01'}).Count -eq 8 -and @($edges.probes|Where-Object {$_.connection -eq 'VS02'}).Count -eq 6) 'Se cubren ambos flancos de las siete escaleras'
foreach($c in $receipt.connections){
    $risks=@($edges.probes|Where-Object {$_.connection -eq $c.plan.id -and !$_.barrier -and !$_.capsuleBlocked -and (!$_.supportBelow -or $_.drop -gt $receipt.warriorStep)})
    if($risks.Count){Check (!$c.technicalIndividual -and !$c.edgeProtectionClear) ('Las caídas reales impiden aprobar '+$c.plan.id)}
    Check (!$c.realPlayersTested) ('Sin atribución de cuatro jugadores reales: '+$c.plan.id)
}
$proposalPath=Join-Path $folder 'PropuestaBarandas_NavMesh.json'
if(Test-Path $proposalPath){
    $proposal=Get-Content $proposalPath -Raw|ConvertFrom-Json
    Check ($proposal.hypotheticalArchitecture -and !$proposal.geometryPersisted -and !$proposal.playerSimulation -and $proposal.sceneBefore -eq $proposal.sceneAfter) 'Propuesta temporal claramente separada de arquitectura existente y movimiento real'
    Check ($proposal.draftPositions.Count -eq 8 -and $proposal.samples.Count -eq 8) 'Ocho barandas propuestas y ocho muestras de huella'
}
$result=[pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');testsPassed=$checks.Count;checks=$checks;approved=@($receipt.connections|Where-Object {$_.technicalIndividual}|ForEach-Object {$_.plan.id});realPlayersTested=$false}
$result|ConvertTo-Json -Depth 4|Set-Content (Join-Path $folder 'Regresiones.json')
Write-Output ('Comprobaciones aprobadas: '+$checks.Count+'; recepción segura: '+$result.approved.Count+'/2')
