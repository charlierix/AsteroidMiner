using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Game.HelperClassesAI;
using Game.HelperClassesCore;
using Game.HelperClassesWPF;
using Game.Newt.v2.GameItems;

namespace Game.Newt.Testers.SwarmBots
{
    /// <summary>
    /// This maintains a list of groups of swarmbots
    /// </summary>
    /// <remarks>
    /// This periodically queries the map, and runs swarmbot positions through a SOM
    /// 
    /// This can be used when searching for nearest group, or assigning objectives
    /// </remarks>
    public class SwarmClusters
    {
        #region Declaration Section

        private readonly Map _map;

        #endregion

        #region Constructor

        public SwarmClusters(Map map)
        {
            _map = map;
        }

        #endregion

        public volatile SwarmCluster[] Clusters = null;

        public void Tick()
        {
            var bots = _map.GetItems<SwarmBot1a>(false).ToArray();

            if (bots.Length == 0)
            {
                this.Clusters = null;
                return;
            }

            SOMInput<SwarmBot1a>[] inputs = bots.
                Select(o => new SOMInput<SwarmBot1a>() { Source = o, Weights = o.PositionWorld.ToArray() }).
                ToArray();

            SOMRules rules = SOMRules.GetRandomRules();

            SOMResult som = SelfOrganizingMaps.TrainSOM(inputs, rules, false);

            SwarmCluster[] clusters = som.InputsByNode.
                Select(o =>
                {
                    SwarmBot1a[] bots2 = o.
                        Select(p => ((SOMInput<SwarmBot1a>)p).Source).
                        ToArray();

                    return new SwarmCluster(bots2);
                }).
                ToArray();

            this.Clusters = clusters;
        }
    }

    #region Class: SwarmCluster

    public class SwarmCluster
    {
        public SwarmCluster(SwarmBot1a[] bots)
        {
            this.Bots = bots;
        }

        public readonly SwarmBot1a[] Bots;
        public readonly long Token = TokenGenerator.NextToken();

        /// <summary>
        /// This looks at all the bots in the cluster that aren't disposed, and returns current centerpoint, velocity, etc
        /// NOTE: This will return null if all bots are disposed
        /// </summary>
        public SwarmClusterInfo GetCurrentInfo()
        {
            Tuple<Point3D, Vector3D>[] positionsVelocities = this.Bots.
                Where(o => !o.IsDisposed).
                Select(o => Tuple.Create(o.PositionWorld, o.VelocityWorld)).
                ToArray();

            if (positionsVelocities.Length == 0)
            {
                return null;
            }

            return new SwarmClusterInfo(positionsVelocities);
        }

        public static bool IsSame(SwarmCluster[] cluster1, SwarmCluster[] cluster2)
        {
            if (cluster1 == null && cluster2 == null)
            {
                return true;
            }
            else if (cluster1 == null || cluster2 == null)
            {
                return false;
            }
            else if (cluster1.Length != cluster2.Length)
            {
                return false;
            }

            for (int cntr = 0; cntr < cluster1.Length; cntr++)
            {
                if (cluster1[cntr].Token != cluster2[cntr].Token)
                {
                    return false;
                }
            }

            return true;
        }
    }

    #endregion
    #region Class: SwarmClusterInfo

    public class SwarmClusterInfo
    {
        public SwarmClusterInfo(Tuple<Point3D, Vector3D>[] positionsVelocities)
        {
            this.PositionsVelocities = positionsVelocities;

            Point3D[] positions = positionsVelocities.
                Select(o => o.Item1).
                ToArray();

            this.Count = positionsVelocities.Length;
            this.Center = Math3D.GetCenter(positions);
            this.Velocity = Math3D.GetAverage(positionsVelocities.Select(o => o.Item2));
            this.AABB = Math3D.GetAABB(positions);

            var avg_stddev = Math1D.Get_Average_StandardDeviation(positions.Select(o => (o - this.Center).Length));

            this.Radius = avg_stddev.Item1 + (avg_stddev.Item2 * 2);      // 95% of items are within 2 standard deviations, so use that as the radius
        }

        public readonly int Count;
        public readonly Point3D Center;
        public readonly Vector3D Velocity;
        public readonly Tuple<Point3D, Point3D> AABB;
        /// <summary>
        /// This is approximate radius.  There will be some bots outside this radius, but most will be inside
        /// </summary>
        public readonly double Radius;

        public readonly Tuple<Point3D, Vector3D>[] PositionsVelocities;
    }

    #endregion
}
