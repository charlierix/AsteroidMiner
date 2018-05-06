using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.Newt.v2.GameItems;
using Game.Newt.v2.GameItems.ShipParts;
using Game.Newt.v2.NewtonDynamics;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    /// <summary>
    /// This is a room that is ready to be used by an evaluator
    /// </summary>
    /// <remarks>
    /// It would be expensive to instantiate/dispose a bot for every evaluation run.  So instead, the room contains a prebuilt
    /// bot, and the evaluator just needs to put its specific brain phenotype into the brain part
    /// </remarks>
    public class TrainingRoom
    {
        /// <summary>
        /// The bot will contain many parts, but this is exposed so that the evaluator can just overwrite this brainpart's phenotype,
        /// let the simulation start, and start scoring
        /// </summary>
        public BrainNEAT BrainPart { get; set; }

        /// <summary>
        /// Some evaluators might require this
        /// </summary>
        public SensorHoming[] HomingParts { get; set; }

        public IPhenomeTickEvaluator<IBlackBox, NeatGenome> Evaluator;

        // ---------------------- Everything below is probably unecessary for the evaluator, but putting it here now to work out what will be needed for a complete test run

        public Bot Bot { get; set; }

        // This should contain any necessary items in the room (treasure boxes to destroy, weapons to pick up, etc)
        public Map Map { get; set; }

        public World World { get; set; }

        public Point3D Center { get; set; }
        public (Point3D, Point3D) AABB { get; set; }

        public int Index { get; set; }
    }
}
