using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    public class Evaluator2 : IPhenomeTickEvaluator<IBlackBox, NeatGenome>
    {
        #region Declaration Section

        private readonly WorldAccessor _worldAccessor;
        private readonly TrainingRoom _room;

        #endregion

        #region Constructor

        public Evaluator2(WorldAccessor worldAccessor, TrainingRoom room)
        {
            _worldAccessor = worldAccessor;
            _room = room;
        }

        #endregion

        #region IPhenomeTickEvaluator<IBlackBox, NeatGenome>

        public ulong EvaluationCount => 0;

        public bool StopConditionSatisfied => false;

        public void Reset()
        {
        }

        public void StartNewEvaluation(IBlackBox phenome, NeatGenome genome)
        {
            _worldAccessor.EnsureInitialized();

            if (_room.Map == null)
            {
                throw new ApplicationException("nooooooooope");
            }

            if (_room.Bot == null)
            {
                throw new ApplicationException("noope");
            }
        }

        public FitnessInfo? EvaluateTick(double elapedTime)
        {
            return null;
        }

        #endregion

        //---------------------- Unit Test 1
        public void TestEvaluate1()
        {
            _worldAccessor.EnsureInitialized();

            if (_guid == null)
            {
                throw new ApplicationException("noooope");
            }
        }

        private Guid? _guid = null;
        public void UnitTest1_FinishInitialization(Guid guid)
        {
            if (_guid != null)
            {
                throw new ApplicationException("");
            }

            _guid = guid;
        }

        //---------------------- Unit Test 2
        public void TestEvaluate2()
        {
            _worldAccessor.EnsureInitialized();

            if(_room.Map == null)
            {
                throw new ApplicationException("nooooooooope");
            }

            //if(_room.Bot == null)
            //{
            //    throw new ApplicationException("noope");
            //}
        }
    }
}
