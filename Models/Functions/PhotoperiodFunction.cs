using System;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>
    /// Returns the duration of the day, or photoperiod, in hours.  This is calculated using the specified latitude (given in the weather file)
    /// and twilight sun angle threshold.  If a variable called ClimateControl.PhotoPeriod is found in the simulation, it will be used instead.
    /// </summary>
    /// <remarks>The day length is calculated with \ref MathUtilities.DayLength.</remarks>
    /// \pre A \ref Models.WeatherFile function has to exist.
    /// \pre A \ref Models.Clock function has to be existed to retrieve day of year
    /// \param Twilight The interval between sunrise or sunset and the time when the true centre of the sun is below the horizon as a specified angle.
    /// \retval The day length of a specified day and location. Variable "photoperiod" will be returned if simulation environment has a variable called ClimateControl.PhotoPeriod.
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PhotoperiodFunction : Model, IFunction
    {

        /// <summary>The met data.</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>The clock.</summary>
        [Link]
        protected IClock Clock = null;

        /// <summary>The twilight angle.</summary>
        [Description("Twilight angle")]
        [Units("degrees")]
        public double Twilight { get; set; }

        /// <summary>The daylight length.</summary>
        [Units("hours")]
        public double DayLength { get; set; }

        /// <summary>Gets the main output of this function.</summary>
        /// <param name="arrayIndex">Not expected for this function.</param>
        /// <returns>The daylight duration (hours).</returns>
        public double Value(int arrayIndex = -1)
        {
            return DayLength;
        }

        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            if (MetData != null)
                DayLength = MetData.CalculateDayLength(Twilight);
            else
                DayLength = 0;
        }
    }
}
