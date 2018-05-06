using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Game.HelperClassesCore;

namespace Game.Newt.v2.GameItems.ShipEditor
{
    #region class: TabControlPartsVM

    public class TabControlPartsVM : INotifyPropertyChanged
    {
        #region Declaration Section

        protected readonly EditorColors _colors;

        #endregion

        #region Constructor

        public TabControlPartsVM()
            : this(new EditorColors()) { }
        public TabControlPartsVM(EditorColors colors)
        {
            _colors = colors;
            this.Tabs = new ObservableCollection<TabItem>();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Properties

        //private System.Collections.ObjectModel.ObservableCollection<TabItem> _tabs2 = new System.Collections.ObjectModel.ObservableCollection<TabItem>();
        //private List<TabItem> _tabs = new List<TabItem>();
        //public IEnumerable<TabItem> Tabs
        //{
        //    get
        //    {
        //    }
        //    set
        //    {

        //    }
        //}

        public ObservableCollection<TabItem> Tabs { get; private set; }

        // A standard panel
        public Brush PanelBackground
        {
            get
            {
                return new SolidColorBrush(_colors.Panel_Background);
            }
        }
        public Brush PanelBorder
        {
            get
            {
                return new SolidColorBrush(_colors.Panel_Border);
            }
        }

        // The color of the current tab page's header
        public Brush TabItemSelectedBackground
        {
            get
            {
                return new SolidColorBrush(_colors.TabItemSelected_Background);
            }
        }
        public Brush TabItemSelectedBorder
        {
            get
            {
                return new SolidColorBrush(_colors.TabItemSelected_Border);
            }
        }
        public Brush TabItemSelectedText
        {
            get
            {
                return new SolidColorBrush(_colors.TabItemSelected_Text);
            }
        }

        // The color of a hovered tab page's header
        public Brush TabItemHoveredBackground
        {
            get
            {
                return new SolidColorBrush(_colors.TabItemHovered_Background);
            }
        }
        public Brush TabItemHoveredBorder
        {
            get
            {
                return new SolidColorBrush(_colors.TabItemHovered_Border);
            }
        }
        public Brush TabItemHoveredText
        {
            get
            {
                return new SolidColorBrush(_colors.TabItemHovered_Text);
            }
        }

        #endregion

        #region Public Methods

        public virtual TabControlParts_DragItem GetDragItem(DependencyObject source)
        {
            //NOTE: It's up to the derived classes to figure out what item they are dragging
            return null;
        }

        public static string[] SortCategories(IEnumerable<string> categories)
        {
            //TODO: May want to hardcode categories that should be together
            return categories.
                Distinct().
                OrderBy(o => o).
                ToArray();
        }

        public static bool IsChildOf(UIElement parent, UIElement possibleChild)
        {
            if (parent == possibleChild)
            {
                return true;
            }

            FrameworkElement childCast = possibleChild as FrameworkElement;
            if (childCast == null)
            {
                return false;
            }

            UIElement parentCast = childCast.Parent as UIElement;
            if (parentCast == null)
            {
                return false;
            }

            // Recurse
            return IsChildOf(parent, parentCast);
        }

        /// <summary>
        /// This would get called if a part is deleted from the surface of the editor.  The editor will give the part back to the tabcontrol
        /// </summary>
        public virtual void AddPart(PartToolItemBase part2D, PartDesignBase part3D)
        {
        }
        /// <summary>
        /// This would get called if the user deleted a part, then later hit undo.  That part needs to be removed from the tab control again
        /// </summary>
        public virtual void RemovePart(PartToolItemBase part2D, PartDesignBase part3D)
        {
        }

        #endregion
        #region Protected Methods

        protected void OnPropertyChanged(string info)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }

    #endregion

    #region class: TabControlPartsVM_CreateNew

    public class TabControlPartsVM_CreateNew : TabControlPartsVM
    {
        #region Declaration Section

        private readonly PartToolItemBase[] _parts;

        #endregion

