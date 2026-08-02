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
    private const int MenuWidth = 960;
    private const int MenuHeight = 540;
    private const int RowHeight = 48;
    private const int ActionPadding = 18;
    private const int ActionGap = 12;
    private static readonly Rectangle ParchmentSourceRect = new(0, 0, 320, 180);

    private readonly List<WarpDestination> allDestinations;
    private readonly List<WarpDestination> destinations;
    private readonly Action<WarpDestination> warp;
    private readonly Action<WarpDestination, bool> setFavorite;
    private readonly Action<WarpDestination> remove;
    private readonly Action<WarpDestination> restoreHidden;
    private readonly Action<WarpDestination> edit;
    private readonly Action restoreDefaults;
    private readonly Action openCoordinates;
    private readonly Func<WarpDestination?> recordCurrent;
    private readonly Func<string, string> translate;
    private readonly string shortcutText;
    private readonly Texture2D parchmentTexture;
    private readonly TextBox searchBox;
    private string previousSearch = "";
    private int selectedIndex;
    private int scrollOffset;
    private string? pendingRemoveId;
    private DestinationCategory selectedCategory = DestinationCategory.All;

    private Rectangle CategoryArea => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + 86, 420, 36);
    private Rectangle ListArea => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + 132, 420, this.height - 220);
    private Rectangle DetailArea => new(this.xPositionOnScreen + 478, this.yPositionOnScreen + 86, this.width - 514, this.height - 174);
    private int ActionButtonWidth => (this.DetailArea.Width - ActionPadding * 2 - ActionGap) / 2;
    private Rectangle WarpButton => this.GetActionButtonBounds(column: 0, rowFromBottom: 0);
    private Rectangle FavoriteButton => this.GetActionButtonBounds(column: 1, rowFromBottom: 0);
    private Rectangle EditButton => this.GetActionButtonBounds(column: 0, rowFromBottom: 1);
    private Rectangle RemoveButton => this.GetActionButtonBounds(column: 1, rowFromBottom: 1);
    private Rectangle RecordButton => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + this.height - 70, 170, 44);
    private Rectangle CoordinateButton => new(this.xPositionOnScreen + 218, this.yPositionOnScreen + this.height - 70, 210, 44);
    private Rectangle RestoreButton => new(this.xPositionOnScreen + 440, this.yPositionOnScreen + this.height - 70, 180, 44);
    private Rectangle SearchArea => new(this.searchBox.X, this.searchBox.Y, this.searchBox.Width, 48);

    public WarpBookmarksMenu(
        IEnumerable<WarpDestination> destinations,
        Action<WarpDestination> warp,
        Action<WarpDestination, bool> setFavorite,
        Action<WarpDestination> remove,
        Action<WarpDestination> restoreHidden,
        Action<WarpDestination> edit,
        Action restoreDefaults,
        Action openCoordinates,
        Func<WarpDestination?> recordCurrent,
        Func<string, string> translate,
        string shortcutText
    )
        : base(
            (Game1.uiViewport.Width - MenuWidth) / 2,
            (Game1.uiViewport.Height - MenuHeight) / 2,
            MenuWidth,
            MenuHeight,
            showUpperRightCloseButton: true
        )
    {
        this.allDestinations = destinations.ToList();
        this.destinations = new List<WarpDestination>(this.allDestinations);
        this.warp = warp;
        this.setFavorite = setFavorite;
        this.remove = remove;
        this.restoreHidden = restoreHidden;
        this.edit = edit;
        this.restoreDefaults = restoreDefaults;
        this.openCoordinates = openCoordinates;
        this.recordCurrent = recordCurrent;
        this.translate = translate;
        this.shortcutText = shortcutText;
        this.parchmentTexture = Game1.content.Load<Texture2D>("LooseSprites\\letterBG");
        Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.searchBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
        {
            X = this.xPositionOnScreen + 594,
            Y = this.yPositionOnScreen + 20,
            Width = 260,
            Selected = false
        };
        Game1.keyboardDispatcher.Subscriber = this.searchBox;
        this.ApplySearchFilter();
    }

    public override void update(GameTime time)
    {
        base.update(time);
        this.searchBox.Update();
        if (string.Equals(this.previousSearch, this.searchBox.Text, StringComparison.Ordinal))
            return;

        this.previousSearch = this.searchBox.Text;
        this.ApplySearchFilter();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.readyToClose() && this.upperRightCloseButton?.containsPoint(x, y) == true)
            return;

        if (this.SearchArea.Contains(x, y))
        {
            this.searchBox.Selected = true;
            return;
        }
        this.searchBox.Selected = false;

        for (int categoryIndex = 0; categoryIndex < 5; categoryIndex++)
        {
            Rectangle bounds = this.GetCategoryBounds(categoryIndex);
            if (!bounds.Contains(x, y))
                continue;
            this.selectedCategory = (DestinationCategory)categoryIndex;
            this.pendingRemoveId = null;
            this.ApplySearchFilter();
            Game1.playSound("smallSelect");
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
                this.allDestinations.Add(created);
                this.searchBox.Text = "";
                this.previousSearch = "";
                this.selectedCategory = DestinationCategory.Bookmarks;
                this.ApplySearchFilter();
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
        if (this.CoordinateButton.Contains(x, y))
        {
            this.openCoordinates();
            return;
        }

        WarpDestination? selected = this.Selected;
        if (selected is null)
            return;

        if (this.WarpButton.Contains(x, y) && selected.CanWarp)
        {
            this.warp(selected);
            return;
        }
        if (this.EditButton.Contains(x, y) && selected.Kind == WarpDestinationKind.Bookmark)
        {
            this.edit(selected);
            return;
        }
        if (this.FavoriteButton.Contains(x, y)
            && !selected.IsHidden
            && (selected.Kind is WarpDestinationKind.Bookmark or WarpDestinationKind.Default))
        {
            selected.IsFavorite = !selected.IsFavorite;
            this.setFavorite(selected, selected.IsFavorite);
            this.ApplySearchFilter();
            Game1.playSound("drumkit6");
            return;
        }
        if (this.RemoveButton.Contains(x, y) && selected.CanRemove)
        {
            if (selected.IsHidden && selected.Kind == WarpDestinationKind.Default)
            {
                this.restoreHidden(selected);
                Game1.playSound("smallSelect");
                return;
            }
            if (this.pendingRemoveId != selected.Id)
            {
                this.pendingRemoveId = selected.Id;
                Game1.playSound("cancel");
                return;
            }

            this.remove(selected);
            if (selected.Kind == WarpDestinationKind.Default)
                return;
            this.allDestinations.RemoveAll(item => item.Id == selected.Id && item.Kind == selected.Kind);
            this.ApplySearchFilter();
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

        // Focused search TextBox owns cursor/editing keys through the keyboard dispatcher.
        if (this.searchBox.Selected)
            return;
        if (key == Keys.Left)
        {
            this.MoveCategory(-1);
            return;
        }
        if (key == Keys.Right)
        {
            this.MoveCategory(1);
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
        if (key is Keys.Enter && this.Selected?.CanWarp == true)
        {
            this.warp(this.Selected);
            return;
        }
        if (key is Keys.Delete && this.Selected?.CanRemove == true && !this.Selected.IsHidden)
        {
            this.pendingRemoveId = this.Selected.Id;
            Game1.addHUDMessage(new HUDMessage(this.translate("hud.remove-confirm"), HUDMessage.error_type));
            return;
        }
        base.receiveKeyPress(key);
    }

    protected override void cleanupBeforeExit()
    {
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this.searchBox))
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
        b.DrawString(Game1.dialogueFont, this.translate("menu.title"), new Vector2(this.xPositionOnScreen + 36, this.yPositionOnScreen + 24), Game1.textColor);
        b.DrawString(Game1.smallFont, this.translate("menu.search"), new Vector2(this.searchBox.X - 70, this.searchBox.Y + 12), Game1.textColor);
        this.searchBox.Draw(b);
        this.DrawCategories(b);

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
        this.DrawButton(b, this.CoordinateButton, this.translate("menu.coordinates-action"), true);
        this.DrawButton(b, this.RestoreButton, this.translate("menu.restore-defaults"), true);
        b.DrawString(Game1.smallFont, this.shortcutText, new Vector2(this.RestoreButton.Right + 18, this.RestoreButton.Y + 11), Color.DarkSlateGray);
        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private WarpDestination? Selected => this.destinations.Count == 0 ? null : this.destinations[Math.Clamp(this.selectedIndex, 0, this.destinations.Count - 1)];

    private void DrawDetails(SpriteBatch b)
    {
        Rectangle area = this.DetailArea;
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

        this.DrawButton(b, this.WarpButton, this.translate("menu.warp"), selected.CanWarp);
        this.DrawButton(b, this.EditButton, this.translate("menu.rename"), selected.Kind == WarpDestinationKind.Bookmark);
        bool canFavorite = !selected.IsHidden
            && (selected.Kind is WarpDestinationKind.Bookmark or WarpDestinationKind.Default);
        this.DrawButton(b, this.FavoriteButton, selected.IsFavorite ? this.translate("menu.unfavorite") : this.translate("menu.favorite"), canFavorite);
        string removeText = selected.IsHidden
            ? this.translate("menu.restore")
            : this.pendingRemoveId == selected.Id
            ? selected.Kind == WarpDestinationKind.Default
                ? this.translate("menu.confirm-hide")
                : this.translate("menu.confirm-delete")
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
        Color tint = !enabled
            ? Color.Gray * 0.65f
            : bounds.Contains(Game1.getMousePosition(true))
                ? Color.Wheat
                : Color.White;
        IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(Game1.smallFont, label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), enabled ? Game1.textColor : Color.DarkGray);
    }

    private Rectangle GetActionButtonBounds(int column, int rowFromBottom)
    {
        int x = this.DetailArea.X + ActionPadding + column * (this.ActionButtonWidth + ActionGap);
        int y = this.DetailArea.Bottom - 62 - rowFromBottom * 54;
        return new Rectangle(x, y, this.ActionButtonWidth, 44);
    }

    private void EnsureSelectedVisible()
    {
        int visibleRows = Math.Max(1, this.ListArea.Height / RowHeight);
        if (this.selectedIndex < this.scrollOffset)
            this.scrollOffset = this.selectedIndex;
        else if (this.selectedIndex >= this.scrollOffset + visibleRows)
            this.scrollOffset = this.selectedIndex - visibleRows + 1;
    }

    private void ApplySearchFilter()
    {
        string selectedId = this.Selected?.Id ?? "";
        WarpDestinationKind? selectedKind = this.Selected?.Kind;
        string query = this.searchBox.Text.Trim();
        this.destinations.Clear();
        this.destinations.AddRange(this.allDestinations.Where(destination =>
            this.MatchesCategory(destination)
            && (query.Length == 0
                || destination.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || destination.Location.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || destination.Location.LocationName.Contains(query, StringComparison.OrdinalIgnoreCase))
        ));

        int previousSelection = this.destinations.FindIndex(item => item.Id == selectedId && item.Kind == selectedKind);
        this.selectedIndex = previousSelection >= 0 ? previousSelection : 0;
        this.scrollOffset = 0;
        this.EnsureSelectedVisible();
    }

    private bool MatchesCategory(WarpDestination destination)
    {
        return this.selectedCategory switch
        {
            DestinationCategory.Favorites => destination.IsFavorite,
            DestinationCategory.Bookmarks => destination.Kind == WarpDestinationKind.Bookmark,
            DestinationCategory.Defaults => destination.Kind == WarpDestinationKind.Default && !destination.IsHidden,
            DestinationCategory.Hidden => destination.IsHidden,
            _ => !destination.IsHidden
        };
    }

    private void DrawCategories(SpriteBatch b)
    {
        for (int categoryIndex = 0; categoryIndex < 5; categoryIndex++)
        {
            DestinationCategory category = (DestinationCategory)categoryIndex;
            Rectangle bounds = this.GetCategoryBounds(categoryIndex);
            bool selected = category == this.selectedCategory;
            Color tint = selected
                ? new Color(196, 135, 70) * 0.55f
                : bounds.Contains(Game1.getMousePosition(true))
                    ? new Color(222, 184, 120) * 0.4f
                    : new Color(120, 78, 48) * 0.12f;
            b.Draw(Game1.staminaRect, bounds, tint);
            string label = this.translate($"category.{category.ToString().ToLowerInvariant()}");
            Vector2 size = Game1.smallFont.MeasureString(label);
            b.DrawString(Game1.smallFont, label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), Game1.textColor);
        }
    }

    private Rectangle GetCategoryBounds(int index)
    {
        const int gap = 6;
        int width = (this.CategoryArea.Width - gap * 4) / 5;
        return new Rectangle(this.CategoryArea.X + index * (width + gap), this.CategoryArea.Y, width, this.CategoryArea.Height);
    }

    private void MoveCategory(int direction)
    {
        int next = Math.Clamp((int)this.selectedCategory + direction, 0, 4);
        if (next == (int)this.selectedCategory)
            return;
        this.selectedCategory = (DestinationCategory)next;
        this.ApplySearchFilter();
        Game1.playSound("smallSelect");
    }

    private enum DestinationCategory
    {
        All,
        Favorites,
        Bookmarks,
        Defaults,
        Hidden
    }
}
