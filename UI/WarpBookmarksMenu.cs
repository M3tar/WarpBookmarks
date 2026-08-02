using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using WarpBookmarks.Models;

namespace WarpBookmarks.UI;

/// <summary>A compact parchment travel book with a scrollable destination list and details panel.</summary>
internal sealed class WarpBookmarksMenu : IClickableMenu
{
    private const int RowHeight = 48;
    private readonly List<WarpDestination> destinations;
    private readonly Action<WarpDestination> warp;
    private readonly Action<WarpDestination, bool> setFavorite;
    private readonly Action<WarpDestination> remove;
    private readonly Action<WarpDestination> edit;
    private readonly Action restoreDefaults;
    private readonly Func<WarpDestination?> recordCurrent;
    private readonly Func<string, string> translate;
    private readonly string shortcutText;
    private int selectedIndex;
    private int scrollOffset;
    private string? pendingRemoveId;

    private Rectangle ListArea => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + 86, 420, this.height - 174);
    private Rectangle DetailArea => new(this.xPositionOnScreen + 478, this.yPositionOnScreen + 86, this.width - 514, this.height - 174);
    private Rectangle WarpButton => new(this.DetailArea.X + 18, this.DetailArea.Bottom - 62, 150, 44);
    private Rectangle FavoriteButton => new(this.DetailArea.X + 180, this.DetailArea.Bottom - 62, 150, 44);
    private Rectangle RemoveButton => new(this.DetailArea.X + 342, this.DetailArea.Bottom - 62, 150, 44);
    private Rectangle EditButton => new(this.DetailArea.X + 18, this.DetailArea.Bottom - 116, 150, 44);
    private Rectangle RecordButton => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + this.height - 70, 210, 44);
    private Rectangle RestoreButton => new(this.xPositionOnScreen + 258, this.yPositionOnScreen + this.height - 70, 210, 44);

    public WarpBookmarksMenu(
        IEnumerable<WarpDestination> destinations,
        Action<WarpDestination> warp,
        Action<WarpDestination, bool> setFavorite,
        Action<WarpDestination> remove,
        Action<WarpDestination> edit,
        Action restoreDefaults,
        Func<WarpDestination?> recordCurrent,
        Func<string, string> translate,
        string shortcutText
    )
        : base(
            Math.Max(16, (Game1.uiViewport.Width - Math.Min(960, Game1.uiViewport.Width - 32)) / 2),
            Math.Max(16, (Game1.uiViewport.Height - Math.Min(600, Game1.uiViewport.Height - 32)) / 2),
            Math.Min(960, Game1.uiViewport.Width - 32),
            Math.Min(600, Game1.uiViewport.Height - 32),
            true
        )
    {
        this.destinations = destinations.ToList();
        this.warp = warp;
        this.setFavorite = setFavorite;
        this.remove = remove;
        this.edit = edit;
        this.restoreDefaults = restoreDefaults;
        this.recordCurrent = recordCurrent;
        this.translate = translate;
        this.shortcutText = shortcutText;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.upperRightCloseButton?.containsPoint(x, y) == true)
        {
            this.exitThisMenu();
            return;
        }

        int visibleRows = Math.Max(1, this.ListArea.Height / RowHeight);
        for (int row = 0; row < visibleRows; row++)
        {
            int index = this.scrollOffset + row;
            Rectangle bounds = new(this.ListArea.X, this.ListArea.Y + row * RowHeight, this.ListArea.Width, RowHeight - 4);
            if (index < this.destinations.Count && bounds.Contains(x, y))
            {
                this.selectedIndex = index;
                this.pendingRemoveId = null;
                Game1.playSound("shiny4");
                return;
            }
        }

        if (this.RecordButton.Contains(x, y))
        {
            WarpDestination? created = this.recordCurrent();
            if (created is not null)
            {
                this.destinations.Add(created);
                this.selectedIndex = this.destinations.Count - 1;
                this.EnsureSelectedVisible();
            }
            return;
        }
        if (this.RestoreButton.Contains(x, y))
        {
            this.restoreDefaults();
            return;
        }

        WarpDestination? selected = this.Selected;
        if (selected is null)
            return;

        if (this.WarpButton.Contains(x, y))
        {
            this.warp(selected);
            return;
        }
        if (this.EditButton.Contains(x, y) && selected.Kind == WarpDestinationKind.Bookmark)
        {
            this.edit(selected);
            return;
        }
        if (this.FavoriteButton.Contains(x, y) && selected.Kind is WarpDestinationKind.Bookmark or WarpDestinationKind.Default)
        {
            selected.IsFavorite = !selected.IsFavorite;
            this.setFavorite(selected, selected.IsFavorite);
            Game1.playSound("drumkit6");
            return;
        }
        if (this.RemoveButton.Contains(x, y) && selected.CanRemove)
        {
            if (this.pendingRemoveId != selected.Id)
            {
                this.pendingRemoveId = selected.Id;
                Game1.playSound("cancel");
                return;
            }

            this.remove(selected);
            this.destinations.RemoveAt(this.selectedIndex);
            this.selectedIndex = Math.Clamp(this.selectedIndex, 0, Math.Max(0, this.destinations.Count - 1));
            this.pendingRemoveId = null;
            this.EnsureSelectedVisible();
            Game1.playSound("trashcan");
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int visibleRows = Math.Max(1, this.ListArea.Height / RowHeight);
        int maxOffset = Math.Max(0, this.destinations.Count - visibleRows);
        this.scrollOffset = Math.Clamp(this.scrollOffset + (direction < 0 ? 1 : -1), 0, maxOffset);
        base.receiveScrollWheelAction(direction);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key is Keys.Escape)
        {
            this.exitThisMenu();
            return;
        }
        if (key is Keys.Down or Keys.S)
        {
            this.selectedIndex = Math.Min(this.destinations.Count - 1, this.selectedIndex + 1);
            this.EnsureSelectedVisible();
            return;
        }
        if (key is Keys.Up or Keys.W)
        {
            this.selectedIndex = Math.Max(0, this.selectedIndex - 1);
            this.EnsureSelectedVisible();
            return;
        }
        if (key is Keys.Enter && this.Selected is not null)
        {
            this.warp(this.Selected);
            return;
        }
        if (key is Keys.Delete && this.Selected?.CanRemove == true)
        {
            this.pendingRemoveId = this.Selected.Id;
            Game1.addHUDMessage(new HUDMessage(this.translate("hud.remove-confirm"), HUDMessage.error_type));
            return;
        }
        base.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        this.drawBackground(b);
        IClickableMenu.drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);
        b.DrawString(Game1.dialogueFont, this.translate("menu.title"), new Vector2(this.xPositionOnScreen + 36, this.yPositionOnScreen + 24), Game1.textColor);
        b.DrawString(Game1.smallFont, this.translate("menu.list-title"), new Vector2(this.ListArea.X, this.ListArea.Y - 30), Game1.textColor);

        int visibleRows = Math.Max(1, this.ListArea.Height / RowHeight);
        for (int row = 0; row < visibleRows; row++)
        {
            int index = this.scrollOffset + row;
            if (index >= this.destinations.Count)
                break;

            WarpDestination destination = this.destinations[index];
            Rectangle bounds = new(this.ListArea.X, this.ListArea.Y + row * RowHeight, this.ListArea.Width, RowHeight - 4);
            Color fill = index == this.selectedIndex ? new Color(196, 135, 70) * 0.55f : new Color(120, 78, 48) * 0.10f;
            b.Draw(Game1.staminaRect, bounds, fill);
            string marker = destination.IsFavorite ? "★ " : destination.Kind switch
            {
                WarpDestinationKind.Home => "⌂ ",
                WarpDestinationKind.Previous => "↩ ",
                _ => "  "
            };
            b.DrawString(Game1.smallFont, marker + destination.Name, new Vector2(bounds.X + 12, bounds.Y + 10), Game1.textColor);
        }

        this.DrawDetails(b);
        this.DrawButton(b, this.RecordButton, this.translate("menu.record"), true);
        this.DrawButton(b, this.RestoreButton, this.translate("menu.restore-defaults"), true);
        b.DrawString(Game1.smallFont, this.shortcutText, new Vector2(this.RestoreButton.Right + 18, this.RestoreButton.Y + 11), Color.DarkSlateGray);
        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private WarpDestination? Selected => this.destinations.Count == 0 ? null : this.destinations[Math.Clamp(this.selectedIndex, 0, this.destinations.Count - 1)];

    private void DrawDetails(SpriteBatch b)
    {
        Rectangle area = this.DetailArea;
        b.Draw(Game1.staminaRect, area, new Color(120, 78, 48) * 0.08f);
        WarpDestination? selected = this.Selected;
        if (selected is null)
        {
            b.DrawString(Game1.smallFont, this.translate("menu.empty"), new Vector2(area.X + 18, area.Y + 18), Game1.textColor);
            return;
        }

        int y = area.Y + 18;
        b.DrawString(Game1.dialogueFont, selected.Name, new Vector2(area.X + 18, y), Game1.textColor);
        y += 62;
        this.DrawDetailLine(b, "menu.location", selected.Location.DisplayName, area.X + 18, ref y);
        this.DrawDetailLine(b, "menu.internal-name", selected.Location.LocationName, area.X + 18, ref y);
        this.DrawDetailLine(b, "menu.coordinates", $"{selected.Location.TileX}, {selected.Location.TileY}", area.X + 18, ref y);
        this.DrawDetailLine(b, "menu.type", this.translate($"kind.{selected.Kind.ToString().ToLowerInvariant()}"), area.X + 18, ref y);

        this.DrawButton(b, this.WarpButton, this.translate("menu.warp"), true);
        this.DrawButton(b, this.EditButton, this.translate("menu.rename"), selected.Kind == WarpDestinationKind.Bookmark);
        bool canFavorite = selected.Kind is WarpDestinationKind.Bookmark or WarpDestinationKind.Default;
        this.DrawButton(b, this.FavoriteButton, selected.IsFavorite ? this.translate("menu.unfavorite") : this.translate("menu.favorite"), canFavorite);
        string removeText = this.pendingRemoveId == selected.Id
            ? this.translate("menu.confirm")
            : selected.Kind == WarpDestinationKind.Default ? this.translate("menu.hide") : this.translate("menu.delete");
        this.DrawButton(b, this.RemoveButton, removeText, selected.CanRemove);
    }

    private void DrawDetailLine(SpriteBatch b, string labelKey, string value, int x, ref int y)
    {
        b.DrawString(Game1.smallFont, $"{this.translate(labelKey)}：{value}", new Vector2(x, y), Game1.textColor);
        y += 36;
    }

    private void DrawButton(SpriteBatch b, Rectangle bounds, string label, bool enabled)
    {
        IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, enabled ? Color.White : Color.Gray * 0.65f);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), enabled ? Game1.textColor : Color.DarkGray);
    }

    private void EnsureSelectedVisible()
    {
        int visibleRows = Math.Max(1, this.ListArea.Height / RowHeight);
        if (this.selectedIndex < this.scrollOffset)
            this.scrollOffset = this.selectedIndex;
        else if (this.selectedIndex >= this.scrollOffset + visibleRows)
            this.scrollOffset = this.selectedIndex - visibleRows + 1;
    }
}
