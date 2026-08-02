using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using WarpBookmarks.Framework;
using WarpBookmarks.Models;

namespace WarpBookmarks.UI;

/// <summary>A visited-map dropdown with separate X/Y tile inputs and an explicit safety preview.</summary>
internal sealed class CoordinateWarpDialog : IClickableMenu
{
    private const int DialogWidth = 800;
    private const int DialogHeight = 450;
    private const int DropdownRowHeight = 36;
    private const int VisibleDropdownRows = 4;
    private static readonly Rectangle ParchmentSourceRect = new(0, 0, 320, 180);

    private readonly WarpService warpService;
    private readonly Func<string, string> translate;
    private readonly Action returnToMenu;
    private readonly Func<WarpDestination, bool> saveBookmark;
    private readonly Texture2D parchmentTexture;
    private readonly TextBox xBox;
    private readonly TextBox yBox;
    private readonly TextBox mapSearchBox;
    private readonly List<LocationOption> locationOptions;
    private readonly List<int> filteredLocationIndices = new();
    private int selectedLocationIndex;
    private int dropdownOffset;
    private int dropdownHighlight;
    private bool dropdownOpen;
    private TextBox? focusedTextBox;
    private string previousX;
    private string previousY;
    private string previousMapSearch = "";
    private string statusText;
    private Color statusColor = Game1.textColor;
    private WarpDestination? previewDestination;
    private bool savedPreview;

    private Rectangle MapButton => new(this.xPositionOnScreen + 190, this.yPositionOnScreen + 92, 520, 46);
    private Rectangle DropdownArea => new(this.MapButton.X, this.MapButton.Bottom, this.MapButton.Width, 48 + DropdownRowHeight * VisibleDropdownRows);
    private Rectangle MapSearchArea => new(this.mapSearchBox.X, this.mapSearchBox.Y, this.mapSearchBox.Width, 42);
    private Rectangle DropdownRowsArea => new(this.MapButton.X, this.MapButton.Bottom + 48, this.MapButton.Width, DropdownRowHeight * VisibleDropdownRows);
    private Rectangle XArea => new(this.xBox.X, this.xBox.Y, this.xBox.Width, 48);
    private Rectangle YArea => new(this.yBox.X, this.yBox.Y, this.yBox.Width, 48);
    private Rectangle PreviewButton => new(this.xPositionOnScreen + 100, this.yPositionOnScreen + 362, 170, 48);
    private Rectangle SaveButton => new(this.xPositionOnScreen + 315, this.yPositionOnScreen + 362, 170, 48);
    private Rectangle WarpButton => new(this.xPositionOnScreen + 530, this.yPositionOnScreen + 362, 170, 48);
    private TextBox? ActiveTextBox => this.focusedTextBox;

    public CoordinateWarpDialog(
        WarpService warpService,
        Func<string, string> translate,
        Action returnToMenu,
        Func<WarpDestination, bool> saveBookmark
    )
        : base(
            (Game1.uiViewport.Width - DialogWidth) / 2,
            (Game1.uiViewport.Height - DialogHeight) / 2,
            DialogWidth,
            DialogHeight,
            showUpperRightCloseButton: true
        )
    {
        this.warpService = warpService;
        this.translate = translate;
        this.returnToMenu = returnToMenu;
        this.saveBookmark = saveBookmark;
        this.parchmentTexture = Game1.content.Load<Texture2D>("LooseSprites\\letterBG");
        Texture2D textBoxTexture = Game1.content.Load<Texture2D>("LooseSprites\\textBox");
        this.xBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
        {
            X = this.xPositionOnScreen + 190,
            Y = this.yPositionOnScreen + 174,
            Width = 190,
            Text = Game1.player.TilePoint.X.ToString(),
            Selected = true
        };
        this.yBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
        {
            X = this.xPositionOnScreen + 454,
            Y = this.yPositionOnScreen + 174,
            Width = 190,
            Text = Game1.player.TilePoint.Y.ToString(),
            Selected = false
        };
        this.mapSearchBox = new TextBox(textBoxTexture, null, Game1.smallFont, Game1.textColor)
        {
            X = this.xPositionOnScreen + 300,
            Y = this.yPositionOnScreen + 142,
            Width = 402,
            Selected = false
        };
        this.previousX = this.xBox.Text;
        this.previousY = this.yBox.Text;
        this.locationOptions = BuildLocationOptions();
        this.selectedLocationIndex = Math.Max(0, this.locationOptions.FindIndex(option =>
            string.Equals(option.LocationName, Game1.currentLocation.NameOrUniqueName, StringComparison.OrdinalIgnoreCase)
        ));
        this.ApplyMapSearch();
        this.statusText = this.translate("coordinate.status-ready-dropdown");
        this.SetKeyboardFocus(this.xBox);
    }

