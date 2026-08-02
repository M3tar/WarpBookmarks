using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace WarpBookmarks.UI;

/// <summary>A controlled bookmark name editor using the game's TextBox input component.</summary>
internal sealed class BookmarkRenameDialog : IClickableMenu
{
    private const int DialogWidth = 640;
    private const int DialogHeight = 300;
    private static readonly Rectangle ParchmentSourceRect = new(0, 0, 320, 180);

    private readonly Action<string> save;
    private readonly Action cancel;
    private readonly Func<string, string> translate;
    private readonly Texture2D parchmentTexture;
    private readonly TextBox inputBox;

    private Rectangle SaveButton => new(this.xPositionOnScreen + 235, this.yPositionOnScreen + 214, 170, 48);

    public BookmarkRenameDialog(string currentName, Action<string> save, Action cancel, Func<string, string> translate)
        : base(
            (Game1.uiViewport.Width - DialogWidth) / 2,
            (Game1.uiViewport.Height - DialogHeight) / 2,
            DialogWidth,
            DialogHeight,
            showUpperRightCloseButton: true
        )
    {
        this.save = save;
        this.cancel = cancel;
        this.translate = translate;
        this.parchmentTexture = Game1.content.Load<Texture2D>("LooseSprites\\letterBG");
        Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.inputBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
        {
            X = this.xPositionOnScreen + 70,
            Y = this.yPositionOnScreen + 120,
            Width = DialogWidth - 140,
            Text = currentName,
            Selected = true
        };
        Game1.keyboardDispatcher.Subscriber = this.inputBox;
    }

    public override void update(GameTime time)
    {
        base.update(time);
        this.inputBox.Update();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.upperRightCloseButton?.containsPoint(x, y) == true)
        {
            this.Cancel();
            return;
        }

        this.inputBox.Selected = true;
        if (this.SaveButton.Contains(x, y))
            this.Save();
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            this.Cancel();
            return;
        }
        if (key == Keys.Enter)
        {
            this.Save();
            return;
        }

        // Keep character input isolated from base-menu shortcuts. Stardew 1.6.15's
        // TextBox still doesn't move its caret for Left/Right in this custom menu;
        // that limitation is documented instead of adding unverified reflection.
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
        b.DrawString(Game1.dialogueFont, this.translate("rename.title"), new Vector2(this.xPositionOnScreen + 48, this.yPositionOnScreen + 30), Game1.textColor);
        b.DrawString(Game1.smallFont, this.translate("rename.name-label"), new Vector2(this.xPositionOnScreen + 70, this.yPositionOnScreen + 88), Game1.textColor);
        this.inputBox.Draw(b);
        this.DrawButton(b, this.SaveButton, this.translate("rename.save"));
        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private void Save()
    {
        string name = this.inputBox.Text.Trim();
        if (name.Length == 0)
        {
            Game1.playSound("cancel");
            return;
        }
        this.exitThisMenu();
        this.save(name);
    }

    private void Cancel()
    {
        this.exitThisMenu();
        this.cancel();
    }

    private void DrawButton(SpriteBatch b, Rectangle bounds, string label)
    {
        Color tint = bounds.Contains(Game1.getMousePosition(true)) ? Color.Wheat : Color.White;
        IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), Game1.textColor);
    }
}
