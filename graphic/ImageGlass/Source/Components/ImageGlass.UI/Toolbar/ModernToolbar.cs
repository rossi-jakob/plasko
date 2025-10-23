﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using ImageGlass.Base;
using ImageGlass.Base.WinApi;
using System.ComponentModel;

namespace ImageGlass.UI;


/// <summary>
/// Modern toolbar
/// </summary>
public class ModernToolbar : ToolStrip
{
    private ToolbarAlignment _alignment = ToolbarAlignment.Center;
    private uint _iconHeight = Const.TOOLBAR_ICON_HEIGHT;

    private readonly ModernTooltip _tooltip = new();
    private CancellationTokenSource _tooltipTokenSrc = new();
    private ToolStripItem? _hoveredItem = null;

    private static Container _mainMenuContainer = new Container();
    private ModernMenu _mainMenu = new(_mainMenuContainer);

    private ToolStripButton _mainMenuButton => new()
    {
        Name = "Btn_MainMenu",
        DisplayStyle = ToolStripItemDisplayStyle.Image,
        TextImageRelation = TextImageRelation.ImageBeforeText,
        Text = "[Main menu]",
        ToolTipText = "[Main menu (Alf+F)]",

        // save icon name to load later
        Tag = new ToolbarItemTagModel()
        {
            Image = nameof(Theme.ToolbarIcons.MainMenu),
        },

        Alignment = ToolStripItemAlignment.Right,
        Overflow = ToolStripItemOverflow.Never,
    };


    #region Public properties

    /// <summary>
    /// Enable transparent background.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EnableTransparent { get; set; } = true;

    /// <summary>
    /// Show or hide main menu button of toolbar
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowMainMenuButton
    {
        get => MainMenuButton.Visible;
        set => MainMenuButton.Visible = value;
    }

    /// <summary>
    /// Gets main menu button
    /// </summary>
    public ToolStripButton MainMenuButton => GetItem(_mainMenuButton.Name) ?? _mainMenuButton;

    /// <summary>
    /// Gets, sets main menu
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ModernMenu MainMenu
    {
        get => _mainMenu;
        set
        {
            _mainMenu.Opened -= MainMenu_Opened;
            _mainMenu.Closed -= MainMenu_Closed;

            _mainMenu = value;

            _mainMenu.Opened += MainMenu_Opened;
            _mainMenu.Closed += MainMenu_Closed;
        }
    }

    /// <summary>
    /// Gets, sets value indicates that the tooltip is shown
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HideTooltips { get; set; } = false;

    /// <summary>
    /// Gets default gap for sizing calculation
    /// </summary>
    public int DefaultGap => ImageScalingSize.Height / 4;

    /// <summary>
    /// Gets, sets items alignment
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolbarAlignment Alignment
    {
        get => _alignment;
        set
        {
            _alignment = value;

            UpdateAlignment();
        }
    }

    /// <summary>
    /// Gets, sets theme
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IgTheme? Theme { get; set; }

    /// <summary>
    /// Gets, sets icons height
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public uint IconHeight
    {
        get => _iconHeight;
        set
        {
            _iconHeight = DpiApi.Scale(value);
            ImageScalingSize = new((int)_iconHeight, (int)_iconHeight);
        }
    }

    /// <summary>
    /// Gets, sets value indicates that the toolstrip will autofocus on hover
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AutoFocusOnHover { get; set; } = true;

    #endregion


    public ModernToolbar() : base()
    {
        // Enable click-through for inactive toolstrip/menustrip
        AllowClickThrough = true;

        ShowItemToolTips = false;
        Items.Insert(0, _mainMenuButton);

        // Apply Windows 11 corner API
        WindowApi.SetRoundCorner(OverflowButton.DropDown.Handle);
    }


