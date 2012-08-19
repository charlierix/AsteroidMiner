using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Game.Newt.NewtonDynamics_153;

namespace Game.Newt.AsteroidMiner_153
{
	public class MaterialManager
	{
		#region Declaration Section

		private MaterialPhysics _materials = null;

		#endregion

		#region Constructor

		public MaterialManager(World world)
		{
			_materials = new MaterialPhysics(world);

			_defaultMaterialID = _materials.AddMaterial(.4, .9, .4, false);
			_asteroidMaterialID = _materials.AddMaterial(.25, .9, .75, false);
			_shipMaterialID = _materials.AddMaterial(.5, .9, .4, true);
			_mineralMaterialID = _materials.AddMaterial(.5, .9, .4, false);
		}

		#endregion

		#region Public Properties

		private int _defaultMaterialID = -1;
		public int DefaultMaterialID
		{
			get
			{
				return _defaultMaterialID;
			}
		}

		private int _asteroidMaterialID = -1;
		public int AsteroidMaterialID
		{
			get
			{
				return _asteroidMaterialID;
			}
		}

		private int _shipMaterialID = -1;
		public int ShipMaterialID
		{
			get
			{
				return _shipMaterialID;
			}
		}

		private int _mineralMaterialID = -1;
		public int MineralMaterialID
		{
			get
			{
				return _mineralMaterialID;
			}
		}

		#endregion

		#region Public Methods

		public void SetCollisionCallback(int material1, int material2, CollisionStartHandler collisionStart, CollisionEndHandler collisionEnd)
		{
			_materials.SetCollisionCallback(material1, material2, collisionStart, collisionEnd);
		}

		#endregion

		#region Event Listeners

		//private void CollisionStartHandler(object sender, CollisionStartEventArgs e)
		//{
		//    Mineral mineral = null;
		//    if (e.Body1 is Mineral)
		//    {
		//        mineral = (Mineral)e.Body1;
		//    }
		//    else
		//    {
		//        mineral = (Mineral)e.Body2;
		//    }

		//    if (mineral.MineralType == MineralType.Ice)
		//    {
		//        e.AllowCollision = false;
		//    }
		//}
		//private void CollisionStopHandler(object sender, CollisionEndEventArgs e)
		//{
		//    Mineral mineral = null;
		//    if (e.Body1 is Mineral)
		//    {
		//        mineral = (Mineral)e.Body1;
		//    }
		//    else
		//    {
		//        mineral = (Mineral)e.Body2;
		//    }

		//    if (mineral.MineralType == MineralType.Ice)
		//    {
		//        e.AllowCollision = false;
		//    }
		//}

		#endregion
	}
}
