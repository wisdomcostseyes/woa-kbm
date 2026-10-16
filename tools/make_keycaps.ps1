# Generates a consistent keyboard key-cap glyph for every original controller glyph the PC build shows,
# one file per original name (used by the mod as exact overrides). Needs reference\glyphs (dumped by the mod with DumpGlyphs=true). Caps are drawn fresh (not from the
# controller art): cream outline, game font label, soft shadow, sized to each slot's art box.
param(
    [string]$Glyphs   = "$PSScriptRoot\..\reference\glyphs",
    [string]$Mapping  = "$PSScriptRoot\..\assets\glyph_map.csv",
    [string]$OutDir   = "$PSScriptRoot\..\assets\keys",
    [string]$Preview  = "$PSScriptRoot\..\archive\keycaps_preview.png",
    [string]$FontName = "FOT-Seurat Pro B"
)
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = "Stop"

$Cream   = [System.Drawing.Color]::FromArgb(255, 239, 231, 181)
$Dark    = [System.Drawing.Color]::FromArgb(255, 41, 24, 8)
$Up = [string][char]0x2191; $Down = [string][char]0x2193; $Left = [string][char]0x2190; $Right = [string][char]0x2192

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

function Get-ArtBox($path) {
    $b = [System.Drawing.Bitmap]::FromFile($path)
    $minX = $b.Width; $maxX = -1; $minY = $b.Height; $maxY = -1
    for ($y = 0; $y -lt $b.Height; $y++) { for ($x = 0; $x -lt $b.Width; $x++) {
        if ($b.GetPixel($x, $y).A -gt 100) { if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }; if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y } }
    } }
    # Is the main shape a circle? Near the top of the art box a circle's solid span is short
    # (chord), a rounded rectangle's is nearly the full width.
    [int]$x0 = $minX; [int]$x1 = $maxX; [int]$y0 = $minY
    $bw = $x1 - $x0 + 1
    $best = 0.0
    for ($yy = $y0 + 1; $yy -le $y0 + 4; $yy++) {
        $lo = -1; $hi = -1
        for ($xx = $x0; $xx -le $x1; $xx++) { if ($b.GetPixel($xx, $yy).A -gt 120) { if ($lo -lt 0) { $lo = $xx }; $hi = $xx } }
        if ($lo -ge 0) { $span = ($hi - $lo + 1) / $bw; if ($span -gt $best) { $best = $span } }
    }
    # centre of mass of solid pixels (for clusters)
    $sx = 0.0; $sy = 0.0; $sn = 0
    for ($yy = 0; $yy -lt $b.Height; $yy++) { for ($xx = 0; $xx -lt $b.Width; $xx++) { if ($b.GetPixel($xx, $yy).A -gt 100) { $sx += $xx; $sy += $yy; $sn++ } } }
    $cxm = if ($sn) { $sx / $sn + 0.5 } else { $b.Width / 2 }; $cym = if ($sn) { $sy / $sn + 0.5 } else { $b.Height / 2 }
    $r = @{ W = $b.Width; H = $b.Height; X = $minX; Y = $minY; BW = ($maxX - $minX + 1); BH = ($maxY - $minY + 1); Round = ($best -lt 0.8); CX = $cxm; CY = $cym }
    $b.Dispose()
    return $r
}

