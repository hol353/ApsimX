using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Interfaces;
using Models.Soils;
using Models.Soils.Nutrients;

namespace Models.Surface
{
    /// <summary>
    /// The surface organic matter model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class SurfaceOrganicMatter : Model, ISurfaceOrganicMatter, IHaveCanopy, IHasDamageableBiomass
    {
        /// <summary>The water balance model</summary>
        [Link]
        ISoilWater waterBalance = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>Link to the summary component</summary>
        [Link]
        private ISummary summary = null;

        /// <summary>Link to the weather component</summary>
        [Link]
        private IWeather weather = null;

        [Link]
        private INutrient nutrient = null;

        /// <summary>Link to NO3 solute.</summary>
        private ISolute NO3Solute = null;

        /// <summary>Link to NH4 solute.</summary>
        private ISolute NH4Solute = null;

        /// <summary>Gets or sets the residue types.</summary>
        [Link(Type = LinkType.Child)]
        private ResidueTypes ResidueTypes = null;

        /// <summary>Surface residue nutrient pool.</summary>
        [Link(Type = LinkType.Child)]
        private OrganicPool surfaceResidue = null;

        /// <summary>The surf om</summary>
        public List<SurfOrganicMatterType> SurfOM = new List<SurfOrganicMatterType>();

        /// <summary>List of canopies that MicroClimate will use.</summary>
        public List<ICanopy> Canopies { get; set; } = new List<ICanopy>();

        /// <summary>The number surfom</summary>
        private int numSurfom = 0;

        /// <summary>The irrig</summary>
        private double irrig;

        /// <summary>The cumeos</summary>
        private double cumeos;

        /// <summary>The potential decomposition</summary>
        private SurfaceOrganicMatterDecompType potentialDecomposition;

        /// <summary>Has potential decomposition been calculated?</summary>
        private bool calculatedPotentialDecomposition;

        private readonly double depthOfDecomposition = 100;  // 100mm

        /// <summary>The determinant of whether a residue type contributes to the calculation of contact factor (1 or 0)</summary>
        private int[] cf_contrib = new int[0];

        /// <summary>Fraction of Carbon in plant material (0-1)</summary>
        private double[] C_fract = new double[0];

        /// <summary>The carbohydrate fraction in fom C pool (0-1)</summary>
        private double[,] frPoolC = new double[maxFr, 0];

        /// <summary>The carbohydrate fraction in fom N pool (0-1)</summary>
        private double[,] frPoolN = new double[maxFr, 0];

        /// <summary>The carbohydrate fraction in fom P pool (0-1)</summary>
        private double[,] frPoolP = new double[maxFr, 0];

        /// <summary>Ammonium component of residue (ppm)</summary>
        private double[] nh4ppm = new double[0];

        /// <summary>Nitrate component of residue (ppm)</summary>
        private double[] no3ppm = new double[0];

        /// <summary>Phosphate component of residue (ppm)</summary>
        private double[] po4ppm = new double[0];

        /// <summary>Specific area of residue (ha/kg)</summary>
        private double[] specific_area = new double[0];

        /// <summary>critical residue weight below which Thorburn"s cover factor equals one</summary>
        private double criticalResidueWeight = 2000;

        /// <summary>temperature at which decomp reaches optimum (oC)</summary>
        private double optimumDecompTemp = 20;

        /// <summary>cumeos at which decomp rate becomes zero. (mm H2O)</summary>
        private double maxCumulativeEOS = 20;

        /// <summary>Coefficient to determine the magnitude of C:N effects on decomposition of residue</summary>
        private double cnRatioDecompCoeff = 0.277;

        /// <summary>C:N above which decomposition rate of residue declines</summary>
        private double cnRatioDecompThreshold = 25;

        /// <summary>total amount of "leaching" rain to remove all soluble N from surfom</summary>
        private double totalLeachRain = 25;

        /// <summary>threshold rainfall amount for leaching to occur</summary>
        private double minRainToLeach = 10;

        /// <summary>critical minimum org C below which potential decomposition rate is 100% (to avoid numerical imprecision)</summary>
        private double criticalMinimumOrganicC = 0.004;

        /// <summary>Default C:P ratio</summary>
        private double DefaultCPRatio = 0;

        /// <summary>extinction coefficient for standing residues</summary>
        private double standingExtinctCoeff = 0.5;

        //private double lyingExtinctionCoeff = 1.0;

        /// <summary>fraction of incoming faeces to add</summary>
        private double fractionFaecesAdded = 1.0;

        /// <summary>This event is invoked when residues are incorperated</summary>
        public event EventHandler<TilledType> Tilled;

        /// <summary>
        /// Number of pools into which carbon is grouped.
        /// Currently there are three, indexed as follows:
        /// 0 = carbohydrate
        /// 1 = cellulose
        /// 2 = lignin.
        /// </summary>
        public static int maxFr = 3;

        /// <summary>Gets or sets the name of initial residue pool</summary>
        [Summary]
        [Description("Name of initial residue pool")]
        [Units("")]
        public string InitialResidueName { get; set; }

        /// <summary>Gets or sets the type of initial residue pool</summary>
        [Summary]
        [Description("Type of initial residue pool")]
        [Display(Type = DisplayType.ResidueName)]
        [Units("")]
        public string InitialResidueType { get; set; }

        /// <summary>Gets or sets the mass of initial residue pool</summary>
        [Summary]
        [Description("Mass of initial surface residue (kg/ha)")]
        [Units("kg/ha")]
        public double InitialResidueMass { get; set; }

        /// <summary>Gets or sets the standing fraction of initial residue pool</summary>
        [Summary]
        [Description("Standing fraction (0-1)")]
        [Units("0-1")]
        public double InitialStandingFraction { get; set; }

        /// <summary>Gets or sets the Carbon:Phosphorus ratio.</summary>
        [Summary]
        [Description("Carbon:Phosphorus ratio")]
        [Units("g/g")]
        public double InitialCPR { get; set; }

        /// <summary>Gets or sets the initial Carbon:Nitrogen ratio.</summary>
        [Summary]
        [Description("Carbon:Nitrogen ratio (g/g)")]
        [Units("g/g")]
        public double InitialCNR { get; set; }

        /// <summary>Total mass of all surface organic materials</summary>
        [Units("kg/ha")]
        public double Wt { get { return SumSurfOMStandingLying(SurfOM, x => x.amount); } }

        /// <summary>Total mass of all standing surface organic materials</summary>
        [Units("kg/ha")]
        public double StandingWt => SurfOM.Sum(om => om.Standing.Sum(organ => organ.amount));

        /// <summary>Total mass of all lying surface organic materials</summary>
        [Units("kg/ha")]
        public double LyingWt => SurfOM.Sum(om => om.Lying.Sum(organ => organ.amount));


        /// <summary>Total carbon of all surface organic carbon</summary>
        [Summary]
        [Description("Carbon content")]
        [Units("kg/ha")]
        public double C { get { return SumSurfOMStandingLying(SurfOM, x => x.C); } }

        /// <summary>Total mass of all surface organic nitrogen</summary>
        [Summary]
        [Description("Nitrogen content")]
        [Units("kg/ha")]
        public double N { get { return SumSurfOMStandingLying(SurfOM, x => x.N); } }

        /// <summary>Total mass of all surface organic phosphor</summary>
        [Summary]
        [Description("Phosphorus content")]
        [Units("kg/ha")]
        public double P { get { return SumSurfOMStandingLying(SurfOM, x => x.P); } }

        /// <summary>Total mass of nitrate</summary>
        [Units("kg/ha")]
        public double NO3 { get { return SumSurfOM(SurfOM, x => x.no3); } }

        /// <summary>Total mass of ammonium</summary>
        [Units("kg/ha")]
        public double NH4 { get { return SumSurfOM(SurfOM, x => x.nh4); } }

        /// <summary>Total mass of labile phosphorus</summary>
        [Units("kg/ha")]
        public double LabileP { get { return SumSurfOM(SurfOM, x => x.po4); } }

        /// <summary>Fraction of ground covered by all surface OMs</summary>
        [Description("Fraction of ground covered by all surface OMs")]
        [Units("m^2/m^2")]
        public double Cover { get { return CoverTotal(); } }

        /// <summary>Temperature factor for decomposition</summary>
        [Units("0-1")]
        public double tf { get { return TemperatureFactor(); } }

        /// <summary>Contact factor for decomposition</summary>
        [Units("0-1")]
        public double cf { get { return ContactFactor(); } }

        /// <summary>Gets the wf.</summary>
        [Units("0-1")]
        public double wf { get { return MoistureFactor(); } }

        /// <summary>Amount of carbon moved from surface residues to soil (kg/ha).</summary>
        [Units("kg/ha")]
        public double IncorporatedC { get; private set; }

        /// <summary>Amount of nitrogen moved from surface residues to soil (kg/ha).</summary>
        [Units("kg/ha")]
        public double IncorporatedN { get; private set; }

        /// <summary>Amount of phosphorus incorporated (kg/ha).</summary>
        [Units("kg/ha")]
        public double IncorporatedP { get; private set; }

        /// <summary>
        /// Fraction of incoming faeces to add.
        /// </summary>
        [Bounds(Lower = 0.0, Upper = 0.0)]
        [Units("0-1")]
        public double FractionFaecesAdded
        {
            get
            {
                return fractionFaecesAdded;
            }
            set
            {
                fractionFaecesAdded = value;
            }
        }

        /// <summary>A list of material (biomass) that can be damaged.</summary>
        public IEnumerable<DamageableBiomass> Material
        {
            get
            {
                // Return an empty live material. Stock won't find the dead material unless there
                // is matching live material.
                yield return new DamageableBiomass("SurfaceOrganicMatter.Residue", new Biomass(), isLive: true);
                yield return new DamageableBiomass("SurfaceOrganicMatter.Residue", new Biomass()
                {
                    StructuralWt = (SurfOM.Sum(som => som.Standing.Sum(om => om.amount)) +
                                    SurfOM.Sum(som => som.Lying.Sum(om => om.amount))) / 10,  // kg/ha to g/m2
                    StructuralN = SurfOM.Sum(som => som.Standing.Sum(om => om.N)) +
                                    SurfOM.Sum(som => som.Lying.Sum(om => om.N)) / 10,   // kg/ha to g/m2
                    MetabolicWt = 0.0,
                    MetabolicN = 0.0,
                    StorageWt = 0.0,
                    StorageN = 0.0,
                }, isLive: false);
            }
        }

        /// <summary>The amount of material incorporated into the soil.</summary>
        public FOMPoolType Incorporated { get; private set; }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return true; } }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove = 0, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0)
        {
            double amountRemoved = 0;
            for (int i = 0; i < SurfOM.Count; i++)
            {
                double totalMassRemoved = 0;
                foreach (var standing in SurfOM[i].Standing)
                {
                    if (standing.amount > 0)
                    {
                        double cFraction = standing.C / standing.amount;
                        double nFraction = standing.N / standing.amount;
                        double pFraction = standing.P / standing.amount;

                        double amountToRemove = standing.amount * deadToRemove;
                        standing.amount -= amountToRemove;
                        totalMassRemoved += amountToRemove;

                        standing.C = cFraction * standing.amount;
                        standing.N = nFraction * standing.amount;
                        standing.P = pFraction * standing.amount;

                        amountRemoved += amountToRemove;
                    }
                }
                foreach (var lying in SurfOM[i].Lying)
                {
                    if (lying.amount > 0)
                    {
                        double cFraction = lying.C / lying.amount;
                        double nFraction = lying.N / lying.amount;
                        double pFraction = lying.P / lying.amount;

                        double amountToRemove = lying.amount * deadToRemove;
                        lying.amount -= amountToRemove;
                        totalMassRemoved += amountToRemove;
                        lying.C = cFraction * lying.amount;
                        lying.N = nFraction * lying.amount;
                        lying.P = pFraction * lying.amount;

                        amountRemoved += amountToRemove;
                    }
                }

                SurfOM[i].no3 -= MathUtilities.Divide(no3ppm[i], 1000000.0, 0.0) * totalMassRemoved;
                SurfOM[i].nh4 -= MathUtilities.Divide(nh4ppm[i], 1000000.0, 0.0) * totalMassRemoved;
                SurfOM[i].po4 -= MathUtilities.Divide(po4ppm[i], 1000000.0, 0.0) * totalMassRemoved;
            }
            return amountRemoved;
        }

        /// <summary>Called when [reset].</summary>
        public void Reset()
        {
            SurfOM = new List<SurfOrganicMatterType>();
            frPoolC = new double[maxFr, 0];
            frPoolN = new double[maxFr, 0];
            frPoolP = new double[maxFr, 0];
            irrig = 0;
            cumeos = 0;
            ReadParam();
        }

        /// <summary>Return a list of known residue types names</summary>
        public List<string> ResidueTypeNames()
        {
            if (ResidueTypes == null && Children.Count > 0)
            {
                // This happens when user clicks on SurfaceOrganicMatter in tree.
                // Links haven't been resolved yet
                ResidueTypes = FindChild<ResidueTypes>();
            }
            return ResidueTypes.Names;
        }

        /// <summary>Incorporates the specified fraction.</summary>
        /// <param name="fraction">The fraction.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="doOutput">Echo the incoporation to the Summary file? Default is true.</param>
        public void Incorporate(double fraction, double depth, bool doOutput = true)
        {
            Incorp(fraction, depth);

            if (Tilled != null)
            {
                TilledType tilled = new TilledType();
                tilled.Fraction = fraction;
                tilled.Depth = depth;
                Tilled.Invoke(this, tilled);
            }

            if (doOutput)
                summary.WriteMessage(this, string.Format(@"Residue removed " + Environment.NewLine +
                                                      "Fraction Incorporated = {0:0.0##}" + Environment.NewLine +
                                                      "Incorporated Depth    = {1:0.0##}",
                                                      fraction, depth), MessageType.Diagnostic);
        }

        /// <summary>Return the potential residue decomposition for today.</summary>
        private void PotentialDecomposition()
        {
            // This method can be called multiple times when nutrient patching is running. Each
            // patch will call this method. Because of the cumeos accumulation below we only
            // want to do this once per timestep, hence the flag on the line below.
            if (!calculatedPotentialDecomposition)
            {
                calculatedPotentialDecomposition = true;
                double precip = weather.Rain + irrig;
                if (precip > 4.0)
                    cumeos = waterBalance.Eos - precip;
                else
                    cumeos = this.cumeos + waterBalance.Eos - precip;
                cumeos = Math.Max(cumeos, 0.0);

                if (precip >= minRainToLeach)
                    Leach(precip);

                irrig = 0.0; // reset irrigation log now that we have used that information;

                potentialDecomposition = GetPotentialDecomposition();
            }

            surfaceResidue.Clear();

            double totalLayerFraction = surfaceResidue.LayerFraction.Sum();
            double totalC = potentialDecomposition.Pool.Select(p => p.FOM.C).Sum();
            double totalN = potentialDecomposition.Pool.Select(p => p.FOM.N).Sum();

            for (int i = 0; i < surfaceResidue.C.Count; i++)
            {
                double amountCToSoil = totalC * (surfaceResidue.LayerFraction[i] / totalLayerFraction);
                double amountNToSoil = totalN * (surfaceResidue.LayerFraction[i] / totalLayerFraction);
                surfaceResidue.Add(i, c: amountCToSoil,
                                      n: amountNToSoil,
                                      p: 0);
                IncorporatedC += amountCToSoil;
                IncorporatedN += amountNToSoil;
            }
            surfaceResidue.DoFlow();
        }

        /// <summary>
        /// Adds excreta in response to an AddFaeces event
        /// This is a still the minimalist version, providing
        /// an alternative to using add_surfaceom directly
        /// </summary>
        /// <param name="data">structure holding description of the added faeces</param>
        public void AddFaeces(AddFaecesType data)
        {
            string Manure = "manure";
            Add((double)(data.OMWeight * fractionFaecesAdded),
                         (double)(data.OMN * fractionFaecesAdded),
                         (double)(data.OMP * fractionFaecesAdded),
                         Manure, "", 0,
                         (double)(data.NO3N * fractionFaecesAdded),
                         (double)(data.NH4N * fractionFaecesAdded));
        }

        /// <summary>
        /// Adds excreta to the SOM
        /// This version is callable from an operation
        /// </summary>
        /// <param name="organicC">Organic C</param>
        /// <param name="organicN">Organic N</param>
        /// <param name="no3N">NO3 N</param>
        /// <param name="nh4N">NH4 N</param>
        /// <param name="p">P</param>
        public void AddFaeces(double organicC, double organicN, double no3N, double nh4N, double p)
        {
            this.FractionFaecesAdded = 1.0;
            AddFaecesType data = new AddFaecesType();
            data.OMWeight = organicC / 0.4;     // because the OM is 40% C
            data.OMN = organicN;
            data.NH4N = nh4N;
            data.NO3N = no3N;
            data.OMP = p;
            this.AddFaeces(data);
        }

        /// <summary>
        /// "cover1" and "cover2" are numbers between 0 and 1 which
        /// indicate what fraction of sunlight is intercepted by the
        /// foliage of plants.  This function returns a number between
        /// 0 and 1 indicating the fraction of sunlight intercepted
        /// when "cover1" is combined with "cover2", i.e. both sets of
        /// plants are present.
        /// </summary>
        /// <param name="cover1">The cover1.</param>
        /// <param name="cover2">The cover2.</param>
        private double AddCover(double cover1, double cover2)
        {
            double bare = (1.0 - cover1) * (1.0 - cover2);
            return 1.0 - bare;
        }

        /// <summary>Get the weight of the given SOM pool</summary>
        /// <param name="pool">Name of the pool to get the weight from.</param>
        /// <returns>The weight of the given pool</returns>
        public double GetWeightFromPool(string pool)
        {
            var SomType = SurfOM.Find(x => x.name.Equals(pool, StringComparison.CurrentCultureIgnoreCase));
            if (SomType == null)
                return 0;
            return SumOMFractionType(SomType.Standing, y => y.amount) +
                SumOMFractionType(SomType.Lying, y => y.amount);
        }

        /// <summary>Sums the surf om standing lying.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        private double SumSurfOMStandingLying(List<SurfOrganicMatterType> var, Func<OMFractionType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(x => SumSurfOMStandingLying(x, func));
        }

        /// <summary>Sums the surf om standing lying.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        private double SumSurfOMStandingLying(SurfOrganicMatterType var, Func<OMFractionType, double> func)
        {
            return var.Lying.Sum<OMFractionType>(func) + var.Standing.Sum<OMFractionType>(func);
        }

        /// <summary>Sums the surf om.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        private double SumSurfOM(List<SurfOrganicMatterType> var, Func<SurfOrganicMatterType, double> func)
        {
            return var.Sum<SurfOrganicMatterType>(func);
        }

        /// <summary>Sums the type of the om fraction.</summary>
        /// <param name="var">The variable.</param>
        /// <param name="func">The function.</param>
        /// <returns></returns>
        private double SumOMFractionType(OMFractionType[] var, Func<OMFractionType, double> func)
        {
            return var.Sum<OMFractionType>(func);
        }

        /// <summary>Called when a simulation commences.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            NO3Solute = this.FindInScope("NO3") as ISolute;
            NH4Solute = this.FindInScope("NH4") as ISolute;
            Reset();
            surfaceResidue.Initialise(soilPhysical.Thickness.Length);
            double[] layerFractions = new double[soilPhysical.Thickness.Length];
            for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                layerFractions[i] = SoilUtilities.ProportionThroughLayer(soilPhysical.Thickness, i, depthOfDecomposition);
            surfaceResidue.SetLayerFraction(layerFractions);
        }

        /// <summary>Called at start of each day.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            calculatedPotentialDecomposition = false;
            Incorporated = null;
            IncorporatedC = 0;
            IncorporatedN = 0;
            IncorporatedP = 0;
        }

        /// <summary>Get irrigation information from an Irrigated event.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, IrrigationApplicationType data)
        {
            irrig += data.Amount;
        }

        /// <summary>Called when a plant drops biomass to the soil surface</summary>
        /// <param name="BiomassRemoved">The biomass removed.</param>
        [EventSubscribe("BiomassRemoved")]
        public void OnBiomassRemoved(BiomassRemovedType BiomassRemoved)
        {
            double surfomAdded = 0;   // amount of residue added (kg/ha)
            double surfomNAdded = 0;  // amount of residue N added (kg/ha)
            double surfomPAdded = 0;  // amount of residue N added (kg/ha)

            if (BiomassRemoved.fraction_to_residue.Sum() != 0)
            {
                // Find the amount of surfom to be added today;
                for (int i = 0; i < BiomassRemoved.fraction_to_residue.Length; i++)
                    surfomAdded += BiomassRemoved.dlt_crop_dm[i] * BiomassRemoved.fraction_to_residue[i];

                if (surfomAdded > 0.0)
                {
                    // Find the amount of N & added in surfom today;
                    for (int i = 0; i < BiomassRemoved.dlt_dm_p.Length; i++)
                    {
                        surfomPAdded += BiomassRemoved.dlt_dm_p[i] * BiomassRemoved.fraction_to_residue[i];
                        surfomNAdded += BiomassRemoved.dlt_dm_n[i] * BiomassRemoved.fraction_to_residue[i];
                    }

                    Add(surfomAdded, surfomNAdded, surfomPAdded, BiomassRemoved.crop_type, "");
                }
            }
        }

        /// <summary>Do the daily residue decomposition for today.</summary>
        /// <param name="sender">The event sender</param>
        /// <param name="args">The event data</param>
        [EventSubscribe("DoSurfaceOrganicMatterPotentialDecomposition")]
        private void DoSurfaceOrganicMatterPotentialDecomposition(object sender, EventArgs args)
        {
            PotentialDecomposition();
        }

        /// <summary>Do the daily residue decomposition for today.</summary>
        /// <param name="sender">The event sender</param>
        /// <param name="args">The event data</param>
        [EventSubscribe("DoSurfaceOrganicMatterDecomposition")]
        private void OnDoSurfaceOrganicMatterDecomposition(object sender, EventArgs args)
        {
            double InitialResidueC = 0;  // Potential residue decomposition provided by surface organic matter model
            double FinalResidueC = 0;    // How much is left after decomposition
            double FractionDecomposed;

            for (int i = 0; i < potentialDecomposition.Pool.Length; i++)
                InitialResidueC += potentialDecomposition.Pool[i].FOM.C;

            for (int i = 0; i < surfaceResidue.C.Count; i++)
                FinalResidueC += surfaceResidue.C[i];

            FractionDecomposed = 1.0 - MathUtilities.Divide(FinalResidueC, InitialResidueC, 0);

            DecomposeSurfom(potentialDecomposition, FractionDecomposed);

            Canopies = new List<ICanopy>();
            foreach (SurfOrganicMatterType pool in SurfOM)
            {
                if (pool.CanopyLying.CoverTotal > 0)
                    Canopies.Add(pool.CanopyLying);
                if (pool.CanopyStanding.CoverTotal > 0)
                    Canopies.Add(pool.CanopyStanding);
            }
        }

        /// <summary>
        /// Read in all parameters from parameter file
        /// </summary>
        private void ReadParam()
        {
            double totC = 0;  // total C in residue;
            double totN = 0;  // total N in residue;
            double totP = 0;  // total P in residue;

            if (InitialResidueMass >= 0.0)
            {
                // Normally the residue shouldn't already exist, and we
                // will need to add it, and normally this should result in SOMNo == i
                // but it's safest not to assume this
                int SOMNo = GetResidueNumber(InitialResidueName);
                if (SOMNo < 0)
                {
                    SOMNo = AddNewSurfOM(InitialResidueName, InitialResidueType);
                }

                // convert the ppm figures into kg/ha;
                SurfOM[SOMNo].no3 += MathUtilities.Divide(no3ppm[SOMNo], 1000000.0, 0.0) * InitialResidueMass;
                SurfOM[SOMNo].nh4 += MathUtilities.Divide(nh4ppm[SOMNo], 1000000.0, 0.0) * InitialResidueMass;
                SurfOM[SOMNo].po4 += MathUtilities.Divide(po4ppm[SOMNo], 1000000.0, 0.0) * InitialResidueMass;

                totC = InitialResidueMass * C_fract[SOMNo];
                totN = MathUtilities.Divide(totC, InitialCNR, 0.0);

                // If a C:P ratio is not provided, use the default
                double cpr = double.IsNaN(InitialCPR) ? DefaultCPRatio : InitialCPR;
                totP = MathUtilities.Divide(totC, cpr, 0.0);

                double standFract = double.IsNaN(InitialStandingFraction) ? DefaultCPRatio : InitialStandingFraction;

                for (int j = 0; j < maxFr; j++)
                {
                    SurfOM[SOMNo].Standing[j].amount += InitialResidueMass * frPoolC[j, SOMNo] * standFract;
                    SurfOM[SOMNo].Standing[j].C += totC * frPoolC[j, SOMNo] * standFract;
                    SurfOM[SOMNo].Standing[j].N += totN * frPoolN[j, SOMNo] * standFract;
                    SurfOM[SOMNo].Standing[j].P += totP * frPoolP[j, SOMNo] * standFract;

                    SurfOM[SOMNo].Lying[j].amount += InitialResidueMass * frPoolC[j, SOMNo] * (1.0 - standFract);
                    SurfOM[SOMNo].Lying[j].C += totC * frPoolC[j, SOMNo] * (1.0 - standFract);
                    SurfOM[SOMNo].Lying[j].N += totN * frPoolN[j, SOMNo] * (1.0 - standFract);
                    SurfOM[SOMNo].Lying[j].P += totP * frPoolP[j, SOMNo] * (1.0 - standFract);
                }
            }
        }

        /// <summary>Get the solutes number</summary>
        /// <param name="surfomname">The surfomname.</param>
        private int GetResidueNumber(string surfomname)
        {
            if (SurfOM == null)
                return -1;

            for (int i = 0; i < SurfOM.Count; i++)
                if (SurfOM[i].name.Equals(surfomname, StringComparison.CurrentCultureIgnoreCase))
                    return i;

            return -1;
        }

        /// <summary>
        /// Performs manure decomposition taking into account environmental;
        /// and manure factors (independant to soil N but N balance can modify;
        /// actual decomposition rates if desired by N model - this is possible;
        /// because pools are not updated until end of time step - see post routine)
        /// </summary>
        /// <param name="c_pot_decomp">The c_pot_decomp.</param>
        /// <param name="n_pot_decomp">The n_pot_decomp.</param>
        /// <param name="p_pot_decomp">The p_pot_decomp.</param>
        private void PotDecomp(out double[] c_pot_decomp, out double[] n_pot_decomp, out double[] p_pot_decomp)
        {
            // (these pools are not updated until end of time step - see post routine)
            c_pot_decomp = new double[numSurfom];
            n_pot_decomp = new double[numSurfom];
            p_pot_decomp = new double[numSurfom];

            double
                mf = MoistureFactor(),     // moisture factor for decomp (0-1)
                tf = TemperatureFactor(),  // temperature factor for decomp (0-1)
                cf = ContactFactor();      // manure/soil contact factor for decomp (0-1)

            for (int residue = 0; residue < numSurfom; residue++)
            {
                double cnrf = CNRatioFactor(residue);    // C:N factor for decomp (0-1) for surfom under consideration

                double
                    Fdecomp = -1,       // decomposition fraction for the given surfom
                    sumC = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C);

                if (sumC < criticalMinimumOrganicC)
                {
                    // Residue wt is sufficiently low to suggest decomposing all;
                    // material to avoid low numbers which can cause problems;
                    // with numerical precision;
                    Fdecomp = 1;
                }
                else
                {
                    // Calculate today"s decomposition  as a fraction of potential rate;
                    Fdecomp = SurfOM[residue].PotDecompRate * mf * tf * cnrf * cf;
                }

                // Now calculate pool decompositions for this residue;
                c_pot_decomp[residue] = Fdecomp * sumC;
                n_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N);
                p_pot_decomp[residue] = Fdecomp * SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P);
            }
        }

        /// <summary>
        /// Calculate temperature factor for manure decomposition (0-1).
        /// <para>
        /// Notes;
        /// The temperature factor is a simple function of the square of daily
        /// average temperature.  The user only needs to give an optimum temperature
        /// and the code will back calculate the necessary coefficient at compile time.
        /// </para>
        /// </summary>
        /// <returns>temperature factor</returns>
        private double TemperatureFactor()
        {
            double
                ave_temp = MathUtilities.Divide((weather.MaxT + weather.MinT), 2.0, 0.0);  // today"s average air temp (oC)

            if (ave_temp > 0.0)
                return MathUtilities.Bound(
                    (double)Math.Pow(MathUtilities.Divide(ave_temp, optimumDecompTemp, 0.0), 2.0),
                    0.0,
                    1.0);
            else
                return 0;
        }

        /// <summary>Calculate manure/soil contact factor for manure decomposition (0-1).</summary>
        private double ContactFactor()
        {
            double effSurfomMass = 0;  // Total residue wt across all instances;

            // Sum the effective mass of surface residues considering lying fraction only.
            // The "effective" weight takes into account the haystack effect and is governed by the;
            // cf_contrib factor (ini file).  ie some residue types do not contribute to the haystack effect.

            for (int residue = 0; residue < numSurfom; residue++)
                effSurfomMass += SurfOM[residue].Lying.Sum<OMFractionType>(x => x.amount) * cf_contrib[residue];

            if (effSurfomMass <= criticalResidueWeight)
                return 1.0;
            else
                return MathUtilities.Bound(MathUtilities.Divide(criticalResidueWeight, effSurfomMass, 0.0), 0, 1);
        }

        /// <summary>Calculate C:N factor for decomposition (0-1).</summary>
        /// <param name="residue">residue number</param>
        /// <returns>C:N factor for decomposition(0-1)</returns>
        private double CNRatioFactor(int residue)
        {
            if (residue < 0)
                return 1;
            else
            {
                // Note: C:N ratio factor only based on lying fraction
                double
                    total_C = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C),        // organic C component of this residue (kg/ha)
                    total_N = SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N),        // organic N component of this residue (kg/ha)
                    total_mineral_n = SurfOM[residue].no3 + SurfOM[residue].nh4,          // mineral N component of this surfom (no3 + nh4)(kg/ha)
                    cnr = MathUtilities.Divide(total_C, (total_N + total_mineral_n), 0.0); // C:N for this residue  (unitless)

                // As C:N increases above optcn cnrf decreases exponentially toward zero;
                // As C:N decreases below optcn cnrf is constrained to one;

                if (cnRatioDecompThreshold == 0)
                    return 1;
                else
                    return MathUtilities.Bound(
                        (double)Math.Exp(-cnRatioDecompCoeff * ((cnr - cnRatioDecompThreshold) / cnRatioDecompThreshold)),
                        0.0,
                        1.0);
            }
        }

        /// <summary>Calculate moisture factor for manure decomposition (0-1).</summary>
        /// <returns></returns>
        private double MoistureFactor()
        {
            return MathUtilities.Bound(1.0 - MathUtilities.Divide(cumeos, maxCumulativeEOS, 0.0), 0.0, 1.0);
        }

        /// <summary>Calculate total cover</summary>
        /// <returns></returns>
        private double CoverTotal()
        {
            double combinedCover = 0;  // effective combined cover(0-1)

            for (int i = 0; i < numSurfom; i++)
                combinedCover = AddCover(combinedCover, CoverOfSOM(i));

            return combinedCover;
        }

        /// <summary>
        /// Remove mineral N and P from surfom with leaching rainfall and;
        /// pass to Soil N and Soil P modules.
        /// </summary>
        /// <param name="leachRain">The leach rain.</param>
        private void Leach(double leachRain)
        {
            double nh4Incorp;
            double no3Incorp;
            double po4Incorp;

            // Apply leaching fraction to all mineral pools and put all mineral NO3,NH4 and PO4 into top layer;
            double leaching_fr = MathUtilities.Bound(MathUtilities.Divide(leachRain, totalLeachRain, 0.0), 0.0, 1.0);
            no3Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.no3) * leaching_fr;
            nh4Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.nh4) * leaching_fr;
            po4Incorp = SurfOM.Sum<SurfOrganicMatterType>(x => x.po4) * leaching_fr;


            // If neccessary, Send the mineral N & P leached to the Soil N&P modules;
            if (no3Incorp > 0.0 || nh4Incorp > 0.0 || po4Incorp > 0.0)
            {
                var delta = new double[soilPhysical.Thickness.Length];
                delta[0] = no3Incorp;
                NO3Solute.AddKgHaDelta(SoluteSetterType.Soil, delta);

                delta[0] = nh4Incorp;
                NH4Solute.AddKgHaDelta(SoluteSetterType.Soil, delta);
            }

            for (int i = 0; i < numSurfom; i++)
            {
                SurfOM[i].no3 = SurfOM[i].no3 * (1.0 - leaching_fr);
                SurfOM[i].nh4 = SurfOM[i].nh4 * (1.0 - leaching_fr);
                SurfOM[i].po4 = SurfOM[i].po4 * (1.0 - leaching_fr);
            }
        }

        /// <summary>Notify other modules of the potential to decompose.</summary>
        /// <returns></returns>
        private SurfaceOrganicMatterDecompType GetPotentialDecomposition()
        {
            SurfaceOrganicMatterDecompType SOMDecomp = new SurfaceOrganicMatterDecompType()
            {
                Pool = new SurfaceOrganicMatterDecompPoolType[numSurfom]
            };

            if (numSurfom <= 0)
                return SOMDecomp;

            double[] c_pot_decomp, n_pot_decomp, p_pot_decomp;
            PotDecomp(out c_pot_decomp, out n_pot_decomp, out p_pot_decomp);

            for (int residue = 0; residue < numSurfom; residue++)
                SOMDecomp.Pool[residue] = new SurfaceOrganicMatterDecompPoolType()
                {
                    Name = SurfOM[residue].name,
                    OrganicMatterType = SurfOM[residue].OrganicMatterType,

                    FOM = new FOMType()
                    {
                        amount = MathUtilities.Divide(c_pot_decomp[residue], C_fract[residue], 0.0),
                        C = c_pot_decomp[residue],
                        N = n_pot_decomp[residue],
                        P = p_pot_decomp[residue],
                        AshAlk = 0.0
                    }
                };
            return SOMDecomp;
        }

        /// <summary>Decomposes the surfom.</summary>
        /// <param name="potSOMDecomp">The potential som decomp.</param>
        /// <param name="fraction">Fraction of potential to decompose.</param>
        private void DecomposeSurfom(SurfaceOrganicMatterDecompType potSOMDecomp, double fraction)
        {
            int numSurfom = potSOMDecomp.Pool.Length;          // local surfom counter from received event;
            int residue_no;                                 // Index into the global array;
            double[] cPotDecomp = new double[numSurfom];  // pot amount of C to decompose (kg/ha)
            double[] nPotDecomp = new double[numSurfom];  // pot amount of N to decompose (kg/ha)
            double[] pTotDecomp = new double[numSurfom];  // pot amount of P to decompose (kg/ha)
            double totCDecomp;    // total amount of c to decompose;
            double totNDecomp;    // total amount of c to decompose;
            double totPDecomp;    // total amount of c to decompose;

            double SOMcnr = 0;
            double SOMc = 0;
            double SOMn = 0;

            // calculate potential decompostion of C, N, and P;
            PotDecomp(out cPotDecomp, out nPotDecomp, out pTotDecomp);

            for (int counter = 0; counter < numSurfom; counter++)
            {
                totCDecomp = potSOMDecomp.Pool[counter].FOM.C * fraction;
                totNDecomp = potSOMDecomp.Pool[counter].FOM.N * fraction;

                residue_no = GetResidueNumber(potSOMDecomp.Pool[counter].Name);

                if (MathUtilities.IsLessThan(totNDecomp, 0.0) || MathUtilities.IsGreaterThan(totNDecomp, nPotDecomp[residue_no]))
                    summary.WriteMessage(this, string.Format(@"'Total n decomposition' out of bounds! {0} < {1} < {2}",
                                                             0.0, totNDecomp, nPotDecomp[residue_no]), MessageType.Warning);

                SOMc = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.C) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.C);
                SOMn = SurfOM[residue_no].Standing.Sum<OMFractionType>(x => x.N) + SurfOM[residue_no].Lying.Sum<OMFractionType>(x => x.N);

                SOMcnr = MathUtilities.Divide(SOMc, SOMn, 0.0);

                const double acceptableErr = 1e-4;

                if (MathUtilities.FloatsAreEqual(totCDecomp, 0.0) && MathUtilities.FloatsAreEqual(totNDecomp, 0.0))
                {
                    // all OK - nothing happening;
                }
                else if (totCDecomp > cPotDecomp[residue_no] + acceptableErr)
                    throw new ApsimXException(this, "SurfaceOM - C decomposition exceeds potential rate");
                else if (totNDecomp > nPotDecomp[residue_no] + acceptableErr)
                    throw new ApsimXException(this, "SurfaceOM - N decomposition exceeds potential rate");

                totPDecomp = totCDecomp * MathUtilities.Divide(pTotDecomp[residue_no], cPotDecomp[residue_no], 0.0);
                Decomp(totCDecomp, totNDecomp, totPDecomp, residue_no);
            }
        }

        /// <summary>Performs updating of pools due to surfom decomposition</summary>
        /// <param name="C_decomp">C to be decomposed</param>
        /// <param name="N_decomp">N to be decomposed</param>
        /// <param name="P_decomp">P to be decomposed</param>
        /// <param name="residue">residue number being dealt with</param>
        private void Decomp(double C_decomp, double N_decomp, double P_decomp, int residue)
        {

            double Fdecomp;  // decomposing fraction;

            // do C
            Fdecomp = MathUtilities.Bound(MathUtilities.Divide(C_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.C), 0.0), 0.0, 1.0);
            for (int i = 0; i < maxFr; i++)
            {
                SurfOM[residue].Lying[i].C = SurfOM[residue].Lying[i].C * (1.0 - Fdecomp);
                SurfOM[residue].Lying[i].amount = SurfOM[residue].Lying[i].amount * (1.0 - Fdecomp);
            }

            // do N
            Fdecomp = MathUtilities.Divide(N_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.N), 0.0);
            for (int i = 0; i < maxFr; i++)
                SurfOM[residue].Lying[i].N = SurfOM[residue].Lying[i].N * (1.0 - Fdecomp);

            // do P
            Fdecomp = MathUtilities.Divide(P_decomp, SurfOM[residue].Lying.Sum<OMFractionType>(x => x.P), 0.0);
            for (int i = 0; i < maxFr; i++)
                SurfOM[residue].Lying[i].P = SurfOM[residue].Lying[i].P * (1.0 - Fdecomp);
        }

        /// <summary>
        /// Calculate surfom incorporation as a result of tillage and update;
        /// residue and N pools.
        /// </summary>
        /// <param name="fIncorp">The f incorp.</param>
        /// <param name="tillageDepth">The tillage depth.</param>
        private void Incorp(double fIncorp, double tillageDepth)
        {
            int deepestLayer;
            int nLayers = soilPhysical.Thickness.Length;
            double F_incorp_layer = 0;
            double[] residueIncorpFraction = new double[nLayers];
            double layerIncorpDepth;
            double[,] CPool = new double[maxFr, nLayers];  // total C in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] NPool = new double[maxFr, nLayers];  // total N in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] PPool = new double[maxFr, nLayers];  // total P in each Om fraction and layer (from all surfOM"s) incorporated;
            double[,] AshAlkPool = new double[maxFr, nLayers]; // total AshAlk in each Om fraction and layer (from all surfOM"s) incorporated;
            double[] no3 = new double[nLayers]; // total no3 to go into each soil layer (from all surfOM"s)
            double[] nh4 = new double[nLayers]; // total nh4 to go into each soil layer (from all surfOM"s)
            double[] po4 = new double[nLayers]; // total po4 to go into each soil layer (from all surfOM"s)
            Incorporated = new FOMPoolType();

            fIncorp = MathUtilities.Bound(fIncorp, 0.0, 1.0);

            deepestLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, tillageDepth);

            double cumDepth = 0.0;

            for (int layer = 0; layer <= deepestLayer; layer++)
            {
                for (int residue = 0; residue < numSurfom; residue++)
                {
                    double depthToGo = tillageDepth - cumDepth;
                    layerIncorpDepth = Math.Min(depthToGo, soilPhysical.Thickness[layer]);
                    F_incorp_layer = MathUtilities.Divide(layerIncorpDepth, tillageDepth, 0.0);
                    for (int i = 0; i < maxFr; i++)
                    {
                        CPool[i, layer] += (SurfOM[residue].Lying[i].C + SurfOM[residue].Standing[i].C) * fIncorp * F_incorp_layer;
                        NPool[i, layer] += (SurfOM[residue].Lying[i].N + SurfOM[residue].Standing[i].N) * fIncorp * F_incorp_layer;
                        PPool[i, layer] += (SurfOM[residue].Lying[i].P + SurfOM[residue].Standing[i].P) * fIncorp * F_incorp_layer;
                    }
                    no3[layer] += SurfOM[residue].no3 * fIncorp * F_incorp_layer;
                    nh4[layer] += SurfOM[residue].nh4 * fIncorp * F_incorp_layer;
                    po4[layer] += SurfOM[residue].po4 * fIncorp * F_incorp_layer;
                }
                cumDepth = cumDepth + soilPhysical.Thickness[layer];
                residueIncorpFraction[layer] = F_incorp_layer;
            }

            Incorporated.Layer = new FOMPoolLayerType[deepestLayer + 1];

            for (int layer = 0; layer <= deepestLayer; layer++)
            {
                Incorporated.Layer[layer] = new FOMPoolLayerType()
                {
                    thickness = soilPhysical.Thickness[layer],
                    no3 = no3[layer],
                    nh4 = nh4[layer],
                    po4 = po4[layer],
                    Pool = new FOMType[maxFr]
                };

                for (int i = 0; i < maxFr; i++)
                    Incorporated.Layer[layer].Pool[i] = new FOMType()
                    {
                        C = CPool[i, layer],
                        N = NPool[i, layer],
                        P = PPool[i, layer],
                        AshAlk = AshAlkPool[i, layer]
                    };
            }

            nutrient.IncorpFOMPool(Incorporated);

            for (int pool = 0; pool < maxFr; pool++)
            {
                for (int i = 0; i < SurfOM.Count; i++)
                {
                    SurfOM[i].Lying[pool].amount = SurfOM[i].Lying[pool].amount * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].amount = SurfOM[i].Standing[pool].amount * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].C = SurfOM[i].Lying[pool].C * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].C = SurfOM[i].Standing[pool].C * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].N = SurfOM[i].Lying[pool].N * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].N = SurfOM[i].Standing[pool].N * (1.0 - fIncorp);

                    SurfOM[i].Lying[pool].P = SurfOM[i].Lying[pool].P * (1.0 - fIncorp);
                    SurfOM[i].Standing[pool].P = SurfOM[i].Standing[pool].P * (1.0 - fIncorp);

                    IncorporatedC += SurfOM[i].Lying[pool].C;
                    IncorporatedN += SurfOM[i].Lying[pool].N;
                    IncorporatedP += SurfOM[i].Lying[pool].P;
                    IncorporatedC += SurfOM[i].Standing[pool].C;
                    IncorporatedN += SurfOM[i].Standing[pool].N;
                    IncorporatedP += SurfOM[i].Standing[pool].P;
                }
            }

            for (int i = 0; i < SurfOM.Count; i++)
            {
                SurfOM[i].no3 *= (1.0 - fIncorp);
                SurfOM[i].nh4 *= (1.0 - fIncorp);
                SurfOM[i].po4 *= (1.0 - fIncorp);
            }
        }

        /// <summary>Adds the new surf om.</summary>
        /// <param name="newName">The new name.</param>
        /// <param name="newType">The new type.</param>
        private int AddNewSurfOM(string newName, string newType)
        {
            if (SurfOM == null)
                SurfOM = new List<SurfOrganicMatterType>();

            SurfOM.Add(new SurfOrganicMatterType(newName, newType));
            numSurfom = SurfOM.Count;
            Array.Resize(ref cf_contrib, numSurfom);
            Array.Resize(ref C_fract, numSurfom);
            Array.Resize(ref nh4ppm, numSurfom);
            Array.Resize(ref no3ppm, numSurfom);
            Array.Resize(ref po4ppm, numSurfom);
            Array.Resize(ref specific_area, numSurfom);
            frPoolC = IncreasePoolArray(frPoolC);
            frPoolN = IncreasePoolArray(frPoolN);
            frPoolP = IncreasePoolArray(frPoolP);

            int SOMNo = numSurfom - 1;
            ReadTypeSpecificConstants(SurfOM[SOMNo].OrganicMatterType, SOMNo, out SurfOM[SOMNo].PotDecompRate);
            return SOMNo;
        }

        /// <summary>
        /// Reads type-specific residue constants from ini-file and places them in c. constants;
        /// </summary>
        /// <param name="surfom_type">The surfom_type.</param>
        /// <param name="i">The i.</param>
        /// <param name="pot_decomp_rate">The pot_decomp_rate.</param>
        private void ReadTypeSpecificConstants(string surfom_type, int i, out double pot_decomp_rate)
        {
            ResidueType thistype = ResidueTypes.GetResidueType(surfom_type);
            if (thistype == null)
                throw new ApsimXException(this, "Cannot find residue type description for '" + surfom_type + "'");

            C_fract[i] = MathUtilities.Bound(thistype.fraction_C, 0.0, 1.0);
            po4ppm[i] = MathUtilities.Bound(thistype.po4ppm, 0.0, 1000.0);
            nh4ppm[i] = MathUtilities.Bound(thistype.nh4ppm, 0.0, 2000.0);
            no3ppm[i] = MathUtilities.Bound(thistype.no3ppm, 0.0, 1000.0);
            specific_area[i] = MathUtilities.Bound(thistype.specific_area, 0.0, 0.01);
            cf_contrib[i] = Math.Max(Math.Min(thistype.cf_contrib, 1), 0);
            pot_decomp_rate = MathUtilities.Bound(thistype.pot_decomp_rate, 0.0, 1.0);

            if (thistype.fr_c.Length != thistype.fr_n.Length || thistype.fr_n.Length != thistype.fr_p.Length)
                throw new ApsimXException(this, "Error reading in fr_c/n/p values, inconsistent array lengths");

            for (int j = 0; j < thistype.fr_c.Length; j++)
            {
                frPoolC[j, i] = thistype.fr_c[j];
                frPoolN[j, i] = thistype.fr_n[j];
                frPoolP[j, i] = thistype.fr_p[j];
            }
        }

        /// <summary>
        /// This function returns the fraction of the soil surface covered by;
        /// residue according to the relationship from Gregory (1982).
        /// <para>Notes;</para>
        /// <para>Gregory"s equation is of the form;</para>
        /// <para>        Fc = 1.0 - exp (- Am * M)   where Fc = Fraction covered;</para>
        /// <para>                                          Am = Specific Area (ha/kg)</para>
        /// <para>                                           M = Mulching rate (kg/ha)</para>
        /// <para>This residue model keeps track of the total residue area and so we can
        /// substitute this value (area residue/unit area) for the product_of Am * M.</para>
        /// </summary>
        /// <param name="SOMindex">The so mindex.</param>
        /// <returns></returns>
        private double CoverOfSOM(int SOMindex)
        {
            double areaLying = 0;
            double areaStanding = 0;
            double amountLying = 0;
            double amountStanding = 0;
            for (int i = 0; i < maxFr; i++)
            {
                areaLying += SurfOM[SOMindex].Lying[i].amount * specific_area[SOMindex];
                amountLying += SurfOM[SOMindex].Lying[i].amount;
                areaStanding += SurfOM[SOMindex].Standing[i].amount * specific_area[SOMindex];
                amountStanding += SurfOM[SOMindex].Standing[i].amount;
            }

            //Very Important Note,  #FixMe,  The following variables have been programmed to get the plumbing working so microclimate deals with
            //interception of radiation and precipitation from residues but have been set to 0 to reproduce existing behaviour until such time
            //that models can be parameterised to include redisue interception accurately
            SurfOM[SOMindex].CanopyLying.LAITotal = 0;//= areaLying;
            SurfOM[SOMindex].CanopyStanding.LAITotal = 0;//= areaStanding;
            SurfOM[SOMindex].CanopyLying.CoverTotal = 0;//= 1 - Math.Exp(-lyingExtinctionCoeff * areaLying);
            SurfOM[SOMindex].CanopyStanding.CoverTotal = 0;//= 1 - Math.Exp(-standingExtinctCoeff * areaStanding);
            SurfOM[SOMindex].CanopyLying.Height = 0;//= 50; //Assuming lying layers are 5 cm deep
            SurfOM[SOMindex].CanopyStanding.Height = 0;//= 700; //Fixme this should come from the crops height
            SurfOM[SOMindex].CanopyLying.Depth = 0;//= SurfOM[SOMindex].CanopyLying.Height;
            SurfOM[SOMindex].CanopyStanding.Depth = 0;//= SurfOM[SOMindex].CanopyStanding.Height;

            double F_Cover = AddCover(1.0 - (double)Math.Exp(-areaLying), 1.0 - (double)Math.Exp(-(standingExtinctCoeff) * areaStanding));
            return MathUtilities.Bound(F_Cover, 0.0, 1.0);
        }

        /// <summary>Adds material to the surface organic matter pool.</summary>
        /// <param name="mass">The amount of biomass added (kg/ha).</param>
        /// <param name="N">The amount of N added (kg/ha).</param>
        /// <param name="P">The amount of P added (kg/ha).</param>
        /// <param name="type">Type of the biomass.</param>
        /// <param name="name">Name of the biomass written to summary file</param>
        /// <param name="fractionStanding">Fraction standing. Defaults to 0</param>
        /// <param name="no3">The amount of no3 added (kg/ha).</param>
        /// <param name="nh4">The amount of nh4 added (kg/ha).</param>
        public void Add(double mass, double N, double P, string type, string name, double fractionStanding = 0, double no3 = -1, double nh4 = -1)
        {
            if (mass > 0 || N > 0 || P > 0)
            {
                // Assume the "cropType" is the unique name.  Now check whether this unique "name" already exists in the system.
                int SOMNo = GetResidueNumber(type);
                if (SOMNo < 0)
                    SOMNo = AddNewSurfOM(type, type);

                summary.WriteMessage(this, $"Adding {mass:F2} kg/ha of biomass ({N:F2} kgN/ha) to pool: {type}. ", MessageType.Diagnostic);

                // convert the ppm figures into kg/ha;
                if (no3 < 0)
                    SurfOM[SOMNo].no3 += MathUtilities.Divide(no3ppm[SOMNo], 1000000.0, 0.0) * mass;
                else
                    SurfOM[SOMNo].no3 += no3;

                if (nh4 < 0) // this was "!0" for some reason
                    SurfOM[SOMNo].nh4 += MathUtilities.Divide(nh4ppm[SOMNo], 1000000.0, 0.0) * mass;
                else
                    SurfOM[SOMNo].nh4 += nh4;

                SurfOM[SOMNo].po4 += MathUtilities.Divide(po4ppm[SOMNo], 1000000.0, 0.0) * mass;

                for (int i = 0; i < maxFr; i++)
                {
                    SurfOM[SOMNo].Standing[i].amount += fractionStanding * mass * frPoolC[i, SOMNo];
                    SurfOM[SOMNo].Standing[i].C += fractionStanding * mass * C_fract[SOMNo] * frPoolC[i, SOMNo];
                    SurfOM[SOMNo].Standing[i].N += fractionStanding * N * frPoolN[i, SOMNo];
                    SurfOM[SOMNo].Standing[i].P += fractionStanding * P * frPoolP[i, SOMNo];

                    double fractionLying = 1 - fractionStanding;
                    SurfOM[SOMNo].Lying[i].amount += fractionLying * mass * frPoolC[i, SOMNo];
                    SurfOM[SOMNo].Lying[i].C += fractionLying * mass * C_fract[SOMNo] * frPoolC[i, SOMNo];
                    SurfOM[SOMNo].Lying[i].N += fractionLying * N * frPoolN[i, SOMNo];
                    SurfOM[SOMNo].Lying[i].P += fractionLying * P * frPoolP[i, SOMNo];
                }
            }
        }

        /// <summary>Remove material from the surface organic matter pool.</summary>
        /// <param name="mass">The amount of biomass to remove (kg/ha).</param>
        public void Remove(double mass)
        {
            if (mass > 0 )
            {

                double fractionToRemove = MathUtilities.Bound(MathUtilities.Divide(mass, Wt, 0), 0, 1);
                if (fractionToRemove > 0)
                {
                    summary.WriteMessage(this, $"Removing {mass:F2} kg/ha of biomass from surface organic matter pool", MessageType.Diagnostic);
                    RemoveBiomass(deadToRemove: fractionToRemove);
                }
            }
        }

        /// <summary>Resize2s the d array.</summary>
        /// <param name="original">The original.</param>
        /// <returns></returns>
        private double[,] IncreasePoolArray(double[,] original)
        {
            double[,] newArray = new double[original.GetLength(0), original.GetLength(1) + 1];

            for (int x = 0; x < original.GetLength(0); x++)
                for (int y = 0; y < original.GetLength(1); y++)
                    newArray[x, y] = original[x, y];

            return newArray;
        }

    }
}