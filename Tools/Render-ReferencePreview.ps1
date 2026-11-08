param([string]$DataDirectory,[string]$OutputDirectory='DevReports/ReferenceLayouts')
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$ink=[Drawing.Color]::FromArgb(9,59,97); $paper=[Drawing.Color]::FromArgb(250,250,245)
$ppu=900.0/15.2; $minX=12-7.6*16/9; $minY=-1.6
function RectPx($x,$y,$w,$h){ [Drawing.RectangleF]::new([single](($x-$minX)*$ppu),[single](900-($y-$minY+$h)*$ppu),[single]($w*$ppu),[single]($h*$ppu)) }
foreach($id in @('2.1','2.2')){
    $d=Get-Content (Join-Path $DataDirectory ('Reference-'+$id+'.json')) -Raw | ConvertFrom-Json
    foreach($mirrored in @($false,$true)){
    $bmp=[Drawing.Bitmap]::new(1600,900); $g=[Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode='AntiAlias'; $g.Clear($paper)
    if($mirrored){ $brush=[Drawing.SolidBrush]::new($ink); $g.FillRectangle($brush,(RectPx .1 .1 23.8 11.8)); $brush.Dispose() }
    $grid=[Drawing.Pen]::new([Drawing.Color]::FromArgb(40,120,155,175),1)
    for($x=0;$x -le 24;$x++){ $r=RectPx $x 0 0 12; $g.DrawLine($grid,$r.X,$r.Y,$r.X,($r.Y+$r.Height)) }
    for($y=0;$y -le 12;$y++){ $r=RectPx 0 $y 24 0; $g.DrawLine($grid,$r.X,$r.Y,($r.X+$r.Width),$r.Y) }
    foreach($t in $d.terrain | Sort-Object phase){
        if(!$mirrored -and $t.phase -eq 1){continue}
        $y=if($mirrored){2*$d.mirrorAxis-$t.rect.y-$t.rect.height}else{$t.rect.y}
        $r=RectPx $t.rect.x $y $t.rect.width $t.rect.height
        $color=if($t.phase -eq 1){$paper}else{$ink}; $brush=[Drawing.SolidBrush]::new($color); $g.FillRectangle($brush,$r); $brush.Dispose()
        if($t.phase -eq 1){ $pen=[Drawing.Pen]::new([Drawing.Color]::FromArgb(110,70,110,135),1); $g.DrawRectangle($pen,$r.X,$r.Y,$r.Width,$r.Height); $pen.Dispose() }
    }
    $font=[Drawing.Font]::new('Arial',22,[Drawing.FontStyle]::Bold)
    foreach($b in $d.boxes){
        $y=if($mirrored){2*$d.mirrorAxis-$b.center.y}else{$b.center.y}
        $r=RectPx ($b.center.x-.5) ($y-.5) 1 1; $outer=[Drawing.SolidBrush]::new($(if($b.phase -eq 0){$paper}else{$ink})); $g.FillRectangle($outer,$r); $outer.Dispose()
        $r.Inflate(-4,-4); $fill=[Drawing.SolidBrush]::new($(if($b.phase -eq 0){$ink}else{$paper})); $g.FillRectangle($fill,$r);$fill.Dispose()
        $txt=[Drawing.SolidBrush]::new($(if($b.phase -eq 0){$paper}else{$ink})); $g.DrawString(($b.id -replace 'Box ',''),$font,$txt,($r.X+12),($r.Y+9));$txt.Dispose()
    }
    foreach($z in $d.zones){
        $y=if($mirrored){2*$d.mirrorAxis-$z.floor}else{$z.floor}
        $r=RectPx $z.x ($y-.035) $z.width .10; $brush=[Drawing.SolidBrush]::new($(if($z.green){[Drawing.Color]::LimeGreen}else{[Drawing.Color]::Gold})); $g.FillRectangle($brush,$r);$brush.Dispose()
    }
    $blue=[Drawing.SolidBrush]::new($ink)
    if(!$mirrored){ $hero=RectPx ($d.spawn.x-.28) ($d.spawn.y-.38) .56 .76; $g.FillRectangle($blue,$hero) }
    $y=if($mirrored){2*$d.mirrorAxis-$d.goal.y}else{$d.goal.y}; $sign=if($mirrored){-1}else{1}
    $gx=[single](($d.goal.x-$minX)*$ppu); $gy=[single](900-($y-$minY)*$ppu); $pen=[Drawing.Pen]::new($ink,4)
    $g.DrawLine($pen,$gx,($gy+32*$sign),$gx,($gy-32*$sign)); $g.FillPolygon($blue,[Drawing.PointF[]]@([Drawing.PointF]::new($gx,$gy-32*$sign),[Drawing.PointF]::new($gx+38,$gy-15*$sign),[Drawing.PointF]::new($gx,$gy)))
    $title=[Drawing.Font]::new('Microsoft YaHei',20,[Drawing.FontStyle]::Bold); $g.DrawString(('一尺蓝染  '+$id+'  新图结构预览（非 Unity 截图）'),$title,$blue,90,30)
    $g.DrawString('24 × 12 world units   |   player 0.56 × 0.76   |   box 1 × 1', [Drawing.Font]::new('Arial',13),$blue,90,66)
    $suffix=if($mirrored){'-White'}else{''}
    $path=Join-Path (Resolve-Path $OutputDirectory) ('Level-'+$id+$suffix+'.png'); $bmp.Save($path,[Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose();$bmp.Dispose();$blue.Dispose();$pen.Dispose();$font.Dispose();$title.Dispose();$grid.Dispose()
    }
}
