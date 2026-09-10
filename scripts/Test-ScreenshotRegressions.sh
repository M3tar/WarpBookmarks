#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
coordinate_source="$project_root/UI/CoordinateWarpDialog.cs"
menu_source="$project_root/UI/WarpBookmarksMenu.cs"
renderer_source="$project_root/UI/MultilingualTextRenderer.cs"
textbox_source="$project_root/UI/MultilingualTextBox.cs"
default_i18n="$project_root/i18n/default.json"
chinese_i18n="$project_root/i18n/zh.json"
failures=0

fail() {
    printf 'FAIL: %s\n' "$1" >&2
    failures=$((failures + 1))
}

if rg -q 'PreviewButton.*170|SaveButton.*170|WarpButton.*170' "$coordinate_source"; then
    fail 'Coordinate actions still use the narrow 170-pixel button layout from screenshot 1.'
fi
if ! rg -q 'textRenderer\.FitText\(label, bounds\.Width - 24' "$coordinate_source"; then
    fail 'Coordinate action labels are not constrained by the shared text renderer.'
fi
if rg -q 'ChineseSmallScale\s*=\s*1\.5f|ChineseTitleScale\s*=\s*2f' "$renderer_source"; then
    fail 'The CJK fallback still uses the oversized scales visible in screenshots 2 and 3.'
fi
if rg -q 'marker \+ destination\.Name' "$menu_source"; then
    fail 'A symbolic marker is still passed through the CJK name fallback and can become a question mark.'
fi
if ! rg -q 'Load<SpriteFont>\("Fonts\\\\SmallFont", LocalizedContentManager\.LanguageCode\.zh\)' "$renderer_source"; then
    fail 'CJK names are not using the same localized SmallFont family as the Chinese UI.'
fi
if rg -q 'Fonts\\\\Chinese|System\.Xml\.Linq|ChineseGlyph' "$renderer_source"; then
    fail 'The old heavy bitmap-font parser is still active.'
fi
if ! rg -q 'DrawPinnedIndicator' "$menu_source" || ! rg -q 'rowBounds\.Right' "$menu_source"; then
    fail 'Pinned state is not drawn in a fixed slot on the right side of each row.'
fi
if rg -q 'bounds\.X \+ 28' "$menu_source"; then
    fail 'List names still shift right to make room for a leading marker.'
fi
if rg -q 'translate\("menu\.search"\).*new Vector2' "$menu_source"; then
    fail 'Search is still rendered as an external label beside the input.'
fi
if ! rg -q 'Placeholder' "$textbox_source" || ! rg -q 'Placeholder = this\.translate\("menu\.search"\)' "$menu_source"; then
    fail 'The search field does not own its localized placeholder text.'
fi
if rg -q 'ListScrollbarTrack[^\r\n]*ListArea\.Height - 12' "$menu_source"; then
    fail 'The main scrollbar still extends below the final visible row.'
fi
if ! rg -q 'ListRowsHeight[^\r\n]*VisibleRows \* RowHeight - 4' "$menu_source" \
    || ! rg -q 'ListScrollbarTrack[^\r\n]*ListRowsHeight - 4' "$menu_source"; then
    fail 'The main scrollbar is not derived from the visible row content height.'
fi
if rg -q 'DrawString\([^\r\n]*(▲|▼)' "$coordinate_source"; then
    fail 'The map dropdown still draws its open/closed state with unsupported font triangle glyphs.'
fi
if ! rg -q 'DrawDropdownCaret' "$coordinate_source"; then
    fail 'The map dropdown has no font-independent caret renderer.'
fi
if [[ "$(jq -r '."menu.warp-to" // empty' "$chinese_i18n")" == *'「'* ]] \
    || [[ "$(jq -r '."menu.warp-to" // empty' "$chinese_i18n")" == *'」'* ]]; then
    fail 'The Chinese warp action still contains unsupported corner brackets that render as question marks.'
fi
if [[ "$(jq -r '."menu.warp-to" // empty' "$chinese_i18n")" != '传送到 {{name}}' ]]; then
    fail 'The Chinese warp action is not the expected punctuation-free label.'
fi
if [[ "$(jq -r '."category.quick-access" // empty' "$default_i18n")" != "Quick Access" ]]; then
    fail 'The English Common category was not renamed to Quick Access.'
fi
if [[ "$(jq -r '."menu.pin" // empty' "$default_i18n")" != "Pin" ]] || [[ "$(jq -r '."menu.unpin" // empty' "$default_i18n")" != "Unpin" ]]; then
    fail 'English actions are not consistently named Pin and Unpin.'
fi
if [[ "$(jq -r '."category.quick-access" // empty' "$chinese_i18n")" != "常用" ]]; then
    fail 'The Chinese Quick Access category label is missing.'
fi
if [[ "$(jq -r '."category.bookmarks" // empty' "$chinese_i18n")" != "书签" ]]; then
    fail 'The Chinese Bookmarks tab still uses the problematic four-character label.'
fi

if (( failures > 0 )); then
    printf 'Screenshot regression checks failed: %d\n' "$failures" >&2
    exit 1
fi

printf 'Screenshot regression checks passed: %s\n' "$project_root"
