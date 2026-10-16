Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path $PSScriptRoot 'Assets'
New-Item -ItemType Directory -Force -Path $assetDir | Out-Null
$images = @()
foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.ScaleTransform($size / 256.0, $size / 256.0)
    $g.Clear([System.Drawing.Color]::Transparent)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(4, 4, 72, 72, 180, 90)
    $path.AddArc(180, 4, 72, 72, 270, 90)
    $path.AddArc(180, 180, 72, 72, 0, 90)
    $path.AddArc(4, 180, 72, 72, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush([System.Drawing.Point]::new(0, 0), [System.Drawing.Point]::new(256, 256), [System.Drawing.Color]::FromArgb(27, 55, 79), [System.Drawing.Color]::FromArgb(13, 24, 42))
    $g.FillPath($brush, $path)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(44, 220, 181), 19)
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'; $pen.LineJoin = 'Round'
    # An arched tunnel with a central path and two connected endpoints.
    $g.DrawArc($pen, 59, 46, 138, 150, 180, 180)
    $g.DrawLine($pen, 59, 121, 59, 181)
    $g.DrawLine($pen, 197, 121, 197, 181)
    $pen.Color = [System.Drawing.Color]::FromArgb(232, 247, 255)
    $pen.Width = 13
    $g.DrawLine($pen, 91, 167, 165, 167)
    $g.DrawLine($pen, 143, 145, 165, 167)
    $g.DrawLine($pen, 143, 189, 165, 167)
    $dot = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(44, 220, 181))
    $g.FillEllipse($dot, 45, 195, 28, 28)
    $g.FillEllipse($dot, 183, 195, 28, 28)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += ,@($size, $stream.ToArray())
    if ($size -eq 256) { $bitmap.Save((Join-Path $assetDir 'tunnel.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $stream.Dispose(); $dot.Dispose(); $pen.Dispose(); $brush.Dispose(); $path.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
$file = [System.IO.File]::Create((Join-Path $assetDir 'tunnel.ico'))
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($entry in $images) {
    $dimension = if ($entry[0] -eq 256) { 0 } else { $entry[0] }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$entry[1].Length); $writer.Write([uint32]$offset)
    $offset += $entry[1].Length
}
foreach ($entry in $images) { $writer.Write([byte[]]$entry[1]) }
$writer.Dispose(); $file.Dispose()
