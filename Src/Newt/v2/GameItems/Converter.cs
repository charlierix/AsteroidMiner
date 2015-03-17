using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.HelperClassesWPF;

namespace Game.Newt.v2.GameItems
{
    /// <summary>
    /// This class pulls stuff from a source container, runs the value through a conversion factor, and puts the converted value into
    /// the destination container
    /// </summary>
    /// <remarks>
    /// The two containers most likely have different meaning (I doubt you would use this to pump water from one container to another)
    /// For instance, the source could be an energy tank, and the destination could represent shields.  (raw materials -> finished product)
    /// </remarks>
    public class Converter
    {
        #region Declaration Section

        private readonly object _lock = new object();

        private readonly IContainer _source;
        private readonly IContainer _destination;

        #endregion

        #region Constructor

        /// <summary>
        /// This constructor tells me that you will be using the timer method for your transfers.  You can also do instantaneous transfers as well.
        /// </summary>
        public Converter(IContainer source, IContainer destination, double conversionRate, double amountToDraw)
        {
            _source = source;
            _destination = destination;
            this.ConversionRate = conversionRate;
            this.AmountToDraw = amountToDraw;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// This is the ratio from the source to the destination (take source times this, and add that to destination)
        /// </summary>
        public double ConversionRate
        {
            get;
            set;
        }
        /// <summary>
        /// This is how much to pull from the source per unit time
        /// </summary>
        public double AmountToDraw
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        //'  The instant transfer will draw either the amount passed in, or as much as I can out of the source container (depending on the
        //'  overloading you call), and place that in the destination container.  I will return the amount left over that I couldn't draw from
        //'  the source (in the sorce's units)
        public static double Transfer(double amountToDraw, IContainer destination, double conversionRate, bool exactAmountOnly)
        {
            // Figure out how much can actually be pulled, and what that equates to
            double actualAmountToPull, actualAmountToPush;
            GetMaxAmountToPull(out actualAmountToPull, out actualAmountToPush, amountToDraw, destination.QuantityMaxMinusCurrent, conversionRate, exactAmountOnly);

            if (actualAmountToPush > 0d)
            {
                // Add this to the destination
                destination.AddQuantity(actualAmountToPush, exactAmountOnly);
            }

            // Return how much of the source couldn't be used
            return amountToDraw - actualAmountToPull;
        }

        /// <summary>
        /// This one transfers as much as possible between the two containers
        /// </summary>
        public static double Transfer(IContainer source, IContainer destination, double conversionRate)
        {
            return Transfer(source, source.QuantityCurrent, destination, conversionRate, false);
        }
        public static double Transfer(IContainer source, double amountToPull, IContainer destination, double conversionRate, bool exactAmountOnly)
        {
            //NOTE: If the containers are being manipulated outside this thread, the transfer won't be accurate (but this method will likely
            //be called from a timer, so the transfer amounts would be somewhat small.  So small blips of innacuracy shouldn't be too bad)

            double requestAmountToPull;

            // See if the amount to pull passed in is more than what the source class holds
            if (amountToPull > source.QuantityCurrent)
            {
                if (exactAmountOnly)
                {
                    return amountToPull;
                }

                requestAmountToPull = source.QuantityCurrent;
            }
            else
            {
                requestAmountToPull = amountToPull;
            }

            // If source only allows multiples to be pulled, then get a multiple it likes
            requestAmountToPull = Container.GetRemoveAmount(source, requestAmountToPull, exactAmountOnly);
            if (requestAmountToPull == 0d)
            {
                return amountToPull;
            }

            // Figure out how much can actually be pulled, and what that equates to
            double actualAmountToPull;
            double actualAmountToPush;
            GetMaxAmountToPull(out actualAmountToPull, out actualAmountToPush, requestAmountToPull, destination.QuantityMaxMinusCurrent, conversionRate, exactAmountOnly);

            if (actualAmountToPull != requestAmountToPull && source.OnlyRemoveMultiples)
            {
                // The destination couldn't hold it all, try again
                actualAmountToPull = Container.GetRemoveAmount(source, actualAmountToPull, exactAmountOnly);
                GetMaxAmountToPull(out actualAmountToPull, out actualAmountToPush, requestAmountToPull, destination.QuantityMaxMinusCurrent, conversionRate, exactAmountOnly);
            }

            if (actualAmountToPull > 0d)
            {
                // Pull this from the source
                source.RemoveQuantity(actualAmountToPull, exactAmountOnly);

                // Add this to the destination
                destination.AddQuantity(actualAmountToPush, exactAmountOnly);
            }

            // Return how much of the source couldn't be used
            return amountToPull - actualAmountToPull;
        }

        public void Transfer(double elapsedTime)
        {
            Transfer(elapsedTime, 1d);
        }
        public void Transfer(double elapsedTime, double percent)
        {
            lock (_lock)
            {
                // Figure out how much to try to pull
                double requestAmoutToPull = this.AmountToDraw * elapsedTime;

                // Do the conversion
                Transfer(_source, requestAmoutToPull, _destination, this.ConversionRate, false);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This takes in the amount you want to pull, how much can be stored, the conversion rate, and returns the max amount you can
        /// pull, and what that amount pulled equates to in destination terms.
        /// </summary>
        /// <param name="maxSourcePull">This is the max amount you can pull and still fit in destination (this value will never be larger than sourceAmtToPull)</param>
        /// <param name="maxDestinationPush">This is how much maxSourcePull equates to after being converted (this value will never be larger than destAmtRemaining)</param>
        /// <param name="sourceAmountToPull">This is how much you want to pull from the source container</param>
        /// <param name="destAmountRemaining">This is how much room is left in the destination container</param>
        /// <param name="conversionRate">This is the conversion rate from source to destination (source * rate = dest)</param>
        private static void GetMaxAmountToPull(out double maxSourcePull, out double maxDestinationPush, double sourceAmountToPull, double destAmountRemaining, double conversionRate, bool exactAmountOnly)
        {
            maxDestinationPush = sourceAmountToPull * conversionRate;

            if (maxDestinationPush <= destAmountRemaining)
            {
                // It all fits
                maxSourcePull = sourceAmountToPull;
            }
            else if (!exactAmountOnly)
            {
                // Pulling less isn't allowed
                maxSourcePull = 0d;
                maxDestinationPush = 0d;
            }
            else
            {
                // The amount requested to pull will exceed the capacity of the destination.  Figure out how much can be pulled
                maxSourcePull = destAmountRemaining / conversionRate;
                maxDestinationPush = destAmountRemaining;
            }
        }

        #endregion
    }
}
