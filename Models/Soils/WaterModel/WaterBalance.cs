using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.WaterModel
{

    /// <summary>
    /// The SoilWater module is a cascading water balance model that owes much to its precursors in
    /// CERES (Jones and Kiniry, 1986) and PERFECT(Littleboy et al, 1992).
    /// The algorithms for redistribution of water throughout the soil profile have been inherited from
    /// the CERES family of models.
    ///
    /// The water characteristics of the soil are specified in terms of the lower limit (ll15),
    /// drained upper limit(dul) and saturated(sat) volumetric water contents. Water movement is
    /// described using separate algorithms for saturated or unsaturated flow. It is notable that
    /// redistribution of solutes, such as nitrate- and urea-N, is carried out in this module.
    ///
    /// Modifications adopted from PERFECT include:
    /// * the effects of surface residues and crop cover on modifying runoff and reducing potential soil evaporation,
    /// * small rainfall events are lost as first stage evaporation rather than by the slower process of second stage evaporation, and
    /// * specification of the second stage evaporation coefficient(cona) as an input parameter, providing more flexibility for describing differences in long term soil drying due to soil texture and environmental effects.
    ///
    /// The module is interfaced with SurfaceOrganicMatter and crop modules so that simulation of the soil water balance
    /// responds to change in the status of surface residues and crop cover(via tillage, decomposition and crop growth).
    ///
    /// Enhancements beyond CERES and PERFECT include:
    /// * the specification of swcon for each layer, being the proportion of soil water above dul that drains in one day
    /// * isolation from the code of the coefficients determining diffusivity as a function of soil water
    ///   (used in calculating unsaturated flow).Choice of diffusivity coefficients more appropriate for soil type have been found to improve model performance.
    /// * unsaturated flow is permitted to move water between adjacent soil layers until some nominated gradient in
    ///   soil water content is achieved, thereby accounting for the effect of gravity on the fully drained soil water profile.
    ///
    /// SoilWater is called by APSIM on a daily basis, and typical of such models, the various processes are calculated consecutively.
    /// This contrasts with models such as SWIM that solve simultaneously a set of differential equations that describe the flow processes.
    /// </summary>
    [ValidParent(ParentType = typeof(Soil))]
    [ViewName("ApsimNG.Resources.Glade.ProfileView.glade")]
    [PresenterName("UserInterface.Presenters.ProfilePresenter")]
    [Serializable]
    public class WaterBalance : Model, ISoilWater
    {
        private Physical physical;
        private HyProps hyprops = new HyProps();

        /// <summary>Link to the soil properties.</summary>
        [Link]
        private Soil soil = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        [Link]
        Water water = null;

        [Link]
        private ISummary summary = null;

        /// <summary>Link to the lateral flow model.</summary>
        [Link]
        private LateralFlowModel lateralFlowModel = null;

        /// <summary>Link to the runoff model.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private RunoffModel runoffModel = null;

        /// <summary>Link to the saturated flow model.</summary>
        [Link]
        private SaturatedFlowModel saturatedFlow = null;

        /// <summary>Link to the unsaturated flow model.</summary>
        [Link]
        private UnsaturatedFlowModel unsaturatedFlow = null;

        /// <summary>Link to the evaporation model.</summary>
        [Link]
        private EvaporationModel evaporationModel = null;

        /// <summary>Link to the water table model.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private WaterTableModel waterTableModel = null;

        [Link(ByName = true)]
        ISolute no3 = null;

        [Link(ByName = true)]
        ISolute nh4 = null;

        [Link(ByName = true)]
        ISolute urea = null;

        [Link(ByName = true, IsOptional = true)]
        ISolute cl = null;

        /// <summary>Irrigation information.</summary>
        [NonSerialized]
        private List<IrrigationApplicationType> irrigations;

        /// <summary>Water content (mm).</summary>
        private double[] waterMM;

        /// <summary>Water content (mm/mm).</summary>
        private double[] waterVolumetric;

        /// <summary>Start date for switch to summer parameters for soil water evaporation (dd-mmm)</summary>
        [Units("dd-mmm")]
        [Caption("Summer date")]
        [Description("Start date for switch to summer parameters for soil water evaporation")]
        public string SummerDate { get; set; } = "1-Nov";

        /// <summary>Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in summer (a.k.a. U)</summary>
        [Bounds(Lower = 0.0, Upper = 40.0)]
        [Units("mm")]
        [Caption("Summer U")]
        [Description("Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in summer (a.k.a. U)")]
        public double SummerU { get; set; } = 6;

        /// <summary>Drying coefficient for stage 2 soil water evaporation in summer (a.k.a. ConA)</summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Caption("Summer ConA")]
        [Description("Drying coefficient for stage 2 soil water evaporation in summer (a.k.a. ConA)")]
        public double SummerCona { get; set; } = 3.5;

        /// <summary>Start date for switch to winter parameters for soil water evaporation (dd-mmm)</summary>
        [Units("dd-mmm")]
        [Caption("Winter date")]
        [Description("Start date for switch to winter parameters for soil water evaporation")]
        public string WinterDate { get; set; } = "1-Apr";

        /// <summary>Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in winter (a.k.a. U).</summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        [Caption("Winter U")]
        [Description("Cummulative soil water evaporation to reach the end of stage 1 soil water evaporation in winter (a.k.a. U).")]
        public double WinterU { get; set; } = 6;

        /// <summary>Drying coefficient for stage 2 soil water evaporation in winter (a.k.a. ConA)</summary>
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Caption("Winter ConA")]
        [Description("Drying coefficient for stage 2 soil water evaporation in winter (a.k.a. ConA)")]
        public double WinterCona { get; set; } = 2.5;

        /// <summary>Constant in the soil water diffusivity calculation (mm2/day)</summary>
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("mm2/day")]
        [Caption("Diffusivity constant")]
        [Description("Constant in the soil water diffusivity calculation")]
        public double DiffusConst { get; set; }

        /// <summary>Effect of soil water storage above the lower limit on soil water diffusivity (/mm)</summary>
        [Bounds(Lower = 0.0, Upper = 100.0)]
        [Units("/mm")]
        [Caption("Diffusivity slope")]
        [Description("Effect of soil water storage above the lower limit on soil water diffusivity")]
        public double DiffusSlope { get; set; }

        /// <summary>Fraction of incoming radiation reflected from bare soil</summary>
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Caption("Albedo")]
        [Description("Fraction of incoming radiation reflected from bare soil")]
        public double Salb { get; set; }

        /// <summary>Runoff Curve Number (CN) for bare soil with average moisture</summary>
        [Bounds(Lower = 1.0, Upper = 100.0)]
        [Caption("CN bare")]
        [Description("Runoff Curve Number (CN) for bare soil with average moisture")]
        public double CN2Bare { get; set; }

        /// <summary>Gets or sets the cn red.</summary>
        [Description("Max. reduction in curve number due to cover")]
        public double CNRed { get; set; } = 20;


        /// <summary>Gets or sets the cn cov.</summary>
        [Description("Cover for max curve number reduction")]
        public double CNCov { get; set; } = 0.8;

        /// <summary>Basal width of the downslope boundary of the catchment for lateral flow calculations (m).</summary>
        [Bounds(Lower = 0.0, Upper = 1.0e8F)]
        [Units("m")]
        [Caption("Basal width")]
        [Description("Basal width of the downslope boundary of the catchment for lateral flow calculations")]
        public double DischargeWidth { get; set; } = 5;

        /// <summary>Catchment area for later flow calculations (m2).</summary>
        [Bounds(Lower = 0.0, Upper = 1.0e8F)]
        [Units("m2")]
        [Caption("Catchment")]
        [Description("Catchment area for lateral flow calculations")]
        public double CatchmentArea { get; set; } = 10;

        /// <summary>
        /// Gets the matric Potential at DUL (cm)
        /// </summary>
        [Description("Matric Potential at DUL (cm)")]
        [Units("cm")]
        [Bounds(Lower = -1e3, Upper = 0.0)]
        public double PSIDul { get; set; } = -100.0;

        /// <summary>Depth strings. Wrapper around Thickness.</summary>
        [Display]
        [Units("mm")]
        [Summary]
        [JsonIgnore]
        public string[] Depth
        {
            get
            {
                return SoilUtilities.ToDepthStrings(Thickness);
            }
            set
            {
                Thickness = SoilUtilities.ToThickness(value);
            }
        }

        /// <summary>Soil layer thickness for each layer (mm).</summary>
        public double[] Thickness { get; set; }

        /// <summary>Amount of water in the soil (mm).</summary>
        [JsonIgnore]
        public double[] Water
        {
            get { return waterMM; }
            set
            {
                waterMM = value;
                if (value == null)
                    waterVolumetric = null;
                else
                    waterVolumetric = MathUtilities.Divide(value, soilPhysical.Thickness);
            }
        }

        /// <summary>Amount of water in the soil (mm/mm).</summary>
        [JsonIgnore]
        public double[] SW
        {
            get { return waterVolumetric; }
            set
            {
                waterVolumetric = value;
                waterMM = MathUtilities.Multiply(value, soilPhysical.Thickness);
            }
        }

        /// <summary>Water potential of layer</summary>
        [Units("cm")]
        public double[] PSI
        {
            get
            {
                double[] psi = new double[soilPhysical.Thickness.Length];
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                {
                    psi[i] = hyprops.Suction(i, SW[i], psi, PSIDul, soilPhysical.LL15, soilPhysical.DUL, soilPhysical.SAT);
                    if (soilPhysical.KS != null)
                        K[i] = hyprops.SimpleK(i, psi[i], soilPhysical.SAT, soilPhysical.KS);
                }
                return psi;
            }
        }

        /// <summary>Hydraulic Conductivity of layer</summary>
        [Units("cm/h")]
        public double[] K { get; private set; }

        ///<summary>Pore Interaction Index for shape of the K(theta) curve for soil hydraulic conductivity</summary>
        [JsonIgnore]
        [Units("-")]
        public double[] PoreInteractionIndex
        {
            get
            {
                return hyprops.PoreInteractionIndex;
            }
            set
            {
                hyprops.PoreInteractionIndex = value;
                if (physical.KS != null)
                    hyprops.SetupKCurve(physical.Thickness.Length, physical.LL15, physical.DUL, physical.SAT, physical.KS, 0.1, PSIDul);
            }
        }

        /// <summary>Runon (mm).</summary>
        [JsonIgnore]
        public double Runon { get; set; }

        /// <summary>The efficiency (0-1) that solutes move down with water.</summary>
        [JsonIgnore]
        public double[] SoluteFluxEfficiency { get; set; }

        /// <summary>The efficiency (0-1) that solutes move up with water.</summary>
        [JsonIgnore]
        public double[] SoluteFlowEfficiency { get; set; }

        /// <summary> This is set by Microclimate and is rainfall less that intercepted by the canopy and residue components </summary>
        [JsonIgnore]
        public double PotentialInfiltration { get; set; }

        // --- Outputs -------------------------------------------------------------------

        /// <summary>Lateral flow (mm).</summary>
        [JsonIgnore]
        public double[] LateralFlow { get { return lateralFlowModel.OutFlow; } }

        /// <summary>Amount of water moving laterally out of the profile (mm)</summary>
        [JsonIgnore]
        public double[] LateralOutflow { get { return LateralFlow; } }

        /// <summary>Runoff (mm).</summary>
        [JsonIgnore]
        public double Runoff { get; private set; }

        /// <summary>Infiltration (mm).</summary>
        [JsonIgnore]
        public double Infiltration { get; private set; }

        /// <summary>Drainage (mm).</summary>
        [JsonIgnore]
        public double Drainage { get { if (Flux == null) return 0; else return Flux[Flux.Length - 1]; } }

        /// <summary>Subsurface drain (mm)</summary>
        [JsonIgnore]
        public double SubsurfaceDrain => 0;

        /// <summary>Evaporation (mm).</summary>
        [JsonIgnore]
        public double Evaporation { get { return evaporationModel.Es; } }

        /// <summary>Water table.</summary>
        [JsonIgnore]
        public double WaterTable { get { return waterTableModel.Depth; } set { waterTableModel.Set(value); } }

        /// <summary>Flux. Water moving down (mm).</summary>
        [JsonIgnore]
        public double[] Flux { get; private set; }

        /// <summary>Flow. Water moving up (mm).</summary>
        [JsonIgnore]
        public double[] Flow { get; private set; }

        /// <summary>Gets todays potential runoff (mm).</summary>
        [JsonIgnore]
        public double PotentialRunoff
        {
            get
            {
                double waterForRunoff = PotentialInfiltration;

                foreach (var irrigation in irrigations)
                {
                    if (irrigation.WillRunoff)
                        waterForRunoff = waterForRunoff + irrigation.Amount;
                }
                return waterForRunoff;
            }
        }

        /// <summary>Provides access to the soil properties.</summary>
        [JsonIgnore]
        public Soil Properties { get { return soil; } }

        ///<summary>Gets soil water content (mm)</summary>
        [JsonIgnore]
        public double[] SWmm { get { return Water; } }

        ///<summary>Gets extractable soil water relative to LL15(mm)</summary>
        [JsonIgnore]
        public double[] ESW { get { return MathUtilities.Subtract(Water, soilPhysical.LL15mm); } }

        ///<summary>Gets potential evaporation from soil surface (mm)</summary>
        [JsonIgnore]
        public double Eos { get { return evaporationModel.Eos; } }

        /// <summary>Gets the actual (realised) soil water evaporation (mm)</summary>
        [JsonIgnore]
        public double Es { get { return evaporationModel.Es; } }

        ///<summary>Time since start of second stage evaporation (days).</summary>
        [JsonIgnore]
        public double T { get { return evaporationModel.t; } }

        /// <summary>Gets potential evapotranspiration of the whole soil-plant system (mm)</summary>
        [JsonIgnore]
        public double Eo { get { return evaporationModel.Eo; } set { evaporationModel.Eo = value; } }

        /// <summary>Fractional amount of water above DUL that can drain under gravity per day.</summary>
        /// <remarks>
        /// Between (SAT and DUL) soil water conductivity constant for each soil layer.
        /// At thicknesses specified in "SoilWater" node of GUI.
        /// Use Soil.SWCON for SWCON in standard thickness
        /// </remarks>
        [Display(Format = "N3")]
        [Summary]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("/d")]
        [Caption("SWCON")]
        public double[] SWCON { get; set; }

        /// <summary>Lateral saturated hydraulic conductivity (KLAT).</summary>
        /// <remarks>
        /// Lateral flow soil water conductivity constant for each soil layer.
        /// At thicknesses specified in "SoilWater" node of GUI.
        /// Use Soil.KLAT for KLAT in standard thickness
        /// </remarks>
        [Display(Format = "N3")]
        [Summary]
        [Bounds(Lower = 0, Upper = 1.0e3F)]
        [Units("mm/d")]
        [Caption("Klat")]
        public double[] KLAT { get; set; }

        /// <summary>Amount of N leaching as NO3-N from the deepest soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double LeachNO3 { get { if (FlowNO3 == null) return 0; else return FlowNO3.Last(); } }

        /// <summary>Amount of N leaching as NH4-N from the deepest soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double LeachNH4 { get { return 0; } }

        /// <summary>Amount of N leaching as urea-N  from the deepest soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double LeachUrea { get { if (FlowUrea == null) return 0; else return FlowUrea.Last(); } }

        /// <summary>Amount of Cl leaching from the deepest soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double LeachCl { get { if (FlowCl == null) return 0; else return FlowCl.Last(); } }

        /// <summary>Amount of N leaching as NO3 from each soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double[] FlowNO3 { get; private set; }

        /// <summary>Amount of N leaching as NH4 from each soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double[] FlowNH4 { get; private set; }

        /// <summary>Amount of N leaching as urea from each soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double[] FlowUrea { get; private set; }

        /// <summary>Amount of Cl leaching as Cl from each soil layer (kg /ha)</summary>
        [JsonIgnore]
        public double[] FlowCl { get; private set; }


        /// <summary> This is set by Microclimate and is rainfall less that intercepted by the canopy and residue components </summary>
        [JsonIgnore]
        public double PrecipitationInterception { get; set; }

        /// <summary>Pond.</summary>
        public double Pond { get { return 0; } }

        /// <summary>Plant available water SW-LL15 (mm/mm).</summary>
        [Units("mm/mm")]
        public double[] PAW
        {
            get
            {
                return APSIM.Shared.APSoil.APSoilUtilities.CalcPAWC(soilPhysical.Thickness,
                                                                  soilPhysical.LL15,
                                                                  SW,
                                                                  null);
            }
        }

        /// <summary>Plant available water SW-LL15 (mm).</summary>
        [Units("mm")]
        public double[] PAWmm
        {
            get
            {
                return MathUtilities.Multiply(PAW, soilPhysical.Thickness);
            }
        }

        // --- Event handlers ------------------------------------------------------------

        /// <summary>Called when a simulation commences.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("Commencing")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            Initialise();
        }

        /// <summary>Called on start of day.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            irrigations.Clear();
            Runon = 0;
        }

        /// <summary>Called when an irrigation occurs.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, IrrigationApplicationType e)
        {
            irrigations.Add(e);
        }

        /// <summary>Called by CLOCK to let this model do its water movement.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event data.</param>
        [EventSubscribe("DoSoilWaterMovement")]
        private void OnDoSoilWaterMovement(object sender, EventArgs e)
        {
            // Calculate lateral flow.
            lateralFlowModel.Calculate();
            if (LateralFlow.Length > 0)
                Water = MathUtilities.Subtract(Water, LateralFlow);

            // Calculate runoff.
            Runoff = runoffModel.Value();

            // Calculate infiltration.
            Infiltration = PotentialInfiltration - Runoff;

            Water[0] = Water[0] + Infiltration + Runon;

            // Allow irrigation to infiltrate.
            foreach (var irrigation in irrigations)
            {
                if (irrigation.Amount > 0)
                {
                    int irrigationLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, Convert.ToInt32(irrigation.Depth, CultureInfo.InvariantCulture));
                    Water[irrigationLayer] += irrigation.Amount;
                    if (irrigationLayer == 0)
                        Infiltration += irrigation.Amount;

                    if (no3 != null)
                        no3.kgha[irrigationLayer] += irrigation.NO3;

                    if (nh4 != null)
                        nh4.kgha[irrigationLayer] += irrigation.NH4;

                    if (cl != null)
                        cl.kgha[irrigationLayer] += irrigation.CL;
                }
            }

            // Saturated flow.
            Flux = saturatedFlow.Values;

            // Add backed up water to runoff.
            Water[0] = Water[0] - saturatedFlow.backedUpSurface;

            // Now reduce the infiltration amount by what backed up.
            Infiltration = Infiltration - saturatedFlow.backedUpSurface;

            // Turn the proportion of the infiltration that backed up into runoff.
            Runoff = Runoff + saturatedFlow.backedUpSurface;

            // Should go to pond if one exists.
            //  pond = Math.Min(Runoff, max_pond);
            MoveDown(Water, Flux);

            double[] no3Values = no3.kgha;
            double[] ureaValues = urea.kgha;

            // Calculate solute movement down with water.
            double[] no3Down = CalculateSoluteMovementDown(no3Values, Water, Flux, SoluteFluxEfficiency);
            MoveDown(no3Values, no3Down);
            double[] ureaDown = CalculateSoluteMovementDown(ureaValues, Water, Flux, SoluteFluxEfficiency);
            MoveDown(ureaValues, ureaDown);

            double[] clValues = null;
            double[] clDown = null;
            if (cl != null)
            {
                clValues = cl.kgha;
                clDown = CalculateSoluteMovementDown(clValues, Water, Flux, SoluteFluxEfficiency);
                MoveDown(clValues, clDown);
            }

            // Calculate evaporation and remove from top layer.
            double es = evaporationModel.Calculate();
            Water[0] = Water[0] - es;

            // Calculate unsaturated flow of water and apply.
            Flow = unsaturatedFlow.Values;
            MoveUp(Water, Flow);

            // Check for errors in water variables.
            //CheckForErrors();

            // Calculate water table depth.
            waterTableModel.Calculate();

            // Calculate and apply net solute movement.
            double[] no3Up = CalculateNetSoluteMovement(no3Values, Water, Flow, SoluteFlowEfficiency);
            MoveUp(no3Values, no3Up);
            double[] ureaUp = CalculateNetSoluteMovement(ureaValues, Water, Flow, SoluteFlowEfficiency);
            MoveUp(ureaValues, ureaUp);

            // Update flow output variables.
            FlowNO3 = MathUtilities.Subtract(no3Down, no3Up);
            FlowUrea = MathUtilities.Subtract(ureaDown, ureaUp);

            // Set solute state variables.
            no3.SetKgHa(SoluteSetterType.Soil, no3Values);
            urea.SetKgHa(SoluteSetterType.Soil, ureaValues);


            if (cl != null)
            {
                double[] clUp = CalculateNetSoluteMovement(clValues, Water, Flow, SoluteFlowEfficiency);
                MoveUp(clValues, clUp);
                FlowCl = MathUtilities.Subtract(clDown, clUp);
                cl.SetKgHa(SoluteSetterType.Soil, clValues);
            }


            // Now that we've finished moving water, calculate volumetric water
            waterVolumetric = MathUtilities.Divide(Water, soilPhysical.Thickness);

            // Update the variable in the water model.
            water.Volumetric = waterVolumetric;
        }

        /// <summary>Move water down the profile</summary>
        /// <param name="water">The water values</param>
        /// <param name="flux">The amount to move down</param>
        private static void MoveDown(double[] water, double[] flux)
        {
            for (int i = 0; i < water.Length; i++)
            {
                if (i == 0)
                    water[i] = water[i] - flux[i];
                else
                    water[i] = water[i] + flux[i - 1] - flux[i];
            }
        }

        /// <summary>Move water up the profile.</summary>
        /// <param name="water">The water values.</param>
        /// <param name="flow">The amount to move up.</param>
        private static void MoveUp(double[] water, double[] flow)
        {
            for (int i = 0; i < water.Length; i++)
            {
                if (i == 0)
                    water[i] = water[i] + flow[i];
                else
                    water[i] = water[i] + flow[i] - flow[i - 1];
            }
        }

        /// <summary>Calculate the solute movement DOWN based on flux.</summary>
        /// <param name="solute"></param>
        /// <param name="water"></param>
        /// <param name="flux"></param>
        /// <param name="efficiency"></param>
        /// <returns></returns>
        private static double[] CalculateSoluteMovementDown(double[] solute, double[] water, double[] flux, double[] efficiency)
        {
            double[] soluteFlux = new double[solute.Length];
            for (int i = 0; i < solute.Length; i++)
            {
                var soluteInLayer = solute[i];
                if (i > 0)
                    soluteInLayer += soluteFlux[i - 1];

                soluteFlux[i] = soluteInLayer * MathUtilities.Divide(flux[i], water[i] + flux[i], 0) * efficiency[i];
                soluteFlux[i] = MathUtilities.Constrain(soluteFlux[i], 0.0, Math.Max(soluteInLayer, 0));
            }

            return soluteFlux;
        }

        /// <summary>Calculate the solute movement UP and DOWN based on flow.</summary>
        /// <param name="solute"></param>
        /// <param name="water"></param>
        /// <param name="flux"></param>
        /// <param name="efficiency"></param>
        /// <returns></returns>
        private static double[] CalculateNetSoluteMovement(double[] solute, double[] water, double[] flux, double[] efficiency)
        {
            double[] soluteUp = CalculateSoluteMovementUp(solute, water, flux, efficiency);

            double[] remaining = new double[flux.Length];
            remaining[0] = soluteUp[0];
            for (int i = 1; i < solute.Length; i++)
                remaining[i] = soluteUp[i] - soluteUp[i - 1];

            double[] soluteDown = new double[solute.Length];
            for (int i = 0; i < solute.Length; i++)
            {
                if (flux[i] < 0)
                {
                    var positiveFlux = flux[i] * -1;
                    var waterInLayer = water[i] + positiveFlux;
                    var soluteInLayer = solute[i] + remaining[i];
                    if (i > 0)
                    {
                        soluteInLayer += soluteDown[i - 1];
                        waterInLayer += flux[i - 1];
                    }

                    soluteDown[i] = positiveFlux * soluteInLayer / waterInLayer * efficiency[i];
                    soluteDown[i] = MathUtilities.Constrain(soluteDown[i], 0, soluteInLayer);
                }
            }
            return MathUtilities.Subtract(soluteUp, soluteDown);
        }

        /// <summary>Calculate the solute movement UP based on flow.</summary>
        /// <param name="solute"></param>
        /// <param name="water"></param>
        /// <param name="flow"></param>
        /// <param name="efficiency"></param>
        /// <returns></returns>
        private static double[] CalculateSoluteMovementUp(double[] solute, double[] water, double[] flow, double[] efficiency)
        {
            // soluteFlow[i] is the solutes flowing into this layer from the layer below.
            // this is the water moving into this layer * solute concentration. That is,
            // water in this layer * solute in this layer / water in this layer.
            //
            // todo: should this be solute[i + 1] because solute concenctration in the water
            // should actually be the solute concenctration in the water moving into this layer
            // from the layer below.
            // flow[i] is the water coming into a layer from the layer below
            double[] soluteFlow = new double[solute.Length];
            for (int i = solute.Length - 2; i >= 0; i--)
            {
                //if (i == 0)
                //    // soluteFlow[i] = 0;?
                //    soluteFlow[i] = flow[i] * solute[i+1] / (water[i+1] + flow[i]);
                //else if (i < solute.Length-2)
                if (flow[i] <= 0)
                    soluteFlow[i] = 0;
                else
                {
                    var soluteInLayer = solute[i + 1] + soluteFlow[i + 1];
                    soluteFlow[i] = flow[i] * soluteInLayer / (water[i + 1] + flow[i] - flow[i + 1]) * efficiency[i];
                    soluteFlow[i] = MathUtilities.Constrain(soluteFlow[i], 0, soluteInLayer);
                }
            }

            return soluteFlow;
        }

        /// <summary>Checks for soil for errors.</summary>
        private void CheckForErrors()
        {
            const double specific_bd = 2.65;

            double min_sw = 0.0;

            for (int i = 0; i < soilPhysical.Thickness.Length; i++)
            {
                double max_sw = 1.0 - MathUtilities.Divide(soilPhysical.BD[i], specific_bd, 0.0);  // ie. Total Porosity

                if (MathUtilities.IsLessThan(soilPhysical.AirDry[i], min_sw))
                    throw new Exception(String.Format("({0} {1:G4}) {2} {3} {4} {5} {6:G4})",
                                               " Air dry lower limit of ",
                                               soilPhysical.AirDry[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is below acceptable value of ",
                                               min_sw));

                if (MathUtilities.IsLessThan(soilPhysical.LL15[i], soilPhysical.AirDry[i]))
                    throw new Exception(String.Format("({0} {1:G4}) {2} {3} {4} {5} {6:G4})",
                                               " 15 bar lower limit of ",
                                               soilPhysical.LL15[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is below air dry value of ",
                                               soilPhysical.AirDry[i]));

                if (MathUtilities.IsLessThanOrEqual(soilPhysical.DUL[i], soilPhysical.LL15[i]))
                    throw new Exception(String.Format("({0} {1:G4}) {2} {3} {4} {5} {6:G4})",
                                               " drained upper limit of ",
                                               soilPhysical.DUL[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is at or below lower limit of ",
                                               soilPhysical.LL15[i]));

                if (MathUtilities.IsLessThanOrEqual(soilPhysical.SAT[i], soilPhysical.DUL[i]))
                    throw new Exception(String.Format("({0} {1:G4}) {2} {3} {4} {5} {6:G4})",
                                               " saturation of ",
                                               soilPhysical.SAT[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is at or below drained upper limit of ",
                                               soilPhysical.DUL[i]));

                if (MathUtilities.IsGreaterThan(soilPhysical.SAT[i], max_sw))
                    throw new Exception(String.Format("({0} {1:G4}) {2} {3} {4} {5} {6:G4} {7} {8} {9:G4} {10} {11} {12:G4})",
                                               " saturation of ",
                                               soilPhysical.SAT[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is above acceptable value of ",
                                               max_sw,
                                               "\n",
                                               "You must adjust bulk density (bd) to below ",
                                               (1.0 - soilPhysical.SAT[i]) * specific_bd,
                                               "\n",
                                               "OR saturation (sat) to below ",
                                               max_sw));

                if (MathUtilities.IsGreaterThan(SW[i], soilPhysical.SAT[i]))
                    throw new Exception(String.Format("({0} {1:G4}) {2} {3} {4} {5} {6:G4}",
                                               " soil water of ",
                                               SW[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is above saturation of ",
                                               soilPhysical.SAT[i]));

                if (MathUtilities.IsLessThan(SW[i], soilPhysical.AirDry[i]))
                    throw new Exception(String.Format("({0} {1:G4}) {2} {3} {4} {5} {6:G4}",
                                               " soil water of ",
                                               SW[i],
                                               " in layer ",
                                               i,
                                               "\n",
                                               "         is below air-dry value of ",
                                               soilPhysical.AirDry[i]));
            }

        }

        ///<summary>Remove water from the profile</summary>
        public void RemoveWater(double[] amountToRemove)
        {
            Water = MathUtilities.Subtract(Water, amountToRemove);
        }

        /// <summary>Sets the water table.</summary>
        /// <param name="InitialDepth">The initial depth.</param>
        public void SetWaterTable(double InitialDepth)
        {
            WaterTable = InitialDepth;
        }

        ///<summary>Perform a reset</summary>
        public void Reset()
        {
            summary.WriteMessage(this, "Resetting Soil Water Balance", MessageType.Diagnostic);
            Initialise();
        }

        /// <summary>Initialise the model.</summary>
        private void Initialise()
        {
            FlowNH4 = MathUtilities.CreateArrayOfValues(0.0, Thickness.Length);
            SoluteFlowEfficiency = MathUtilities.CreateArrayOfValues(1.0, Thickness.Length);
            SoluteFluxEfficiency = MathUtilities.CreateArrayOfValues(1.0, Thickness.Length);
            Water = water.InitialValuesMM;
            Runon = 0;
            Runoff = 0;
            PotentialInfiltration = 0;
            Flux = null;
            Flow = null;
            evaporationModel.Initialise();
            irrigations = new List<IrrigationApplicationType>();

            int n = soilPhysical.Thickness.Length;
            hyprops.ResizePropfileArrays(n);
            hyprops.SetupThetaCurve(PSIDul, n - 1, soilPhysical.LL15, soilPhysical.DUL, soilPhysical.SAT);
            K = new double[n];
        }

        ///<summary>Perform tillage</summary>
        public void Tillage(TillageType Data)
        {
            if ((Data.cn_red <= 0) || (Data.cn_rain <= 0))
            {
                string message = "tillage:- " + Data.Name + " has incorrect values for " + Environment.NewLine +
                    "CN reduction = " + Data.cn_red + Environment.NewLine + "Acc rain     = " + Data.cn_red;
                throw new Exception(message);
            }

            double reduction = MathUtilities.Constrain(Data.cn_red, 0.0, CN2Bare);

            runoffModel.TillageCnCumWater = Data.cn_rain;
            runoffModel.TillageCnRed = reduction;
            runoffModel.CumWaterSinceTillage = 0.0;

            var line = string.Format("Soil tilled. CN reduction = {0}. Cumulative rain = {1}",
                                     reduction, Data.cn_rain);
            summary.WriteMessage(this, line, MessageType.Diagnostic);
        }

        ///<summary>Perform tillage</summary>
        public void Tillage(string tillageType)
        {
            throw new NotImplementedException();
        }

        /// <summary>Set the physical node.</summary>
        /// <remarks>I'm not sure why this is necessary</remarks>
        public void SetPhysical(Physical physical)
        {
            this.physical = physical;
        }
    }
}
