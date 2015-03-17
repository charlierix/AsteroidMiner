using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Newt.v1.AsteroidMiner1.ShipAddons
{
	/// <summary>
	/// This defines a class that is designed to hang on to quantities of stuff
	/// </summary>
	/// <remarks>
	/// You would use a container for all kinds of purposes (Ammo Clip, Money, Fuel Tank).  Basically anything that needs to be 
	/// packaged and transported around.
	/// 
	/// I was batting around the idea of allowing flow restrictions.  Think of a senario with an energy tank, a fuel tank, and an 
	/// energy-matter converter sitting between them.  But I decided that the restriction is in the converter, not the tanks.  The 
	/// biggest problem with having flow restrictions at the container level is how to enforce it.  If the max flow is ten, and there 
	/// were 100 requests for 10, then the container just let out 1000 fluid.  Did this occur over a second, or an hour?  Now every 
	/// container needs to get updates on the game's time.  While this is a technically accurate model, it is over complex, because
	/// it is most likely the user of my contents that has the real bottleneck (a machine gun has a very predictable rate of fire, etc.)
	/// </remarks>
	public interface IContainer
	{
		//TODO: This would be nice to have in the future
		//public event ValueMilestone(enum whichMileStone?, bool isUpStroke, double currentValue);

		double QuantityCurrent { get; set; }
		double QuantityMax { get; set; }
		double QuantityMaxMinusCurrent { get; }      // This one is for cases that need this value, and don't want to use two gets every time they need it calculated.

		/// <summary>
		/// This function will try to add to this my current quantity.  I will return the amount left over.  The only time the amount left over will
		/// be greater than zero is if I have exceeded my capacity.
		/// </summary>
		double AddQuantity(double amount, bool exactAmountOnly);

		/// <summary>
		/// This function will transfer stuff from the container passed in into me.  I will return the amount that I COULDN'T transfer.
		/// </summary>
		/// <remarks>
		/// There are a couple situations that could happen.  I could be too full to hold all that was requested.  The source container may not have had
		/// enough to fulfill the request.
		/// </remarks>
		double AddQuantity(IContainer pullFrom, double amount, bool exactAmountOnly);

		/// <summary>
		/// This function will suck everything I possibly can from the container passed in, and add that to myself.
		/// The return variable will hold the amount that I COULDN'T transfer.  So if I sucked it all, I will return zero.
		/// </summary>
		double AddQuantity(IContainer pullFrom, bool exactAmountOnly);

		/// <summary>
		/// This function will try to deplete my current quantity
		/// </summary>
		/// <remarks>
		/// I will return the amount left over from the request.  The only time the amount left over will be greater than zero is if I have hit zero
		/// capacity.  Or if exactAmountOnly is true, and I couldn't remove exactly that amount.
		/// </remarks>
		double RemoveQuantity(double amount, bool exactAmountOnly);
	}
}
