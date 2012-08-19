namespace Game.Newt.NewtonDynamics_153.Api
{
	public enum EPlatformArchitecture
	{
		ForceHardware = 0,
		FloatingPointEnhancement = 1,
		BestHardwareSetting = 2,
	}

	public enum SolverModel
	{
		ExactMode = 0,
		AdaptativeMode = 1,
		LinearMode_2Passes = 2,
		LinearMode_3Passes = 3,
		LinearMode_4Passes = 4,
		LinearMode_5Passes = 5,
		LinearMode_6Passes = 6,
		LinearMode_7Passes = 7,
		LinearMode_8Passes = 8,
		LinearMode_9Passes = 9,
	}

	public enum FrictionModel
	{
		ExactModel = 0,
		AdaptativeModel = 1,
	}
}
