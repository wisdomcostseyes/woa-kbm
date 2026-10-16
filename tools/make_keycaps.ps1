# Generates blank keyboard glyph shapes for every controller glyph the PC build shows, one PNG per original
# sprite name, plus a JSON sidecar listing the label boxes (pixel rects, top-left origin) and the control each
# box stands for. The mod renders the key names into those boxes at runtime with the game's font, so remapping
# keys updates every prompt.
#
# Shapes follow the originals: round sources -> circle; shoulders/triggers/tabs -> rounded rectangle on the
# original footprint; world-prompt discs -> solid circle; sticks and d-pad -> clusters of mini caps.
# Needs reference\glyphs (dump them with the mod's DumpGlyphs=true option).
param(
    [string]$Glyphs   = "$PSScriptRoot\..\reference\glyphs",
    [string]$OutDir   = "$PSScriptRoot\..\assets\keys",
    [string]$Preview  = "$PSScriptRoot\..\archive\keycaps_preview.png"
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"

$Cream = [System.Drawing.Color]::FromArgb(255, 239, 231, 181)
$Dark  = [System.Drawing.Color]::FromArgb(255, 41, 24, 8)

# Original sprite name -> canonical Xbox control (mirrors KeyLabels.ControlForSpriteName in the plugin).
function Get-Control([string]$name) {
    $s = $name.Substring($name.IndexOf('icon_guide_') + 11)
    if ($s -match '\.png$') { $s = $s.Substring(0, $s.Length - 4) }
    $stick = $false
    foreach ($p in 'toptab_btn_', 'actioncom_btn_', 'btn_', 'stick_') {
        if ($s.StartsWith($p)) { $stick = ($p -eq 'stick_'); $s = $s.Substring($p.Length); break }
    }
    $i = $s.IndexOf('_0'); if ($i -gt 0) { $s = $s.Substring(0, $i) }
    $s = $s.ToLower()
    if (-not $stick -and $s.StartsWith('stick_')) { $s = $s.Substring(6) + '3'; $stick = $true }   # btn_stick_L = L3
    if ($stick) {
        switch ($s) { 'l' {return 'leftStick'} 'l3' {return 'leftStickPress'} 'lupdown' {return 'leftStick/ud'} 'lleftright' {return 'leftStick/lr'} 'r' {return 'rightStick'} 'r3' {return 'rightStickPress'} 'rupdown' {return 'rightStick/ud'} 'rleftright' {return 'rightStick/lr'} }
        return $null
    }
    switch ($s) {
        'a' {return 'buttonSouth'} 'circle' {return 'buttonSouth'}
        'b' {return 'buttonEast'}
        'x' {return 'buttonWest'} 'square' {return 'buttonWest'}
        'y' {return 'buttonNorth'} 'triangle' {return 'buttonNorth'}
        'l' {return 'leftShoulder'} 'lb' {return 'leftShoulder'} 'l1' {return 'leftShoulder'}
        'r' {return 'rightShoulder'} 'rb' {return 'rightShoulder'} 'r1' {return 'rightShoulder'}
        'zl' {return 'leftTrigger'} 'lt' {return 'leftTrigger'} 'l2' {return 'leftTrigger'}
        'zr' {return 'rightTrigger'} 'rt' {return 'rightTrigger'} 'r2' {return 'rightTrigger'}
        'minus' {return 'select'} 'back' {return 'select'} 'pad' {return 'select'} 'view' {return 'select'} 'share' {return 'select'} 'touchpad' {return 'select'}
        'plus' {return 'start'} 'start' {return 'start'} 'option' {return 'start'} 'menu' {return 'start'}
        'dir_updown' {return 'dpad/ud'} 'dir_leftright' {return 'dpad/lr'}
    }
    return $null
}

# Cluster layouts: rows of (control) for stick/d-pad glyphs.
function Get-Cluster([string]$control) {
    switch ($control) {
        'leftStick'     { return ,@(@('leftStick/up'), @('leftStick/left', 'leftStick/down', 'leftStick/right')) }
        'rightStick'    { return ,@(@('rightStick/up'), @('rightStick/left', 'rightStick/down', 'rightStick/right')) }
        'leftStick/ud'  { return ,@(@('leftStick/up'), @('leftStick/down')) }
        'leftStick/lr'  { return ,@(@('leftStick/left', 'leftStick/right')) }
        'rightStick/ud' { return ,@(@('rightStick/up'), @('rightStick/down')) }
        'rightStick/lr' { return ,@(@('rightStick/left', 'rightStick/right')) }
        'dpad/ud'       { return ,@(@('dpad/up'), @('dpad/down')) }
        'dpad/lr'       { return ,@(@('dpad/left', 'dpad/right')) }
    }
    return $null
}

function New-RoundRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

# Art box of solid pixels, whether the main shape is round, and the art's centre of mass.
function Get-ArtBox($path) {
    $b = [System.Drawing.Bitmap]::FromFile($path)
    $minX = $b.Width; $maxX = -1; $minY = $b.Height; $maxY = -1; $sx = 0.0; $sy = 0.0; $sn = 0
    for ($y = 0; $y -lt $b.Height; $y++) { for ($x = 0; $x -lt $b.Width; $x++) {
        if ($b.GetPixel($x, $y).A -gt 100) {
            if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }; if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
            $sx += $x; $sy += $y; $sn++
        }
    } }
    [int]$x0 = $minX; [int]$x1 = $maxX; [int]$y0 = $minY
    $bw = $x1 - $x0 + 1; $best = 0.0
    for ($yy = $y0 + 1; $yy -le $y0 + 4; $yy++) {
        $lo = -1; $hi = -1
        for ($xx = $x0; $xx -le $x1; $xx++) { if ($b.GetPixel($xx, $yy).A -gt 120) { if ($lo -lt 0) { $lo = $xx }; $hi = $xx } }
        if ($lo -ge 0) { $span = ($hi - $lo + 1) / $bw; if ($span -gt $best) { $best = $span } }
    }
    # ring centre: midpoint of the widest solid row (the ring is the widest feature of every stick glyph;
    # bases below and press arrows above are narrower, so this ignores them)
    $wide = 0; $wideY = 0; $wideLo = 0; $wideHi = 0
    for ($yy = 0; $yy -lt $b.Height; $yy++) {
        $lo = -1; $hi = -1
        for ($xx = 0; $xx -lt $b.Width; $xx++) { if ($b.GetPixel($xx, $yy).A -gt 120) { if ($lo -lt 0) { $lo = $xx }; $hi = $xx } }
        if ($lo -ge 0 -and ($hi - $lo) -gt $wide) { $wide = $hi - $lo; $wideY = $yy; $wideLo = $lo; $wideHi = $hi }
    }
    $r = @{ W = $b.Width; H = $b.Height; X = $minX; Y = $minY; BW = ($maxX - $minX + 1); BH = ($maxY - $minY + 1); Round = ($best -lt 0.8)
            CX = (($wideLo + $wideHi) / 2 + 0.5); CY = ($wideY + 0.5) }
    $b.Dispose()
    return $r
}

