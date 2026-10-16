# Grouped preview: one row per key label; each slot shown as original -> generated.
param(
    [string]$Glyphs  = "$PSScriptRoot\..\reference\glyphs",
    [string]$Gen     = "$PSScriptRoot\..\assets\keys",
    [string]$Mapping = "$PSScriptRoot\..\assets\glyph_map.csv",
    [string]$Out     = "$PSScriptRoot\..\archive\keycaps_groups.png",
    [int]$Scale = 3
)
Add-Type -AssemblyName System.Drawing
$rows = Import-Csv $Mapping | Where-Object { Test-Path (Join-Path $Gen $_.original_file) }
$groups = $rows | Group-Object key_label | Sort-Object { $_.Group[0].base_on }
$cell = 64 * $Scale; $pairW = ($cell * 2 + 24); $labelW = 170
$maxPairs = ($groups | ForEach-Object { $_.Count } | Measure-Object -Maximum).Maximum
[int]$w = $labelW + $maxPairs * $pairW + 20; [int]$h = $groups.Count * ($cell + 16) + 16
$bmp = New-Object System.Drawing.Bitmap -ArgumentList $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(255, 70, 46, 34)); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.TextRenderingHint = 'AntiAliasGridFit'
$font = New-Object System.Drawing.Font('Segoe UI', 16, [System.Drawing.FontStyle]::Bold)
$small = New-Object System.Drawing.Font('Segoe UI', 9)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 239, 231, 181))
$dim = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(160, 239, 231, 181))
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 239, 231, 181), 2)
$y = 8
foreach ($grp in $groups) {
    $g.DrawString($grp.Name, $font, $brush, 8, $y + $cell / 2 - 14)
    $g.DrawString($grp.Group[0].base_on, $small, $dim, 8, $y + $cell / 2 + 10)
    $x = $labelW
    foreach ($r in $grp.Group) {
        $o = [System.Drawing.Bitmap]::FromFile((Join-Path $Glyphs $r.original_file))
        $g.DrawImage($o, $x, $y + ($cell - $o.Height * $Scale) / 2, $o.Width * $Scale, $o.Height * $Scale); $o.Dispose()
        $ax = $x + $cell + 4
        $g.DrawLine($pen, $ax, $y + $cell / 2, $ax + 14, $y + $cell / 2)
        $g.DrawLine($pen, $ax + 8, $y + $cell / 2 - 5, $ax + 14, $y + $cell / 2)
        $g.DrawLine($pen, $ax + 8, $y + $cell / 2 + 5, $ax + 14, $y + $cell / 2)
        $n = [System.Drawing.Bitmap]::FromFile((Join-Path $Gen $r.original_file))
        $g.DrawImage($n, $x + $cell + 24, $y + ($cell - $n.Height * $Scale) / 2, $n.Width * $Scale, $n.Height * $Scale); $n.Dispose()
        $x += $pairW
    }
    $y += $cell + 16
    $g.DrawLine((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(40, 239, 231, 181), 1)), 0, $y - 8, $w, $y - 8)
}
$g.Dispose(); $bmp.Save($Out); $bmp.Dispose()
Write-Output $Out


