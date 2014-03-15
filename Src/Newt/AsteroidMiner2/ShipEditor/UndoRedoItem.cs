using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Game.Newt.AsteroidMiner2.ShipEditor
{
    #region Class: UndoRedoAddRemove

    public class UndoRedoAddRemove : UndoRedoBase
    {
        /// <summary>
        /// NOTE: This constructor clones the part, makes sure the token is the same, and clears guide lines
        /// </summary>
        public UndoRedoAddRemove(bool isAdd, DesignPart part, int layerIndex)
            : base(part.Part3D.Token, layerIndex)
        {
            this.IsAdd = isAdd;

            this.Part = part.Clone();
            this.Part.Part3D.Token = this.Token;
            this.Part.GuideLines = null;
        }

        public bool IsAdd
        {
            get;
            private set;
        }

        /// <summary>
        /// This is a clone of the part (be sure to set the token)
        /// </summary>
        public DesignPart Part
        {
            get;
            private set;
        }
    }

    #endregion
    #region Class: UndoRedoTransformChange

    public class UndoRedoTransformChange : UndoRedoBase
    {
        public UndoRedoTransformChange(long token, int layerIndex)
            : base(token, layerIndex) { }

        // Only need to set the ones that changed
        public Vector3D? Scale
        {
            get;
            set;
        }
        public Point3D? Position
        {
            get;
            set;
        }
        public Quaternion? Orientation
        {
            get;
            set;
        }
    }

    #endregion
    #region Class: UndoRedoLockUnlock

    public class UndoRedoLockUnlock : UndoRedoBase
    {
        public UndoRedoLockUnlock(long token, bool isLock, int layerIndex)
            : base(token, layerIndex)
        {
            this.IsLock = isLock;
        }

        public bool IsLock
        {
            get;
            private set;
        }
    }

    #endregion
    #region Class: UndoRedoLayerAddRemove

    public class UndoRedoLayerAddRemove : UndoRedoBase
    {
        public UndoRedoLayerAddRemove(int layerIndex, bool isAdd, string name)
            : base(-1, layerIndex)
        {
            this.IsAdd = isAdd;
            this.Name = name;
        }

        public bool IsAdd
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }
    }

    #endregion

    #region Class: UndoRedoBase

    public class UndoRedoBase
    {
        public UndoRedoBase(long token, int layerIndex)
        {
            this.Token = token;
            this.LayerIndex = layerIndex;
        }

        /// <summary>
        /// This is the affected part's token (if the undo is about a part)
        /// </summary>
        public long Token
        {
            get;
            private set;
        }

        /// <summary>
        /// This is the layer that the part was on, or the layer being changed
        /// </summary>
        public int LayerIndex
        {
            get;
            private set;
        }
    }

    #endregion
}
