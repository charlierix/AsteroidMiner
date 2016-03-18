using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.Newt.v2.NewtonDynamics;

namespace Game.Newt.v2.MissileCommand
{
    public class DefenseBot : Bot
    {
        #region Constructor

        public DefenseBot(BotConstruction_Result construction)
            : base(construction)
        {
        }

        #endregion
    }
}