function Fit-Font($g, $text, $maxW, $maxH) {
    $fmt = [System.Drawing.StringFormat]::GenericTypographic
    for ($size = 40; $size -ge 4; $size -= 0.5) {
        $f = New-Object System.Drawing.Font($FontName, [float]$size, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $m = $g.MeasureString($text, $f, 1000, $fmt)
        if ($m.Width -le $maxW -and $m.Height * 0.74 -le $maxH) { return $f }
        $f.Dispose()
    }
    return New-Object System.Drawing.Font($FontName, 4, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
}

# Draw one key cap: outline (or solid) rounded square at (x,y,w,h) with centred label.
function Draw-Cap($g, [float]$x, [float]$y, [float]$w, [float]$h, [string]$label, [bool]$solid, [float]$stroke, [bool]$round = $false) {
    # round slots (rings, discs, sticks) keep a circle; wide labels on them become a stadium
    $r = if ($round) { [Math]::Min($w, $h) / 2 - 0.01 } else { [Math]::Min($w, $h) * 0.22 }
    $path = New-RoundRect $x $y $w $h $r
    # soft halo hugging the stroke (like the originals): widening low-alpha passes, centred, nudged 1px down
    if ($solid) {
        foreach ($pass in @(@(3, 10), @(1.5, 16))) {
            $sp = New-RoundRect ($x - $pass[0] / 2) ($y - $pass[0] / 2 + 1) ($w + $pass[0]) ($h + $pass[0]) ($r + $pass[0] / 2)
            $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($pass[1], $Dark))), $sp); $sp.Dispose()
        }
        # the game's world-prompt disc: (41,25,11) at 75% opacity
        $g.FillPath((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(191, 41, 25, 11))), $path)
    } else {
        # tight halo like the originals (two faint passes), then a solid stroke drawn twice so the
        # anti-aliased edge is mostly opaque cream rather than cream blended into the dark halo
        foreach ($pass in @(@(3, 14), @(1.5, 22))) {
            $hp = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb($pass[1], $Dark), ($stroke + $pass[0])); $hp.LineJoin = 'Round'
            $sp = New-RoundRect $x ($y + 1) $w $h $r
            $g.DrawPath($hp, $sp); $sp.Dispose(); $hp.Dispose()
        }
        $pen = New-Object System.Drawing.Pen($Cream, $stroke); $pen.LineJoin = 'Round'
        $g.DrawPath($pen, $path); $g.DrawPath($pen, $path); $pen.Dispose()
    }
    $path.Dispose()
    # label: sized to a fixed share of the cap (not "as big as fits"), centred on its ink bounds
    $maxInkH = $h * 0.46
    $maxInkW = if ($round) { $w * 0.72 } else { $w - 2 * [Math]::Max(2.5, $stroke + 1.5) }
    $family = New-Object System.Drawing.FontFamily($FontName)
    $em = 40.0; $path = $null
    while ($em -ge 3) {
        $p = New-Object System.Drawing.Drawing2D.GraphicsPath
        $p.AddString($label, $family, [int][System.Drawing.FontStyle]::Regular, [float]$em, (New-Object System.Drawing.PointF(0, 0)), [System.Drawing.StringFormat]::GenericTypographic)
        $b = $p.GetBounds()
        if ($b.Height -le $maxInkH -and $b.Width -le $maxInkW) { $path = $p; break }
        $p.Dispose(); $em -= 0.5
    }
    if ($path -eq $null) { return }
    $b = $path.GetBounds()
    $cx = $x + $w / 2; $cy = $y + $h / 2
    $m = New-Object System.Drawing.Drawing2D.Matrix
    $m.Translate([float]($cx - ($b.X + $b.Width / 2)), [float]($cy - ($b.Y + $b.Height / 2)))
    $path.Transform($m)
    $sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(22, $Dark))
    foreach ($o in @(@(0,1),@(1,0),@(1,1),@(1,2),@(2,1),@(0,2),@(2,0),@(-1,1),@(1,-1))) {
        $mm = New-Object System.Drawing.Drawing2D.Matrix; $mm.Translate([float]$o[0], [float]$o[1])
        $sp = $path.Clone(); $sp.Transform($mm); $g.FillPath($sb, $sp); $sp.Dispose()
    }
    $g.FillPath((New-Object System.Drawing.SolidBrush($Cream)), $path)
    $path.Dispose(); $family.Dispose()
}