    #region Protected methods


    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);

        if (!EnableTransparent)
        {
            e.Graphics.Clear(TopLevelControl.BackColor);
        }

        using var bgBrush = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
    }


    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        HideItemTooltip();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        HideItemTooltip();
    }

    //protected override void OnMouseEnter(EventArgs e)
    //{
    //    if (AutoFocusOnHover && CanFocus && !Focused)
    //        Focus();

    //    base.OnMouseEnter(e);
    //}

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        HideItemTooltip();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            OverflowButton.DropDown.Opening -= OverflowDropDown_Opening;
            _tooltip.Dispose();
            _tooltipTokenSrc.Dispose();
            _mainMenuContainer.Dispose();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        UpdateAlignment();

        base.OnSizeChanged(e);

        UpdateAlignment();
    }

    protected override Padding DefaultPadding
    {
        get
        {
            return new Padding(DefaultGap, 0, DefaultGap, 0);
        }
    }

    protected override void OnRightToLeftChanged(EventArgs e)
    {
        base.OnRightToLeftChanged(e);

        foreach (ToolStripItem item in Items)
        {
            if (item.DisplayStyle == ToolStripItemDisplayStyle.ImageAndText
                && item.TextImageRelation == TextImageRelation.ImageBeforeText)
            {
                item.TextAlign = ContentAlignment.MiddleCenter;
                item.ImageAlign = ContentAlignment.MiddleRight;
            }
        }
    }

    protected override void OnItemClicked(ToolStripItemClickedEventArgs e)
    {
        base.OnItemClicked(e);

        // filter out BtnMainMenu
        if (e.ClickedItem.Name == MainMenuButton.Name)
        {
            // on main menu button clicked
            ShowMainMenu();
        }
    }

    protected override void OnItemAdded(ToolStripItemEventArgs e)
    {
        base.OnItemAdded(e);

        e.Item.MouseEnter += Item_MouseEnter;
    }

    protected override void OnItemRemoved(ToolStripItemEventArgs e)
    {
        base.OnItemRemoved(e);

        e.Item.MouseEnter -= Item_MouseEnter;
    }

    #endregion


    #region Private functions
    private void MainMenu_Opened(object? sender, EventArgs e)
    {
        MainMenuButton.Checked = true;
    }
    private void MainMenu_Closed(object? sender, ToolStripDropDownClosedEventArgs e)
    {
        MainMenuButton.Checked = false;
    }


    private void Item_MouseEnter(object? sender, EventArgs e)
    {
        if (HideTooltips) return;

        var item = (ToolStripItem?)sender;

        if (item == null)
        {
            HideItemTooltip();
            _hoveredItem = null;
        }
        else if (item != _hoveredItem)
        {
            _hoveredItem = item;

            var parent = item.GetCurrentParent();
            if (parent == OverflowButton.DropDown)
            {
                var loc = PointToClient(parent.Location);
                ShowItemTooltip(_hoveredItem, loc);
            }
            else
            {
                ShowItemTooltip(_hoveredItem);
            }

        }
    }



    /// <summary>
    /// Updates overflow button and dropdown
    /// </summary>
    private void UpdateOverflow()
    {
        // overflow size
        OverflowButton.Margin = new(0, 0, DefaultGap, 0);
        OverflowButton.Padding = new(DefaultGap);

        // dropdown size
        OverflowButton.DropDown.AutoSize = false;
        OverflowButton.DropDown.Padding = new(DefaultGap * 2, DefaultGap, DefaultGap * 2, DefaultGap);

        // fix the size of overflow dropdown
        OverflowButton.DropDown.Opening -= OverflowDropDown_Opening;
        OverflowButton.DropDown.Opening += OverflowDropDown_Opening;

        if (Theme is not null)
        {
            OverflowButton.DropDown.BackColor = BackColor.WithAlpha(255);
            OverflowButton.DropDown.ForeColor = ForeColor;
        }
    }


    private void OverflowDropDown_Opening(object? sender, CancelEventArgs e)
    {
        UpdateOverflowDropdownSize();
    }


    /// <summary>
    /// Update overflow dropdown size
    /// </summary>
    private void UpdateOverflowDropdownSize()
    {
        var maxItemHeight = 0;
        var fullDropdownWidth = OverflowButton.DropDown.Padding.Left + OverflowButton.DropDown.Padding.Right;

        foreach (ToolStripItem item in Items)
        {
            if (!item.IsOnDropDown) continue;

            fullDropdownWidth += item.Width
                + item.Margin.Left
                + item.Margin.Right;

            maxItemHeight = Math.Max(maxItemHeight, item.Height + item.Margin.Top + item.Margin.Bottom);
        }

        var maxDropdownWidth = Screen.FromControl(this).WorkingArea.Width / 2;
        var dropdownWidth = Math.Min(fullDropdownWidth, maxDropdownWidth);
        var dropdownHeight = (int)(Math.Ceiling(fullDropdownWidth * 1f / dropdownWidth)
            * maxItemHeight
            + OverflowButton.DropDown.Padding.Top
            + OverflowButton.DropDown.Padding.Bottom);

        OverflowButton.DropDown.Width = dropdownWidth;
        OverflowButton.DropDown.Height = dropdownHeight;
    }


    #endregion


    #region Public functions

    /// <summary>
    /// Shows main menu
    /// </summary>
    public void ShowMainMenu()
    {
        // update correct height of the menu
        MainMenu.FixGeneralIssues();

        var x = MainMenuButton.Bounds.Left + MainMenuButton.Bounds.Width - MainMenu.Width;
        var y = Visible ? Height : 10;

        var workingArea = Screen.FromControl(this).WorkingArea;
        var screenMenuBottom = PointToScreen(new Point(0, y + MainMenu.Height)).Y;

        // make sure main menu does not cover the toolbar
        if (screenMenuBottom > workingArea.Bottom)
        {
            y = y - MainMenu.Height - Height;
        }

        MainMenu.Show(this, x, y);
    }

    /// <summary>
    /// Hide item's tooltip
    /// </summary>
    public void HideItemTooltip()
    {
        _tooltipTokenSrc.Cancel();
        _tooltip.Hide(this);
    }

    /// <summary>
    /// Shows item tooltip
    /// </summary>
    public async void ShowItemTooltip(ToolStripItem? item, Point baseLocation = new(),
        int duration = 4000, int delay = 400)
    {
        if (item is null || string.IsNullOrEmpty(item.ToolTipText))
            return;

        _tooltipTokenSrc?.Cancel();
        _tooltipTokenSrc = new();

        _tooltip.Hide(this);

        try
        {
            var tooltipPosX = item.Bounds.X;
            var tooltipPosY = 0;

            if (Dock == DockStyle.Bottom)
            {
                // tooltip direction is bottom
                tooltipPosY = item.Bounds.Top
                    - item.Padding.Top
                    - (int)DpiApi.Scale(SystemInformation.MenuFont.Size);
            }
            else
            {
                // tooltip direction is top
                tooltipPosY = item.Bounds.Bottom + item.Padding.Bottom;
            }

            // delay
            await Task.Delay(delay, _tooltipTokenSrc.Token);


            tooltipPosX += baseLocation.X;
            tooltipPosY += baseLocation.Y;

            // show tooltip
            _tooltip.Show(item.ToolTipText, this, tooltipPosX, tooltipPosY);

            // duration
            await Task.Delay(duration, _tooltipTokenSrc.Token);
        }
        catch { }

        _tooltip.Hide(this);
    }

    /// <summary>
    /// Update the alignment if toolstrip items
    /// </summary>
    public void UpdateAlignment()
    {
        if (Items.Count < 1) return;

        // find the first left-aligned button
        ToolStripItem? firstBtn = null;
        foreach (ToolStripItem item in Items)
        {
            if (item.Alignment == ToolStripItemAlignment.Left)
            {
                firstBtn = item;
                break;
            }
        }

        if (firstBtn == null) return;


        var defaultMargin = new Padding(0, firstBtn.Margin.Top, firstBtn.Margin.Right, firstBtn.Margin.Bottom);

        // reset the alignment to left
        firstBtn.Margin = defaultMargin;

        if (Alignment != ToolbarAlignment.Center) return;



        // get the correct content width, excluding the sticky right items
        var toolbarContentWidth = 0;
        var rightContentWidth = 0;
        foreach (ToolStripItem item in Items)
        {
            toolbarContentWidth += item.Width;

            if (item.Alignment == ToolStripItemAlignment.Right)
            {
                rightContentWidth += item.Width;
            }

            // reset margin
            item.Margin = defaultMargin;
        }

        if (ShowMainMenuButton)
        {
            toolbarContentWidth += MainMenuButton.Width;
        }
        else
        {
            rightContentWidth -= MainMenuButton.Width;
            toolbarContentWidth -= MainMenuButton.Width;
        }


        // if the content cannot fit the toolbar size:
        if (rightContentWidth + toolbarContentWidth >= Width)
        {
            // align left
            firstBtn.Margin = defaultMargin;
        }
        else
        {
            // the default margin (left alignment)
            var margin = defaultMargin;

            // get the gap of content width and toolbar width
            var gap = Math.Abs(Width - toolbarContentWidth);

            // update the left margin value
            margin.Left = gap / 2;

            // align the first item
            firstBtn.Margin = margin;
        }

    }


    /// <summary>
    /// Update toolbar theme
    /// </summary>
    public async Task UpdateThemeAsync(uint? iconHeight = null)
    {
        if (iconHeight is not null)
        {
            IconHeight = iconHeight.Value;
        }

        if (Theme is null) return;

        _tooltip.DarkMode = Theme.Settings.IsDarkMode;
        SuspendLayout();

        // update toolbar theme
        BackColor = Theme.Colors.ToolbarBgColor;
        ForeColor = Theme.Colors.ToolbarTextColor;
        Renderer = new ModernToolbarRenderer(this);

        // Overflow button and Overflow dropdown
        UpdateOverflow();


        // load toolbar button icons
        await Parallel.ForAsync(0, Items.Count, async (i, _) =>
        {
            if (Items[i] is ToolStripSeparator tItem)
            {
                tItem.AutoSize = false;
                tItem.Height = (int)IconHeight;
                tItem.Width = (int)IconHeight / 2;
            }

            if (Items[i] is ToolStripButton bItem)
            {
                // update font and alignment
                bItem.ForeColor = Theme.Colors.ToolbarTextColor;
                bItem.Padding = new(DefaultGap);
                bItem.Margin = new(0, DefaultGap, DefaultGap / 2, DefaultGap);

                // update item from metadata
                var tagModel = bItem.Tag as ToolbarItemTagModel;
                bItem.Image = await Theme.GetToolbarIconAsync(tagModel?.Image, IconHeight);
            }

        });

        ResumeLayout(false);


        // update items alignment
        UpdateAlignment();
    }


    /// <summary>
    /// Gets item by name
    /// </summary>
    /// <typeparam name="T">Type of ToolstripItem to convert</typeparam>
    /// <param name="name">Name of item</param>
    /// <returns></returns>
    public T? GetItem<T>(string name)
    {
        var item = Items[name];

        if (item is null || item.GetType() != typeof(T))
        {
            return default;
        }

        return (T)Convert.ChangeType(item, typeof(T));
    }


    /// <summary>
    /// Gets ToolStripButton by name
    /// </summary>
    /// <param name="name">Name of item</param>
    public ToolStripButton? GetItem(string name)
    {
        return GetItem<ToolStripButton>(name);
    }


    /// <summary>
    /// Adds new toolbar item.
    /// </summary>
    /// <param name="model">Item model</param>
    /// <param name="position">The location in the items list at which to insert the toolbar item</param>
    /// <param name="modifier">Modifier function to modify item properties</param>
    public ToolbarAddItemResult AddItem(ToolbarItemModel model,
        int? position = null,
        Action<ToolStripItem>? modifier = null)
    {
        position ??= Items.Count;

        // separator
        if (model.Type == ToolbarItemModelType.Separator)
        {
            var sItem = new ToolStripSeparator();
            modifier?.Invoke(sItem);

            Items.Insert(position.Value, sItem);
            return ToolbarAddItemResult.Success;
        }


        if (GetItem<ToolStripItem>(model.Id) is not null)
            return ToolbarAddItemResult.ItemExists;


        ToolStripItem? toolstripItem = null;

        // text label
        if (model.DisplayStyle == ToolStripItemDisplayStyle.Text)
        {
            toolstripItem = new ToolStripLabel()
            {
                Name = model.Id,
                DisplayStyle = model.DisplayStyle,
                Text = model.Text,
                Alignment = model.Alignment,

                TextImageRelation = TextImageRelation.TextBeforeImage,
                TextAlign = ContentAlignment.MiddleCenter,

                // save metadata
                Tag = new ToolbarItemTagModel()
                {
                    OnClick = model.OnClick,
                },
            };
        }
        // button
        else
        {
            toolstripItem = new ToolStripButton()
            {
                Name = model.Id,
                DisplayStyle = model.DisplayStyle,
                Text = model.Text,
                ToolTipText = model.Text,
                Alignment = model.Alignment,

                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleRight,

                // save metadata
                Tag = new ToolbarItemTagModel()
                {
                    Image = model.Image,
                    CheckableConfigBinding = model.CheckableConfigBinding,
                    OnClick = model.OnClick,
                },

                Image = Theme?.GetToolbarIcon(model.Image, IconHeight),
            };
        }

        if (toolstripItem == null)
        {
            return ToolbarAddItemResult.InvalidModel;
        }

        modifier?.Invoke(toolstripItem);
        Items.Insert(position.Value, toolstripItem);

        return ToolbarAddItemResult.Success;
    }


    /// <summary>
    /// Adds list of toolbar items
    /// </summary>
    /// <param name="list">The list of item models</param>
    /// <param name="modifier">Modifier function to modify item properties</param>
    public void AddItems(IEnumerable<ToolbarItemModel> list,
        Action<ToolStripItem>? modifier = null)
    {
        SuspendLayout();

        foreach (var item in list)
        {
            _ = AddItem(item, null, modifier);
        }

        ResumeLayout(false);
    }


    /// <summary>
    /// Clears toolbar items, then adds <see cref="MainMenuButton"/>.
    /// </summary>
    public void ClearItems()
    {
        Items.Clear();
        Items.Insert(0, _mainMenuButton);
    }

    #endregion


}


/// <summary>
/// Toolbar items alignment.
/// </summary>
public enum ToolbarAlignment
{
    Left = 0,
    Center = 1,
}