    public override void update(GameTime time)
    {
        base.update(time);
        this.xBox.Update();
        this.yBox.Update();
        this.mapSearchBox.Update();
        this.RestoreFocusedTextBoxState();
        if (!string.Equals(this.previousMapSearch, this.mapSearchBox.Text, StringComparison.Ordinal))
        {
            this.previousMapSearch = this.mapSearchBox.Text;
            this.ApplyMapSearch();
        }
        if (string.Equals(this.previousX, this.xBox.Text, StringComparison.Ordinal)
            && string.Equals(this.previousY, this.yBox.Text, StringComparison.Ordinal))
        {
            return;
        }

        this.previousX = this.xBox.Text;
        this.previousY = this.yBox.Text;
        this.InvalidatePreview("coordinate.status-changed");
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.upperRightCloseButton?.containsPoint(x, y) == true)
        {
            this.ReturnToMenu();
            return;
        }

        if (this.dropdownOpen)
        {
            if (this.MapSearchArea.Contains(x, y))
            {
                this.SetKeyboardFocus(this.mapSearchBox);
                return;
            }
            if (this.DropdownRowsArea.Contains(x, y))
            {
                int visibleIndex = (y - this.DropdownRowsArea.Y) / DropdownRowHeight;
                int filteredIndex = this.dropdownOffset + visibleIndex;
                if (filteredIndex >= 0 && filteredIndex < this.filteredLocationIndices.Count)
                    this.SelectLocation(this.filteredLocationIndices[filteredIndex]);
                return;
            }
            this.dropdownOpen = false;
            this.SetKeyboardFocus(null);
        }

        if (this.MapButton.Contains(x, y))
        {
            this.dropdownOpen = true;
            this.mapSearchBox.Text = "";
            this.previousMapSearch = "";
            this.ApplyMapSearch();
            this.SetKeyboardFocus(this.mapSearchBox);
            Game1.playSound("shiny4");
            return;
        }
        if (this.XArea.Contains(x, y))
        {
            this.SetKeyboardFocus(this.xBox);
            return;
        }
        if (this.YArea.Contains(x, y))
        {
            this.SetKeyboardFocus(this.yBox);
            return;
        }
        if (this.PreviewButton.Contains(x, y))
        {
            this.BuildPreview();
            return;
        }
        if (this.WarpButton.Contains(x, y) && this.previewDestination is not null)
        {
            this.warpService.TryWarp(this.previewDestination);
            return;
        }
        if (this.SaveButton.Contains(x, y) && this.previewDestination is not null && !this.savedPreview)
        {
            this.savedPreview = this.saveBookmark(this.previewDestination);
            if (this.savedPreview)
            {
                this.statusText = this.translate("coordinate.status-saved");
                this.statusColor = Game1.textColor;
            }
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (!this.dropdownOpen)
            return;
        int maxOffset = Math.Max(0, this.filteredLocationIndices.Count - VisibleDropdownRows);
        this.dropdownOffset = Math.Clamp(this.dropdownOffset + (direction < 0 ? 1 : -1), 0, maxOffset);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (this.dropdownOpen)
        {
            if (key == Keys.Escape)
            {
                this.dropdownOpen = false;
                this.SetKeyboardFocus(null);
                return;
            }
            if (key == Keys.Down)
            {
                this.MoveDropdownHighlight(1);
                return;
            }
            if (key == Keys.Up)
            {
                this.MoveDropdownHighlight(-1);
                return;
            }
            if (key == Keys.Enter)
            {
                if (this.filteredLocationIndices.Count > 0)
                    this.SelectLocation(this.filteredLocationIndices[this.dropdownHighlight]);
                return;
            }
            return;
        }

        if (key == Keys.Escape)
        {
            this.ReturnToMenu();
            return;
        }
        if (key == Keys.Enter)
        {
            this.BuildPreview();
            return;
        }
        // Focused TextBoxes own cursor/editing keys through the keyboard dispatcher.
        if (this.ActiveTextBox is not null)
            return;
        base.receiveKeyPress(key);
    }

