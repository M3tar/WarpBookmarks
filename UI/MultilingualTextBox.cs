using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace WarpBookmarks.UI;

/// <summary>A game text box whose visible text can fall back to the built-in Chinese font.</summary>
internal sealed class MultilingualTextBox : TextBox
{
    private readonly MultilingualTextRenderer textRenderer;

    public string Placeholder { get; set; } = "";

    public MultilingualTextBox(
        Texture2D textBoxTexture,
        Texture2D? caretTexture,
        SpriteFont font,
        Color textColor,
        MultilingualTextRenderer textRenderer
    )
        : base(textBoxTexture, caretTexture, font, textColor)
    {
        this.textRenderer = textRenderer;
    }

    public override void Draw(SpriteBatch spriteBatch, bool drawShadow = true)
    {
        if (this._textBoxTexture is not null)
        {
            spriteBatch.Draw(this._textBoxTexture, new Rectangle(this.X, this.Y, 16, this.Height), new Rectangle(0, 0, 16, this.Height), Color.White);
            spriteBatch.Draw(this._textBoxTexture, new Rectangle(this.X + 16, this.Y, this.Width - 32, this.Height), new Rectangle(16, 0, 4, this.Height), Color.White);
            spriteBatch.Draw(this._textBoxTexture, new Rectangle(this.X + this.Width - 16, this.Y, 16, this.Height), new Rectangle(this._textBoxTexture.Bounds.Width - 16, 0, 16, this.Height), Color.White);
        }
        else
        {
            Game1.drawDialogueBox(this.X - 32, this.Y - 102, this.Width + 80, this.Height, speaker: false, drawOnlyBox: true);
        }

        bool showingPlaceholder = !this.Selected && string.IsNullOrEmpty(this.Text) && this.Placeholder.Length > 0;
        string visibleText = showingPlaceholder ? this.Placeholder : this.Text;
        while (visibleText.Length > 0
            && this.textRenderer.MeasureString(visibleText, this._font).X > this.Width - 32)
        {
            visibleText = visibleText[1..];
        }

        Vector2 position = new(this.X + 16, this.Y + (this._textBoxTexture is not null ? 12 : 8));
        Color visibleColor = showingPlaceholder ? Color.DarkSlateGray * 0.65f : this._textColor;
        if (drawShadow && !showingPlaceholder)
            this.textRenderer.DrawString(spriteBatch, visibleText, position + new Vector2(2f, 2f), Color.Black * 0.35f, this._font);
        this.textRenderer.DrawString(spriteBatch, visibleText, position, visibleColor, this._font);

        bool caretVisible = Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1000 >= 500;
        if (caretVisible && this.Selected)
        {
            int caretX = this.X + 18 + (int)this.textRenderer.MeasureString(visibleText, this._font).X;
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(caretX, this.Y + 8, 4, 32), this._textColor);
        }
    }
}