# For slots whose silhouette should be kept (shoulders/triggers): erase the original label, keep the
# border + halo, and return the interior box for the new label. Returns $null if nothing enclosed.
function Keep-Border($bmp) {
    $w = $bmp.Width; $h = $bmp.Height
    $a = New-Object 'int[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { $a[$x, $y] = $bmp.GetPixel($x, $y).A } }
    # outside: reachable from the border through transparent or faint-shadow pixels
    $outside = New-Object 'bool[,]' $w, $h
    $q = New-Object System.Collections.Generic.Queue[int[]]
    for ($x = 0; $x -lt $w; $x++) { foreach ($y in 0, ($h - 1)) { if ($a[$x, $y] -le 100 -and -not $outside[$x, $y]) { $outside[$x, $y] = $true; $q.Enqueue(@($x, $y)) } } }
    for ($y = 0; $y -lt $h; $y++) { foreach ($x in 0, ($w - 1)) { if ($a[$x, $y] -le 100 -and -not $outside[$x, $y]) { $outside[$x, $y] = $true; $q.Enqueue(@($x, $y)) } } }
    while ($q.Count -gt 0) {
        $p = $q.Dequeue()
        foreach ($d in @(@(1,0),@(-1,0),@(0,1),@(0,-1))) {
            $nx = $p[0] + $d[0]; $ny = $p[1] + $d[1]
            if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $w -or $ny -ge $h) { continue }
            if (-not $outside[$nx, $ny] -and $a[$nx, $ny] -le 100) { $outside[$nx, $ny] = $true; $q.Enqueue(@($nx, $ny)) }
        }
    }
    # distance (Chebyshev, capped 6) to outside
    $minX = $w; $maxX = -1; $minY = $h; $maxY = -1
    $clear = [System.Drawing.Color]::FromArgb(0, 41, 24, 8)
    $dist = New-Object 'int[,]' $w, $h
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        if ($outside[$x, $y]) { continue }
        $best = 7
        for ($r = 1; $r -le 6 -and $best -gt 6; $r++) { for ($dy = -$r; $dy -le $r -and $best -gt 6; $dy++) { for ($dx = -$r; $dx -le $r; $dx++) {
            if ([Math]::Max([Math]::Abs($dx), [Math]::Abs($dy)) -ne $r) { continue }
            $nx = $x + $dx; $ny = $y + $dy
            if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $w -or $ny -ge $h) { continue }
            if ($outside[$nx, $ny]) { $best = $r; break }
        } } }
        $solidPx = $a[$x, $y] -ge 100
        $dist[$x, $y] = $best
        if ($solidPx -and $best -gt 3) { $bmp.SetPixel($x, $y, $clear); $a[$x, $y] = 0 }
        if ($best -gt 3) { if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }; if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y } }
    } }
    # second pass: faint (shadow) pixels inside keep only if they hug a surviving border pixel
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        if ($outside[$x, $y] -or $dist[$x, $y] -le 3) { continue }
        if ($a[$x, $y] -le 0 -or $a[$x, $y] -ge 100) { continue }
        $near = $false
        for ($dy = -3; $dy -le 3 -and -not $near; $dy++) { for ($dx = -3; $dx -le 3; $dx++) {
            $nx = $x + $dx; $ny = $y + $dy
            if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $w -or $ny -ge $h) { continue }
            if ($a[$nx, $ny] -ge 100 -and $dist[$nx, $ny] -le 3) { $near = $true; break }
        } }
        if (-not $near) { $bmp.SetPixel($x, $y, $clear) }
    } }
    if ($maxX -lt 0) { return $null }
    return @{ X = [int]$minX; Y = [int]$minY; W = [int]($maxX - $minX + 1); H = [int]($maxY - $minY + 1) }
}

