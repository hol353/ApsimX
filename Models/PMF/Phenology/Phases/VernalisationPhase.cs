using System;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage
    /// and reaches end when vernalisation saturation occurs.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class VernalisationPhase : Model, IPhase, IPhaseWithTarget
    {
        [Link]
        private IVrnExpression CAMP = null;

        private bool firstDay = true;
        private double relativeVernalisationAtEmergence { get; set; }

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
        public double FractionComplete { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        public double Target { get; set; }

        /// <summary>Summarise gene expression from CAMP into phenological progress</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            Target = 1 + CAMP.Vrn2;
            double RelativeVernalisation = Math.Min((CAMP.BaseVrn + CAMP.Vrn1 + CAMP.Vrn3) / Target, CAMP.MaxVrn);
            if (firstDay)
            {
                relativeVernalisationAtEmergence = RelativeVernalisation;
                firstDay = false;
            }
            double ProgressThroughPhase = Math.Min(1, (RelativeVernalisation - relativeVernalisationAtEmergence) / (1 - relativeVernalisationAtEmergence));
            FractionComplete = ProgressThroughPhase;
            return CAMP.IsVernalised;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            firstDay = true;
            relativeVernalisationAtEmergence = 0.0;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }

    }
}