# Draw one blank cap at (x,y,w,h); returns the inner label box.
function Draw-Cap($g, [float]$x, [float]$y, [float]$w, [float]$h, [bool]$solid, [float]$stroke, [bool]$round) {
    $r = if ($round) { [Math]::Min($w, $h) / 2 - 0.01 } else { [Math]::Min($w, $h) * 0.22 }
    $path = New-RoundRect $x $y $w $h $r
    if ($solid) {
        foreach ($pass in @(@(3, 10), @(1.5, 16))) {
            $sp = New-RoundRect ($x - $pass[0] / 2) ($y - $pass[0] / 2 + 1) ($w + $pass[0]) ($h + $pass[0]) ($r + $pass[0] / 2)
            $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($pass[1], $Dark))), $sp); $sp.Dispose()
        }
        # the game's world-prompt disc: (41,25,11) at 75% opacity
        $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(191, 41, 25, 11))), $path)
    } else {
        foreach ($pass in @(@(3, 14), @(1.5, 22))) {
            $hp = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb($pass[1], $Dark), ($stroke + $pass[0])); $hp.LineJoin = 'Round'
            $sp = New-RoundRect $x ($y + 1) $w $h $r
            $g.DrawPath($hp, $sp); $sp.Dispose(); $hp.Dispose()
        }
        $pen = New-Object System.Drawing.Pen($Cream, $stroke); $pen.LineJoin = 'Round'
        $g.DrawPath($pen, $path); $g.DrawPath($pen, $path); $pen.Dispose()
    }
    $path.Dispose()
    $inset = if ($solid) { 2.0 } else { $stroke + 1.5 }
    if ($round) { $inset += [Math]::Min($w, $h) * 0.08 }   # keep text off the curve
    return @{ x = [Math]::Round($x + $inset, 1); y = [Math]::Round($y + $inset, 1); w = [Math]::Round($w - 2 * $inset, 1); h = [Math]::Round($h - 2 * $inset, 1) }
}