# Label only (no shape): sized to a share of the box, centred on its ink. Used with Keep-Border.
function Draw-Text($g, [float]$x, [float]$y, [float]$w, [float]$h, [string]$label) {
    $maxInkH = $h * 0.80; $maxInkW = $w * 1.05
    $family = New-Object System.Drawing.FontFamily($FontName)
    $em = 40.0; $path = $null
    while ($em -ge 3) {
        $p = New-Object System.Drawing.Drawing2D.GraphicsPath
        $p.AddString($label, $family, [int][System.Drawing.FontStyle]::Regular, [float]$em, (New-Object System.Drawing.PointF(0, 0)), [System.Drawing.StringFormat]::GenericTypographic)
        $b = $p.GetBounds()
        if ($b.Height -le $maxInkH -and $b.Width -le $maxInkW) { $path = $p; break }
        $p.Dispose(); $em -= 0.5
    }
    if ($path -eq $null) { return }
    $b = $path.GetBounds()
    $m = New-Object System.Drawing.Drawing2D.Matrix
    $m.Translate([float]($x + $w / 2 - ($b.X + $b.Width / 2)), [float]($y + $h / 2 - ($b.Y + $b.Height / 2)))
    $path.Transform($m)
    $sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(22, $Dark))
    foreach ($o in @(@(0,1),@(1,0),@(1,1),@(1,2),@(2,1),@(0,2),@(2,0),@(-1,1),@(1,-1))) {
        $mm = New-Object System.Drawing.Drawing2D.Matrix; $mm.Translate([float]$o[0], [float]$o[1])
        $sp = $path.Clone(); $sp.Transform($mm); $g.FillPath($sb, $sp); $sp.Dispose()
    }
    $g.FillPath((New-Object System.Drawing.SolidBrush($Cream)), $path)
    $path.Dispose(); $family.Dispose()
}

# Cluster layouts for sticks / d-pad: rows of mini caps inside the box.
function Get-Cluster($label) {
    switch ($label) {
        'WASD'                      { return ,@(@('W'), @('A','S','D')) }
        'W/S'                       { return ,@(@('W'), @('S')) }
        ($Up + $Down + $Left + $Right) { return ,@(@($Up), @($Left, $Down, $Right)) }
        ($Up + $Down)               { return ,@(@($Up), @($Down)) }
        ($Left + $Right)            { return ,@(@($Left, $Right)) }
    }
    return $null
}

