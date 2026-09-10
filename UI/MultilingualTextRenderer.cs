using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace WarpBookmarks.UI;

/// <summary>Draws user-provided text with the active font and the Chinese UI SmallFont as fallback.</summary>
internal sealed class MultilingualTextRenderer
{
    private const float ChineseBodyScale = 1f;
    private const float ChineseTitleScale = 1.18f;

    private readonly IMonitor monitor;
    private bool triedLoadingChineseFont;
    private SpriteFont? chineseSmallFont;

    public MultilingualTextRenderer(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public Vector2 MeasureString(string text, SpriteFont primaryFont, bool title = false)
    {
        if (!this.ShouldUseChineseFallback(text, primaryFont) || !this.TryLoadChineseFont())
            return primaryFont.MeasureString(text);

        SpriteFont chineseFont = this.chineseSmallFont!;
        float chineseScale = title ? ChineseTitleScale : ChineseBodyScale;
        float lineWidth = 0;
        float maximumWidth = 0;
        int lineCount = 1;
        foreach (char character in text)
        {
            if (character == '\n')
            {
                maximumWidth = Math.Max(maximumWidth, lineWidth);
                lineWidth = 0;
                lineCount++;
                continue;
            }
            if (character == '\r')
                continue;

            lineWidth += GetAdvance(character, primaryFont, chineseFont, chineseScale);
        }

        float lineHeight = Math.Max(primaryFont.LineSpacing, chineseFont.LineSpacing * chineseScale);
        return new Vector2(Math.Max(maximumWidth, lineWidth), lineCount * lineHeight);
    }

    public void DrawString(SpriteBatch batch, string text, Vector2 position, Color color, SpriteFont primaryFont, bool title = false)
    {
        if (!this.ShouldUseChineseFallback(text, primaryFont) || !this.TryLoadChineseFont())
        {
            batch.DrawString(primaryFont, text, position, color);
            return;
        }

        SpriteFont chineseFont = this.chineseSmallFont!;
        float chineseScale = title ? ChineseTitleScale : ChineseBodyScale;
        float lineHeight = Math.Max(primaryFont.LineSpacing, chineseFont.LineSpacing * chineseScale);
        float lineStartX = position.X;
        foreach (char character in text)
        {
            if (character == '\n')
            {
                position.X = lineStartX;
                position.Y += lineHeight;
                continue;
            }
            if (character == '\r')
                continue;

            if (primaryFont.Characters.Contains(character))
            {
                batch.DrawString(primaryFont, character.ToString(), position, color);
            }
            else if (chineseFont.Characters.Contains(character))
            {
                float verticalOffset = (lineHeight - chineseFont.LineSpacing * chineseScale) / 2f;
                batch.DrawString(
                    chineseFont,
                    character.ToString(),
                    position + new Vector2(0, verticalOffset),
                    color,
                    0f,
                    Vector2.Zero,
                    chineseScale,
                    SpriteEffects.None,
                    1f
                );
            }
            else
            {
                batch.DrawString(primaryFont, "?", position, color);
            }

            position.X += GetAdvance(character, primaryFont, chineseFont, chineseScale);
        }
    }

    public string FitText(string text, float maximumWidth, SpriteFont primaryFont, bool title = false)
    {
        if (this.MeasureString(text, primaryFont, title).X <= maximumWidth)
            return text;

        const string ellipsis = "…";
        string shortened = text;
        while (shortened.Length > 0
            && this.MeasureString(shortened + ellipsis, primaryFont, title).X > maximumWidth)
        {
            shortened = shortened[..^1];
        }
        return shortened.Length == 0 ? ellipsis : shortened + ellipsis;
    }

    public string WrapText(string text, float maximumWidth, SpriteFont primaryFont)
    {
        if (this.MeasureString(text, primaryFont).X <= maximumWidth)
            return text;

        List<string> lines = new();
        string remaining = text.Trim();
        while (remaining.Length > 0)
        {
            int length = 1;
            int lastSpace = -1;
            while (length <= remaining.Length
                && this.MeasureString(remaining[..length], primaryFont).X <= maximumWidth)
            {
                if (char.IsWhiteSpace(remaining[length - 1]))
                    lastSpace = length - 1;
                length++;
            }

            if (length > remaining.Length)
            {
                lines.Add(remaining);
                break;
            }

            int breakAt = lastSpace > 0 ? lastSpace : Math.Max(1, length - 1);
            lines.Add(remaining[..breakAt].TrimEnd());
            remaining = remaining[breakAt..].TrimStart();
        }

        return string.Join("\n", lines);
    }

    private bool ShouldUseChineseFallback(string text, SpriteFont primaryFont)
    {
        return text.Any(character => IsCjk(character) && !primaryFont.Characters.Contains(character));
    }

    private bool TryLoadChineseFont()
    {
        if (this.triedLoadingChineseFont)
            return this.chineseSmallFont is not null;

        this.triedLoadingChineseFont = true;
        try
        {
            SpriteFont font = Game1.content.Load<SpriteFont>("Fonts\\SmallFont", LocalizedContentManager.LanguageCode.zh);
            if (!font.Characters.Any(IsCjk))
                throw new InvalidOperationException("The localized Chinese SmallFont contains no CJK glyphs.");
            this.chineseSmallFont = font;
        }
        catch (Exception ex)
        {
            this.monitor.Log($"Couldn't load the Chinese UI SmallFont fallback. Saved CJK names may be unreadable in a Latin-language UI. {ex}", LogLevel.Warn);
        }

        return this.chineseSmallFont is not null;
    }

    private static float GetAdvance(char character, SpriteFont primaryFont, SpriteFont chineseFont, float chineseScale)
    {
        if (primaryFont.Characters.Contains(character))
            return primaryFont.MeasureString(character.ToString()).X;
        if (chineseFont.Characters.Contains(character))
            return chineseFont.MeasureString(character.ToString()).X * chineseScale;
        return primaryFont.MeasureString("?").X;
    }

    private static bool IsCjk(char character)
    {
        return character is >= '\u3000' and <= '\u303f'
            or >= '\u3400' and <= '\u4dbf'
            or >= '\u4e00' and <= '\u9fff'
            or >= '\uf900' and <= '\ufaff'
            or >= '\uff00' and <= '\uffef';
    }
}
