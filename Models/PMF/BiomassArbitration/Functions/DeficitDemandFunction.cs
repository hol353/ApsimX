using System;
using Models.Core;
using Models.Functions;
using Models.Interfaces;

namespace Models.PMF
{
    /// <summary>
    /// Calculates the Deficit of a given labile nutrient pool and returns it to use for a demand.
    /// </summary>
    [Serializable]
    [Description("This function calculates demands for metabolic and storage pools based on the size of the potential deficits of these pools.  For nutrients is uses maximum, critical and minimum concentration thresholds and for carbon it uses structural, metabolic and storage partitioning proportions to ")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(NutrientDemandFunctions))]
    [ValidParent(ParentType = typeof(IFunction))]
    public class DeficitDemandFunction : Model, IFunction
    {
        /// <summary>Value to multiply demand for.  Use to switch demand on and off</summary>
        [Link(IsOptional = true, Type = LinkType.Child, ByName = true)]
        [Description("Multiplies calculated demand.  Use to switch demand on and off")]
        [Units("unitless")]
        public IFunction multiplier = null;

        private Organ parentOrgan = null;
        private OrganNutrientDelta organNutrientDelta = null;
       
        /// <summary>Name of the organ and nutrient represented by this instance</summary>
        public string OrganAndNutrient
        { get { return parentOrgan.Name+organNutrientDelta.Name; } }

        // Declare a delegate type for calculating the IFunction return value:
        private delegate double DeficitCalculation();
        private DeficitCalculation CalcDeficit;

        private Organ FindParentOrgan(IModel model)
        {
            if (model is Organ) return model as Organ;

            if ((model is IPlant) || (model is IZone) || (model is Simulation)) 
                throw new Exception(Name + "cannot find parent organ to get Structural and Storage DM status");

            return FindParentOrgan(model.Parent);
        }
        private OrganNutrientDelta FindParentNutrientDelta(IModel model)
        {
            if (model is OrganNutrientDelta) return model as OrganNutrientDelta;

            if ((model is Organ) || (model is IPlant) || (model is IZone) || (model is Simulation))
                throw new Exception(Name + "cannot find parent Nutrient Delta");

            return FindParentNutrientDelta(model.Parent);
        }

        private string checkPoolName(string name)
        {
            if ((name == "Structural")|| (name == "Metabolic")|| (name == "Storage"))
                return name;
            else 
                throw new Exception("DeficitDemandFunction must be named \"Structural\", \"Metabolic\" or \"Storage\". Currently named " + name);
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            parentOrgan = FindParentOrgan(this.Parent);
            organNutrientDelta = FindParentNutrientDelta(this.Parent);
            string poolName = checkPoolName(this.Name);
            
            //setup a function pointer (delegate) at simulation commencing so it isn't performing multiple if statements every day
            //cleaner method would probably be to use classes
            var nutrientName = organNutrientDelta.Name;//this.Parent.Parent.Name;
            if (nutrientName == "Nitrogen")
            {
                switch (poolName)
                {
                    case "Structural":
                        CalcDeficit = calcStructuralNitrogenDemand;
                        break;
                    case "Metabolic":
                        CalcDeficit = () => calcDeficitForNitrogenPool(parentOrgan.Live.Nitrogen.Metabolic, organNutrientDelta.ConcentrationOrFraction.Metabolic, organNutrientDelta.ConcentrationOrFraction.Structural);
                        break;
                    case "Storage":
                        CalcDeficit = () => calcDeficitForNitrogenPool(parentOrgan.Live.Nitrogen.Storage, organNutrientDelta.ConcentrationOrFraction.Storage, organNutrientDelta.ConcentrationOrFraction.Metabolic);
                        break;
                };
            }
            else if (nutrientName == "Carbon")
            {
                switch (poolName)
                {
                    case "Structural":
                        CalcDeficit = calcStructuralCarbonDemand;
                        break;
                    case "Metabolic":
                        CalcDeficit = () => calcDeficitForCarbonPool(parentOrgan.Live.Carbon.Metabolic, organNutrientDelta.ConcentrationOrFraction.Metabolic);
                        break;
                    case "Storage":
                        CalcDeficit = () => calcDeficitForCarbonPool(parentOrgan.Live.Carbon.Storage, organNutrientDelta.ConcentrationOrFraction.Storage);
                        break;
                };
            }
            else
            {
                //throw exception for unhandled nutrient as parent?
            }
        }

        private double calcStructuralNitrogenDemand()
        {
            OrganNutrientDelta Carbon = parentOrgan.FindChild("Carbon") as OrganNutrientDelta;
            return Carbon.DemandsAllocated.Total / parentOrgan.Cconc * organNutrientDelta.ConcentrationOrFraction.Structural;
        }

        private double calcDeficitForNitrogenPool(double currentAmount, double upperConc, double LowerConc)
        {
            OrganNutrientDelta Carbon = parentOrgan.FindChild("Carbon") as OrganNutrientDelta;
            double PotentialWt = (parentOrgan.Live.Carbon.Total + Carbon.DemandsAllocated.Total) / parentOrgan.Cconc;
            double targetAmount = (PotentialWt * upperConc) - (PotentialWt * LowerConc);
            return targetAmount - currentAmount;
        }

        private double calcStructuralCarbonDemand()
        {
            return parentOrgan.totalCarbonDemand * organNutrientDelta.ConcentrationOrFraction.Structural;
        }

        private double calcDeficitForCarbonPool(double currentAmount, double poolTargetConc)
        {
            double potentialStructuralC = parentOrgan.Live.Carbon.Structural + calcStructuralCarbonDemand();
            double potentialTotalC = potentialStructuralC / organNutrientDelta.ConcentrationOrFraction.Structural;

            double targetAmount = potentialTotalC * poolTargetConc;
            return targetAmount - currentAmount;
        }


        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double deficit = CalcDeficit();

            if (Double.IsNaN(deficit))
                throw new Exception(this.FullPath + " Must be named Metabolic or Structural to represent the pool it is parameterising and be placed on a NutrientDemand Object which is on Carbon or Nitrogen OrganNutrienDeltaObject");

            // Deficit is limited to max of zero as it cases where ConcentrationsOrProportions change deficits can become negative.
            if (multiplier != null)
                return Math.Max(0, deficit * multiplier.Value());
            else
                return Math.Max(0, deficit);
        }
    }
}
