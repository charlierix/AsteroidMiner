using SharpNeat.Core;

namespace Game.Newt.v2.Arcanorum.ArenaTester
{
    /// <summary>
    /// This holds common functionality of evaluators
    /// </summary>
    public static class EvaluatorUtil
    {
        public static double GetActualElapsedTime(ref double runningTime, double elapedTime, double maxEvalTime)
        {
            double actualElapsed = elapedTime;
            if (runningTime + elapedTime > maxEvalTime)
            {
                actualElapsed = maxEvalTime - runningTime;
            }

            runningTime += elapedTime;

            return actualElapsed;
        }

        public static FitnessInfo FinishEvaluation(ref bool isEvaluating, double runningTime, double  error, double maxEvalTime, BotTrackingStorage log, TrainingRoom room, BotTrackingEntry[] snapshots)
        {
            isEvaluating = false;

            double fitness = runningTime - error;

            if (fitness < 0)
            {
                fitness = 0;
            }

            fitness *= 1000 / maxEvalTime;     // scale the score to be 1000

            FitnessInfo retVal = new FitnessInfo(fitness, fitness);

            log.AddEntry(new BotTrackingRun(room.Center, room.AABB.Item1, room.AABB.Item2, retVal, snapshots));

            return retVal;
        }
    }
}
