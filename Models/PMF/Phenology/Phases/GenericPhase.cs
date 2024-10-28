using System;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// The phase goes from the a start stage to and end stage. The class requires a target and a progression function.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class GenericPhase : Model, IPhase, IPhaseWithTarget
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction target = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction progression = null;

        // 2. Public properties
        //-----------------------------------------------------------------------------------------------------------------

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
        public double FractionComplete
        {
            get
            {
                if (Target == 0.0)
                    return 1.0;
                else
                    return ProgressThroughPhase / Target;
            }
        }

        /// <summary>Units of progress through phase on this time step.</summary>
        [JsonIgnore]
        public double ProgressionForTimeStep { get; set; }

        /// <summary>Accumulated units of progress through this phase.</summary>
        [JsonIgnore]
        public double ProgressThroughPhase { get; set; }

        /// <summary>Thermal time target to end this phase.</summary>
        [JsonIgnore]
        [Units("oD")]
        public double Target { get { return target.Value(); } }

        // 3. Public methods
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>Compute the phenological development during one time-step.</summary>
        /// <remarks>Returns true when target is met.</remarks>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;

            if (ProgressThroughPhase >= Target)
            {
                // We have entered this timestep after Target decrease below progress so exit without doing anything
                proceedToNextPhase = true;
            }
            else
            {
                ProgressionForTimeStep = progression.Value() * propOfDayToUse;
                ProgressThroughPhase += ProgressionForTimeStep;

                if (ProgressThroughPhase > Target)
                {
                    if (ProgressionForTimeStep > 0.0)
                    {
                        proceedToNextPhase = true;
                        propOfDayToUse *= (ProgressThroughPhase - Target) / ProgressionForTimeStep;
                        ProgressionForTimeStep *= (1 - propOfDayToUse);
                    }
                    ProgressThroughPhase = Target;
                }
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase() { ProgressThroughPhase = 0.0; }

        // 4. Private method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }
    }
}



