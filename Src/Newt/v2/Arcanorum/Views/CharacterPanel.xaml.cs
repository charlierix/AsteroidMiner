using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.Arcanorum.Views
{
    public partial class CharacterPanel : UserControl
    {
        #region enum: Tabs

        public enum Tabs
        {
            Inventory,
            CharacterStats
        }

        #endregion

        #region Constructor

        public CharacterPanel()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        public World World
        {
            get
            {
                return pnlCharStats.World;
            }
            set
            {
                pnlCharStats.World = value;
            }
        }

        #endregion

        #region Public Methods

        public void SetCurrentTab(Tabs tab)
        {
            switch (tab)
            {
                case Tabs.Inventory:
                    tabInventory.IsSelected = true;
                    break;

                case Tabs.CharacterStats:
                    tabCharStats.IsSelected = true;
                    break;

                default:
                    throw new ApplicationException("Unknown Tabs: " + tab.ToString());
            }
        }

        #endregion
    }
}
