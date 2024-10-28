using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.PMF
{

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    public class NutrientsStates : Model
    {
        /// <summary>Carbon</summary>
        public double C { get; private set; }
        /// <summary>Nitrogen</summary>
        public double N { get; private set; }
        /// <summary>Phospherous</summary>
        public double P { get; private set; }
        /// <summary>Potassium</summary>
        public double K { get; private set; }

        /// <summary>Constructor</summary>
        public NutrientsStates(double c, double n, double p, double k)
        {
            C = c;
            N = n;
            P = p;
            K = k;
        }
    }

    /// <summary>
    /// Daily state of flows into and out of each organ
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Organ))]
    public class OrganNutrientsState : Model
    {
        /// <summary> The weight of the organ</summary>
        public NutrientPoolsState Weight => Cconc > 0 ? Carbon / Cconc : new NutrientPoolsState(0, 0, 0);

        /// <summary> The weight of the organ</summary>
        public double Wt => Weight.Total;
            
        /// <summary> The Carbon of the organ</summary>
        public double C => Carbon.Total;
            
        /// <summary> The Nitrogen of the organ</summary>
        public double N => Nitrogen.Total;

        /// <summary> The Phosphorus of the organ</summary>
        public double P => Phosphorus.Total;

        /// <summary> The Potassium of the organ</summary>
        public double K => Potassium.Total;
            
        /// <summary> The N concentration of the organ</summary>
        public double NConc => Wt > 0 ? N / Wt : 0;

        /// <summary> The P concentration of the organ</summary>
        public double PConc => Wt > 0 ? P / Wt : 0;

        /// <summary> The K concentration of the organ</summary>
        public double KConc => Wt > 0 ? K / Wt : 0;


        /// <summary> The concentraion of carbon in total dry weight</summary>
        public double Cconc { get; private set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolsState Carbon { get; private set; }

        /// <summary> The organs Carbon components </summary>
        public NutrientPoolsState Nitrogen { get; private set; }

        /// <summary> The organs phosphorus </summary>
        public NutrientPoolsState Phosphorus { get; private set; }

        /// <summary> The organs Potasium components </summary>
        public NutrientPoolsState Potassium { get; private set; }

        /// <summary>Constructor </summary>
        public OrganNutrientsState(NutrientPoolsState carbon, NutrientPoolsState nitrogen, NutrientPoolsState phosphorus, NutrientPoolsState potassium, double cconc)
        {
            Carbon = carbon;
            Nitrogen = nitrogen;
            Phosphorus = phosphorus;
            Potassium = potassium;
            Cconc = cconc;
        }

        /// <summary>Constructor </summary>
        public OrganNutrientsState(OrganNutrientsState values, double Cconc)
        {
            Set(values, Cconc);
        }

        /// <summary>Constructor </summary>
        public void Clear()
        {
            Carbon = new NutrientPoolsState();
            Nitrogen = new NutrientPoolsState();
            Phosphorus = new NutrientPoolsState();
            Potassium = new NutrientPoolsState();
            Cconc = 0;
        }

        /// <summary>Set the current state </summary>
        public void Set(OrganNutrientsState values, double cconc)
        {
            Carbon = values.Carbon;
            Nitrogen = values.Nitrogen;
            Phosphorus = values.Phosphorus;
            Potassium = values.Potassium;
            Cconc = cconc;
        }

        /// <summary>Constructor </summary>
        public OrganNutrientsState()
        {
            Carbon = new NutrientPoolsState();
            Nitrogen = new NutrientPoolsState();
            Phosphorus = new NutrientPoolsState();
            Potassium = new NutrientPoolsState();
            Cconc = 1.0;
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState Divide(OrganNutrientsState a, double b, double cconc)
        {
            OrganNutrientsState ret = new OrganNutrientsState();
            ret.Carbon = a.Carbon / b;
            ret.Nitrogen = a.Nitrogen / b;
            ret.Phosphorus = a.Phosphorus / b;
            ret.Potassium = a.Potassium / b;
            ret.Cconc = cconc;
            return ret;

        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState Divide(OrganNutrientsState a, OrganNutrientsState b, double cconc)
        {
            OrganNutrientsState ret = new OrganNutrientsState();
            ret.Carbon = a.Carbon / b.Carbon;
            ret.Nitrogen = a.Nitrogen / b.Nitrogen;
            ret.Phosphorus = a.Phosphorus / b.Phosphorus;
            ret.Potassium = a.Potassium / b.Potassium;
            ret.Cconc = cconc;
            return ret;
        }

        /// <summary>return pools multiplied by value</summary>
        public static OrganNutrientsState Multiply(OrganNutrientsState a, double b, double cconc)
        {
            OrganNutrientsState ret = new OrganNutrientsState();
            ret.Carbon = a.Carbon * b;
            ret.Nitrogen = a.Nitrogen * b;
            ret.Phosphorus = a.Phosphorus * b;
            ret.Potassium = a.Potassium * b;
            ret.Cconc = cconc;
            return ret;
        }

        /// <summary>return pools divied by value</summary>
        public static OrganNutrientsState Multiply(OrganNutrientsState a, OrganNutrientsState b, double cconc)
        {
            OrganNutrientsState ret = new OrganNutrientsState();
            ret.Carbon = a.Carbon * b.Carbon;
            ret.Nitrogen = a.Nitrogen * b.Nitrogen;
            ret.Phosphorus = a.Phosphorus * b.Phosphorus;
            ret.Potassium = a.Potassium * b.Potassium;
            ret.Cconc = cconc;
            return ret;
        }

        /// <summary>return sum or two pools</summary>
        public static OrganNutrientsState Add(OrganNutrientsState a, OrganNutrientsState b, double cconc)
        {
            OrganNutrientsState ret = new OrganNutrientsState();
            ret.Carbon = a.Carbon + b.Carbon;
            ret.Nitrogen = a.Nitrogen + b.Nitrogen;
            ret.Phosphorus = a.Phosphorus + b.Phosphorus;
            ret.Potassium = a.Potassium + b.Potassium;
            ret.Cconc = cconc;
            return ret;
        }

        /// <summary>return sum or two pools</summary>
        public static OrganNutrientsState Subtract(OrganNutrientsState a, OrganNutrientsState b, double cconc)
        {
            OrganNutrientsState ret = new OrganNutrientsState();
            ret.Carbon = a.Carbon - b.Carbon;
            ret.Nitrogen = a.Nitrogen - b.Nitrogen;
            ret.Phosphorus = a.Phosphorus - b.Phosphorus;
            ret.Potassium = a.Potassium - b.Potassium;
            ret.Cconc = cconc;
            return ret;
        }

        /// <summary>Initializes a new instance of the <see cref="Biomass"/> class from the OrganNutrientState passed in</summary>
        public Biomass ToBiomass
        {
            get
            {
                Biomass retBiomass = new Biomass();
                retBiomass.StructuralWt = this.Weight.Structural;
                retBiomass.MetabolicWt = this.Weight.Metabolic;
                retBiomass.StorageWt = this.Weight.Storage;
                retBiomass.StructuralN = this.Nitrogen.Structural;
                retBiomass.MetabolicN = this.Nitrogen.Metabolic;
                retBiomass.StorageN = this.Nitrogen.Storage;
                return retBiomass;
            }
        }
    }
    

    /// <summary>
    /// This is a composite biomass class, representing the sum of 1 or more biomass objects.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class CompositeStates : OrganNutrientsState
    {
        private List<OrganNutrientsState> components = new List<OrganNutrientsState>();

        /// <summary>List of Organ states to include in composite state</summary>
        [Description("List of organs to agregate into composite biomass.")]
        public string[] Propertys { get; set; }

        /// <summary>Clear ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (string PropertyName in Propertys)
            {
                OrganNutrientsState c = (OrganNutrientsState)(this.FindByPath(PropertyName)?.Value);
                if (c == null)
                    throw new Exception("Cannot find: " + PropertyName + " in composite state: " + this.Name);
            }
        }

        /// <summary>/// Add components together to give composite/// </summary>

        [EventSubscribe("PartitioningComplete")]
        public void onPartitioningComplete(object sender, EventArgs e)
        {
            Clear();
            foreach (string PropertyName in Propertys)
            {
                OrganNutrientsState c = (OrganNutrientsState)(this.FindByPath(PropertyName)?.Value);
                AddDelta(c);
            }
        }

        private void AddDelta(OrganNutrientsState delta)
        {
            double agrigatedCconc = (this.Carbon.Total + delta.Carbon.Total) / (this.Wt + delta.Wt);
            Set(OrganNutrientsState.Add(this, delta,agrigatedCconc), agrigatedCconc);
        }

        /// <summary>/// The constructor </summary>
        public CompositeStates() : base() { }

    }
}
