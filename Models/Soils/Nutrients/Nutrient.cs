using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils.NutrientPatching;
using Models.Surface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// The soil nutrient model includes functionality for simulating pools of organic matter and mineral nitrogen.  The processes for each are described below.
    /// </summary>
    /// <structure>
    /// Soil organic matter is modelled as a series of discrete organic matter pools which are described in terms of their masses of carbon and nutrients. These pools are initialised according to approaches specific to each pool.  Organic matter pools may have carbon flows, such as a decomposition process, associated to them.  These carbon flows are also specific to each pool, are independently specified, and are described in each case in the documentation for each organic matter pool below.
    /// 
    /// Mineral nutrient pools (e.g. Nitrate, Ammonium, Urea) are described as solutes within the model.  Each pool captures the mass of the nutrient (e.g. N,P) and they may also contain nutrient flows to describe losses or transformations for that particular compound (e.g. denitrification of nitrate, hydrolysis of urea).
    /// </structure>
    /// <pools>
    /// A nutrient pool class is used to encapsulate the carbon and nitrogen within each soil organic matter pool.  Child functions within these classes provide information for initialisation and flows of C and N to other pools, or losses from the system.
    ///
    /// The soil organic matter pools used within the model are described in the following sections in terms of their initialisation and the carbon flows occurring from them.
    /// </pools>
    /// <solutes>
    /// The soil mineral nutrient pools used within the model are described in the following sections in terms of their initialisation and the flows occurring from them.
    /// </solutes>
    [Serializable]
    [ScopedModel]
    [ValidParent(ParentType = typeof(NutrientPatchManager))]
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("UserInterface.Views.DirectedGraphView")]
    [PresenterName("UserInterface.Presenters.DirectedGraphPresenter")]
    public class Nutrient : Model, INutrient, IVisualiseAsDirectedGraph
    {
        private readonly double CinFOM = 0.4;      // Carbon content of FOM
        private double[] totalOrganicN;
        private double[] fomCNRFactor;
        private double[] cnrf;
        private double[] catm;
        private double[] natm;
        private double[] n2oatm;
        private double[] totalC;
        private double[] denitrifiedN;
        private double[] nitrifiedN;
        private double[] mineralisedN;
        private CompositeNutrientPool organic;
        private NFlow hydrolysis = null;
        private NFlow denitrification = null;
        private NFlow nitrification = null;


        /// <summary>Summary file Link</summary>
        [Link]
        private readonly ISummary summary = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private readonly IPhysical soilPhysical = null;

        /// <summary>The Urea pool.</summary>
        [NonSerialized]
        private IEnumerable<ISolute> solutes = null;

        /// <summary>Child carbon flows.</summary>
        [Link(Type = LinkType.Child)]
        private readonly OrganicPool[] nutrientPools = null;

        /// <summary>Child nutrient flows.</summary>
        [Link(Type = LinkType.Child)]
        private readonly NFlow[] nutrientFlows = null;

        /// <summary>Surface residue decomposition pool.</summary>
        [Link(ByName = true)]
        private readonly OrganicPool surfaceResidue = null;

        /// <summary>Child carbon flows.</summary>
        [NonSerialized]
        private IEnumerable<OrganicFlow> organicFlows = null;

        /// <summary>The inert pool.</summary>
        public IOrganicPool Inert { get; private set; }

        /// <summary>The microbial pool.</summary>
        public IOrganicPool Microbial { get; private set; }

        /// <summary>The humic pool.</summary>
        public IOrganicPool Humic { get; private set; }

        /// <summary>The fresh organic matter cellulose pool.</summary>
        public IOrganicPool FOMCellulose { get; private set; }

        /// <summary>The fresh organic matter carbohydrate pool.</summary>
        public IOrganicPool FOMCarbohydrate { get; private set; }

        /// <summary>The fresh organic matter lignin pool.</summary>
        public IOrganicPool FOMLignin { get; private set; }

        /// <summary>The NO3 pool.</summary>
        public ISolute NO3 { get; private set; }

        /// <summary>The NH4 pool.</summary>
        public ISolute NH4 { get; private set; }

        /// <summary>The Urea pool.</summary>
        public ISolute Urea { get; private set; }

        /// <summary>The fresh organic matter pool.</summary>
        public IOrganicPool FOM { get; private set; }

        /// <summary>Get directed graph from model</summary>
        public DirectedGraph DirectedGraphInfo { get; set; }

        /// <summary>Total C in each soil layer</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> TotalC => totalC;

        /// <summary>Total C lost to the atmosphere</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> Catm => catm;
       
        /// <summary>Total N lost to the atmosphere</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> Natm => natm;

        /// <summary>Total N2O lost to the atmosphere</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> N2Oatm => n2oatm;

        /// <summary>Denitrified Nitrogen (N flow from NO3).</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> DenitrifiedN => denitrifiedN;

        /// <summary>Nitrified Nitrogen (from NH4 to either NO3 or N2O).</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> NitrifiedN => nitrifiedN;
        
        /// <summary>Urea converted to NH4 via hydrolysis.</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> HydrolysedN => hydrolysis.Value;

        /// <summary>Total Net N Mineralisation in each soil layer</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> MineralisedN => mineralisedN;

        /// <summary>Soil organic nitrogen (FOM + Microbial + Humic + Inert)</summary>
        public IOrganicPool Organic => organic;

        /// <summary>Total organic N in each soil layer, organic and mineral (kg/ha).</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> TotalOrganicN => totalOrganicN;

        /// <summary>Carbon to Nitrogen Ratio for Fresh Organic Matter used by low level functions.</summary>
        public IReadOnlyList<double> FOMCNRFactor => fomCNRFactor;

        /// <summary>Carbon to Nitrogen Ratio for Fresh Organic Matter used by low level functions.</summary>
        public IReadOnlyList<double> CNRF => cnrf;

        /// <summary>Total Mineral N in each soil layer</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> MineralN
        {
            get
            {
                int numLayers;
                if (FOMLignin.C == null)
                    numLayers = 0;
                else
                    numLayers = FOMLignin.C.Count;
                double[] values = new double[numLayers];
                double[] nh4 = NH4.kgha;
                double[] no3 = NO3.kgha;
                values = MathUtilities.Add(values, nh4);
                values = MathUtilities.Add(values, no3);
                if (Urea != null)
                {
                    double[] urea = Urea.kgha;
                    values = MathUtilities.Add(values, urea);
                }
                return values;
            }
        }

        /// <summary>Total N in each soil layer, organic, mineral and nitrogen solutes (kg/ha).</summary>
        [Units("kg/ha")]
        public IReadOnlyList<double> TotalN
        {
            get
            {
                double[] totalN = new double[totalOrganicN.Length];

                // I don't like having hard coded solutes here but I can't think of another
                // way to do this easily.
                for (int i = 0; i < totalN.Length; i++)
                    totalN[i] = totalOrganicN[i] + Urea.kgha[i] + NO3.kgha[i] + NH4.kgha[i];

                return totalN;
            }
        }


        /// <summary>Incorporate the given FOM C and N into each layer</summary>
        /// <param name="FOMdata">The in fo mdata.</param>
        public void DoIncorpFOM(FOMLayerType FOMdata)
        {
            bool nSpecified = false;
            for (int layer = 0; layer < FOMdata.Layer.Length; layer++)
            {
                // If the caller specified CNR values then use them to calculate N from Amount.
                if (FOMdata.Layer[layer].CNR > 0.0)
                    FOMdata.Layer[layer].FOM.N = (FOMdata.Layer[layer].FOM.amount * CinFOM) / FOMdata.Layer[layer].CNR;
                // Was any N specified?
                nSpecified |= FOMdata.Layer[layer].FOM.N != 0.0;
            }

            if (nSpecified)
            {

                // Now convert the IncorpFOM.DeltaWt and IncorpFOM.DeltaN arrays to include fraction information and add to pools.
                for (int layer = 0; layer < FOMdata.Layer.Length; layer++)
                {
                    if (layer < FOMCarbohydrate.C.Count)
                    {
                        FOMCarbohydrate.Add(layer, c: FOMdata.Layer[layer].FOM.amount * 0.2 * CinFOM,
                                                   n: FOMdata.Layer[layer].FOM.N * 0.2,
                                                   p:0);
                        FOMCellulose.Add(layer, c: FOMdata.Layer[layer].FOM.amount * 0.7 * CinFOM,
                                                n: FOMdata.Layer[layer].FOM.N * 0.7,
                                                p: 0);
                        FOMLignin.Add(layer, c: FOMdata.Layer[layer].FOM.amount * 0.1 * CinFOM,
                                             n: FOMdata.Layer[layer].FOM.N * 0.1,
                                             p: 0);
                    }
                    else
                        summary.WriteMessage(this, " Number of FOM values given is larger than the number of layers, extra values will be ignored", MessageType.Diagnostic);
                }
            }
        }

        /// <summary>Partition the given FOM C and N into fractions in each layer (FOM pools)</summary>
        /// <param name="FOMPoolData">The in fom pool data.</param>
        public void IncorpFOMPool(FOMPoolType FOMPoolData)
        {
            if (FOMPoolData.Layer.Length > FOMLignin.C.Count)
                throw new Exception("Incorrect number of soil layers of IncorporatedFOM");

            for (int layer = 0; layer < FOMPoolData.Layer.Length; layer++)
            {
                FOMCarbohydrate.Add(layer, c:FOMPoolData.Layer[layer].Pool[0].C,
                                           n:FOMPoolData.Layer[layer].Pool[0].N,
                                           p: 0);

                FOMCellulose.Add(layer, c: FOMPoolData.Layer[layer].Pool[1].C,
                                        n: FOMPoolData.Layer[layer].Pool[1].N,
                                        p: 0);

                FOMLignin.Add(layer, c: FOMPoolData.Layer[layer].Pool[2].C,
                                     n: FOMPoolData.Layer[layer].Pool[2].N,
                                     p: 0);
            }
        }

        /// <summary>Reset all pools, flows and solutes</summary> 
        public void Reset()
        {
            foreach (OrganicPool pool in nutrientPools)
                pool.Initialise(soilPhysical.Thickness.Length);

            foreach (NFlow flow in nutrientFlows)
                flow.Initialise(soilPhysical.Thickness.Length);

            foreach (Solute solute in solutes)
                solute.Reset();
        }

        /// <summary>
        /// Perform initialisation so that instance is valid.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            fomCNRFactor = new double[soilPhysical.Thickness.Length];
            cnrf = new double[soilPhysical.Thickness.Length];
            totalOrganicN = new double[soilPhysical.Thickness.Length];
            catm = new double[soilPhysical.Thickness.Length];
            natm = new double[soilPhysical.Thickness.Length];
            totalC = new double[soilPhysical.Thickness.Length];
            n2oatm = new double[soilPhysical.Thickness.Length];
            denitrifiedN = new double[soilPhysical.Thickness.Length];
            nitrifiedN = new double[soilPhysical.Thickness.Length];
            mineralisedN = new double[soilPhysical.Thickness.Length];

            // Try getting solutes from children first. This happens when using NutrientPatchManager.
            // If not found, use scope to locate solutes.
            solutes = FindAllChildren<ISolute>();
            if (!solutes.Any())
                solutes = FindAllInScope<ISolute>();

            Inert = nutrientPools.First(pool => pool.Name == "Inert");
            Microbial = nutrientPools.First(pool => pool.Name == "Microbial");
            Humic = nutrientPools.First(pool => pool.Name == "Humic");
            FOMCellulose = nutrientPools.First(pool => pool.Name == "FOMCellulose");
            FOMCarbohydrate = nutrientPools.First(pool => pool.Name == "FOMCarbohydrate");
            FOMLignin = nutrientPools.First(pool => pool.Name == "FOMLignin");
            NO3 = solutes.First(solute => solute.Name == "NO3");
            NH4 = solutes.First(solute => solute.Name == "NH4");
            Urea = solutes.FirstOrDefault(solute => solute.Name == "Urea");
            hydrolysis = nutrientFlows.First(flow => flow.Name == "Hydrolysis");
            denitrification = nutrientFlows.First(flow => flow.Name == "Denitrification");
            nitrification = nutrientFlows.First(flow => flow.Name == "Nitrification");
            organicFlows = FindAllDescendants<OrganicFlow>().ToList();

            Reset();
            FOM = new CompositeNutrientPool(new IOrganicPool[] { FOMCarbohydrate, FOMCellulose, FOMLignin });
            organic = new CompositeNutrientPool(nutrientPools);
        }

        /// <summary>
        /// Give all variables an initial value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        { 
            CalculateVariables();
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            for (int layer = 0; layer < fomCNRFactor.Length; layer++)
            {
                fomCNRFactor[layer] = MathUtilities.Divide(FOMCarbohydrate.C[layer] + FOMCellulose.C[layer] + FOMLignin.C[layer],
                                                     FOMCarbohydrate.N[layer] + FOMCellulose.N[layer] + FOMLignin.N[layer] + NH4.kgha[layer] + NO3.kgha[layer], 0.0);

                cnrf[layer] = Math.Exp(-0.693 * (fomCNRFactor[layer] - 25) / 25);
                cnrf[layer] = MathUtilities.Bound(cnrf[layer], 0, 1);
            }

            // Perform all flows.
            foreach (var pool in nutrientPools)
                pool.DoFlow();
            foreach (var flow in nutrientFlows)
                flow.DoFlow();

            CalculateVariables();
        }

        /// <summary>Calculate all variables.</summary>
        private void CalculateVariables()
        {
            Array.Clear(totalOrganicN);
            Array.Clear(catm);
            Array.Clear(totalC);
            Array.Clear(natm);
            Array.Clear(n2oatm);
            Array.Clear(mineralisedN);

            // In some simulations (e.g. when NutrientPatchManager adds instances of Nutrient at 'OnCommencing') surfaceResidue
            // may not have initialised itself yet, hence the if statement below. This is a design fault in APSIM that
            // needs to be fixed at some point.
            if (surfaceResidue.Catm != null)
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                    catm[i] = surfaceResidue.Catm[i];

            foreach (OrganicPool pool in nutrientPools)
            {
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                {
                    totalOrganicN[i] += pool.N[i];
                    catm[i] += pool.Catm[i];
                    totalC[i] += pool.C[i];
                }
            }

            foreach (NFlow flow in nutrientFlows)
            {
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                {
                    natm[i] += flow.Natm[i];
                    n2oatm[i] += flow.N2Oatm[i];
                }
            }

            foreach (OrganicFlow flow in organicFlows)
            {
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                    mineralisedN[i] += flow.MineralisedN[i];
            }

            Array.Clear(denitrifiedN);
            Array.Clear(nitrifiedN);
            for (int i = 0; i < soilPhysical.Thickness.Length; i++)
            {
                denitrifiedN[i] += denitrification.Value[i] + denitrification.Natm[i];
                nitrifiedN[i] += nitrification.Value[i] + nitrification.Natm[i];
            }

            organic.Calculate();
            (FOM as CompositeNutrientPool).Calculate();
        }

    }
}