        #region Constructor

        public TabControlPartsVM_CreateNew(EditorColors colors, IEnumerable<PartToolItemBase> parts)
            : base(colors)
        {
            _parts = parts.ToArray();

            BuildToolBox(_parts);
        }

        #endregion

        #region Public Methods

        public override TabControlParts_DragItem GetDragItem(DependencyObject source)
        {
            UIElement sourceCast = source as UIElement;
            if (sourceCast == null)
            {
                return null;
            }

            foreach (PartToolItemBase part in _parts)
            {
                if (IsChildOf(part.Visual2D, sourceCast))
                {
                    return new TabControlParts_DragItem(part.GetNewDesignPart(), part);
                }
            }

            return null;
        }

        //NOTE: Ignoring base.Add and Remove.  This has a fixed number of blueprint parts

        #endregion

        #region Private Methods

        private void BuildToolBox(IEnumerable<PartToolItemBase> parts)
        {
            string[] tabNames = parts.
                Select(o => o.TabName).
                Distinct().
                ToArray();

            if (tabNames.Length > 1)
            {
                throw new ArgumentException("parts span tabNames, not sure how to handle that: " + string.Join(", ", tabNames));
            }

            string[] categories = SortCategories(parts.Select(o => o.Category));

            Brush brushPrimary = new SolidColorBrush(_colors.TabIcon_Primary);
            Brush brushSecondary = new SolidColorBrush(_colors.TabIcon_Secondary);

            // Remove existing tabs
            base.Tabs.Clear();

            // Make the tabs
            foreach (string category in categories)
            {
                TabItem tab = new TabItem()
                {
                    DataContext = this,
                    Header = PartCategoryIcons.GetIcon(tabNames[0], category, brushPrimary, brushSecondary, 24),
                    ToolTip = category,
                };

                ScrollViewer scroll = new ScrollViewer()
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(0, 0, 6, 0),
                };

                UniformGrid panel = new UniformGrid()
                {
                    Columns = 1,
                    MaxWidth = 200,
                };
                scroll.Content = panel;

                tab.Content = scroll;

                // Add the parts that belong in this tab/category
                //TODO: Sort these
                foreach (PartToolItemBase part in parts.Where(o => o.TabName == tabNames[0] && o.Category == category))
                {
                    panel.Children.Add(part.Visual2D);
                }

                base.Tabs.Add(tab);
            }
        }

        #endregion
    }

    #endregion

    #region class: TabControlPartsVM_FixedSupply

    public class TabControlPartsVM_FixedSupply : TabControlPartsVM
    {
        #region Declaration Section

        //private readonly List<Tuple<PartToolItemBase, PartDesignBase>> _tabParts = new List<Tuple<PartToolItemBase, PartDesignBase>>();
        private readonly ObservableCollection<Tuple<PartToolItemBase, PartDesignBase>> _tabParts = new ObservableCollection<Tuple<PartToolItemBase, PartDesignBase>>();

        private readonly List<Tuple<string, UniformGrid>> _tabStats = new List<Tuple<string, UniformGrid>>();

        private readonly Brush _brushPrimary;
        private readonly Brush _brushSecondary;

        private string _tabName = null;

        #endregion

        #region Constructor

        public TabControlPartsVM_FixedSupply(EditorColors colors, IEnumerable<PartDesignBase> parts)
            : base(colors)
        {
            _brushPrimary = new SolidColorBrush(_colors.TabIcon_Primary);
            _brushSecondary = new SolidColorBrush(_colors.TabIcon_Secondary);

            _tabParts.CollectionChanged += TabParts_CollectionChanged;

            foreach (PartDesignBase part in parts)
            {
                AddToTab(part);
            }
        }

        #endregion