New-Item -ItemType Directory -Force $OutDir, (Split-Path $Preview) | Out-Null
$rows = Import-Csv $Mapping
$made = @()
foreach ($row in $rows) {
    $name = $row.original_file
    $label = $row.key_label
    if (-not $label -or $label -eq '(unmapped)') { continue }
    $src = Join-Path $Glyphs $name
    if (-not (Test-Path $src)) { continue }
    $box = Get-ArtBox $src
    $isStick = $name -match 'stick'
    $solid = $name -match 'actioncom'
    $cluster = Get-Cluster $label
    # shoulders/triggers (including D-shaped bumpers and tab pills) are keycaps, never circles
    if ($name -match '_(L|R|LB|RB|L1|R1|ZL|ZR|LT|RT|L2|R2)_0' -and -not $isStick) { $box.Round = $false }

    # Shoulders/triggers: the game's D-shaped pills and flared triggers are distinctive silhouettes.
    # Keep the original border exactly and only swap the label.
    if ($false -and $name -match '_(L|R|LB|RB|L1|R1|ZL|ZR|LT|RT|L2|R2)_0' -and -not $isStick) {
        $bmp = New-Object System.Drawing.Bitmap($src)
        $inner = Keep-Border $bmp
        if ($inner -ne $null) {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'; $g.PixelOffsetMode = 'HighQuality'
            Draw-Text $g $inner.X $inner.Y $inner.W $inner.H $label
            $g.Dispose()
        }
        $bmp.Save((Join-Path $OutDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $made += $name
        continue
    }

    $bmp = New-Object System.Drawing.Bitmap($box.W, $box.H, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'; $g.PixelOffsetMode = 'HighQuality'
    $g.Clear([System.Drawing.Color]::FromArgb(0, 41, 24, 8))

    # Key cap footprint: the slot's art box, but a stick's box includes its base below the ring,
    # so use the top square of it; keep a 1px margin for the shadow.
    $bx = $box.X; $by = $box.Y; $bw = $box.BW; $bh = $box.BH
    if ($isStick -and $bh -gt $bw) { $bh = $bw }
    # stroke weight as in the originals: 2px on the 40px slots, a touch more on the 60px tab slots
    $stroke = if ($box.W -ge 60) { 2.8 } else { 2.3 }

    if ($cluster) {
        # clusters need room: use the whole slot width and the full art height (ring + base)
        $bx = 1; $bw = $box.W - 2; $by = $box.Y; $bh = $box.BH
        $nRows = $cluster.Count
        $maxCols = ($cluster | ForEach-Object { $_.Count } | Measure-Object -Maximum).Maximum
        $gap = 1.5
        $m = [Math]::Floor([Math]::Min(($bw - ($maxCols - 1) * $gap) / $maxCols, ($bh - ($nRows - 1) * $gap) / $nRows))
        if ($m -gt 12) { $m = 12 }   # consistent mini-cap size across clusters
        $totalH = $nRows * $m + ($nRows - 1) * $gap
        # centre on the original art's centre of mass (lands on the ring, ignoring base/arrows)
        $cy = $box.CY
        $y0 = $cy - $totalH / 2
        if ($y0 -lt 1) { $y0 = 1 }
        for ($ri = 0; $ri -lt $nRows; $ri++) {
            $cols = $cluster[$ri].Count
            $totalW = $cols * $m + ($cols - 1) * $gap
            $x0 = $bx + ($bw - $totalW) / 2
            for ($ci = 0; $ci -lt $cols; $ci++) {
                Draw-Cap $g ($x0 + $ci * ($m + $gap)) ($y0 + $ri * ($m + $gap)) $m $m $cluster[$ri][$ci] $solid 1.6
            }
        }
    }
    else {
        $side = [Math]::Min($bw, $bh)
        $w = $side; $h = $side
        if (-not $box.Round) { $w = $bw; $h = $bh }   # keycap fills the original pill footprint
        if ($label.Length -gt 1 -and -not $box.Round -and $w -lt $side * 1.3) {
            # wide cap for multi-letter labels, up to the canvas width minus shadow room
            $w = [Math]::Min($box.W - 3, [Math]::Max($side, $side * (0.55 * $label.Length + 0.3)))
        }
        $x = $bx + ($bw - $w) / 2; $y = $by + ($bh - $h) / 2
        if ($x -lt 1) { $x = 1 }; if ($x + $w -gt $box.W - 2) { $x = $box.W - 2 - $w }
        Draw-Cap $g $x $y $w $h $label $solid $stroke $box.Round
    }
    $g.Dispose()
    $bmp.Save((Join-Path $OutDir $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $made += $name
}
Write-Host "wrote $($made.Count) files to $OutDir"

# Preview sheet: original above, generated below, 4x, grouped as listed in the mapping.
$scale = 4; $cell = 64; $perRow = 14
$count = $made.Count; $rowsN = [Math]::Ceiling($count / $perRow)
$sheet = New-Object System.Drawing.Bitmap (($perRow * $cell * $scale), ($rowsN * 2 * $cell * $scale))
$sg = [System.Drawing.Graphics]::FromImage($sheet)
$sg.Clear([System.Drawing.Color]::FromArgb(255, 70, 46, 34)); $sg.InterpolationMode = 'NearestNeighbor'; $sg.PixelOffsetMode = 'Half'
for ($i = 0; $i -lt $count; $i++) {
    $cx = ($i % $perRow) * $cell * $scale; $cy = [Math]::Floor($i / $perRow) * 2 * $cell * $scale
    $o = [System.Drawing.Bitmap]::FromFile((Join-Path $Glyphs $made[$i])); $sg.DrawImage($o, $cx, $cy, $o.Width * $scale, $o.Height * $scale); $o.Dispose()
    $n = [System.Drawing.Bitmap]::FromFile((Join-Path $OutDir $made[$i])); $sg.DrawImage($n, $cx, ($cy + $cell * $scale), $n.Width * $scale, $n.Height * $scale); $n.Dispose()
}
$sg.Dispose(); $sheet.Save($Preview); $sheet.Dispose()
Write-Host "preview: $Preview"













