# Reproducible original PARADOX sci-fi / Among Us-inspired wizard artwork.
# Generates local BMP assets at build time, without downloading external artwork.
[CmdletBinding()]
param([string]$OutDir = 'dist/installer/branding')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function C([string]$hex) { return [Drawing.ColorTranslator]::FromHtml($hex) }
function Draw-Branding([string]$path, [int]$width, [int]$height, [bool]$compact) {
    $bmp = [Drawing.Bitmap]::new($width, $height)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    try {
        $g.Clear((C '#090D20'))
        for ($y = 0; $y -lt $height; $y++) {
            $tone = [int](20 + (26 * $y / $height))
            $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(11, 15, $tone))
            try { $g.DrawLine($pen, 0, $y, $width, $y) } finally { $pen.Dispose() }
        }
        $cyan = [Drawing.Pen]::new((C '#51EAF3'), 2.5)
        $violet = [Drawing.Pen]::new((C '#A38CFF'), 2)
        $visor = [Drawing.SolidBrush]::new((C '#A4F8FF'))
        $highlight = [Drawing.SolidBrush]::new((C '#EDFFFF'))
        $suit = [Drawing.SolidBrush]::new((C '#D83B61'))
        $deep = [Drawing.SolidBrush]::new((C '#772E63'))
        $glass = [Drawing.SolidBrush]::new((C '#365D92'))
        $stars = [Drawing.SolidBrush]::new((C '#637BA4'))
        $text = [Drawing.SolidBrush]::new((C '#EDF8FF'))
        try {
            $cx = $width / 2.0
            $cy = if ($compact) { $height / 2.0 } else { $height * 0.55 }
            $scale = if ($compact) { 0.39 } else { 1.0 }

            for ($i = 0; $i -lt 34; $i++) {
                $x = ($i * 47 + 19) % $width
                $y = ($i * 61 + 23) % $height
                $g.FillEllipse($stars, $x, $y, 1.6, 1.6)
            }
            $ring = 66 * $scale
            $g.DrawEllipse($violet, [float]($cx-$ring), [float]($cy-$ring),
                           [float](2*$ring), [float](2*$ring))
            $g.DrawArc($cyan, [float]($cx-$ring-10*$scale), [float]($cy-$ring-10*$scale),
                       [float](2*$ring+20*$scale), [float](2*$ring+20*$scale), 215, 130)

            # Backpack, boots and original red spacesuit silhouette.
            $g.FillRectangle($deep, [float]($cx-45*$scale), [float]($cy-17*$scale),
                             [float](23*$scale), [float](67*$scale))
            $g.FillRectangle($suit, [float]($cx-28*$scale), [float]($cy+23*$scale),
                             [float](20*$scale), [float](41*$scale))
            $g.FillRectangle($suit, [float]($cx+12*$scale), [float]($cy+23*$scale),
                             [float](20*$scale), [float](41*$scale))
            $g.FillEllipse($suit, [float]($cx-32*$scale), [float]($cy-48*$scale),
                           [float](68*$scale), [float](95*$scale))
            $g.DrawEllipse($violet, [float]($cx-32*$scale), [float]($cy-48*$scale),
                           [float](68*$scale), [float](95*$scale))
            $g.FillEllipse($glass, [float]($cx-13*$scale), [float]($cy-25*$scale),
                           [float](52*$scale), [float](31*$scale))
            $g.FillEllipse($visor, [float]($cx-9*$scale), [float]($cy-22*$scale),
                           [float](45*$scale), [float](25*$scale))
            $g.FillEllipse($highlight, [float]($cx-1*$scale), [float]($cy-19*$scale),
                           [float](18*$scale), [float](6*$scale))

            if (-not $compact) {
                $font = [Drawing.Font]::new('Arial', 17, [Drawing.FontStyle]::Bold)
                $small = [Drawing.Font]::new('Arial', 9, [Drawing.FontStyle]::Bold)
                try {
                    $g.DrawString('PARADOX', $font, $text, [float]8, [float]26)
                    $g.DrawLine($cyan, 11, 55, $width-11, 55)
                    $g.DrawString('AMONG US', $small, $visor, [float]36, [float]68)
                    $g.DrawString('by DonMillson', $small, $text, [float]26, [float]283)
                } finally { $font.Dispose(); $small.Dispose() }
            }
        } finally {
            foreach ($part in @($cyan, $violet, $visor, $highlight, $suit, $deep, $glass, $stars, $text)) {
                $part.Dispose()
            }
        }
        $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Bmp)
    } finally { $g.Dispose(); $bmp.Dispose() }
}
Draw-Branding (Join-Path $OutDir 'paradox-sidebar.bmp') 164 314 $false
Draw-Branding (Join-Path $OutDir 'paradox-header.bmp') 55 58 $true
Write-Host "Generated PARADOX wizard branding in $OutDir"