        public IEnumerable<PartDesignBase> TabParts_DEBUG
        {
            get
            {
                return _tabParts.Select(o => o.Item2);
            }
        }
        public readonly List<Tuple<long, DateTime>> PreviousRemoved = new List<Tuple<long, DateTime>>();
        public readonly long Token = TokenGenerator.NextToken();
        public readonly List<Tuple<System.Collections.Specialized.NotifyCollectionChangedAction, string, long, DateTime>> PartChangeHistory = new List<Tuple<System.Collections.Specialized.NotifyCollectionChangedAction, string, long, DateTime>>();

        private void TabParts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Collections.IList list = null;
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                list = e.NewItems;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                list = e.OldItems;
            }

            long token = -1;
            if (list != null && list.Count == 1)
            {
                var cast = list[0] as Tuple<PartToolItemBase, PartDesignBase>;

                if (cast != null && cast.Item2 != null)
                {
                    token = cast.Item2.Token;
                }
            }

            this.PartChangeHistory.Add(Tuple.Create(e.Action, Environment.StackTrace, token, DateTime.UtcNow));
        }

        #region Public Methods

        public override TabControlParts_DragItem GetDragItem(DependencyObject source)
        {
            UIElement sourceCast = source as UIElement;
            if (sourceCast == null)
            {
                return null;
            }

            foreach (var part in _tabParts)
            {
                if (IsChildOf(part.Item1.Visual2D, sourceCast))
                {
                    return new TabControlParts_DragItem(part.Item2, dropped: Part_Dropped);
                }
            }

            return null;
        }

        public override void AddPart(PartToolItemBase part2D, PartDesignBase part3D)
        {
            if (part3D != null)
            {
                AddToTab(part3D);
            }
        }
        public override void RemovePart(PartToolItemBase part2D, PartDesignBase part3D)
        {
            if (part3D != null)
            {
                RemoveFromTab(part3D);

                this.PreviousRemoved.Add(Tuple.Create(part3D.Token, DateTime.UtcNow));
            }
        }

        #endregion

        #region Event Listeners

        private void Part_Dropped(TabControlParts_DragItem e)
        {
            RemoveFromTab(e.Part3D);

            this.PreviousRemoved.Add(Tuple.Create(e.Part3D.Token, DateTime.UtcNow));
        }

        #endregion

        #region Private Methods

        private void AddToTab(PartDesignBase part)
        {
            var key = new Tuple<PartToolItemBase, PartDesignBase>(part.GetToolItem(), part);

            _tabParts.Add(key);

            if (_tabName == null)
            {
                _tabName = key.Item1.TabName;
            }
            else if (_tabName != key.Item1.TabName)
            {
                throw new ArgumentException(string.Format("parts span tabNames, not sure how to handle that: \"{0}\", \"{1}\"", _tabName, key.Item1.TabName));
            }

            int tabIndex = FindOrCreateTab(key);

            // The standard 2D element is pretty basic, add a tooltip
            FrameworkElement asFramework = key.Item1.Visual2D as FrameworkElement;
            if (asFramework != null && asFramework.ToolTip == null)
            {
                asFramework.ToolTip = new TextBlock()
                {
                    Text = string.Format("volume {0}", Math1D.Avg(part.Scale.X, part.Scale.Y, part.Scale.Z).ToStringSignificantDigits(3)),
                };
            }

            _tabStats[tabIndex].Item2.Children.Add(key.Item1.Visual2D);
        }
        private void RemoveFromTab(PartDesignBase part)
        {
            var previouslyRemoved = this.PreviousRemoved.
                Where(o => o.Item1 == part.Token).
                OrderByDescending(o => o.Item2).
                FirstOrDefault();

            if(previouslyRemoved != null)
            {
                DateTime utcNow = DateTime.UtcNow;
                TimeSpan gap = utcNow - previouslyRemoved.Item2;
                double milliseconds = gap.TotalMilliseconds;
            }





            // Find the part
            int partIndex = FindPart(part);
            if (partIndex < 0)
            {
                // This seems to be a reentry thing
                //throw new ApplicationException("Couldn't find part");
                string trace = Environment.StackTrace;
                return;
            }

            // Find the tab
            int tabIndex = FindTab(_tabParts[partIndex]);
            if (tabIndex < 0)
            {
                throw new ApplicationException("Couldn't find tab");
            }

            // Remove from part
            _tabStats[tabIndex].Item2.Children.Remove(_tabParts[partIndex].Item1.Visual2D);





            if (_tabParts[partIndex].Item2.Token != part.Token)
            {
                throw new ApplicationException("different");
            }

            _tabParts.RemoveAt(partIndex);





            // Maybe remove tab
            if (_tabStats[tabIndex].Item2.Children.Count == 0)
            {
                _tabStats.RemoveAt(tabIndex);
                base.Tabs.RemoveAt(tabIndex);
            }
        }

        private int FindOrCreateTab(Tuple<PartToolItemBase, PartDesignBase> part)
        {
            int index = FindTab(part);
            if (index >= 0)
            {
                return index;
            }

            string category = part.Item1.Category;

            // Figure out where to insert it
            string[] sorted = SortCategories(UtilityCore.Iterate<string>(_tabStats.Select(o => o.Item1), category));

            index = Array.IndexOf<string>(sorted, category);

            #region create tab

            TabItem tab = new TabItem()
            {
                DataContext = this,
                Header = PartCategoryIcons.GetIcon(part.Item1.TabName, category, _brushPrimary, _brushSecondary, 24),
                ToolTip = category,
            };

            ScrollViewer scroll = new ScrollViewer()
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(0, 0, 6, 0),
            };

            UniformGrid panel = new UniformGrid()
            {
                Columns = 1,
                MaxWidth = 200,
            };
            scroll.Content = panel;

            tab.Content = scroll;

            #endregion

            _tabStats.Insert(index, Tuple.Create(category, panel));
            base.Tabs.Insert(index, tab);

            return index;
        }
        private int FindTab(Tuple<PartToolItemBase, PartDesignBase> part)
        {
            if (_tabStats.Count != this.Tabs.Count)
            {
                throw new ApplicationException(string.Format("_tabStats and this.Tabs are out of sync: {0}, {1}", _tabStats.Count, this.Tabs.Count));
            }

            string category = part.Item1.Category;

            // Find existing
            for (int cntr = 0; cntr < _tabStats.Count; cntr++)
            {
                if (_tabStats[cntr].Item1 == category)
                {
                    return cntr;
                }
            }

            return -1;
        }

        private int FindPart(PartDesignBase part)
        {
            for (int cntr = 0; cntr < _tabParts.Count; cntr++)
            {
                if (_tabParts[cntr].Item2.Token == part.Token)
                {
                    return cntr;
                }
            }

            return -1;
        }

        #endregion
    }

    #endregion

    #region class: TabControlParts_DragObject

    /// <summary>
    /// This will hold a part that they drag from the tab control onto the 3D surface
    /// </summary>
    public class TabControlParts_DragItem
    {
        /// <summary>
        /// This will only be populated if the part can be freely copied.  It won't be populated if the part is a specific item from inventory
        /// </summary>
        /// <remarks>
        /// The editor could be used in two ways:
        ///     To design a new ship from a list of all possible parts
        ///     To edit an existing ship with an inventory of owened parts
        /// </remarks>
        public readonly PartToolItemBase Part2D;
        public readonly PartDesignBase Part3D;

        /// <summary>
        /// Listen to this if you need to know whether the item was dropped
        /// </summary>
        private readonly Action<TabControlParts_DragItem> _dropped;

        public TabControlParts_DragItem(PartDesignBase part3D, PartToolItemBase part2D = null, Action<TabControlParts_DragItem> dropped = null)
        {
            this.Part2D = part2D;
            this.Part3D = part3D;
            _dropped = dropped;
        }

        /// <summary>
        /// When a drop successfully happens, call this.  This tells the source tab control to remove the item that was dragged
        /// </summary>
        public void Dropped()
        {
            if (_dropped != null)
            {
                _dropped(this);
            }
        }
    }

    #endregion
}