    protected override void cleanupBeforeExit()
    {
        if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this.xBox)
            || ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this.yBox)
            || ReferenceEquals(Game1.keyboardDispatcher.Subscriber, this.mapSearchBox))
        {
            Game1.keyboardDispatcher.Subscriber = null;
        }
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
        b.DrawString(Game1.dialogueFont, this.translate("coordinate.title"), new Vector2(this.xPositionOnScreen + 48, this.yPositionOnScreen + 28), Game1.textColor);
        b.DrawString(Game1.smallFont, this.translate("coordinate.map-label"), new Vector2(this.xPositionOnScreen + 70, this.MapButton.Y + 12), Game1.textColor);
        this.DrawMapButton(b);
        b.DrawString(Game1.smallFont, "X", new Vector2(this.xPositionOnScreen + 150, this.xBox.Y + 12), Game1.textColor);
        this.xBox.Draw(b);
        b.DrawString(Game1.smallFont, ",", new Vector2(this.xPositionOnScreen + 418, this.xBox.Y + 12), Game1.textColor);
        b.DrawString(Game1.smallFont, "Y", new Vector2(this.xPositionOnScreen + 420, this.yBox.Y + 12), Game1.textColor);
        this.yBox.Draw(b);
        b.DrawString(Game1.smallFont, this.translate("coordinate.xy-hint"), new Vector2(this.xPositionOnScreen + 190, this.yPositionOnScreen + 230), Color.DarkSlateGray);
        b.DrawString(Game1.smallFont, this.statusText, new Vector2(this.xPositionOnScreen + 70, this.yPositionOnScreen + 294), this.statusColor);
        this.DrawButton(b, this.PreviewButton, this.translate("coordinate.preview"), enabled: true);
        this.DrawButton(b, this.SaveButton, this.savedPreview ? this.translate("coordinate.saved") : this.translate("coordinate.save"), enabled: this.previewDestination is not null && !this.savedPreview);
        this.DrawButton(b, this.WarpButton, this.translate("coordinate.warp"), enabled: this.previewDestination is not null);
        if (this.dropdownOpen)
            this.DrawDropdown(b);
        this.upperRightCloseButton?.draw(b);
        this.drawMouse(b);
    }

    private void BuildPreview()
    {
        if (this.locationOptions.Count == 0
            || !int.TryParse(this.xBox.Text.Trim(), out int x)
            || !int.TryParse(this.yBox.Text.Trim(), out int y))
        {
            this.previewDestination = null;
            this.statusText = this.translate("error.coordinate-numbers");
            this.statusColor = Color.DarkRed;
            Game1.playSound("cancel");
            return;
        }

        LocationOption option = this.locationOptions[this.selectedLocationIndex];
        WarpDestination candidate = new()
        {
            Id = "Coordinate",
            Name = $"{option.DisplayName} ({x}, {y})",
            Kind = WarpDestinationKind.Coordinate,
            Location = new LocationReference
            {
                LocationName = option.LocationName,
                DisplayName = option.DisplayName,
                TileX = x,
                TileY = y,
                FacingDirection = 2
            }
        };
        if (!this.warpService.TryValidate(candidate, out GameLocation target, out Point safeTile, out string reasonKey))
        {
            this.previewDestination = null;
            this.statusText = this.translate(reasonKey);
            this.statusColor = Color.DarkRed;
            Game1.playSound("cancel");
            return;
        }

        candidate.Location.LocationName = target.NameOrUniqueName;
        candidate.Location.DisplayName = target.DisplayName;
        candidate.Name = $"{target.DisplayName} ({x}, {y})";
        this.previewDestination = candidate;
        this.savedPreview = false;
        this.statusText = this.translate("coordinate.status-valid")
            .Replace("{{location}}", target.DisplayName)
            .Replace("{{x}}", safeTile.X.ToString())
            .Replace("{{y}}", safeTile.Y.ToString());
        this.statusColor = Game1.textColor;
        Game1.playSound("smallSelect");
    }

    private void SelectLocation(int index)
    {
        if (this.locationOptions.Count == 0)
            return;
        this.selectedLocationIndex = Math.Clamp(index, 0, this.locationOptions.Count - 1);
        this.dropdownOpen = false;
        this.SetKeyboardFocus(null);
        this.InvalidatePreview("coordinate.status-changed");
        Game1.playSound("smallSelect");
    }

    private void SetKeyboardFocus(TextBox? textBox)
    {
        this.focusedTextBox = textBox;
        this.RestoreFocusedTextBoxState();
        Game1.keyboardDispatcher.Subscriber = textBox;
    }

    private void RestoreFocusedTextBoxState()
    {
        this.xBox.Selected = ReferenceEquals(this.focusedTextBox, this.xBox);
        this.yBox.Selected = ReferenceEquals(this.focusedTextBox, this.yBox);
        this.mapSearchBox.Selected = ReferenceEquals(this.focusedTextBox, this.mapSearchBox);
    }

    private void ApplyMapSearch()
    {
        string query = this.mapSearchBox.Text.Trim();
        this.filteredLocationIndices.Clear();
        for (int index = 0; index < this.locationOptions.Count; index++)
        {
            LocationOption option = this.locationOptions[index];
            if (query.Length == 0
                || option.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || option.LocationName.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                this.filteredLocationIndices.Add(index);
            }
        }

        int selectedPosition = this.filteredLocationIndices.IndexOf(this.selectedLocationIndex);
        this.dropdownHighlight = selectedPosition >= 0 ? selectedPosition : 0;
        this.dropdownOffset = 0;
        this.EnsureDropdownHighlightVisible();
    }

    private void MoveDropdownHighlight(int direction)
    {
        if (this.filteredLocationIndices.Count == 0)
            return;
        this.dropdownHighlight = Math.Clamp(
            this.dropdownHighlight + direction,
            0,
            this.filteredLocationIndices.Count - 1
        );
        this.EnsureDropdownHighlightVisible();
        Game1.playSound("shiny4");
    }

    private void EnsureDropdownHighlightVisible()
    {
        if (this.dropdownHighlight < this.dropdownOffset)
            this.dropdownOffset = this.dropdownHighlight;
        else if (this.dropdownHighlight >= this.dropdownOffset + VisibleDropdownRows)
            this.dropdownOffset = this.dropdownHighlight - VisibleDropdownRows + 1;
    }

    private void InvalidatePreview(string statusKey)
    {
        this.previewDestination = null;
        this.savedPreview = false;
        this.statusText = this.translate(statusKey);
        this.statusColor = Game1.textColor;
    }

    private void DrawMapButton(SpriteBatch b)
    {
        Color tint = this.MapButton.Contains(Game1.getMousePosition(true)) ? Color.Wheat : Color.White;
        IClickableMenu.drawTextureBox(b, this.MapButton.X, this.MapButton.Y, this.MapButton.Width, this.MapButton.Height, tint);
        string label = this.locationOptions.Count == 0
            ? this.translate("coordinate.no-maps")
            : this.locationOptions[this.selectedLocationIndex].Label;
        b.DrawString(Game1.smallFont, label, new Vector2(this.MapButton.X + 14, this.MapButton.Y + 11), Game1.textColor);
        b.DrawString(Game1.smallFont, this.dropdownOpen ? "▲" : "▼", new Vector2(this.MapButton.Right - 34, this.MapButton.Y + 11), Game1.textColor);
    }

    private void DrawDropdown(SpriteBatch b)
    {
        int rows = Math.Min(VisibleDropdownRows, Math.Max(0, this.filteredLocationIndices.Count - this.dropdownOffset));
        Rectangle background = new(this.DropdownArea.X, this.DropdownArea.Y, this.DropdownArea.Width, 48 + Math.Max(1, rows) * DropdownRowHeight);
        b.Draw(Game1.staminaRect, background, new Color(245, 222, 174));
        b.DrawString(Game1.smallFont, this.translate("coordinate.map-search"), new Vector2(background.X + 12, background.Y + 12), Game1.textColor);
        this.mapSearchBox.Draw(b);
        if (rows == 0)
        {
            b.DrawString(Game1.smallFont, this.translate("coordinate.map-no-results"), new Vector2(this.DropdownRowsArea.X + 12, this.DropdownRowsArea.Y + 7), Color.DarkRed);
            return;
        }
        for (int row = 0; row < rows; row++)
        {
            int filteredPosition = this.dropdownOffset + row;
            int optionIndex = this.filteredLocationIndices[filteredPosition];
            Rectangle bounds = new(this.DropdownRowsArea.X, this.DropdownRowsArea.Y + row * DropdownRowHeight, this.DropdownRowsArea.Width, DropdownRowHeight);
            if (filteredPosition == this.dropdownHighlight)
                b.Draw(Game1.staminaRect, bounds, new Color(196, 135, 70) * 0.55f);
            else if (bounds.Contains(Game1.getMousePosition(true)))
                b.Draw(Game1.staminaRect, bounds, new Color(222, 184, 120) * 0.4f);
            b.DrawString(Game1.smallFont, this.locationOptions[optionIndex].Label, new Vector2(bounds.X + 12, bounds.Y + 7), Game1.textColor);
        }
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

    private void ReturnToMenu()
    {
        this.exitThisMenu();
        this.returnToMenu();
    }

    private static List<LocationOption> BuildLocationOptions()
    {
        Dictionary<string, GameLocation> locations = new(StringComparer.OrdinalIgnoreCase);
        void Add(GameLocation? location)
        {
            if (location is null || LocationPolicy.IsRestricted(location))
                return;
            string key = location.NameOrUniqueName;
            bool visited = location == Game1.currentLocation
                || string.Equals(location.Name, "Farm", StringComparison.OrdinalIgnoreCase)
                || Game1.player.locationsVisited.Contains(location.Name)
                || Game1.player.locationsVisited.Contains(key);
            if (visited && !string.IsNullOrWhiteSpace(key))
                locations[key] = location;
        }

        Add(Game1.currentLocation);
        foreach (GameLocation location in Game1.locations)
            Add(location);
        foreach (string visitedName in Game1.player.locationsVisited)
            Add(Game1.getLocationFromName(visitedName));

        return locations.Values
            .Select(location => new LocationOption(location.NameOrUniqueName, location.DisplayName))
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(option => option.LocationName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class LocationOption
    {
        public string LocationName { get; }
        public string DisplayName { get; }
        public string Label => string.Equals(this.DisplayName, this.LocationName, StringComparison.OrdinalIgnoreCase)
            ? this.DisplayName
            : $"{this.DisplayName} ({this.LocationName})";

        public LocationOption(string locationName, string displayName)
        {
            this.LocationName = locationName;
            this.DisplayName = string.IsNullOrWhiteSpace(displayName) ? locationName : displayName;
        }
    }
}
