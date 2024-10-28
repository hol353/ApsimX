using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// Multiplies the start value each day by the value of each child during the parallel phase
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.")]
    public class ParallelPhaseValue : Model, IFunction
    {
        /// <summary>The name of the parallel phase that this function is active in</summary>
        [Description("Parallel Phase Name")]
        public string ParallelPhaseName { get; set; }

        private IParallelPhase pPhase { get; set; }

        [Link(Type = LinkType.Ancestor)] IPlant plant = null;

        /// <summary>The value to have from the start of the simulation until IsInPhase is true</summary>
        [Description("Value from start of simulatoin until IsInPhase")]
        [Link(Type = LinkType.Child, ByName = true)] 
        private IFunction StartValue = null;
       
        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        private double currentValue { get; set; }

        
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Phase start name not set: + Name
        /// or
        /// Phase end name not set: + Name
        /// </exception>
        public double Value(int arrayIndex = -1)
        {
            return currentValue;
        }


        [EventSubscribe("PostPhenology")]
        private void onPostPhenology(object sender, EventArgs e)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            if (pPhase.IsInPhase)
            {
                foreach (IFunction kid in ChildFunctions)
                {
                    if (kid.Name != "StartValue")
                        currentValue *= kid.Value(-1);
                }
            }
        }

        private  bool cropEndedYesterday = false;

        [EventSubscribe("StageWasReset")]
        private void onStageWasReset(object sender, StageSetType sst)
        {
            if ((sst.StageNumber <= pPhase.StartStage)||(pPhase.StartStage==0))
            {
                cropEndedYesterday = true;
            }
        }

        [EventSubscribe("StartOfDay")]
        private void onStartOfDay(object sender, EventArgs e)
        {
            if (cropEndedYesterday) 
            {
                currentValue = StartValue.Value();
                cropEndedYesterday = false;
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            currentValue = StartValue.Value();
            pPhase = plant.FindDescendant<IParallelPhase>(ParallelPhaseName);
        }
    }
}