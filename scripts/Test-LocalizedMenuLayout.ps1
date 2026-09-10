[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$MenuSource = Get-Content (Join-Path $ProjectRoot "UI\WarpBookmarksMenu.cs") -Raw
$CoordinateSource = Get-Content (Join-Path $ProjectRoot "UI\CoordinateWarpDialog.cs") -Raw
$RendererSource = Get-Content (Join-Path $ProjectRoot "UI\MultilingualTextRenderer.cs") -Raw
$TextBoxSource = Get-Content (Join-Path $ProjectRoot "UI\MultilingualTextBox.cs") -Raw
$CatalogSource = Get-Content (Join-Path $ProjectRoot "Framework\DestinationCatalog.cs") -Raw
$DefaultI18n = Get-Content (Join-Path $ProjectRoot "i18n\default.json") -Raw | ConvertFrom-Json
$ChineseI18n = Get-Content (Join-Path $ProjectRoot "i18n\zh.json") -Raw | ConvertFrom-Json
$Failures = @()

if ($MenuSource -notmatch 'CategoryArea\s*=>\s*new\([^;]*this\.width\s*-\s*72') {
    $Failures += "The five categories aren't using the full parchment content width."
}
if ($MenuSource -notmatch 'DetailArea\s*=>\s*new\([^;]*yPositionOnScreen\s*\+\s*132') {
    $Failures += "The details panel doesn't start below the full-width category bar."
}
if ($MenuSource -match 'translate\(labelKey\)\}：\{value\}') {
    $Failures += "The details panel still hard-codes Chinese punctuation in C# source."
}
if ($DefaultI18n.'menu.detail-format' -ne '{{label}}: {{value}}') {
    $Failures += "The English detail format doesn't use a Latin colon."
}
if ($ChineseI18n.'menu.detail-format' -ne '{{label}}：{{value}}') {
    $Failures += "The Chinese detail format doesn't preserve Chinese punctuation."
}
if ($ChineseI18n.'category.bookmarks' -ne '书签') {
    $Failures += "The Chinese Bookmarks tab still uses the problematic longer label."
}
if ($MenuSource -notmatch 'WrapText\(this\.translate\(emptyKey\)') {
    $Failures += "Long empty-state text isn't constrained to the details panel."
}
if ($MenuSource -match 'ListScrollbarTrack[^\r\n]*ListArea\.Height - 12' -or $MenuSource -notmatch 'ListRowsHeight[^\r\n]*VisibleRows \* RowHeight - 4' -or $MenuSource -notmatch 'ListScrollbarTrack[^\r\n]*ListRowsHeight - 4') {
    $Failures += "The main scrollbar doesn't end at the final visible row."
}
if ($RendererSource -notmatch 'Load<SpriteFont>\("Fonts\\\\SmallFont", LocalizedContentManager\.LanguageCode\.zh\)' -or $RendererSource -notmatch 'ShouldUseChineseFallback') {
    $Failures += "The menu doesn't load the localized Chinese SmallFont only when the current font lacks glyphs."
}
if ($TextBoxSource -notmatch 'class\s+MultilingualTextBox\s*:\s*TextBox' -or $TextBoxSource -notmatch 'textRenderer\.DrawString') {
    $Failures += "Text input doesn't preserve visible CJK text in a Latin-language UI."
}
if ($CoordinateSource -match 'PreviewButton[^\r\n]*170|SaveButton[^\r\n]*170|WarpButton[^\r\n]*170') {
    $Failures += "The coordinate dialog still uses action buttons too narrow for English labels."
}
if ($CoordinateSource -notmatch 'textRenderer\.FitText\(label, bounds\.Width - 24') {
    $Failures += "Coordinate action labels aren't constrained to their button bounds."
}
if ($CoordinateSource -match 'DrawString\([^\r\n]*(▲|▼)' -or $CoordinateSource -notmatch 'DrawDropdownCaret') {
    $Failures += "The coordinate map dropdown still depends on unsupported font triangle glyphs."
}
if ($ChineseI18n.'menu.warp-to' -ne '传送到 {{name}}') {
    $Failures += "The Chinese warp action still contains punctuation that can render as question marks."
}
if ($RendererSource -match 'ChineseSmallScale\s*=\s*1\.5f|ChineseTitleScale\s*=\s*2f') {
    $Failures += "CJK fallback text still uses the oversized initial scale."
}
if ($MenuSource -match 'marker\s*\+\s*destination\.Name' -or $MenuSource -notmatch 'DrawPinnedIndicator' -or $MenuSource -notmatch 'rowBounds\.Right') {
    $Failures += "Pinned state isn't drawn as a font-independent marker in a fixed right-side slot."
}
if ($CatalogSource -notmatch 'Name\s*=\s*bookmark\.Name' -or $CatalogSource -notmatch 'GetLocalizedLocation\(bookmark\.Location\)') {
    $Failures += "Saved names aren't preserved independently from the current localized map display name."
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "Localized menu layout guard failed with $($Failures.Count) error(s)."
}

Write-Host "Localized menu layout guards passed: $ProjectRoot"
