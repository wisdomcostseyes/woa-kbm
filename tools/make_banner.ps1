# Nexus header banner (1300x372) in the game's prompt style: cream key caps on the dark bar colour.
param([string]$Out = "$PSScriptRoot\..\dist\banner.png", [string]$FontName = "FOT-Seurat Pro B")
Add-Type -AssemblyName System.Drawing
$W = 1300; $H = 372
$Cream = [System.Drawing.Color]::FromArgb(255, 239, 231, 181)
$Dark  = [System.Drawing.Color]::FromArgb(255, 41, 24, 8)
$Bg    = [System.Drawing.Color]::FromArgb(255, 58, 38, 28)
$bmp = New-Object System.Drawing.Bitmap -ArgumentList $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'; $g.PixelOffsetMode = 'HighQuality'
# background: the game's guide-bar brown with a soft vignette
$g.Clear($Bg)
$vig = New-Object System.Drawing.Drawing2D.LinearGradientBrush((New-Object System.Drawing.Point(0, 0)), (New-Object System.Drawing.Point(0, $H)), [System.Drawing.Color]::FromArgb(0, 0, 0, 0), [System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$g.FillRectangle($vig, 0, 0, $W, $H)

function RoundRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath; $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90); $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90); $p.CloseFigure(); return $p
}
function Cap([float]$x, [float]$y, [float]$w, [float]$h, [string]$label, [bool]$round, [bool]$solid) {
    $r = if ($round) { [Math]::Min($w, $h) / 2 - 0.01 } else { [Math]::Min($w, $h) * 0.22 }
    $stroke = 7.0
    foreach ($pass in @(@(10, 14), @(5, 22))) {
        $hp = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb($pass[1], $Dark), ($stroke + $pass[0])); $hp.LineJoin = 'Round'
        $sp = RoundRect $x ($y + 3) $w $h $r; $g.DrawPath($hp, $sp); $sp.Dispose(); $hp.Dispose()
    }
    $path = RoundRect $x $y $w $h $r
    if ($solid) { $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(191, 41, 25, 11))), $path) }
    else { $pen = New-Object System.Drawing.Pen($Cream, $stroke); $pen.LineJoin = 'Round'; $g.DrawPath($pen, $path); $pen.Dispose() }
    $path.Dispose()
    # label: fit ink to ~55% height / 80% width, centred on ink bounds
    $family = New-Object System.Drawing.FontFamily($FontName)
    $em = 160.0; $tp = $null
    while ($em -ge 6) {
        $p = New-Object System.Drawing.Drawing2D.GraphicsPath
        $p.AddString($label, $family, 0, [float]$em, (New-Object System.Drawing.PointF(0, 0)), [System.Drawing.StringFormat]::GenericTypographic)
        $b = $p.GetBounds()
        if ($b.Height -le $h * 0.55 -and $b.Width -le $w * 0.78) { $tp = $p; break }
        $p.Dispose(); $em -= 2
    }
    if ($tp) {
        $b = $tp.GetBounds(); $m = New-Object System.Drawing.Drawing2D.Matrix
        $m.Translate([float]($x + $w / 2 - ($b.X + $b.Width / 2)), [float]($y + $h / 2 - ($b.Y + $b.Height / 2))); $tp.Transform($m)
        $sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(40, $Dark))
        foreach ($o in @(@(0,3),@(3,0),@(3,3),@(2,5),@(5,2))) { $mm = New-Object System.Drawing.Drawing2D.Matrix; $mm.Translate([float]$o[0], [float]$o[1]); $sp = $tp.Clone(); $sp.Transform($mm); $g.FillPath($sb, $sp); $sp.Dispose() }
        $g.FillPath((New-Object System.Drawing.SolidBrush($Cream)), $tp); $tp.Dispose()
    }
}
function Title([string]$text, [float]$cx, [float]$cy, [float]$em) {
    $family = New-Object System.Drawing.FontFamily($FontName)
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddString($text, $family, 0, [float]$em, (New-Object System.Drawing.PointF(0, 0)), [System.Drawing.StringFormat]::GenericTypographic)
    $b = $p.GetBounds(); $m = New-Object System.Drawing.Drawing2D.Matrix
    $m.Translate([float]($cx - ($b.X + $b.Width / 2)), [float]($cy - ($b.Y + $b.Height / 2))); $p.Transform($m)
    $sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70, $Dark))
    foreach ($o in @(@(0,4),@(4,0),@(4,4),@(2,6),@(6,2))) { $mm = New-Object System.Drawing.Drawing2D.Matrix; $mm.Translate([float]$o[0], [float]$o[1]); $sp = $p.Clone(); $sp.Transform($mm); $g.FillPath($sb, $sp); $sp.Dispose() }
    $g.FillPath((New-Object System.Drawing.SolidBrush($Cream)), $p); $p.Dispose()
}

# row of caps: F (solid disc, like the world prompt), Esc, Tab, Q, E, Shift, WASD cluster, arrows
$y = 70; $s = 100; $x = 44; $gap = 16
Cap $x $y $s $s 'F' $true $true;                         $x += $s + $gap
Cap $x $y $s $s 'Esc' $true $false;                      $x += $s + $gap
Cap $x $y ($s * 1.5) $s 'Tab' $false $false;             $x += $s * 1.5 + $gap
Cap $x $y $s $s 'Q' $false $false;                       $x += $s + $gap
Cap $x $y $s $s 'E' $false $false;                       $x += $s + $gap
Cap $x $y ($s * 1.7) $s 'Shift' $false $false;           $x += $s * 1.7 + $gap
# WASD cluster
$m = 46; $cx0 = $x
Cap ($cx0 + $m + 6) ($y - 2) $m $m 'W' $false $false
Cap $cx0 ($y + $m + 6) $m $m 'A' $false $false; Cap ($cx0 + $m + 6) ($y + $m + 6) $m $m 'S' $false $false; Cap ($cx0 + 2 * ($m + 6)) ($y + $m + 6) $m $m 'D' $false $false
$x += 3 * $m + 12 + $gap
# arrow cross
$cx0 = $x
Cap ($cx0 + $m + 6) ($y - 2) $m $m ([string][char]0x2191) $false $false
Cap $cx0 ($y + $m + 6) $m $m ([string][char]0x2190) $false $false; Cap ($cx0 + $m + 6) ($y + $m + 6) $m $m ([string][char]0x2193) $false $false; Cap ($cx0 + 2 * ($m + 6)) ($y + $m + 6) $m $m ([string][char]0x2192) $false $false

Title 'Keyboard & Mouse Support' ($W / 2) 262 74
Title ('with key prompts  ' + [char]0x00B7 + '  Harvest Moon: The Winds of Anthos') ($W / 2) 325 30

$g.Dispose(); New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null; $bmp.Save($Out); $bmp.Dispose(); Write-Output $Out

