param([string]$ProjectRoot=(Get-Location).Path)
$ErrorActionPreference='Stop'
$folder=Join-Path $ProjectRoot 'Backups/CiudadDelSol/Interior/Etapa3_20261117'
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$ok,[string]$message){if(!$ok){throw ('FALLO: '+$message)};$checks.Add($message)}
$manifest=Get-Content (Join-Path $folder 'Antes_SHA256.json') -Raw|ConvertFrom-Json
foreach($p in $manifest|Where-Object {$_.path -notlike '*CiudadDelSolInteriorStageTwo.cs'}){
    Check ((Get-FileHash -LiteralPath (Join-Path $ProjectRoot $p.path) -Algorithm SHA256).Hash -eq $p.sha256) ('SHA-256 protegido: '+$p.path)
}
$previous=Get-Content (Join-Path $folder 'Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs') -Raw
$current=Get-Content (Join-Path $ProjectRoot 'Assets/ROM/Editor/CiudadDelSolInteriorStageTwo.cs') -Raw
$pattern='(?s)        static bool Ground\(.*?(?=        static void Write\()'
Check ([regex]::Match($previous,$pattern).Value -ceq [regex]::Match($current,$pattern).Value) 'Motor de apoyo, huella, cápsula, barridos, pendientes, anchura y NavMesh idéntico al receptor recibido'
$pattern='(?s)        static Plan\[\] Plans\(\).*?(?=        static bool Ground\()'
Check ([regex]::Match($previous,$pattern).Value -ceq [regex]::Match($current,$pattern).Value) 'Selección original de las seis conexiones de Etapa 2 intacta'
$selection=Get-Content (Join-Path $ProjectRoot 'Assets/ROM/Editor/CiudadDelSolInteriorStageThree.cs') -Raw
Check ($selection -notmatch 'SaveScene|BuildNavMesh|NavMeshBuilder|Instantiate\(|NavMeshQuery\(') 'Selección de Etapa 3 no construye, guarda, instancia ni crea consultas experimentales'
$original=Get-Content (Join-Path $ProjectRoot 'Docs/CiudadDelSol/Interior/Plan_20261117/Conexiones.json') -Raw|ConvertFrom-Json
$plans=[regex]::Matches($selection,'(?s)P\("(VE01|VE02|VD01)","([^"]+)","([^"]+)",(.*?)(?=\),\s*P\(|\)\s*\n\s*\};)')
Check ($plans.Count -eq 3) 'Exactamente tres selecciones documentales de Etapa 3'
$stairs=0
foreach($plan in $plans){
    $id=$plan.Groups[1].Value
    $points=@([regex]::Matches($plan.Groups[4].Value,'V\(([-\d.]+),([-\d.]+),([-\d.]+)\)')|ForEach-Object {,@([double]$_.Groups[1].Value,[double]$_.Groups[2].Value,[double]$_.Groups[3].Value)})
    $design=$original|Where-Object {$_.id -eq $id}
    Check (($points[0]-join ',') -eq ($design.points[0]-join ',') -and ($points[-1]-join ',') -eq ($design.points[-1]-join ',')) ('Extremos originales: '+$id)
    $index=0
    foreach($q in $design.points){while($index -lt $points.Count -and ($points[$index]-join ',') -ne ($q-join ',')){$index++};Check ($index -lt $points.Count) ('Punto documental conservado '+$id+': '+($q-join ','))}
    for($i=1;$i -lt $points.Count;$i++){
        $a=$points[$i-1];$b=$points[$i];$dy=$b[1]-$a[1]
        if([Math]::Abs($dy) -gt .001){$run=[Math]::Sqrt([Math]::Pow($b[0]-$a[0],2)+[Math]::Pow($b[2]-$a[2],2));Check ([Math]::Abs($dy+2) -lt .001 -and [Math]::Abs($run-6) -lt .001) ('Escalera original 2/6: '+$id);$stairs++}
    }
}
Check ($stairs -eq 9) 'Nueve tramos originales; ninguna conexión vertical ficticia'
$nativePath=Join-Path $folder 'Recepcion.json'
$nativeAvailable=Test-Path -LiteralPath $nativePath
$approved=@()
if($nativeAvailable){
    $r=Get-Content $nativePath -Raw|ConvertFrom-Json
    Check ([string]::IsNullOrEmpty($r.error) -and $r.filesUnchanged -and !$r.sceneDirty) 'Recepción nativa sin errores ni cambios de escena'
    Check (!$r.playMode -and !$r.simulation) 'Prueba nativa de Edit Mode; no simulación de jugadores'
    Check ($r.connections.Count -eq 3 -and $r.negativeControlsRejectObstaclesAndOtherLevels) 'Tres conexiones y controles negativos satisfactorios'
    $stage2=Get-Content (Join-Path $folder 'RegresionEtapa2/Recepcion.json') -Raw|ConvertFrom-Json
    Check ($stage2.filesUnchanged -and [string]::IsNullOrEmpty($stage2.error) -and $stage2.connections.Count -eq 6 -and @($stage2.connections|Where-Object {!$_.technicalIndividual}).Count -eq 0) 'Regresión nativa de Etapa 2: seis conexiones aprobadas'
    foreach($c in $r.connections){
        Check (!$c.realPlayersTested) ('No se atribuye prueba de cuatro jugadores: '+$c.plan.id)
        if($c.technicalIndividual){
            Check ($c.forward.complete -and $c.reverse.complete -and $c.forward.physical -and $c.reverse.physical -and $c.forward.blockedRaycasts -eq 0 -and $c.reverse.blockedRaycasts -eq 0 -and $c.edgeProtectionClear) ('Recepción bidireccional completa y física: '+$c.plan.id)
            $approved+=$c.plan.id
        }
    }
}
if($nativeAvailable){
    $entry=$r.connections|Where-Object {$_.plan.id -eq 'VE01'}
    $exit=$r.connections|Where-Object {$_.plan.id -eq 'VE02'}
    if($entry.technicalIndividual -and $exit.technicalIndividual){
        $a=$entry.plan.points[-1];$b=$exit.plan.points[0]
        Check ([Math]::Abs($a.x-$b.x) -lt .001 -and [Math]::Abs($a.y-$b.y) -lt .001 -and [Math]::Abs($a.z-$b.z) -lt .001) 'VE01 y VE02 comparten exactamente el mismo descanso físico'
        Check ($a.x -ge 108 -and $a.x -le 132 -and $a.z -ge -48 -and $a.z -le -36 -and [Math]::Abs($a.y+30) -lt .001) 'Ancla compartida dentro de la terraza B06 y en su planta real'
    }
}
$result=[pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');testsPassed=$checks.Count;nativeAvailable=$nativeAvailable;approved=$approved;realPlayersTested=$false;checks=$checks}
$result|ConvertTo-Json -Depth 5|Set-Content (Join-Path $folder 'Regresiones.json')
Write-Output ('Comprobaciones aprobadas: '+$checks.Count+'; evidencia nativa Etapa 3: '+$nativeAvailable+'; conexiones nativas aprobadas: '+$approved.Count+'/3')
