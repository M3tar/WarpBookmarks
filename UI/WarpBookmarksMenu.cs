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
    private const int ActionGap = 10;
    private const int PinnedIndicatorWidth = 32;
    private static readonly Rectangle ParchmentSourceRect = new(0, 0, 320, 180);
    private static readonly string[] PinnedStarPattern =
    {
        "..#..",
        "#.#.#",
        ".###.",
        "#####",
        ".###.",
        ".#.#.",
        "#...#"
    };

    private readonly List<WarpDestination> allDestinations;
    private readonly List<WarpDestination> destinations;
    private readonly Action<WarpDestination> warp;
    private readonly Action<WarpDestination, bool> setPinned;
    private readonly Action<WarpDestination> remove;
    private readonly Action<WarpDestination> restoreHidden;
    private readonly Action<WarpDestination> edit;
    private readonly Action restoreDefaults;
    private readonly Action openCoordinates;
    private readonly Action recordCurrent;
    private readonly Action<DestinationCategory> categoryChanged;
    private readonly Func<string, string> translate;
    private readonly MultilingualTextRenderer textRenderer;
    private readonly string shortcutText;
    private readonly Texture2D parchmentTexture;
    private readonly TextBox searchBox;
    private string previousSearch = "";
    private int selectedIndex;
    private int scrollOffset;
    private string? pendingRemoveId;
    private DestinationCategory selectedCategory;

    private Rectangle CategoryArea => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + 86, this.width - 72, 36);
    private Rectangle ListArea => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + 132, 420, this.height - 220);
    private int ListRowsHeight => this.VisibleRows * RowHeight - 4;
    private Rectangle ListRowsArea => new(this.ListArea.X, this.ListArea.Y, this.ListArea.Width - 18, this.ListRowsHeight);
    private Rectangle ListScrollbarTrack => new(this.ListArea.Right - 12, this.ListArea.Y + 4, 9, this.ListRowsHeight - 4);
    private Rectangle DetailArea => new(this.xPositionOnScreen + 478, this.yPositionOnScreen + 132, this.width - 514, this.height - 220);
    private int ThreeActionButtonWidth => (this.DetailArea.Width - ActionPadding * 2 - ActionGap * 2) / 3;
    private int TwoActionButtonWidth => (this.DetailArea.Width - ActionPadding * 2 - ActionGap) / 2;
    private Rectangle BookmarkPinButton => this.GetThreeActionButtonBounds(0);
    private Rectangle BookmarkEditButton => this.GetThreeActionButtonBounds(1);
    private Rectangle BookmarkRemoveButton => this.GetThreeActionButtonBounds(2);
    private Rectangle ContextLeftButton => this.GetTwoActionButtonBounds(0);
    private Rectangle ContextRightButton => this.GetTwoActionButtonBounds(1);
    private Rectangle RecordButton => new(this.xPositionOnScreen + 36, this.yPositionOnScreen + this.height - 70, 170, 44);
    private Rectangle CoordinateButton => new(this.xPositionOnScreen + 218, this.yPositionOnScreen + this.height - 70, 210, 44);
    private Rectangle PrimaryWarpButton => new(this.xPositionOnScreen + 594, this.yPositionOnScreen + this.height - 70, 330, 44);
    private Rectangle SearchArea => new(this.searchBox.X, this.searchBox.Y, this.searchBox.Width, 48);
    private int VisibleRows => Math.Max(1, this.ListArea.Height / RowHeight);

    public WarpBookmarksMenu(
        IEnumerable<WarpDestination> destinations,
        Action<WarpDestination> warp,
        Action<WarpDestination, bool> setPinned,
        Action<WarpDestination> remove,
        Action<WarpDestination> restoreHidden,
        Action<WarpDestination> edit,
        Action restoreDefaults,
        Action openCoordinates,
        Action recordCurrent,
        DestinationCategory initialCategory,
        Action<DestinationCategory> categoryChanged,
        Func<string, string> translate,
        MultilingualTextRenderer textRenderer,
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
        this.setPinned = setPinned;
        this.remove = remove;
        this.restoreHidden = restoreHidden;
        this.edit = edit;
        this.restoreDefaults = restoreDefaults;
        this.openCoordinates = openCoordinates;
        this.recordCurrent = recordCurrent;
        this.selectedCategory = initialCategory;
        this.categoryChanged = categoryChanged;
        this.translate = translate;
        this.textRenderer = textRenderer;
        this.shortcutText = shortcutText;
        this.parchmentTexture = Game1.content.Load<Texture2D>("LooseSprites\\letterBG");
        Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.searchBox = new MultilingualTextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor, this.textRenderer)
        {
            X = this.xPositionOnScreen + 570,
            Y = this.yPositionOnScreen + 20,
            Width = 290,
            Placeholder = this.translate("menu.search"),
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
            this.categoryChanged(this.selectedCategory);
            this.pendingRemoveId = null;
            this.ApplySearchFilter();
            Game1.playSound("smallSelect");
            return;
        }

        if (this.ListScrollbarTrack.Contains(x, y) && this.destinations.Count > this.VisibleRows)
        {
            Rectangle thumb = this.GetListScrollbarThumb();
            if (!thumb.Contains(x, y))
            {
                int pageDirection = y < thumb.Center.Y ? -1 : 1;
                this.ScrollList(pageDirection * this.VisibleRows);
                Game1.playSound("shiny4");
            }
            return;
        }

        int visibleRows = Math.Max(1, this.ListArea.Height / RowHeight);
        for (int row = 0; row < visibleRows; row++)
        {
            int index = this.scrollOffset + row;
            Rectangle bounds = new(this.ListRowsArea.X, this.ListRowsArea.Y + row * RowHeight, this.ListRowsArea.Width, RowHeight - 4);
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
            this.recordCurrent();
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

        if (this.PrimaryWarpButton.Contains(x, y) && selected.CanWarp)
        {
            this.warp(selected);
            return;
        }

        if (selected.Kind == WarpDestinationKind.Bookmark)
        {
            if (this.BookmarkPinButton.Contains(x, y))
                this.TogglePinned(selected);
            else if (this.BookmarkEditButton.Contains(x, y))
                this.edit(selected);
            else if (this.BookmarkRemoveButton.Contains(x, y))
                this.HandleRemove(selected);
            return;
        }

        if (selected.Kind == WarpDestinationKind.Default && selected.IsHidden)
        {
            if (this.ContextLeftButton.Contains(x, y))
            {
                this.restoreHidden(selected);
                Game1.playSound("smallSelect");
            }
            else if (this.ContextRightButton.Contains(x, y))
            {
                this.restoreDefaults();
                Game1.playSound("smallSelect");
            }
            return;
        }

        if (selected.Kind == WarpDestinationKind.Default)
        {
            if (this.ContextLeftButton.Contains(x, y))
                this.TogglePinned(selected);
            else if (this.ContextRightButton.Contains(x, y))
                this.HandleRemove(selected);
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        this.ScrollList(direction < 0 ? 3 : -3);
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
        if (key == Keys.PageDown)
        {
            this.selectedIndex = Math.Min(this.destinations.Count - 1, this.selectedIndex + this.VisibleRows);
            this.EnsureSelectedVisible();
            return;
        }
        if (key == Keys.PageUp)
        {
            this.selectedIndex = Math.Max(0, this.selectedIndex - this.VisibleRows);
            this.EnsureSelectedVisible();
            return;
        }
        if (key == Keys.Home && this.destinations.Count > 0)
        {
            this.selectedIndex = 0;
            this.EnsureSelectedVisible();
            return;
        }
        if (key == Keys.End && this.destinations.Count > 0)
        {
            this.selectedIndex = this.destinations.Count - 1;
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
        this.searchBox.Draw(b);
        this.DrawCategories(b);

        int visibleRows = Math.Max(1, this.ListArea.Height / RowHeight);
        for (int row = 0; row < visibleRows; row++)
        {
            int index = this.scrollOffset + row;
            if (index >= this.destinations.Count)
                break;

            WarpDestination destination = this.destinations[index];
            Rectangle bounds = new(this.ListRowsArea.X, this.ListRowsArea.Y + row * RowHeight, this.ListRowsArea.Width, RowHeight - 4);
            Color fill = index == this.selectedIndex ? new Color(196, 135, 70) * 0.55f : new Color(120, 78, 48) * 0.10f;
            b.Draw(Game1.staminaRect, bounds, fill);
            string rowText = this.textRenderer.FitText(destination.Name, bounds.Width - 24 - PinnedIndicatorWidth, Game1.smallFont);
            this.textRenderer.DrawString(b, rowText, new Vector2(bounds.X + 12, bounds.Y + 10), Game1.textColor, Game1.smallFont);
            this.DrawPinnedIndicator(b, bounds, destination.IsFavorite);
        }
        this.DrawListScrollbar(b);

        this.DrawDetails(b);
        this.DrawButton(b, this.RecordButton, this.translate("menu.record"), true);
        this.DrawButton(b, this.CoordinateButton, this.translate("menu.coordinates-action"), true);
        WarpDestination? selected = this.Selected;
        string warpText = selected?.CanWarp == true
            ? this.translate("menu.warp-to").Replace("{{name}}", selected.Name)
            : this.translate("menu.select-to-warp");
        this.DrawButton(b, this.PrimaryWarpButton, warpText, selected?.CanWarp == true);
        b.DrawString(Game1.smallFont, this.shortcutText, new Vector2(this.xPositionOnScreen + 36, this.yPositionOnScreen + this.height - 100), Color.DarkSlateGray);
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
            string emptyKey = this.selectedCategory == DestinationCategory.Hidden ? "menu.hidden-empty" : "menu.empty";
            string emptyText = this.textRenderer.WrapText(this.translate(emptyKey), area.Width - 36, Game1.smallFont);
            this.textRenderer.DrawString(b, emptyText, new Vector2(area.X + 18, area.Y + 18), Game1.textColor, Game1.smallFont);
            return;
        }

        int y = area.Y + 18;
        string detailName = this.textRenderer.FitText(selected.Name, area.Width - 36, Game1.dialogueFont, title: true);
        this.textRenderer.DrawString(b, detailName, new Vector2(area.X + 18, y), Game1.textColor, Game1.dialogueFont, title: true);
        y += 62;
        this.DrawDetailLine(b, "menu.location", selected.Location.DisplayName, area.X + 18, ref y);
        this.DrawDetailLine(b, "menu.internal-name", selected.Location.LocationName, area.X + 18, ref y);
        this.DrawDetailLine(b, "menu.coordinates", $"{selected.Location.TileX}, {selected.Location.TileY}", area.X + 18, ref y);
        this.DrawDetailLine(b, "menu.type", this.translate($"kind.{selected.Kind.ToString().ToLowerInvariant()}"), area.X + 18, ref y);

        if (selected.Kind == WarpDestinationKind.Bookmark)
        {
            this.DrawButton(b, this.BookmarkPinButton, selected.IsFavorite ? this.translate("menu.unpin") : this.translate("menu.pin"), true);
            this.DrawButton(b, this.BookmarkEditButton, this.translate("menu.rename"), true);
            string removeText = this.pendingRemoveId == selected.Id ? this.translate("menu.confirm-delete") : this.translate("menu.delete");
            this.DrawButton(b, this.BookmarkRemoveButton, removeText, true);
        }
        else if (selected.Kind == WarpDestinationKind.Default && selected.IsHidden)
        {
            this.DrawButton(b, this.ContextLeftButton, this.translate("menu.restore-this"), true);
            this.DrawButton(b, this.ContextRightButton, this.translate("menu.restore-defaults"), true);
        }
        else if (selected.Kind == WarpDestinationKind.Default)
        {
            this.DrawButton(b, this.ContextLeftButton, selected.IsFavorite ? this.translate("menu.unpin") : this.translate("menu.pin"), true);
            string hideText = this.pendingRemoveId == selected.Id ? this.translate("menu.confirm-hide") : this.translate("menu.hide");
            this.DrawButton(b, this.ContextRightButton, hideText, true);
        }
    }

    private void DrawDetailLine(SpriteBatch b, string labelKey, string value, int x, ref int y)
    {
        string text = this.translate("menu.detail-format")
            .Replace("{{label}}", this.translate(labelKey))
            .Replace("{{value}}", value);
        string fittedText = this.textRenderer.FitText(text, this.DetailArea.Width - 36, Game1.smallFont);
        this.textRenderer.DrawString(b, fittedText, new Vector2(x, y), Game1.textColor, Game1.smallFont);
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
        string fittedLabel = this.textRenderer.FitText(label, bounds.Width - 24, Game1.smallFont);
        Vector2 size = this.textRenderer.MeasureString(fittedLabel, Game1.smallFont);
        Color textColor = !enabled ? Color.DarkGray : Game1.textColor;
        this.textRenderer.DrawString(b, fittedLabel, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), textColor, Game1.smallFont);
    }

    private Rectangle GetThreeActionButtonBounds(int column)
    {
        int x = this.DetailArea.X + ActionPadding + column * (this.ThreeActionButtonWidth + ActionGap);
        return new Rectangle(x, this.DetailArea.Bottom - 62, this.ThreeActionButtonWidth, 44);
    }

    private Rectangle GetTwoActionButtonBounds(int column)
    {
        int x = this.DetailArea.X + ActionPadding + column * (this.TwoActionButtonWidth + ActionGap);
        return new Rectangle(x, this.DetailArea.Bottom - 62, this.TwoActionButtonWidth, 44);
    }

    private void TogglePinned(WarpDestination destination)
    {
        if (destination.IsHidden || destination.Kind is not (WarpDestinationKind.Bookmark or WarpDestinationKind.Default))
            return;

        destination.IsFavorite = !destination.IsFavorite;
        this.setPinned(destination, destination.IsFavorite);
        this.ApplySearchFilter();
        Game1.addHUDMessage(new HUDMessage(
            this.translate(destination.IsFavorite ? "hud.pinned" : "hud.unpinned"),
            HUDMessage.newQuest_type
        ));
        Game1.playSound("drumkit6");
    }

    private void HandleRemove(WarpDestination destination)
    {
        if (!destination.CanRemove || destination.IsHidden)
            return;
        if (this.pendingRemoveId != destination.Id)
        {
            this.pendingRemoveId = destination.Id;
            Game1.playSound("cancel");
            return;
        }

        this.remove(destination);
        if (destination.Kind == WarpDestinationKind.Default)
            return;

        this.allDestinations.RemoveAll(item => item.Id == destination.Id && item.Kind == destination.Kind);
        this.ApplySearchFilter();
        this.selectedIndex = Math.Clamp(this.selectedIndex, 0, Math.Max(0, this.destinations.Count - 1));
        this.pendingRemoveId = null;
        this.EnsureSelectedVisible();
        Game1.playSound("trashcan");
    }

    private void ScrollList(int amount)
    {
        int maxOffset = Math.Max(0, this.destinations.Count - this.VisibleRows);
        this.scrollOffset = Math.Clamp(this.scrollOffset + amount, 0, maxOffset);
    }

    private Rectangle GetListScrollbarThumb()
    {
        int total = Math.Max(1, this.destinations.Count);
        int trackHeight = this.ListScrollbarTrack.Height;
        int thumbHeight = Math.Max(24, trackHeight * this.VisibleRows / total);
        int maxOffset = Math.Max(1, total - this.VisibleRows);
        int travel = Math.Max(0, trackHeight - thumbHeight);
        int thumbY = this.ListScrollbarTrack.Y + travel * this.scrollOffset / maxOffset;
        return new Rectangle(this.ListScrollbarTrack.X, thumbY, this.ListScrollbarTrack.Width, thumbHeight);
    }

    private void DrawListScrollbar(SpriteBatch b)
    {
        if (this.destinations.Count <= this.VisibleRows)
            return;
        b.Draw(Game1.staminaRect, this.ListScrollbarTrack, new Color(120, 78, 48) * 0.18f);
        b.Draw(Game1.staminaRect, this.GetListScrollbarThumb(), new Color(120, 78, 48) * 0.62f);
    }

    private void DrawPinnedIndicator(SpriteBatch b, Rectangle rowBounds, bool isPinned)
    {
        if (!isPinned)
            return;

        const int pixelSize = 2;
        int starWidth = PinnedStarPattern[0].Length * pixelSize;
        int starHeight = PinnedStarPattern.Length * pixelSize;
        int originX = rowBounds.Right - PinnedIndicatorWidth + (PinnedIndicatorWidth - starWidth) / 2;
        int originY = rowBounds.Center.Y - starHeight / 2;
        this.DrawPixelStar(b, originX + 1, originY + 1, pixelSize, new Color(92, 55, 24) * 0.35f);
        this.DrawPixelStar(b, originX, originY, pixelSize, new Color(218, 154, 45));
    }

    private void DrawPixelStar(SpriteBatch b, int originX, int originY, int pixelSize, Color color)
    {
        for (int row = 0; row < PinnedStarPattern.Length; row++)
        {
            for (int column = 0; column < PinnedStarPattern[row].Length; column++)
            {
                if (PinnedStarPattern[row][column] == '#')
                {
                    b.Draw(
                        Game1.staminaRect,
                        new Rectangle(originX + column * pixelSize, originY + row * pixelSize, pixelSize, pixelSize),
                        color
                    );
                }
            }
        }
    }

    private void EnsureSelectedVisible()
    {
        if (this.destinations.Count == 0)
        {
            this.selectedIndex = 0;
            this.scrollOffset = 0;
            return;
        }

        this.selectedIndex = Math.Clamp(this.selectedIndex, 0, this.destinations.Count - 1);
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
            DestinationCategory.QuickAccess => !destination.IsHidden
                && (destination.Kind is WarpDestinationKind.Home or WarpDestinationKind.Previous
                    || destination.IsFavorite),
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
            string categoryKey = category == DestinationCategory.QuickAccess
                ? "quick-access"
                : category.ToString().ToLowerInvariant();
            string label = this.translate($"category.{categoryKey}");
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
        this.categoryChanged(this.selectedCategory);
        this.ApplySearchFilter();
        Game1.playSound("smallSelect");
    }
}
