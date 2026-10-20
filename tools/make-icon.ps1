Add-Type -AssemblyName System.Drawing

function New-RoundedPath {
    param([float]$X, [float]$Y, [float]$W, [float]$H, [float]$R)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $R * 2
    $path.AddArc($X, $Y, $d, $d, 180, 90)
    $path.AddArc($X + $W - $d, $Y, $d, $d, 270, 90)
    $path.AddArc($X + $W - $d, $Y + $H - $d, $d, $d, 0, 90)
    $path.AddArc($X, $Y + $H - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = New-Object System.Collections.ArrayList

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = [single]($size / 256.0)
    $g.ScaleTransform($scale, $scale)

    $bgPath = New-RoundedPath 9 9 238 238 54
    $bgRect = New-Object System.Drawing.Rectangle(9, 9, 238, 238)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $bgRect,
        [System.Drawing.Color]::FromArgb(255, 15, 23, 37),
        [System.Drawing.Color]::FromArgb(255, 54, 104, 229),
        42.0)
    $g.FillPath($bgBrush, $bgPath)

    $glowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $glowPath.AddEllipse(24, 16, 190, 150)
    $glowBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($glowPath)
    $glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(46, 255, 255, 255)
    $glowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillPath($glowBrush, $glowPath)

    $borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 143, 185, 255), 2.2)
    $g.DrawPath($borderPen, $bgPath)

    # Tap ripples
    $rippleColor = [System.Drawing.Color]::FromArgb(235, 255, 186, 90)
    foreach ($r in @(37, 58, 79)) {
        $pen = New-Object System.Drawing.Pen($rippleColor, 7.0)
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawArc($pen, 80 - $r, 64 - $r, $r * 2, $r * 2, -48, 88)
        $pen.Dispose()
    }

    # Cursor shadow
    $shadowPoints = @(
        (New-Object System.Drawing.PointF(76, 58)),
        (New-Object System.Drawing.PointF(76, 191)),
        (New-Object System.Drawing.PointF(109, 158)),
        (New-Object System.Drawing.PointF(132, 212)),
        (New-Object System.Drawing.PointF(158, 200)),
        (New-Object System.Drawing.PointF(135, 146)),
        (New-Object System.Drawing.PointF(181, 143))
    )
    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(95, 5, 10, 20))
    $shadowOffset = @($shadowPoints | ForEach-Object { New-Object System.Drawing.PointF(($_.X + 2), ($_.Y + 5)) })
    $g.FillPolygon($shadowBrush, $shadowOffset)

    $cursorBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillPolygon($cursorBrush, $shadowPoints)
    $cursorPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 9, 17, 30), 6.0)
    $cursorPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPolygon($cursorPen, $shadowPoints)

    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $locked = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $stride = $locked.Stride
    $pixels = New-Object byte[] ($stride * $size)
    [System.Runtime.InteropServices.Marshal]::Copy($locked.Scan0, $pixels, 0, $pixels.Length)
    $bmp.UnlockBits($locked)

    $xor = New-Object byte[] ($size * $size * 4)
    for ($y = 0; $y -lt $size; $y++) {
        [Array]::Copy($pixels, $y * $stride, $xor, ($size - 1 - $y) * $size * 4, $size * 4)
    }
    $maskStride = [int]([math]::Floor(($size + 31) / 32) * 4)
    $mask = New-Object byte[] ($maskStride * $size)

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([uint32]40)
    $bw.Write([int32]$size)
    $bw.Write([int32]($size * 2))
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]0)
    $bw.Write([uint32]($xor.Length + $mask.Length))
    $bw.Write([int32]0)
    $bw.Write([int32]0)
    $bw.Write([uint32]0)
    $bw.Write([uint32]0)
    $bw.Write($xor)
    $bw.Write($mask)
    $bw.Flush()
    [void]$frames.Add($ms.ToArray())
    $bw.Dispose()
    $ms.Dispose()
    $cursorPen.Dispose()
    $cursorBrush.Dispose()
    $shadowBrush.Dispose()
    $borderPen.Dispose()
    $glowBrush.Dispose()
    $glowPath.Dispose()
    $bgBrush.Dispose()
    $bgPath.Dispose()
    $g.Dispose()
    $bmp.Dispose()
}

$outPath = Join-Path $PSScriptRoot '..\assets\app.ico'
$fs = [System.IO.File]::Create([System.IO.Path]::GetFullPath($outPath))
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$frames.Count)

$offset = 6 + (16 * $frames.Count)
for ($i = 0; $i -lt $frames.Count; $i++) {
    $size = $sizes[$i]
    $widthByte = if ($size -ge 256) { 0 } else { $size }
    $heightByte = if ($size -ge 256) { 0 } else { $size }
    $bw.Write([byte]$widthByte)
    $bw.Write([byte]$heightByte)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$frames[$i].Length)
    $bw.Write([uint32]$offset)
    $offset += $frames[$i].Length
}
foreach ($frame in $frames) {
    $bw.Write($frame)
}
$bw.Flush()
$bw.Dispose()
$fs.Dispose()

Write-Host "Wrote $outPath ($($frames.Count) sizes)"
