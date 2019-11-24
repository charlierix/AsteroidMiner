﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Game.HelperClassesCore;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipEditor;
using Game.HelperClassesWPF;
using Game.Newt.v2.NewtonDynamics;
using Game.Newt.v2.Arcanorum.Parts;

namespace Game.Newt.v2.Arcanorum.MapObjects
{
    public class ArcBotNPC : ArcBot
    {
        #region Declaration Section

        private bool _hasSetRam = false;

        #endregion

        #region Constructor

        public ArcBotNPC(BotDNA dna, int level, Point3D position, World world, Map map, KeepItems2D keepItems2D, MaterialIDs materialIDs, Viewport3D viewport, EditorOptions editorOptions, ItemOptionsArco itemOptions, IGravityField gravity, DragHitShape dragPlane, Point3D homingPoint, double homingRadius, bool runNeural, bool repairPartPositions)
            : base(dna, level, position, world, map, keepItems2D, materialIDs, viewport, editorOptions, itemOptions, gravity, dragPlane, homingPoint, homingRadius, runNeural, repairPartPositions) { }

        #endregion

        #region Public Properties

        public PartBase[] Parts => _parts?.AllParts;        // once _parts is non null, it will never go back to null

        public NeuralUtility.ContainerOutput[] NeuronLinks => _parts?.Links;

        private long? _nestToken = null;
        /// <summary>
        /// If this is part of a nest, then this will hold that token
        /// </summary>
        public long? NestToken
        {
            get
            {
                return _nestToken;
            }
            set
            {
                _nestToken = value;

                if(_parts != null)
                {
                    foreach (SensorVision sensor in Parts.Where(o => o is SensorVision))
                    {
                        sensor.NestToken = value;
                    }
                }
            }
        }

        #endregion

        #region Overrides

        public override void Update_MainThread(double elapsedTime)
        {
            base.Update_MainThread(elapsedTime);

            if (_aiMousePlate == null)
            {
                // This is only populated if the dna specified a plate writer (if there is no plate writer, then this npc bot will sit forever).
                // The parts are created async, so there could be a few update ticks before the parts are finished being created
                return;
            }

            if (!_hasSetRam)
            {
                _hasSetRam = true;
                this.Ram.SetAIStuff(_aiMousePlate);
            }

            Point3D position = this.PositionWorld;

            // Make sure the plate is not sitting on the drag plane (this is assuming that the bot IS on the drag plane)
            Vector3D? normal = _dragPlane.GetNormal(position);
            if (normal != null)
            {
                normal = normal.Value.ToUnit();

                _aiMousePlate.Position = this.PositionWorld + (normal.Value * 10);
                //NOTE: The drag plane will either be a plane or cylinder, so an up vector of 0,1,0 should always work (normal should always be in the xz plane)
                _aiMousePlate.Look = normal.Value * -1;
            }

            #region CRUDE

            //if (StaticRandom.Next(50) == 0)
            //{
            //    _aiMousePlate.CurrentPoint2D = Math3D.GetRandomVector(_aiMousePlate.MaxXY).ToPoint2D();
            //}

            #endregion

            // Project the 2D point into a real 3D point, and chase after that
            Point3D? chasePoint = _aiMousePlate.ProjectTo3D();

            chasePoint = AdjustIfOffPlane(position, this.Radius, _dragPlane, chasePoint);

            if (chasePoint != null)
            {
                this.DraggingBot.SetPosition(chasePoint.Value);
            }
        }

        protected override void OnConstructFinished()
        {
            // Force the property set to fire now that _parts is populated
            NestToken = _nestToken;
        }

        #endregion

        #region Private Methods

        //TODO: This functionality should be part of MapObjectChaseVelocity
        private static Point3D? AdjustIfOffPlane(Point3D itemPos, double itemRadius, DragHitShape dragPlane, Point3D? chasePoint)
        {
            const double MAXOFFSET = 1.5;
            const double PERCENTATMAX = .01;        // even if they are really far off the plane, don't completely chase the plane, some percent needs to go toward the chase point passed in

            Point3D? pointOnPlane = dragPlane.CastRay(itemPos);
            if (pointOnPlane == null)
            {
                // Don't know where they are relative to the plane (this should never happen)
                return chasePoint;
            }
            else if (Math3D.IsNearValue(pointOnPlane.Value, itemPos))
            {
                // They're position is the same as the point on the plane (they are already on the plane)
                return chasePoint;
            }

            Vector3D vectToPlane = (itemPos - pointOnPlane.Value);
            double distToPlane = vectToPlane.Length;
            if (distToPlane < itemRadius * .01)
            {
                // They are less than a percent of their body size off the plane, just return the point passed in
                return chasePoint;
            }

            if (chasePoint == null)
            {
                // Null was passed in, just go straight for the plane (this should never happen)
                return pointOnPlane;
            }

            // Figure out how much to dive for the plane vs go for the chase point
            double offset = distToPlane / itemRadius;
            double percent = PERCENTATMAX;
            if (offset < MAXOFFSET)
            {
                // They are less than the max allowable distance from the plane
                percent = UtilityCore.GetScaledValue_Capped(PERCENTATMAX, 1d, 0, MAXOFFSET, MAXOFFSET - offset);
            }

            Vector3D direction = chasePoint.Value - pointOnPlane.Value;

            return pointOnPlane.Value + (direction * percent);
        }

        #endregion
    }

    // Inputs:
    //  Current Velocity
    //  Weapon Tip Locations
    //  Weapon Angular Velocity
    //
    //  Vision
    //      - have either one or multiple rings
    //      - have it light up for any item, or tuned to certain types
    //
    //  Need a way to see other bot weapon stats (type, position, ang vel)



    // Outputs:
    //  Ring
    //      - simple version will just be a single ring
    //      - more complex could be a ring with a distance modifier (but that would give a lot of power to that modifier's neuron single)
    //      - more complex could be a few rings
}
