using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using WarpBookmarks.Models;

namespace WarpBookmarks.UI;

/// <summary>Names a manually-created bookmark before it is persisted.</summary>
internal sealed class BookmarkCreateDialog : IClickableMenu
{
    private const int DialogWidth = 700;
    private const int DialogHeight = 390;
    private static readonly Rectangle ParchmentSourceRect = new(0, 0, 320, 180);

    private readonly LocationReference location;
    private readonly string suggestedName;
    private readonly Action<string> save;
    private readonly Action cancel;
    private readonly Func<string, string> translate;
    private readonly MultilingualTextRenderer textRenderer;
    private readonly Texture2D parchmentTexture;
    private readonly TextBox inputBox;

    private Rectangle SuggestionButton => new(this.xPositionOnScreen + 70, this.yPositionOnScreen + 245, 250, 48);
    private Rectangle SaveButton => new(this.xPositionOnScreen + 380, this.yPositionOnScreen + 245, 250, 48);

    public BookmarkCreateDialog(
        LocationReference location,
        string suggestedName,
        Action<string> save,
        Action cancel,
        Func<string, string> translate,
        MultilingualTextRenderer textRenderer
    )
        : base(
            (Game1.uiViewport.Width - DialogWidth) / 2,
            (Game1.uiViewport.Height - DialogHeight) / 2,
            DialogWidth,
            DialogHeight,
            showUpperRightCloseButton: true
        )
    {
        this.location = location;
        this.suggestedName = suggestedName;
        this.save = save;
        this.cancel = cancel;
        this.translate = translate;
        this.textRenderer = textRenderer;
        this.parchmentTexture = Game1.content.Load<Texture2D>("LooseSprites\\letterBG");
        Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.inputBox = new MultilingualTextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor, this.textRenderer)
        {
            X = this.xPositionOnScreen + 70,
            Y = this.yPositionOnScreen + 164,
            Width = DialogWidth - 140,
            Text = "",
            Selected = true
        };
        Game1.keyboardDispatcher.Subscriber = this.inputBox;
    }

    public override void update(GameTime time)
    {
        base.update(time);
        this.inputBox.Update();
        this.inputBox.Selected = true;
        Game1.keyboardDispatcher.Subscriber = this.inputBox;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.upperRightCloseButton?.containsPoint(x, y) == true)
        {
            this.Cancel();
            return;
        }

        this.inputBox.Selected = true;
        if (this.SuggestionButton.Contains(x, y))
        {
            this.inputBox.Text = this.suggestedName;
            Game1.playSound("smallSelect");
            return;
        }
        if (this.SaveButton.Contains(x, y) && this.CanSave)
            this.Save();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            this.Cancel();
            return;
        }
        if (key == Keys.Enter && this.CanSave)
        {
            this.Save();
            return;
        }

        // The empty initial value avoids requiring Left/Right caret movement, which
        // Stardew 1.6.15 doesn't deliver reliably to TextBox in custom menus.
        if (this.inputBox.Selected)
            return;
        base.receiveKeyPress(key);
    }

    protected override void cleanupBeforeExit()
    {
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this.inputBox))
            Game1.keyboardDispatcher.Subscriber = null;
        base.cleanupBeforeExit();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(
            this.parchmentTexture,
            new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height),
            ParchmentSourceRect,
            Color.White
        );
        b.DrawString(Game1.dialogueFont, this.translate("create.title"), new Vector2(this.xPositionOnScreen + 48, this.yPositionOnScreen + 28), Game1.textColor);
        string locationText = this.translate("create.location-summary")
            .Replace("{{location}}", this.location.DisplayName)
            .Replace("{{map}}", this.location.LocationName)
            .Replace("{{x}}", this.location.TileX.ToString())
            .Replace("{{y}}", this.location.TileY.ToString());
        this.textRenderer.DrawString(b, locationText, new Vector2(this.xPositionOnScreen + 70, this.yPositionOnScreen + 91), Color.DarkSlateGray, Game1.smallFont);
        b.DrawString(Game1.smallFont, this.translate("create.name-label"), new Vector2(this.xPositionOnScreen + 70, this.yPositionOnScreen + 132), Game1.textColor);
        this.inputBox.Draw(b);
        string suggestionText = this.textRenderer.FitText(this.translate("create.suggestion-label") + this.suggestedName, DialogWidth - 140, Game1.smallFont);
        this.textRenderer.DrawString(b, suggestionText, new Vector2(this.xPositionOnScreen + 70, this.yPositionOnScreen + 217), Color.DarkSlateGray, Game1.smallFont);
        this.DrawButton(b, this.SuggestionButton, this.translate("create.use-suggestion"), enabled: true);
        this.DrawButton(b, this.SaveButton, this.translate("create.save"), enabled: this.CanSave);
        b.DrawString(Game1.smallFont, this.translate("create.input-hint"), new Vector2(this.xPositionOnScreen + 70, this.yPositionOnScreen + 318), Color.DarkSlateGray);
        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private bool CanSave => this.inputBox.Text.Trim().Length > 0;

    private void Save()
    {
        if (!this.CanSave)
        {
            Game1.playSound("cancel");
            return;
        }
        string name = this.inputBox.Text.Trim();
        this.exitThisMenu();
        this.save(name);
    }

    private void Cancel()
    {
        this.exitThisMenu();
        this.cancel();
    }

    private void DrawButton(SpriteBatch b, Rectangle bounds, string label, bool enabled)
    {
        Color tint = !enabled
            ? Color.Gray * 0.65f
            : bounds.Contains(Game1.getMousePosition(true))
                ? Color.Wheat
                : Color.White;
        IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), enabled ? Game1.textColor : Color.DarkGray);
    }
}