New-Item -ItemType Directory -Force $OutDir, (Split-Path $Preview) | Out-Null
$made = @()
foreach ($f in Get-ChildItem $Glyphs -Filter "ui_common_icon_guide_*.png" | Where-Object { $_.Name -notmatch '_ps5?\.png$' }) {
    $name = $f.Name
    $control = Get-Control $name
    if (-not $control) { Write-Warning "no control for $name"; continue }
    $box = Get-ArtBox $f.FullName
    $isStick = $name -match 'stick'
    $solid = $name -match 'actioncom'
    if ($name -match '_(L|R|LB|RB|L1|R1|ZL|ZR|LT|RT|L2|R2)_0' -and -not $isStick) { $box.Round = $false }
    $cluster = Get-Cluster $control

    $bmp = New-Object System.Drawing.Bitmap($box.W, $box.H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::FromArgb(0, 41, 24, 8))

    $bx = $box.X; $by = $box.Y; $bw = $box.BW; $bh = $box.BH
    if ($isStick -and $bh -gt $bw) { $bh = $bw }
    $stroke = if ($box.W -ge 60) { 2.8 } else { 2.3 }
    $labels = @()

    if ($cluster) {
        $bx = 1; $bw = $box.W - 2; $by = $box.Y; $bh = $box.BH
        $nRows = $cluster.Count
        $maxCols = ($cluster | ForEach-Object { $_.Count } | Measure-Object -Maximum).Maximum
        $gap = 1.5
        $m = [Math]::Floor([Math]::Min(($bw - ($maxCols - 1) * $gap) / $maxCols, ($bh - ($nRows - 1) * $gap) / $nRows))
        if ($m -gt 12) { $m = 12 }
        $totalH = $nRows * $m + ($nRows - 1) * $gap
        $y0 = $box.CY - $totalH / 2; if ($y0 -lt 1) { $y0 = 1 }
        for ($ri = 0; $ri -lt $nRows; $ri++) {
            $cols = $cluster[$ri].Count
            $totalW = $cols * $m + ($cols - 1) * $gap
            $x0 = $bx + ($bw - $totalW) / 2
            for ($ci = 0; $ci -lt $cols; $ci++) {
                $lb = Draw-Cap $g ($x0 + $ci * ($m + $gap)) ($y0 + $ri * ($m + $gap)) $m $m $solid 1.6 $false
                $lb.control = $cluster[$ri][$ci]
                $labels += $lb
            }
        }
    }
    else {
        $side = [Math]::Min($bw, $bh)
        $w = $side; $h = $side
        if (-not $box.Round) { $w = $bw; $h = $bh }
        $x = $bx + ($bw - $w) / 2; $y = $by + ($bh - $h) / 2
        # stick glyphs carry a press arrow above the ring: centre the cap on the ring (centre of mass), not the box top
        if ($isStick) { $x = $box.CX - $w / 2; $y = $box.CY - $h / 2 }
        $lb = Draw-Cap $g $x $y $w $h $solid $stroke $box.Round
        $lb.control = $control
        $labels += $lb
    }
    $g.Dispose()
    $bmp.Save((Join-Path $OutDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $sidecar = @{ canvas = @($box.W, $box.H); labels = @($labels | ForEach-Object { [ordered]@{ control = $_.control; x = $_.x; y = $_.y; w = $_.w; h = $_.h } }) }
    $json = $sidecar | ConvertTo-Json -Depth 4 -Compress
    Set-Content (Join-Path $OutDir ($name -replace '\.png$', '.json')) $json -Encoding UTF8
    $made += $name
}
Write-Host "wrote $($made.Count) shapes + sidecars to $OutDir"

# Preview sheet: original above, blank shape below.
$scale = 4; $cell = 64; $perRow = 14
$count = $made.Count; $rowsN = [Math]::Ceiling($count / $perRow)
$sheet = New-Object System.Drawing.Bitmap -ArgumentList ($perRow * $cell * $scale), ($rowsN * 2 * $cell * $scale)
$sg = [System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(255, 70, 46, 34)); $sg.InterpolationMode = 'NearestNeighbor'; $sg.PixelOffsetMode = 'Half'
for ($i = 0; $i -lt $count; $i++) {
    $cx = ($i % $perRow) * $cell * $scale; $cy = [Math]::Floor($i / $perRow) * 2 * $cell * $scale
    $o = [System.Drawing.Bitmap]::FromFile((Join-Path $Glyphs $made[$i])); $sg.DrawImage($o, $cx, $cy, $o.Width * $scale, $o.Height * $scale); $o.Dispose()
    $n = [System.Drawing.Bitmap]::FromFile((Join-Path $OutDir $made[$i])); $sg.DrawImage($n, $cx, ($cy + $cell * $scale), $n.Width * $scale, $n.Height * $scale); $n.Dispose()
}
$sg.Dispose(); $sheet.Save($Preview); $sheet.Dispose()
Write-Host "preview: $Preview"


