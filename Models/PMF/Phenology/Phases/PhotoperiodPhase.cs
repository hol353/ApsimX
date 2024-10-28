using System;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage and reaches
    /// the end stage when the photo period passes a user-defined critical value.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class PhotoperiodPhase : Model, IPhase
    {
        [Link(ByName = true)]
        IFunction Photoperiod = null;

        [Link(ByName = true)]
        IFunction PhotoperiodDelta = null;

        /// <summary>Critical photoperiod to move into next phase</summary>
        [Description("Critical photoperiod to move into next phase")]
        public double CricialPhotoperiod { get; set; }

        /// <summary>
        ///  Photoperiod Type
        /// </summary>
        public enum PPType
        {
            /// <summary>
            /// Increasing Photoperiod
            /// </summary>
            Increasing,
            /// <summary>
            /// Decreasing Photoperiod
            /// </summary>
            Decreasing
        }

        /// <summary>Flag to specify whether photoperiod should be increasing</summary>
        [Description("Flag to specify whether photoperiod should be increasing")]
        public PPType PPDirection { get; set; }

        /// <summary>The phenological stage at the start of this phase.</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The phenological stage at the end of this phase.</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>Fraction of phase that is complete (0-1).</summary>
        [JsonIgnore]
        public double FractionComplete { get; }

        /// <summary>Units of progress through phase on this time step.</summary>
        [JsonIgnore]
        public double ProgressionForTimeStep { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase;

            if (Photoperiod.Value() > CricialPhotoperiod && PhotoperiodDelta.Value() > 0 && PPDirection == PPType.Increasing)
                proceedToNextPhase = true;
            else if (Photoperiod.Value() < CricialPhotoperiod && PhotoperiodDelta.Value() < 0 && PPDirection == PPType.Decreasing)
                proceedToNextPhase = true;
            else
                proceedToNextPhase = false;

            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { }

    }
}
