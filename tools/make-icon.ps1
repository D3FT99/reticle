# Renders Assets/Reticle.ico: an azure tile with a dwell ring and crosshair ticks, PNG-encoded at standard sizes.
param([string]$Out = (Join-Path $PSScriptRoot '..\src\Reticle\Assets\Reticle.ico'))

Add-Type -AssemblyName System.Drawing
$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$frames = foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    $r = $s * 0.22
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $w = $s - 1
    $path.AddArc(0, 0, 2 * $r, 2 * $r, 180, 90)
    $path.AddArc($w - 2 * $r, 0, 2 * $r, 2 * $r, 270, 90)
    $path.AddArc($w - 2 * $r, $w - 2 * $r, 2 * $r, 2 * $r, 0, 90)
    $path.AddArc(0, $w - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $path.CloseFigure()
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point $s, $s), ([System.Drawing.Color]::FromArgb(255, 0x3D, 0x8B, 0xFF)), ([System.Drawing.Color]::FromArgb(255, 0x2B, 0xB3, 0xFF))
    $g.FillPath($grad, $path)

    $stroke = [Math]::Max(1.5, $s * 0.1)
    $inset = $s * 0.22
    $d = $s - 2 * $inset
    $track = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(70, 255, 255, 255)), $stroke
    $g.DrawEllipse($track, [single]$inset, [single]$inset, [single]$d, [single]$d)
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), $stroke
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $g.DrawArc($pen, [single]$inset, [single]$inset, [single]$d, [single]$d, -90, 270)
    # Crosshair ticks crossing the ring at 12, 3, 6 and 9 o'clock
    if ($s -ge 24) {
        $tick = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([Math]::Max(1.0, $s * 0.05))
        $tick.StartCap = 'Round'; $tick.EndCap = 'Round'
        $c = $s / 2; $outer = $s * 0.40; $inner = $s * 0.20
        $g.DrawLine($tick, [single]$c, [single]($c - $outer), [single]$c, [single]($c - $inner))
        $g.DrawLine($tick, [single]$c, [single]($c + $inner), [single]$c, [single]($c + $outer))
        $g.DrawLine($tick, [single]($c - $outer), [single]$c, [single]($c - $inner), [single]$c)
        $g.DrawLine($tick, [single]($c + $inner), [single]$c, [single]($c + $outer), [single]$c)
    }
    $dot = $s * 0.16
    $g.FillEllipse([System.Drawing.Brushes]::White, [single](($s - $dot) / 2), [single](($s - $dot) / 2), [single]$dot, [single]$dot)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    , @($s, $ms.ToArray())
}

New-Item -ItemType Directory -Force -Path (Split-Path $Out) | Out-Null
$fs = [System.IO.File]::Create($Out)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($f in $frames) {
    $s = $f[0]; $data = $f[1]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$data.Length); $bw.Write([uint32]$offset)
    $offset += $data.Length
}
foreach ($f in $frames) { $bw.Write([byte[]]$f[1]) }
$bw.Dispose()
Write-Output "Wrote $Out"
