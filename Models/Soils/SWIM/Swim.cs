using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;

namespace Models.Soils
{
    ///<summary>
    /// .NET port of the Fortran SWIM3 model
    /// Ported by Eric Zurcher July 2014
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]   // Until we have a better view for SWIM...
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Soil))]
    public class Swim3 : Model, ISoilWater
    {
        [Link]
        private IClock clock = null;

        [Link]
        private ISummary summary = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical physical = null;

        [Link]
        private Water water = null;

        /// <summary>Link to NO3.</summary>
        [Link]
        private List<ISolute> solutes = null;

        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        [Link]
        private List<ICanopy> canopies = null;

        [Link(IsOptional = true)]
        private SwimSubsurfaceDrain subsurfaceDrain = null;

        private const double floatComparisonTolerance = 1e-16;
        private const double divideTolerance = 1e-8;
        private const double effpar = 0.184;
        private const double a_to_evap_fact = 0.44; // converts residue specfic area 'A' to"

        /// <summary>
        /// coef. in exp effect of canopy on soil water evaporation. In previous version initialised to 1.7.
        /// </summary>
        private const double canopy_eos_coef = 1.7;

        /// <summary>
        /// reducing pot. soil evap. = 1.7 Adams, Arkin and Ritchie 1976
        /// Set the default rainfall and evaporation daily time courses
        /// these are used if the user does not specify them in the met file.
        /// Canopy factors for cover runoff effect
        /// </summary>
        private double[] canopy_fact = new double[] { 1.0, 1.0, 0.0, 0.0 };

        /// <summary>
        /// heights for canopy factors (mm)
        /// </summary>
        private double[] canopy_fact_height = new double[] { 0, 600, 1800, 30000 };

        /// <summary>
        /// default canopy factor in absence of height
        /// </summary>
        private const double canopy_fact_default = 0.5;

        /// <summary>
        /// Negative solute concentration below which a warning error is thrown
        /// </summary>
        private const double negative_conc_warn = 0;

        /// <summary>
        /// Negative solute concentration below which a fatal error is thrown
        /// </summary>
        private const double negative_conc_fatal = 0;

        /// <summary>
        /// number of iterations before timestep is halved
        /// </summary>
        private const int max_iterations = 50;

        /// <summary>
        ///
        /// </summary>
        private const double ersoil = 0.000001;

        /// <summary>
        ///
        /// </summary>
        private const double ernode = 0.000001;

        /// <summary>
        ///
        /// </summary>
        private const double errex = 0.01;

        /// <summary>
        ///
        /// </summary>
        private const double dppl = 1;

        /// <summary>
        ///
        /// </summary>
        private const double dpnl = 1;

        /// <summary>
        /// </summary>
        private const double slcerr = 0.000001;

        /// <summary>
        /// default time of rainfall (hh:mm)
        /// </summary>
        private const string default_rain_time = "00:00";

        /// <summary>
        /// default duration of rainfall (min)
        /// </summary>
        private const double default_rain_duration = 1440.0;

        private const double hydrol_effective_depth = 450;

        private double[] _swf;
        private string rain_time = null;
        private double rain_durn = Double.NaN;
        private double rain_int = Double.NaN;
        private double[] SWIMRainTime = new double[0];
        private double[] SWIMRainAmt = new double[0];
        private double[] SWIMEqRainTime = new double[0];
        private double[] SWIMEqRainAmt = new double[0];
        private double[] SWIMEvapTime = new double[0];
        private double[] SWIMEvapAmt = new double[0];
        private double[][] SWIMSolTime;
        private double[][] SWIMSolAmt;
        private double[] SubSurfaceInFlow;
        private double TD_runoff;
        private double TD_rain;
        private double TD_evap;
        private double TD_pevap;
        private double TD_drain;
        private double TD_subsurface_drain;
        private double[] TD_soldrain;
        private double[] TD_slssof;
        private double[] TD_wflow;
        private double[][] TD_sflow;
        private double t;
        private double _dt;
        private double _wp;
        private double[] _p;
        private double[] _psi = null;
        private double[] th = null;
        private double[] thold;
        private double[] hk;
        private double[] q;
        private double _h;
        private double hold;
        private double ron;
        private double roff;
        private double res;
        private double resp;
        private double rex;
        private double rssf;
        private double[] qs;
        private double[] qex;
        private double[] qexpot;
        private double[] qssif;
        private double[] qssof;
        private double[][] dc;
        private double[][] csl;
        private double[][] cslt;
        private double[][] qsl;
        private double[][] qsls;
        private double[] slsur;
        private double[] cslsur;
        private double[] rslon;
        private double[] rsloff;
        private double[] rslex;
        private bool[][] demand_is_met;
        private int[] solute_owners;
        private double _work;
        private double slwork;
        private double _hmin = 0;
        private double gsurf;
        private double initial_conductance = Double.NaN;
        private int day;
        private int year;
        private double apsim_timestep = 1440.0;
        private int start_day;
        private int start_year;
        private int _apsimTimeMinutes = 0;
        private bool run_has_started;
        private double[] psimin;
        private double[] rtp;
        private double[] rt;
        private double[] ctp;
        private double[] ct;
        private double[][] qr;
        private double[][] qrpot;
        private string[] crop_names;
        private string[] crop_owners;
        private int[] crop_owner_id;
        private bool[] crop_in;
        private bool[] demand_received;
        private int num_crops;
        private int[] supply_event_id;  // Indicates the event number for sending CohortWaterSupply
        private int[] uptake_water_id; // Property number for returning crop water uptake
        private int[][] supply_solute_id; // Property number for returning crop solute supply
        private int[] leach_id;
        private int[] flow_id;
        private int[] exco_id;
        private int[] conc_water_id;
        private int[] conc_adsorb_id;
        private int[] subsurface_drain_id;
        private int nveg = 0;
        private double[][] RootRadius; // Was root_radius
        private double[][] RootConductance; // was root_conductance
        private double[] pep;
        private double[][] solute_demand;
        private double[] canopy_height;
        private double[] cover_tot;
        private double crop_cover = 0;
        private double residue_cover;
        private double _cover_green_sum;
        private double _cover_surface_runoff;
        private double qbp;
        private double[] qslbp;
        private double gf;
        private double[] swta;
        private double[][][] psuptake;
        private double[][] pwuptake;
        private double[][] pwuptakepot;
        private double[][] cslold;
        private double[][] cslstart;
        private double[] _psix;
        private double CN_runoff;
        private HyProps HP = new HyProps();
        private int ifirst = 0;
        private int ilast = 0;
        private double gr = 0.0;
        private string evap_source = "calc";
        private string echo_directives = null;
        private double[] c;
        private double[] k;
        private int isbc = 0; // No storage of water on soil surface
        private int itbc = 0; // Infinite surface conductance
        private int ibbc;
        private string[] solute_names;
        private int num_solutes = 0;
        private double _hm0;
        private double hm0 = 0;
        private double minimum_surface_storage = Double.NaN;
        private double _hm1;
        private double hm1 = 0;
        private double maximum_surface_storage = Double.NaN;
        private double _hrc;
        private double hrc = 0;
        private double precipitation_constant = Double.NaN;
        private double roff0 = 0;
        private double roff1 = 0;
        private double _g0;
        private double g0 = Double.NaN;
        private double minimum_conductance = Double.NaN;
        private double _g1;
        private double g1 = Double.NaN;
        private double maximum_conductance = Double.NaN;
        private double _grc;
        private double grc = 0;
        private double[][] ex;
        private double[] cslgw;
        private double[] slupf;
        private double[] slsci;
        private double[] slscr;
        private double[] dcon;
        private double[][] fip;
        private double bbc_value;
        private double[] init_psi;
        private double[][] exco;
        private int n;
        private double[] x;
        private double[] dx;
        private bool initDone = false;
        private double[] reset_psi = null;
        private double[] reset_theta = null;
        private double[] slos;
        private double[] d0;
        private bool cover_effects = true; // When true, the effect of residue and canopy cover is implemented as in the soilwat model.

        /// <summary>Base soil albedo</summary>
        [Description("Bare soil albedo")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        public double Salb { get; set; }

        /// <summary>Bare soil runoff curve number</summary>
        [Description("Bare soil runoff curve number")]
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double CN2Bare { get; set; }

        /// <summary>Max. reduction in curve number due to cover</summary>
        [Description("Max. reduction in curve number due to cover")]
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double CNRed { get; set; }

        /// <summary>Cover for max curve number reduction</summary>
        [Description("Cover for max curve number reduction")]
        [Bounds(Lower = 0.0, Upper = 100.0)]
        public double CNCov { get; set; }

        /// <summary>Hydraulic conductivity at DUL</summary>
        [Description("Hydraulic conductivity at DUL (mm/d)")]
        [Units("mm/d")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        public double KDul { get; set; }

        /// <summary>Matric Potential at DUL.</summary>
        [Description("Matric Potential at DUL (cm)")]
        [Units("cm")]
        [Bounds(Lower = -1e3, Upper = 0.0)]
        public double PSIDul { get; set; }

        /// <summary>Vapour Conductivity Calculations.</summary>
        [Description("Vapour Conductivity Calculations?")]
        public bool VC { get; set; }

        /// <summary>Minimum Timestep.</summary>
        [Description("Minimum Timestep (min)")]
        [Units("min")]
        [Bounds(Lower = 0.0, Upper = 1440.0)]
        public double DTMin { get; set; }

        /// <summary>Maximum Timestep.</summary>
        [Description("Maximum Timestep (min)")]
        [Units("min")]
        [Bounds(Lower = 0.01, Upper = 1440.0)]
        public double DTMax { get; set; }

        /// <summary>Maximum water increment.</summary>
        [Description("Maximum water increment (mm)")]
        [Units("mm")]
        public double MaxWaterIncrement { get; set; }

        /// <summary>Space weighting factor.</summary>
        [Description("Space weighting factor")]
        public double SpaceWeightingFactor { get; set; }

        /// <summary>Solute space weighting factor.</summary>
        [Description("Solute space weighting factor")]
        public double SoluteSpaceWeightingFactor { get; set; }

        /// <summary>Dispersivity.</summary>
        [Description("Dispersivity((cm^2/h)/(cm/h)^p)")]
        [Units("(cm^2/h)/(cm/h)^p")]
        public double Dis { get; set; }

        /// <summary>Dispersivity Power.</summary>
        [Description("Dispersivity Power")]
        public double Disp { get; set; }

        /// <summary>Tortuosity Constant.</summary>
        [Description("Tortuosity Constant")]
        public double A { get; set; }

        /// <summary>Tortuoisty Offset.</summary>
        [Description("Tortuoisty Offset")]
        public double DTHC { get; set; }

        /// <summary>Tortuoisty Power.</summary>
        [Description("Tortuoisty Power")]
        public double DTHP { get; set; }

        /// <summary>Internal representation of eo_time as minute of day.</summary>
        private int _eoTimeMinutes = 360;

        /// <summary>Time of evaporation (hh:mm).</summary>
        [Description("Time of evaporation (hh:mm). Default: 06:00")]
        public string eo_time
        {
            get => $"{_eoTimeMinutes / 60:D2}:{_eoTimeMinutes % 60:D2}";
            set => _eoTimeMinutes = TimeToMins(value);
        }

        /// <summary>Duration of evaporation (min).</summary>
        [Description("Duration of evaporation (min). Default: 720")]
        public double eo_durn { get; set; } = 720;

        /// <summary>Show diagnostic information?</summary>
        [Description("Diagnostic Information?")]
        public bool Diagnostics { get; set; }

        /// <summary>Theta</summary>
        [JsonIgnore]
        [Units("cm^3/cm^3")]
        public double[] Theta
        {
            get
            {
                return th;
            }
            set
            {
                th = value;
                reset_theta = th;
            }
        }

        ///<summary>Volumetric water content</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double[] SW { get { return th; } set { th = value; } }

        ///<summary>Water content</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] SWmm { get { return MathUtilities.Multiply(th, physical.Thickness); } }

        /// <summary>Plant available water SW-LL15.</summary>
        [Units("mm/mm")]
        public double[] PAW => APSIM.Shared.APSoil.APSoilUtilities.CalcPAWC(physical.Thickness,
                                                                            physical.LL15,
                                                                            SW,
                                                                            null);

        /// <summary>Plant available water SW-LL15.</summary>
        [Units("mm")]
        public double[] PAWmm => MathUtilities.Multiply(PAW, physical.Thickness);

        ///<summary>Extractable soil water relative to LL15</summary>
        [JsonIgnore]
        [Units("mm")]
        public double[] ESW
        {
            get
            {
                double[] value = new double[n + 1];
                for (int i = 0; i <= n; i++)
                    value[i] = Math.Max(0.0, (th[i] - physical.LL15[i]) * physical.Thickness[i]);
                return value;
            }
        }

        /// <summary>Amount of rainfall intercepted by crop and residue canopies</summary>
        [JsonIgnore]
        public double PrecipitationInterception { get; set; }

        /// <summary>Water potential of layer</summary>
        [JsonIgnore]
        [Units("cm")]
        public double[] PSI
        {
            get
            {
                return _psi;
            }
            set
            {
                if (initDone)
                    ResetWaterBalance(2, ref value);
                else
                {
                    _psi = value;
                    reset_psi = _psi;
                }
            }
        }

        /// <summary>Water potential of layer</summary>
        [JsonIgnore]
        [Units("cm/h")]
        public double[] K
        {
            get
            {
                double[] k = new double[n+1];

                for (int i = 0; i <= n; i++)
                    k[i] = HP.SimpleK(i, _psi[i], physical.SAT, physical.KS);
                return k;
            }
        }


        ///<summary>Pore Interaction Index for shape of the K(theta) curve for soil hydraulic conductivity</summary>
        [JsonIgnore]
        [Units("-")]
        public double[] PoreInteractionIndex
        {
            get
            {
                return HP.PoreInteractionIndex;
            }
            set
            {
                HP.PoreInteractionIndex = value;
                HP.SetupKCurve(n, physical.LL15, physical.DUL, physical.SAT, physical.KS, KDul, PSIDul);
            }
        }

        /// <summary>
        /// Soil water potential including solute concentration effects. Not currently active.
        /// Maybe useful in the future for salinity effects on plant water uptake.
        /// </summary>
        [Units("cm")]
        private double[] psio
        {
            get
            {
                double[] value = new double[n + 1];
                for (int i = 0; i <= n; i++)
                {
                    value[i] = _psi[i];
                    for (int solnum = 0; solnum < slos.Length; solnum++)
                        value[i] -= slos[solnum] * csl[solnum][i];
                }
                return value;
            }
        }

        /// <summary>Water runoff</summary>
        [Units("mm")]
        public double Runoff => TD_runoff;

        /// <summary>Surface cover effects on runoff curve number reduction.</summary>
        [Units("0-1")]
        public double CoverSurfaceRunoff => _cover_surface_runoff;

        /// <summary>Water infiltration (rainfall and irrigation) into the surface layer.</summary>
        [Units("mm")]
        public double Infiltration => TD_wflow[0];

        /// <summary>Actual (realised) soil water evaporation</summary>
        [Units("mm")]
        public double Es => TD_evap;

        ///<summary>Potential evaporation from soil surface</summary>
        [JsonIgnore]
        [Units("mm")]
        public double Eos => TD_pevap;

        /// <summary>Water drainage from bottom of profile</summary>
        [JsonIgnore]
        [Units("mm")]
        public double Drainage => TD_drain;

        /// <summary>Potential evapotranspiration of the whole soil-plant system</summary>
        [JsonIgnore]
        [Units("mm")]
        public double Eo { get; set; }

        /// <summary>Amount of water moving upward from each soil layer during unsaturated flow (negative value means downward movement)</summary>
        [JsonIgnore]
        [Units("kg/ha")]
        public double[] Flow
        {
            get
            {
                // Flow represents flow downward out of a layer
                // and so start at node 1 (not 0)
                double[] value = new double[n];
                for (int i = 1; i < n; i++)
                    value[i] = TD_wflow[i];
                return value;
            }
        }

        /// <summary>Pond depth.</summary>
        [Units("mm")]
        public double Pond => _h * 10.0;

        /// <summary>Subsurface drain.</summary>
        [Units("mm")]
        public double SubsurfaceDrain => TD_subsurface_drain;

        /// <summary>Water table depth (mm)</summary>
        [JsonIgnore]
        [Units("mm")]
        public double WaterTable
        {
            get
            {
                return CalculateWaterTable();
            }
            set
            {
                throw new NotImplementedException("Cannot set water table in SWIM");
            }
        }

        /// <summary>Rainfall less than intercepted by the canopy and residue components (Set by Microclimate).</summary>
        [JsonIgnore]
        public double PotentialInfiltration { get; set; }

        ///<summary>Soil thickness for each layer (mm)(</summary>
        [JsonIgnore]
        public double[] Thickness { get { return physical.Thickness; } }

        /// <summary>Amount of water moving laterally out of the profile (mm)</summary>
        [JsonIgnore]
        public double[] LateralOutflow { get { throw new NotImplementedException("SWIM doesn't implement a LateralOutflow property"); } }

        /// <summary>NO3 movement out of a layer. </summary>
        public double[] FlowNO3 => TD_sflow[SoluteIndex("NO3")];

        /// <summary>NH4 movement out of a layer. </summary>
        public double[] FlowNH4 => TD_sflow[SoluteIndex("NH4")];

        /// <summary>NH4 movement out of a layer. </summary>
        public double[] FlowUrea => TD_sflow[SoluteIndex("Urea")];

        /// <summary>CL movement out of a layer. </summary>
        public double[] FlowCl => TD_sflow[SoluteIndex("Cl")];

        /// <summary>NO3 movement out of a sub surface drain. </summary>
        public double SubsurfaceDrainNO3 => TD_slssof[SoluteIndex("NO3")];

        /// <summary>NH4 movement out of a sub surface drain. </summary>
        public double SubsurfaceDrainNH4 => TD_slssof[SoluteIndex("NH4")];

        /// <summary>NH4 movement out of a sub surface drain. </summary>
        public double SubsurfaceDrainUrea => TD_slssof[SoluteIndex("Urea")];

        /// <summary>CL movement out of a sub surface drain. </summary>
        public double SubsurfaceDrainCL => TD_slssof[SoluteIndex("CL")];

        /// <summary>NO3 leached from the bottom of the profile.</summary>
        public double LeachNO3 => TD_soldrain[SoluteIndex("NO3")];

        /// <summary>NH4 leached from the bottom of the profile.</summary>
        public double LeachNH4 => TD_soldrain[SoluteIndex("NH4")];

        /// <summary>Urea leached from the bottom of the profile.</summary>
        public double LeachUrea => TD_soldrain[SoluteIndex("Urea")];

        /// <summary>CL leached from the bottom of the profile.</summary>
        public double LeachCl => TD_soldrain[SoluteIndex("Cl")];

        /// <summary>Amount of NO3 not adsorbed (ppm).</summary>
        public double[] ConcWaterNO3 => ConcWaterSolute(SoluteIndex("NO3"));

        /// <summary>Amount of NH4 not adsorbed (ppm).</summary>
        public double[] ConcWaterNH4 => ConcWaterSolute(SoluteIndex("NH4"));

        /// <summary>Amount of Urea not adsorbed (ppm).</summary>
        public double[] ConcWaterUrea => ConcWaterSolute(SoluteIndex("Urea"));

        /// <summary>Amount of CL not adsorbed (ppm).</summary>
        public double[] ConcWaterCl => ConcWaterSolute(SoluteIndex("Cl"));

        /// <summary>Amount of water moving downward out of each soil layer due to gravity drainage (above DUL) (mm)</summary>
        [JsonIgnore]
        public double[] Flux { get { throw new NotImplementedException("SWIM doesn't implement a Flux property"); } }

        /// <summary>The efficiency (0-1) that solutes move down with water.</summary>
        [JsonIgnore]
        public double[] SoluteFluxEfficiency { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>The efficiency (0-1) that solutes move up with water.</summary>
        [JsonIgnore]
        public double[] SoluteFlowEfficiency { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        // In the Fortran version, the data for ponding water was held in
        // array members with an index of -1.
        // In this version, I've created this structure to hold those values.
        // Note, however, that SWIM3 in APSIM never allowed the user to
        // set the value for isbc, which controls the way ponding is handled;
        // as a consequence, this version of the logic remains untested.
        private struct PondingData
        {
            public double b;
            public double c;
            public double rhs;
            public double v;
        };

        /// <summary>
        /// Runoff calculated by curve number - no ponding allowed.
        /// </summary>
        public void SetSurfaceBCForCurveNumber()
        {
            isbc = 0;
        }

        /// <summary>
        /// Runoff calculated by a power function.
        /// </summary>
        /// <param name="minimumSurfaceStorage">Minimum surface storage (mm).</param>
        /// <param name="maximumSurfaceStorage">Maximum surface storage (mm).</param>
        /// <param name="initialSurfaceStorage">Initial surface storage (mm).</param>
        /// <param name="precipitationConstant">Precipitation constant (mm).</param>
        /// <param name="runoffRateFactor">Runoff rate factor (mm/mm^p).</param>
        /// <param name="runoffRatePower">Runoff rate power ().</param>
        public void SetSurfaceBCForPowerFunction(double minimumSurfaceStorage, double maximumSurfaceStorage,
                                                 double initialSurfaceStorage, double precipitationConstant,
                                                 double runoffRateFactor, double runoffRatePower)
        {
            isbc = 2;
            minimum_surface_storage = minimumSurfaceStorage;
            maximum_surface_storage = maximumSurfaceStorage;
            _hmin = initialSurfaceStorage;
            precipitation_constant = precipitationConstant;
            roff0 = runoffRateFactor;
            roff1 = runoffRatePower;
        }

        /// <summary>
        /// Set the lower boundary condition for gradient.
        /// </summary>
        /// <param name="bbcGradient">Bottom boundary condition (cm).</param>
        public void SetLowerBCForGradient(double bbcGradient)
        {
            bbc_value = bbcGradient;
            if (ibbc != 0)
            {
                ibbc = 0;
                summary.WriteMessage(this, "Bottom boundary condition now constant gradient", MessageType.Information);
            }
        }

        /// <summary>
        /// Set the constant potential bottom boundary.
        /// </summary>
        /// <param name="bbcPotential">Constant potential bottom boundary (cm).</param>
        public void SetLowerBCForGivenPotential(double bbcPotential)
        {
            bbc_value = bbcPotential;
            if (ibbc != 1)
            {
                ibbc = 1;
                summary.WriteMessage(this, "Bottom boundary condition now constant potential", MessageType.Information);
            }
        }

        /// <summary>
        /// Set the constant potential bottom boundary.
        /// </summary>
        /// <param name="bbcPotentialSeepage">Constant potential bottom boundary (cm).</param>
        public void SetLowerBCForSeepage(double bbcPotentialSeepage)
        {
            bbc_value = bbcPotentialSeepage;
            if (ibbc != 3)
            {
                ibbc = 3;
                summary.WriteMessage(this, "Bottom boundary condition now a seepage potential", MessageType.Information);
            }
        }

        /// <summary>
        /// Set the top boundary condition for infinite surface conductance.
        /// </summary>
        public void SetTopBCForInfiniteSurfaceConductance()
        {
            itbc = 0;
        }

        /// <summary>
        /// Set the top boundary condition for constant potential.
        /// </summary>
        public void SetTopBCForConstantPotential()
        {
            itbc = 1;
        }

        /// <summary>
        /// Set the top boundary condition for conductance function.
        /// </summary>
        /// <param name="minimumConductance">Minimum conductance (/h).</param>
        /// <param name="maximumConductance">Maximum conductance (/h).</param>
        /// <param name="initialConductance">Initial conductance (/h).</param>
        /// <param name="precipitationConstant">Precipitation constant (cm).</param>
        public void SetTopBCForConductanceFunction(double minimumConductance, double maximumConductance,
                                                   double initialConductance, double precipitationConstant)
        {
            itbc = 2;
            minimum_conductance = minimumConductance;
            maximum_conductance = maximumConductance;
            initial_conductance = initialConductance;
            precipitation_constant = precipitationConstant;
        }


        /// <summary>
        /// Reset the model
        /// </summary>
        public void Reset()
        {
            ZeroVariables();
            GetOtherVariables();
            InitDefaults();
            ReadParam();
            ReadSoluteParams();
            InitCalc();
            CheckInputs();
        }

        /// <summary>
        /// Perform tillage
        /// </summary>
        /// <param name="Tillage"></param>
        public void Tillage(TillageType Tillage)
        {
            if (echo_directives != null && echo_directives.Trim() == "on")
                summary.WriteMessage(this, "APSwim responding to tillage", MessageType.Diagnostic);
            // THIS ISN'T RIGHT.
            // I think the values for hm1, hm0, etc, are meant to be recovered from the event data,
            // but they aren't fields in the current TillageType structure.
            if (!Double.IsNaN(hm1))
                _hm1 = hm1 / 10.0;
            if (!Double.IsNaN(hm0))
                _hm0 = hm0 / 10.0;
            if (!Double.IsNaN(hrc))
                _hrc = hrc / 10.0;
            // Now set current storage to max storage
            _hmin = _hm1;
            if (!Double.IsNaN(g1))
                _g1 = g1;
            if (!Double.IsNaN(g0))
                _g0 = g0;
            if (!Double.IsNaN(grc))
                _grc = grc / 10.0;
            //  Now set current surface conductance to max
            gsurf = g1;
        }

        /// <summary>
        ///
        /// </summary>
        public void Sum_Report()
        {
            //Manager module can request that each module write its variables out to the summary file. This handles that event.

            //+  Purpose
            //   Report all initial conditions and input parameters to the
            //   summary file.

            //+  Constant Values
            const int num_psio = 13;

            double[,] tho = new double[n + 1, num_psio];
            double[,] hklo = new double[n + 1, num_psio];
            double[,] hko = new double[n + 1, num_psio];
            double thd;
            double hklgd;

            //+  Initial Data Values
            double[] psio = new double[num_psio] { -0.01, -1, -10.0, -25.0, -100.0, -330.0, -1000.0, -3000.0, -15000.0, -1.0e5, -5.0e5, -1.0e6, -6.0e6 };

            summary.WriteMessage(this, "APSIM Soil Profile", MessageType.Diagnostic);

            summary.WriteMessage(this, "---------------------------------------------------------------", MessageType.Diagnostic);
            summary.WriteMessage(this, " x    dlayer   BD   SW     LL15   DUL   SAT      Ks      Psi", MessageType.Diagnostic);
            summary.WriteMessage(this, "---------------------------------------------------------------", MessageType.Diagnostic);

            for (int layer = 0; layer <= n; layer++)
            {
                summary.WriteMessage(this, String.Format("{0,6:F1} {1,6:F1}  {2,4:F3}  {3,5:F3}  {4,5:F3}  {5,5:F3}  {6,5:F3} {7,6:F2} {8,8:F2}",
                                           x[layer] * 10.0,
                                           physical.Thickness[layer], physical.BD[layer], th[layer],
                                           physical.LL15[layer], physical.DUL[layer], physical.SAT[layer], physical.KS[layer],
                                           _psi[layer]), MessageType.Diagnostic);

            }
            summary.WriteMessage(this, "---------------------------------------------------------------", MessageType.Diagnostic);

            // calculate Theta and g%hk for each psio

            for (int i = 0; i < num_psio; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    Interp(j, psio[i], out tho[j, i], out thd, out hklo[j, i], out hklgd);
                    hko[j, i] = Math.Pow(10.0, hklo[j, i]);
                }
            }

            summary.WriteMessage(this, "Soil Moisture Characteristics (volumetric water content)", MessageType.Diagnostic);
            summary.WriteMessage(this, "----------------------------------------------------------------------------------------------------", MessageType.Diagnostic);
            summary.WriteMessage(this, "                         Soil Water Potential (cm)", MessageType.Diagnostic);
            summary.WriteMessage(this, "    x       0       1      10      25    100    330   1000   3000  15000  1.0e5  5.0e5  1.0e6  6.0e6", MessageType.Diagnostic);
            summary.WriteMessage(this, "----------------------------------------------------------------------------------------------------", MessageType.Diagnostic);

            for (int j = 0; j <= n; j++)
            {
                summary.WriteMessage(this, String.Format("{0,6:F1} | {1,6:F4} {2,6:F4} {3,6:F4} {4,6:F4} {5,6:F4} {6,6:F4} {7,6:F4} {8,6:F4} {9,6:F4} {10,6:F4} {11,6:F4} {12,6:F4} {13,6:F4}",
                    x[j] * 10.0, tho[j, 0], tho[j, 1], tho[j, 2], tho[j, 3], tho[j, 4], tho[j, 5], tho[j, 6], tho[j, 7], tho[j, 8], tho[j, 9], tho[j, 10], tho[j, 11], tho[j, 12]), MessageType.Diagnostic);
            }

            summary.WriteMessage(this, "----------------------------------------------------------------------------------------------------", MessageType.Diagnostic);
            summary.WriteMessage(this, "Soil Hydraulic Conductivity", MessageType.Diagnostic);
            summary.WriteMessage(this, "----------------------------------------------------------------------------------------------------", MessageType.Diagnostic);
            summary.WriteMessage(this, "                         Soil Water Potential (cm)", MessageType.Diagnostic);
            summary.WriteMessage(this, "    x       0       1      10      25    100    330   1000   3000  15000  1.0e5  5.0e5  1.0e6  6.0e6", MessageType.Diagnostic);
            summary.WriteMessage(this, "----------------------------------------------------------------------------------------------------", MessageType.Diagnostic);

            for (int j = 0; j <= n; j++)
            {
                summary.WriteMessage(this, String.Format("{0,6:F1} | {1,8:G3} {2,8:G3} {3,8:G3} {4,8:G3} {5,8:G3} {6,8:G3} {7,8:G3} {8,8:G3} {9,8:G3} {10,8:G3} {11,8:G3} {12,8:G3} {13,8:G3}",
                    x[j] * 10.0, hko[j, 0] * 24.0 * 10.0, hko[j, 1] * 24.0 * 10.0, hko[j, 2] * 24.0 * 10.0, hko[j, 3] * 24.0 * 10.0,
                                 hko[j, 4] * 24.0 * 10.0, hko[j, 5] * 24.0 * 10.0, hko[j, 6] * 24.0 * 10.0, hko[j, 6] * 24.0 * 10.0 , hko[j, 7] * 24.0 * 10.0 , hko[j, 8] * 24.0 * 10.0 , hko[j, 9] * 24.0 * 10.0 , hko[j, 10] * 24.0 * 10.0 , hko[j, 11] * 24.0 * 10.0, hko[j, 12] * 24.0 * 10.0), MessageType.Diagnostic);
            }

            summary.WriteMessage(this, "-----------------------------------------------------------------------", MessageType.Diagnostic);
            summary.WriteMessage(this, Environment.NewLine, MessageType.Diagnostic);

            if (ibbc == 0)
                summary.WriteMessage(this, String.Format("     bottom boundary condition = specified gradient ({0,10:F3})",
                                  bbc_value), MessageType.Diagnostic);
            else if (ibbc == 1)
                summary.WriteMessage(this, "     bottom boundary condition = specified potential", MessageType.Diagnostic);
            else if (ibbc == 2)
                summary.WriteMessage(this, "     bottom boundary condition = zero flux", MessageType.Diagnostic);
            else if (ibbc == 3)
                summary.WriteMessage(this, "     bottom boundary condition = free drainage", MessageType.Diagnostic);
            else
                throw new Exception("bad bottom boundary conditions switch");

            summary.WriteMessage(this, "     Evaporation Source        = " + evap_source + Environment.NewLine, MessageType.Diagnostic);

            if (subsurfaceDrain != null)
            {
                summary.WriteMessage(this, " Subsurface Drain Model", MessageType.Diagnostic);
                summary.WriteMessage(this, " ======================" + Environment.NewLine, MessageType.Diagnostic);

                summary.WriteMessage(this, string.Format("     Drain Depth (mm) ={0,10:F3}", subsurfaceDrain.DrainDepth), MessageType.Diagnostic);
                summary.WriteMessage(this, string.Format("     Drain Spacing (mm) ={0,10:F3}", subsurfaceDrain.DrainSpacing), MessageType.Diagnostic);
                summary.WriteMessage(this, string.Format("     Drain Radius (mm) ={0,10:F3}", subsurfaceDrain.DrainRadius), MessageType.Diagnostic);
                summary.WriteMessage(this, string.Format("     Imperm Layer Depth (mm)  ={0,10:F3}", subsurfaceDrain.ImpermDepth), MessageType.Diagnostic);
                summary.WriteMessage(this, string.Format("     Lateral Conductivity (mm/d)  ={0,10:F3}", subsurfaceDrain.Klat), MessageType.Diagnostic);
            }

        }


        ///<summary>Remove water from the profile</summary>
        public void RemoveWater(double[] dlt_sw_dep)
        {
            if (MathUtilities.Sum(dlt_sw_dep) > 0)
            {
                // convert to volumetric
                double[] newSW = MathUtilities.Divide(dlt_sw_dep, physical.Thickness, divideTolerance);
                newSW = MathUtilities.Subtract(th, newSW);
                ResetWaterBalance(1, ref newSW);
                run_has_started = false;
            }
        }

        /// <summary>Sets the water table.</summary>
        /// <param name="InitialDepth">The initial depth.</param>
        public void SetWaterTable(double InitialDepth)
        {
            bbc_value = x[n] - InitialDepth / 10.0;
            ibbc = 1;
        }

        ///<summary>Perform tillage</summary>
        public void Tillage(string tillageType)
        {
            throw new NotImplementedException("SWIM doesn't implement a tillage method");
        }

        /// <summary>
        /// Start of simulation event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnInitialised(object sender, EventArgs e)
        {
            // If curve number is zero then allow ponding
            if (CN2Bare == 0)
                isbc = 1;

            ErrorChecking();
            Reset();
            initDone = true;
            Sum_Report();
        }

        /// <summary>
        /// Start of simulation event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            //+  Purpose
            //     Update internal time record and reset daily state variables.
            if (initDone)
            {
                day = clock.Today.DayOfYear;
                year = clock.Today.Year;


                // dph - need to setup g%apsim_time and g%apsim_timestep
                //call handler_ONtick(g%day, g%year, g%apsim_time ,intTimestep)
                //g%apsim_timestep = intTimestep

                // NIH - assume daily time step for now until someone needs to
                //       do otherwise.
                _apsimTimeMinutes = 0;
                apsim_timestep = 1440;

                // Started new timestep so purge all old timecourse information
                // ============================================================

                double start_timestep = Time(year, day, _apsimTimeMinutes);

                PurgeLogInfo(start_timestep, ref SWIMRainTime, ref SWIMRainAmt);

                PurgeLogInfo(start_timestep, ref SWIMEvapTime, ref SWIMEvapAmt);

                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    int numPairs = SWIMSolTime[solnum].Length;
                    double[] TEMPSolTime = new double[numPairs];
                    double[] TEMPSolAmt = new double[numPairs];
                    for (int counter = 0; counter < numPairs; counter++)
                    {
                        TEMPSolTime[counter] = SWIMSolTime[solnum][counter];
                        TEMPSolAmt[counter] = SWIMSolAmt[solnum][counter];
                    }

                    PurgeLogInfo(start_timestep, ref TEMPSolTime, ref TEMPSolAmt);

                    numPairs = TEMPSolTime.Length;
                    Array.Resize(ref SWIMSolTime[solnum], numPairs);
                    Array.Resize(ref SWIMSolAmt[solnum], numPairs);
                    for (int counter = 0; counter < TEMPSolTime.Length; counter++)
                    {
                        SWIMSolTime[solnum][counter] = TEMPSolTime[counter];
                        SWIMSolAmt[solnum][counter] = TEMPSolAmt[counter];
                    }

                    // Zero the amount solute lost in runoff array
                    Array.Clear(solutes[solnum].AmountLostInRunoff, 0, Thickness.Length);
                }

                SubSurfaceInFlow = new double[n + 1];
            }
        }

        /// <summary>Called when an irrigation occurs.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="Irrigated">The event data.</param>
        [EventSubscribe("Irrigated")]
        private void OnIrrigated(object sender, IrrigationApplicationType Irrigated)
        {
            //+  Assumptions
            //   That g%day and g%year have already been updated before entry into this
            //   routine. e.g. Prepare stage executed already.

            //+  Changes
            //   neilh - 19-01-1995 - Programmed and Specified
            //   neilh - 28-05-1996 - Added call to get_other_variables to make
            //                        sure g%day and g%year are up to date.
            //      21-06-96 NIH Changed extract calls to collect calls
            //   neilh - 22-07-1996 removed data_String from arguments
            //   neilh - 29-08-1997 added test for whether directives are to be echoed

            if (echo_directives != null && echo_directives.Trim() == "on")
            {
                // flag this event in output file
                summary.WriteMessage(this, "APSwim adding irrigation to log", MessageType.Diagnostic);
            }

            double amount = Irrigated.Amount;
            double duration = Irrigated.Duration;

            // get information regarding time etc.
            GetOtherVariables();

            double irrigation_time = Time(year, day, 0);

            // allow 1 sec numerical error as data resolution is
            // 60 sec.
            if (irrigation_time < (t - 1.0 / 3600.0))
                throw new Exception("Irrigation has been specified for an already processed time period");


            InsertLoginfo(irrigation_time, duration, amount, ref SWIMRainTime, ref SWIMRainAmt);

            RecalcEqrain();

            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                double solconc = 0.0;
                if (solute_names[solnum].Equals("no3", StringComparison.InvariantCultureIgnoreCase))
                    solconc = Irrigated.NO3;
                else if (solute_names[solnum].Equals("nh4", StringComparison.InvariantCultureIgnoreCase))
                    solconc = Irrigated.NH4;
                else if (solute_names[solnum].Equals("cl", StringComparison.InvariantCultureIgnoreCase))
                    solconc = Irrigated.CL;

                if (solconc > 0.0)
                {
                    int numPairs = SWIMSolTime[solnum].Length;
                    double[] TEMPSolTime = new double[numPairs];
                    double[] TEMPSolAmt = new double[numPairs];
                    for (int counter = 0; counter < numPairs; counter++)
                    {
                        TEMPSolTime[counter] = SWIMSolTime[solnum][counter];
                        TEMPSolAmt[counter] = SWIMSolAmt[solnum][counter];
                    }
                    InsertLoginfo(irrigation_time, duration, solconc, ref TEMPSolTime, ref TEMPSolAmt);

                    int nPairs = TEMPSolTime.Length;
                    Array.Resize(ref SWIMSolTime[solnum], nPairs);
                    Array.Resize(ref SWIMSolAmt[solnum], nPairs);

                    for (int counter = 0; counter < nPairs; counter++)
                    {
                        SWIMSolTime[solnum][counter] = TEMPSolTime[counter];
                        SWIMSolAmt[solnum][counter] = TEMPSolAmt[counter];
                    }
                }
            }
        }

        /// <summary>
        /// Start of simulation event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoSoilWaterMovement")]
        private void OnDoSoilWaterMovement(object sender, EventArgs e)
        {
            //  Purpose
            //    Perform calculations before the current timestep.
            GetOtherVariables();
            GetRainVariables();
            RecalcEqrain();
            if (evap_source != null && evap_source.Trim() == "calc")
                CalcEvapVariables();
            else
                GetObsEvapVariables();
            OnProcess();

            // Update the variable in the water model.
            water.Volumetric = SW;
        }

        private void ErrorChecking()
        {
            if (physical.KS == null
                || !MathUtilities.ValuesInArray(physical.KS)
                || physical.KS.All(ks => ks == 0))
                throw new ApsimXException(this, "KS not provided. Check Soil.Physical configuration");
        }

        private void OnProcess()
        {
            //  Purpose
            //      Perform actions for current g%day.

            //  Notes
            //       The method of limiting timestep to rainfall data will mean that
            //       insignificantly small rainfall events could tie up processor time
            //       for limited gain in precision.  We may need to adress this later
            //       by enabling two small rainfall periods to be summed to create
            //       one timestep instead of two.
            ResetDailyTotals();
            GetOtherVariables();
            GetSoluteVariables();
            GetCropVariables();
            residue_cover = surfaceOrganicMatter.Cover;
            CNRunoff();

            double timestepStart = Time(year, day, _apsimTimeMinutes);
            double timestep = apsim_timestep / 60.0;

            bool fail = DoSwim(timestepStart, timestep);
            if (fail)
            {
                ReportStatus();
                throw new Exception("Swim failed to find solution");
            }
            else
            {
                //````````````````````````````````````````````````````````````````````````````````
                //Test for negative values of g%csl coming from the thomas algorithm.
                //! disregard if very small, else cause fatal error
                //RC            Changes by RCichota, 30/Jan/2010, ammended in 10/Jul/2010

                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    for (int node = 0; node <= n; node++)
                    {
                        if (Math.Abs(csl[solnum][node]) < -slcerr)
                        {
                            // value is negative, throw a fatal error (using same error limit as for thomas algorithm, may need to look at this better)

                            string mess = String.Format("  Solution {0}({1,3}) = {2,12:G6}",
                                         solute_names[solnum],
                                         node,
                                         csl[solnum][node]);
                            throw new Exception("-ve value when calculating solute movement" + Environment.NewLine + mess);

                            //csl[solnum][node] = 0.0;
                        }
                        else if (csl[solnum][node] < 1e-100)
                        {
                            // Value is REALLY small, set to zero to avoid underflow with reals
                            csl[solnum][node] = 0.0;
                        }
                        // else value is OK. proceed with calculations
                    }
                }

                //````````````````````````````````````````````````````````````````````````````````

                SetOtherVariables();
                SetSoluteVariables();
            }
            PublishUptakes();
        }

        private void ZeroVariables()
        {
            // This will require some thought. If we get a "reset", what values need to be re-zeroed?
        }

        private void InitDefaults()
        {
            gf = 1.0;  // gravity factor will always be one(i.e. vertical profile)

            // It would be difficult to have all solutes in surface water at
            // initialisation specified and so we will not allow surface water
            // at initialisation.
            _h = 0.0;
            //nh      g%cslsur = 0.0

            if (year == 0)
            {
                year = clock.Today.Year;
                day = clock.Today.Day;
            }
            start_day = day;
            start_year = year;

            //nh swim2 set soil surface stuff to no solute and no roughness at start
            //nh until some cultivation takes place.
            //nh      tzero = 100.*365.*24.
            //nh      g%cslsur = 0.d0 ! its an array now


            // initial surface conditions are set to initial maximums.
            //      tzero = 0.d0
            //      eqr0  = 0.d0

            // No solutes uptakes are calculated by this model
            slupf = new double[num_solutes];
        }

        private void InitCalc()
        {
            //+  Purpose
            //   Perform initial calculations from input parameters and prepare for simulation

            // change units of params to normal SWIM units
            // ie. cm and hours etc.
            InitChangeUnits();

            // ------------------- CALCULATE CURRENT TIME -------------------------
            t = Time(year, day, _apsimTimeMinutes);

            // ----------------- SET UP NODE SPECIFICATIONS -----------------------

            // safer to use number returned from read routine
            int num_layers = physical.Thickness.Length;
            if (n != num_layers - 1)
                ResizeProfileArrays(num_layers);

            for (int i = 0; i <= n; i++)
                dx[i] = physical.Thickness[i] / 10.0;

            x[0] = 0.0;
            double cumDepth = dx[0];

            for (int i = 1; i < n; i++)
            {
                x[i] = cumDepth + dx[i] / 2.0;
                cumDepth += dx[i];
            }

            x[n] = MathUtilities.Sum(dx);

            //      p%dx(0) = 0.5*(p%x(1) - p%x(0))
            //      do 10 i=1,p%n-1
            //         p%dx(i) = 0.5*(p%x(i+1)-p%x(i-1))
            //   10 continue
            //      p%dx(p%n) = 0.5*(p%x(p%n)-p%x(p%n-1))


            // ------- IF USING SIMPLE SOIL SPECIFICATION CALCULATE PROPERTIES -----



            HP.SetupThetaCurve(PSIDul, n, physical.LL15, physical.DUL, physical.SAT);
            HP.SetupKCurve(n, physical.LL15, physical.DUL, physical.SAT, physical.KS, KDul, PSIDul);

            // ---------- NOW SET THE ACTUAL WATER BALANCE STATE VARIABLES ---------

            if (th[1] != 0.0)
            {
                // water content was supplied in input file
                // so calculate matric potential
                ResetWaterBalance(1, ref th);
            }
            else
            {
                // matric potential was supplied in input file
                // so calculate water content
                ResetWaterBalance(2, ref _psi);
            }

            // Calculate Water Table If Required
            if (ibbc == 1)
            {
                bbc_value = x[n] - bbc_value / 10.0;
            }
        }

        private void CheckInputs()
        {
            // Does nothing
        }

        private void ReportStatus()
        {
            //+  Purpose
            //   Dump a series of values to output file to be used by users in
            //   determining convergence problems, etc.

            double[] t_psi = new double[n + 1];
            double[] t_th = new double[n + 1];
            double d1, d2, d3;
            for (int i = 0; i <= n; i++)
            {
                Trans(_p[i], out t_psi[i], out d1, out d2);
                Interp(i, t_psi[i], out t_th[i], out d1, out d2, out d3);
            }


            summary.WriteMessage(this, "================================", MessageType.Diagnostic);
            summary.WriteMessage(this, "     Error Report Status", MessageType.Diagnostic);
            summary.WriteMessage(this, "================================", MessageType.Diagnostic);
            summary.WriteMessage(this, String.Format("time = {0} {1} {2}", day, year, (t - _dt) % 24.0), MessageType.Diagnostic);
            summary.WriteMessage(this, String.Format("dt= {0}", _dt * 2.0), MessageType.Diagnostic);
            Console.Write("psi =");
            for (int i = 0; i <= n; i++)
                Console.Write(String.Format(" {0}", t_psi[i]));

            Console.Write("th =");
            for (int i = 0; i <= n; i++)
                Console.Write(String.Format(" {0}", t_th[i]));

            summary.WriteMessage(this, String.Format("h = {0}", _h), MessageType.Diagnostic);
            summary.WriteMessage(this, String.Format("ron ={0}", ron), MessageType.Diagnostic);
            summary.WriteMessage(this, "================================", MessageType.Diagnostic);
        }

        private void InitChangeUnits()
        {
            if (initDone)
                return;
            //+  Purpose
            //   To keep in line with APSIM standard units we input many parameters
            //   in APSIM compatible units and convert them here to SWIM compatible
            //   units.

            DTMin = DTMin / 60.0;  // convert to hours
            DTMax = DTMax / 60.0;  // convert to hours
            MaxWaterIncrement = MaxWaterIncrement / 10.0;        // convert to cm

            grc = grc / 10.0;      // convert mm to cm

            hm1 = hm1 / 10.0;      // convert mm to cm
            hm0 = hm0 / 10.0;      // convert mm to cm
            hrc = hrc / 10.0;      // convert mm to cm
            _hmin = _hmin / 10.0;    // convert mm to cm
            roff0 = roff0 * Math.Pow(10.0, roff1) / 10.0; // convert (mm/h)/mm^P to
                                                          // (cm/h)/(cm^P)

            for (int crop = 0; crop < num_crops; crop++)
                for (int i = 0; i <= n; i++)
                    RootRadius[i][crop] = RootRadius[i][crop] / 10.0;
        }

        private void ReadParam()
        {
            if (reset_theta != null)
                th = reset_theta;
            if (reset_psi != null)
                _psi = reset_psi;

            if (reset_theta != null && reset_psi != null)
                throw new Exception("Both psi and Theta have been supplied by user.");

            if (reset_theta == null && reset_psi == null)
            {
                th = water.InitialValues.Clone() as double[];
            }

            ibbc = 0;
            bbc_value = 0.0;

            if (isbc == 2) // There doesn't seem to be anywhere for isbc to acquire a value other than 0  !!!!
            {
                if (Double.IsNaN(minimum_surface_storage))
                    throw new Exception("No value provided for minimum_surface_storage");
                _hm0 = minimum_surface_storage;
                if (Double.IsNaN(maximum_surface_storage))
                    throw new Exception("No value provided for maximum_surface_storage");
                _hm1 = maximum_surface_storage;
                if (_hm1 <= _hm0)
                    throw new Exception("Minimum_surface_storage must exceed maximum_surface_storage");
                if (Double.IsNaN(_hmin))
                    throw new Exception("No value provided for initial_surface_storage");
                if (_hmin <= _hm0 || _hmin >= _hm1)
                    throw new Exception("Initial_surface_storage must lie between minimum_surface_storage and maximum_surface_storage");
                if (Double.IsNaN(precipitation_constant))
                    throw new Exception("No value provided for precipitation_constant");
                _hrc = precipitation_constant;
                if (Double.IsNaN(roff0))
                    throw new Exception("No value provided for runoff_rate_factor");
                if (Double.IsNaN(roff1))
                    throw new Exception("No value provided for runoff_rate_power");
            }
            if (itbc == 2) // Nor does there seem to be anywhere for itbc to acquire a value other than 0  !!!!
            {
                if (Double.IsNaN(minimum_conductance))
                    throw new Exception("No value provided for minimum_conductance");
                _g0 = minimum_conductance;
                if (Double.IsNaN(maximum_conductance))
                    throw new Exception("No value provided for maximum_conductance");
                _g1 = maximum_conductance;
                if (_g1 < _g0)
                    throw new Exception("Minimum_conductance must exceed maximum_conductance");
                if (Double.IsNaN(initial_conductance))
                    throw new Exception("No value provided for initial_conductance");
                gsurf = initial_conductance;
                if (gsurf < g0 || gsurf > g1)
                    throw new Exception("Initial_conductance must lie between minimum_conductance and maximum_conductance");
                if (Double.IsNaN(precipitation_constant))
                    throw new Exception("No value provided for precipitation_constant");
                _grc = precipitation_constant;
            }

            if (subsurfaceDrain != null)
            {
                if (Double.IsNaN(subsurfaceDrain.DrainSpacing))
                    throw new Exception("No value provided for drainspacing");
                if (Double.IsNaN(subsurfaceDrain.DrainRadius))
                    throw new Exception("No value provided for drainradius");
                if (Double.IsNaN(subsurfaceDrain.ImpermDepth))
                    throw new Exception("No value provided for impermdepth");
                if (subsurfaceDrain.ImpermDepth < subsurfaceDrain.DrainDepth)
                    throw new Exception("Impermdepth must exceed draindepth");
                if (Double.IsNaN(subsurfaceDrain.Klat))
                    throw new Exception("No value provided for Klat");
            }
        }

        //private void RegisterCropOutputs(int vegnum)
        //{
        //    //+  Purpose
        //    //     Register any crop related output variables

        //    string cropname;
        //    if (supply_event_id[vegnum] == 0)
        //        cropname = crop_names[vegnum];
        //    else
        //        cropname = crop_owners[vegnum] + "_" + crop_names[vegnum];

        //    string variable_name = "uptake_water_" + cropname;

        //    uptake_water_id[vegnum] = My.RegisterProperty(variable_name, "<type kind=\"double\" array=\"T\" unit=\"mm\"/>", true, false, false, "", "", getPropertyValue);

        //    for (int solnum = 0; solnum < num_solutes; solnum++)
        //    {
        //        variable_name = "supply_" + solute_names[solnum] + "_" + cropname;
        //        supply_solute_id[vegnum][solnum] = My.RegisterProperty(variable_name, "<type kind=\"double\" array=\"T\" unit=\"kg/ha\"/>", true, false, false, "", "", getPropertyValue);
        //    }

        //    //      do vegnum = 1, g%num_crops
        //    //         do solnum = 1, p%num_solutes

        //    //            variable_name = 'uptake_'//trim(p%solute_names(solnum))
        //    //     :                       //'_'//trim(g%crop_names(vegnum))
        //    //            id = Add_Registration (respondToGetSetReg, Variable_name,
        //    //     :                       DoubleArrayTypeDDML, ' ')

        //    //         end do
        //    //      end do

        //}

        private int SoluteIndex(string soluteName)
        {
            int soluteIndex = Array.FindIndex(solute_names, s => s.Equals(soluteName, StringComparison.InvariantCultureIgnoreCase));
            if (soluteIndex == -1)
                throw new Exception($"Invalid solute name: {soluteName}");
            return soluteIndex;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="vegnum"></param>
        /// <param name="uarray"></param>
        /// <param name="uflag"></param>
        private void GetSWUptake(int vegnum, out double[] uarray, out bool uflag)
        {
            uflag = false;
            uarray = new double[n + 1];

            if (vegnum < num_crops)
            {
                uflag = true;
                for (int node = 0; node <= n; node++)
                    // uptake may be very small -ve - assume error small
                    uarray[node] = Math.Max(pwuptake[vegnum][node], 0.0);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="vegnum"></param>
        /// <param name="sol"></param>
        /// <param name="uarray"></param>
        /// <param name="uflag"></param>
        private void GetSupply(int vegnum, int sol, out double[] uarray, out bool uflag)
        {
            uflag = false;
            uarray = new double[n + 1];

            if (vegnum < num_crops && sol < num_solutes)
            {
                for (int node = 0; node <= n; node++)
                    uarray[node] = Math.Max(psuptake[sol][vegnum][node], 0.0);
                uflag = true;
            }
        }

        private void ResizeProfileArrays(int newSize)
        {
            int oldSize = n + 1;

            HP.ResizePropfileArrays(newSize);


            Array.Resize(ref _swf, newSize);
            Array.Resize(ref SubSurfaceInFlow, newSize);
            Array.Resize(ref TD_wflow, newSize + 1);
            Array.Resize(ref _p, newSize);
            Array.Resize(ref _psi, newSize);
            Array.Resize(ref th, newSize);
            Array.Resize(ref thold, newSize);
            Array.Resize(ref hk, newSize);
            Array.Resize(ref q, newSize + 1);
            Array.Resize(ref qs, newSize);
            Array.Resize(ref qex, newSize);
            Array.Resize(ref qexpot, newSize);
            Array.Resize(ref qssif, newSize);
            Array.Resize(ref qssof, newSize + 1);
            Array.Resize(ref qr, newSize);
            Array.Resize(ref qrpot, newSize);
            Array.Resize(ref RootRadius, newSize);
            Array.Resize(ref RootConductance, newSize);
            Array.Resize(ref swta, newSize);

            //Array.Resize(ref _dlayer, newSize);
            //Array.Resize(ref _ll15, newSize);
            //Array.Resize(ref soil.DUL, newSize);
            //Array.Resize(ref _sat, newSize);
            //Array.Resize(ref _ks, newSize);
            //Array.Resize(ref _air_dry, newSize);
            Array.Resize(ref c, newSize);
            Array.Resize(ref k, newSize);

            Array.Resize(ref init_psi, newSize);
            Array.Resize(ref x, newSize);
            Array.Resize(ref dx, newSize);

            for (int i = oldSize; i < newSize; i++)
            {
                Array.Resize(ref qr[i], num_crops);
                Array.Resize(ref qrpot[i], num_crops);
                Array.Resize(ref RootRadius[i], num_crops);
                Array.Resize(ref RootConductance[i], num_crops);
            }

            for (int crop = 0; crop < num_crops; crop++)
            {
                Array.Resize(ref pwuptake[crop], newSize);
                Array.Resize(ref pwuptakepot[crop], newSize);
            }

            for (int sol = 0; sol < num_solutes; sol++)
            {
                Array.Resize(ref TD_sflow[sol], newSize + 1);
                Array.Resize(ref dc[sol], newSize);
                Array.Resize(ref csl[sol], newSize + 1);
                Array.Resize(ref cslt[sol], newSize);
                Array.Resize(ref qsl[sol], newSize + 1);
                Array.Resize(ref qsls[sol], newSize);
                Array.Resize(ref cslold[sol], newSize);
                Array.Resize(ref cslstart[sol], newSize);
                Array.Resize(ref exco[sol], newSize);
                Array.Resize(ref ex[sol], newSize);
                Array.Resize(ref fip[sol], newSize);

                for (int crop = 0; crop < num_crops; crop++)
                {
                    Array.Resize(ref psuptake[sol][crop], newSize);
                }
            }

            n = newSize - 1;
        }

        private void ResizeCropArrays(int newSize)
        {
            int oldSize = num_crops;

            Array.Resize(ref psimin, newSize);
            Array.Resize(ref rtp, newSize);
            Array.Resize(ref rt, newSize);
            Array.Resize(ref ctp, newSize);
            Array.Resize(ref ct, newSize);
            Array.Resize(ref crop_names, newSize);
            Array.Resize(ref crop_owners, newSize);
            Array.Resize(ref crop_owner_id, newSize);
            Array.Resize(ref crop_in, newSize);
            Array.Resize(ref demand_received, newSize);
            Array.Resize(ref supply_event_id, newSize);
            Array.Resize(ref pep, newSize);
            Array.Resize(ref canopy_height, newSize);
            Array.Resize(ref cover_tot, newSize);
            Array.Resize(ref _psix, newSize);
            Array.Resize(ref pwuptake, newSize);
            Array.Resize(ref pwuptakepot, newSize);
            Array.Resize(ref uptake_water_id, newSize);
            Array.Resize(ref supply_solute_id, newSize);
            Array.Resize(ref demand_is_met, newSize);
            Array.Resize(ref solute_demand, newSize);

            for (int idx = oldSize; idx < newSize; idx++)
            {
                Array.Resize(ref pwuptake[idx], n + 1);
                Array.Resize(ref pwuptakepot[idx], n + 1);

                Array.Resize(ref demand_is_met[idx], num_solutes);
                Array.Resize(ref solute_demand[idx], num_solutes);
                Array.Resize(ref supply_solute_id[idx], num_solutes);
            }

            for (int i = 0; i <= n; i++)
            {
                Array.Resize(ref qr[i], newSize);
                Array.Resize(ref qrpot[i], newSize);
                Array.Resize(ref RootRadius[i], newSize);
                Array.Resize(ref RootConductance[i], newSize);

            }

            for (int sol = 0; sol < num_solutes; sol++)
            {
                Array.Resize(ref psuptake[sol], newSize);
                for (int idx = oldSize; idx < newSize; idx++)
                {
                    Array.Resize(ref psuptake[sol][idx], n + 1);
                }
            }

            /* Don't resize these; they are Params
            Array.Resize(ref crop_name, newSize);
            Array.Resize(ref min_xylem_potential, newSize);
            Array.Resize(ref root_radius, newSize);
            Array.Resize(ref root_conductance, newSize);
             */

            num_crops = newSize;
        }

        private void ResizeSoluteArrays(int newSize)
        {
            int oldSize = num_solutes;
            Array.Resize(ref solute_names, newSize);
            Array.Resize(ref solute_owners, newSize);

            Array.Resize(ref SWIMSolTime, newSize);
            Array.Resize(ref SWIMSolAmt, newSize);
            Array.Resize(ref TD_soldrain, newSize);
            Array.Resize(ref TD_slssof, newSize);
            Array.Resize(ref slsur, newSize);
            Array.Resize(ref cslsur, newSize);
            Array.Resize(ref rslon, newSize);
            Array.Resize(ref rsloff, newSize);
            Array.Resize(ref rslex, newSize);
            Array.Resize(ref qslbp, newSize);
            Array.Resize(ref cslgw, newSize);
            Array.Resize(ref slupf, newSize);
            Array.Resize(ref slsci, newSize);
            Array.Resize(ref slscr, newSize);
            Array.Resize(ref dcon, newSize);
            Array.Resize(ref slos, newSize);
            Array.Resize(ref d0, newSize);

            Array.Resize(ref TD_sflow, newSize);
            Array.Resize(ref dc, newSize);
            Array.Resize(ref csl, newSize);
            Array.Resize(ref cslt, newSize);
            Array.Resize(ref qsl, newSize);
            Array.Resize(ref qsls, newSize);
            Array.Resize(ref cslold, newSize);
            Array.Resize(ref cslstart, newSize);
            Array.Resize(ref ex, newSize);
            Array.Resize(ref fip, newSize);
            Array.Resize(ref exco, newSize);

            Array.Resize(ref leach_id, newSize);
            Array.Resize(ref flow_id, newSize);
            Array.Resize(ref exco_id, newSize);
            Array.Resize(ref conc_water_id, newSize);
            Array.Resize(ref conc_adsorb_id, newSize);
            Array.Resize(ref subsurface_drain_id, newSize);

            Array.Resize(ref psuptake, newSize); // 0->num_crops, 0->n

            for (int idx = oldSize; idx < newSize; idx++)
            {
                Array.Resize(ref TD_sflow[idx], n + 2);
                Array.Resize(ref dc[idx], n + 1);
                Array.Resize(ref csl[idx], n + 2);
                Array.Resize(ref cslt[idx], n + 1);
                Array.Resize(ref qsl[idx], n + 2);
                Array.Resize(ref qsls[idx], n + 1);
                Array.Resize(ref cslold[idx], n + 1);
                Array.Resize(ref cslstart[idx], n + 1);
                Array.Resize(ref ex[idx], n + 1);
                Array.Resize(ref fip[idx], n + 1);
                Array.Resize(ref exco[idx], n + 1);
                SWIMSolTime[idx] = new double[0];
                SWIMSolAmt[idx] = new double[0];

                Array.Resize(ref psuptake[idx], num_crops);
                for (int crop = 0; crop < num_crops; crop++)
                    Array.Resize(ref psuptake[idx][crop], n + 1);
            }

            for (int i = 0; i < num_crops; i++)
            {
                Array.Resize(ref demand_is_met[i], newSize);
                Array.Resize(ref solute_demand[i], newSize);
                Array.Resize(ref supply_solute_id[i], newSize);
            }
            num_solutes = newSize;
        }

        private void ReadSoluteParams()
        {
            ResizeSoluteArrays(solutes.Count);
            for (int i = 0; i < solutes.Count; i++)
            {
                solute_names[i] = solutes[i].Name;
                var soluteParam = Parent.FindChild<Solute>(solute_names[i]);
                if (soluteParam == null)
                    throw new Exception("Could not find parameters for solute called " + solute_names[i]);
                if (soluteParam.FIP == null || double.IsNaN(MathUtilities.Sum(soluteParam.FIP)))
                    throw new Exception("Solute " + solute_names[i] + " does not have FIP values.");
                if (soluteParam.Exco == null || double.IsNaN(MathUtilities.Sum(soluteParam.Exco)))
                    throw new Exception("Solute " + solute_names[i] + " does not have EXCO values.");

                fip[i] = SoilUtilities.MapConcentration(soluteParam.FIP, soluteParam.Thickness, physical.Thickness, double.NaN);
                exco[i] = SoilUtilities.MapConcentration(soluteParam.Exco, soluteParam.Thickness, physical.Thickness, double.NaN);
                ex[i] = MathUtilities.Multiply(exco[i], physical.BD);
                cslgw[i] = soluteParam.WaterTableConcentration;
                d0[i] = soluteParam.D0;

            }
        }

        private void GetOtherVariables()
        {
            //// In the .NET version, we don't actually get the variables here; we just check them for sanity
            //if (radn < 0.0 || radn > 50.0)
            //    IssueWarning("Value for radn outside expected range");
            //if (maxt < -50.0 || maxt > 70.0)
            //    IssueWarning("Value for maxt outside expected range");
            //if (mint < -50.0 || mint > 50.0)
            //    IssueWarning("Value for mint outside expected range");


        }

        private void GetRainVariables()
        {
            double amount = PotentialInfiltration;
            double duration = 0.0;
            double intensity;
            int timeOfDay;
            if (string.IsNullOrWhiteSpace(rain_time))
            {
                timeOfDay = 0;
                duration = default_rain_duration;
            }
            else
            {
                timeOfDay = TimeToMins(rain_time);
                if (Double.IsNaN(rain_durn))
                {
                    if (Double.IsNaN(rain_int))
                    {
                        throw new Exception("Failure to supply rainfall duration or intensity data");
                    }
                    else
                    {
                        intensity = rain_int;
                        if (intensity < 0.0 || intensity > 254.0)  // 10 inches per hour
                            summary.WriteMessage(this, "Value for rain_int outside expected range", MessageType.Warning);
                        if (intensity > 0.0)
                            duration = amount / intensity * 60.0; // hrs -> mins
                    }
                }
                else
                {
                    duration = rain_durn;
                    if (duration < 0.0 || duration > 1440.0 * 30)
                        summary.WriteMessage(this, "Value for rain_durn outside expected range", MessageType.Warning);
                }
            }
            if (amount > 0.0)
            {
                double timeMins = Time(year, day, timeOfDay);
                InsertLoginfo(timeMins, duration, amount, ref SWIMRainTime, ref SWIMRainAmt);
            }
        }

        /// <summary>
        /// Get the values of solute variables from other modules
        /// </summary>
        private void GetCropVariables()
        {
            double bare = 1.0;                // amount of bare area

            ResizeCropArrays(canopies.Count);

            for (var vegnum = 0; vegnum < canopies.Count; vegnum++)
            {
                var model = canopies[vegnum] as IModel;
                var canopy = canopies[vegnum];

                var plant = model as IPlant;
                if (plant == null)
                    plant = model.FindAncestor<IPlant>(); // the canopy might be a leaf or energybalance and we need to find what plant it is on.
                if (plant != null)
                {
                    if (plant.IsAlive)
                    {
                        pep[vegnum] = canopies[vegnum].WaterDemand / 10.0; // convert mm to cm
                        canopy_height[vegnum] = canopy.Height;
                        cover_tot[vegnum] = canopy.CoverTotal;
                        bare = bare * (1.0 - cover_tot[vegnum]);
                    }
                    else
                    {
                        pep[vegnum] = 0.0;
                        canopy_height[vegnum] = 0.0;
                        cover_tot[vegnum] = 0.0;
                    }
                }
                else
                    throw new Exception($"A canopy was found that isn't on a plant. Name: {(canopies[vegnum] as IModel).Name}");
            }
            crop_cover = 1.0 - bare;
        }

        private void RemoveFromRainfall(double amount)
        {
            if (amount > 0.0)
            {
                // Firstly, find the record for start of rainfall for the
                // current day - ie assume interception cannot come from
                // rainfall that started before the current day.

                double start_timestep = Time(year, day, _apsimTimeMinutes);
                int start = 0;

                for (int counter = 0; counter < SWIMRainTime.Length; counter++)
                {
                    if (SWIMRainTime[counter] >= start_timestep)
                    {
                        // we have found the first record for the current timestep
                        start = counter;
                        break;
                    }
                }

                // Assume that interception is taken over all rainfall
                // information given thus far - can do nothing better than this

                double tot_rain = SWIMRainAmt[SWIMRainAmt.Length - 1] - SWIMRainAmt[start];

                double fraction = MathUtilities.Divide(amount, tot_rain, 1e6);
                if (fraction > 1.0)
                    throw new Exception("Interception > Rainfall");
                else
                    for (int counter = start + 1; counter < SWIMRainAmt.Length; counter++)
                    {
                        SWIMRainAmt[counter] = SWIMRainAmt[start] + (SWIMRainAmt[counter] - SWIMRainAmt[start]) * (1.0 - fraction);
                    }
            }
        }

        private void CalcEvapVariables()
        {
            if (!MathUtilities.FloatsAreEqual(apsim_timestep, 1440.0, floatComparisonTolerance))
                throw new Exception("apswim can only calculate Eo for daily timestep");

            if (eo_durn < 0.0 || eo_durn > 1440.0 * 30)
                summary.WriteMessage(this, "Value for eo duration outside expected range", MessageType.Warning);

            double timeMins = Time(year, day, _eoTimeMinutes);

            _cover_green_sum = GetGreenCover();

            InsertLoginfo(timeMins, eo_durn, Eo, ref SWIMEvapTime, ref SWIMEvapAmt);
        }

        private double GetGreenCover()
        {
            double bare = 1.0;
            foreach (ICanopy canopy in canopies)
            {
                // Note - this is based on a reduction of Beers law
                // cover1+cover2 = 1 - exp (-(k1*lai1 + k2*lai2))
                bare = bare * (1.0 - canopy.CoverGreen);
            }
            return 1.0 - bare;
        }

        private void SetOtherVariables()
        {
            // Does nothing!
        }

        private void SetSoluteVariables()
        {
            double[] solute_n = new double[n + 1];     // solute concn in layers(kg/ha)
            double[] dlt_solute_s = new double[n + 1]; // solute concn in layers(kg/ha)
            double[] depthMidPoints = SoilUtilities.ToMidPoints(physical.Thickness);

            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                if (!solute_names[solnum].StartsWith("PlantAvailable"))
                {
                    for (int node = 0; node <= n; node++)
                    {
                        // Step One - calculate total solute in node from solute in
                        // water and Freundlich isotherm.

                        if (csl[solnum][node] < 0.0)
                        {
                            string mess = String.Format(" solution {0}({1,3}) = {2,12:G6}",
                                         solute_names[solnum],
                                         node,
                                         csl[solnum][node]);
                            throw new Exception("-ve concentration in apswim_set_solute_variables" + Environment.NewLine + mess);
                        }
                        double Ctot, dCtot;
                        Freundlich(node, solnum, ref csl[solnum][node], out Ctot, out dCtot);

                        // Note:- Sometimes small numerical errors can leave
                        // -ve concentrations. Test if values are within limits.

                        if (Math.Abs(Ctot) < 1e-100)
                        {
                            // Ctot is REALLY small, its value can be disregarded
                            // set to zero to avoid underflow with reals

                            Ctot = 0.0;
                        }

                        else if (Ctot < 0.0)
                        {
                            // Ctot is negative and a fatal error is thrown. Should not happen as it has been tested on apswim_freundlich
                            string mess = String.Format(" Total {0}({1,3}) = {2,12:G6}",
                                                solute_names[solnum],
                                                node,
                                                Ctot);
                            throw new Exception("-ve value for solute concentration" + Environment.NewLine + mess);
                            //Ctot = 0.0;
                        }
                        //else Ctot is positive

                        // convert solute ug/cc soil to kg/ha for node
                        //
                        //  kg      ug        cc soil      kg
                        //  -- = -------- p%x -------- p%x --
                        //  ha    cc soil        ha        ug

                        Ctot = Ctot                   // ug/cc soil
                             * (dx[node] * 1.0e8)     // cc soil/ha
                             * 1e-9;                  // kg/ug

                        // finished testing - assign value to array element
                        solute_n[node] = Ctot;
                        dlt_solute_s[node] = Ctot - cslstart[solnum][node];
                    }

                    solutes[solnum].SetKgHa(SoluteSetterType.Soil, solute_n);

                    // Calculate an amount of solution in solution (kg/ha)
                    double[] concInWater = ConcWaterSolute(solnum);
                    solutes[solnum].AmountInSolution = MathUtilities.Multiply(concInWater, th);
                    solutes[solnum].AmountInSolution = SoilUtilities.ppm2kgha(physical.Thickness, physical.BD,
                                                                              solutes[solnum].AmountInSolution);
                    solutes[solnum].ConcAdsorpSolute = ConcAdsorptionSolute(solnum);

                    // Calculate amount of solute lost (kg/ha) in runoff water.
                    if (TD_runoff > 0 &&
                        solutes[solnum].DepthConstant > 0 &&
                        solutes[solnum].MaxDepthSoluteAccessible > 0 &&
                        solutes[solnum].MaxEffectiveRunoff > 0 &&
                        solutes[solnum].RunoffEffectivenessAtMovingSolute > 0)
                    {
                        for (int layer = 0; layer < depthMidPoints.Length; layer++)
                        {
                            double depthAvailabilityFactor = 0;
                            if (depthMidPoints[layer] <= solutes[solnum].MaxDepthSoluteAccessible)
                                depthAvailabilityFactor = Math.Pow(solutes[solnum].DepthConstant / depthMidPoints[layer], 2);

                            double runoffAmountFactor = Math.Min(1.0, TD_runoff / solutes[solnum].MaxEffectiveRunoff);

                            solutes[solnum].AmountLostInRunoff[layer] = solutes[solnum].AmountInSolution[layer] *
                                                                        depthAvailabilityFactor *
                                                                        runoffAmountFactor *
                                                                        solutes[solnum].RunoffEffectivenessAtMovingSolute;
                        }
                        var delta = MathUtilities.Multiply_Value(solutes[solnum].AmountLostInRunoff, -1.0);
                        solutes[solnum].AddKgHaDelta(SoluteSetterType.Soil, delta);
                    }
                }
            }
        }

        private void PublishUptakes()
        {
            //CohortWaterSupplyType Supply = new CohortWaterSupplyType();
            //WaterUptakesCalculatedType Water = new WaterUptakesCalculatedType();
            //Water.Uptakes = new WaterUptakesCalculatedUptakesType[num_crops];

            //for (int counter = 0; counter < num_crops; counter++)
            //{
            //    Water.Uptakes[counter] = new WaterUptakesCalculatedUptakesType();
            //    if (supply_event_id[counter] != 0)
            //    {
            //        Supply.CohortID = crop_names[counter];
            //        Supply.RootSystemLayer = new CohortWaterSupplyRootSystemLayerType[n + 1];
            //        double bottom = 0.0;
            //        for (int node = 0; node <= n; node++)
            //        {
            //            // This uses "bottom", not "dlayer", so we need to convert
            //            bottom += _dlayer[node];
            //            Supply.RootSystemLayer[node].Bottom = bottom;

            //            // uptake may be very small -ve - assume error small
            //            Supply.RootSystemLayer[node].Supply = Math.Max(pwuptake[counter][node], 0.0);
            //        }
            //        CohortWaterSupply.Invoke(Supply);
            //    }

            //    string cropname;
            //    if (supply_event_id[counter] == 0)
            //        cropname = crop_owners[counter];
            //    else
            //        cropname = crop_owners[counter] + "_" + crop_names[counter];

            //    Water.Uptakes[counter].Name = cropname;
            //    Water.Uptakes[counter].Amount = new double[n + 1];

            //    for (int node = 0; node <= n; node++)
            //    {
            //        // uptake may be very small -ve - assume error small
            //        Water.Uptakes[counter].Amount[node] = Math.Max(pwuptake[counter][node], 0.0);
            //    }
            //}

            //WaterUptakesCalculated.Invoke(Water);
        }

        private void GetObsEvapVariables()
        {
            ////  Purpose
            ////      Get the evap values from other modules
            //double amount;

            //if (!My.Get(evap_source, out amount))
            //    throw new Exception("No module provided Eo value for APSwim");

            //if (amount < 0.0 || amount > 1000.0)
            //    IssueWarning("Value for evaporation outside expected range");

            //string time;
            //double duration = 0.0;
            //if (String.IsNullOrWhiteSpace(eo_time))
            //{
            //    time = default_evap_time;
            //    duration = default_evap_duration;
            //}
            //else
            //{
            //    time = eo_time;
            //    if (Double.IsNaN(eo_durn))
            //        throw new Exception("Failure to supply eo duration data");
            //    duration = eo_durn;
            //}
            //if (duration < 0.0 || duration > 1440.0 * 30)
            //    IssueWarning("Value for eo duration outside expected range");

            //int timeOfDay = TimeToMins(time);
            //double timeMins = Time(year, day, timeOfDay);
            //InsertLoginfo(timeMins, duration, amount, ref SWIMEvapTime, ref SWIMEvapAmt);
        }

        private void RecalcEqrain()
        {
            Array.Resize(ref SWIMEqRainTime, SWIMRainTime.Length);
            Array.Resize(ref SWIMEqRainAmt, SWIMRainAmt.Length);
            // leave the first element alone to keep magnitude in order
            for (int counter = 1; counter < SWIMRainAmt.Length; counter++)
            {
                double eqrain = 0.0;
                double amount = (SWIMRainAmt[counter] - SWIMRainAmt[counter - 1]) / 10.0;
                double duration = SWIMRainTime[counter] - SWIMRainTime[counter - 1];
                double avinten = MathUtilities.Divide(amount, duration, 0.0);

                if (avinten > 0.0)
                    eqrain = (1.0 + effpar * Math.Log(avinten / 2.5)) * amount;

                SWIMEqRainTime[counter] = SWIMRainTime[counter];
                SWIMEqRainAmt[counter] = SWIMRainAmt[counter] + eqrain;
            }
        }

        private int TimeToMins(string timeString)
        {
            DateTime timeValue;
            if (!DateTime.TryParseExact(timeString, "H:m", new CultureInfo("en-AU"), DateTimeStyles.AllowWhiteSpaces, out timeValue))
                throw new Exception("bad time format");
            return timeValue.Hour * 60 + timeValue.Minute;
        }

        private void CNRunoff()
        {
            CalculateCoverSurfaceRunoff(ref _cover_surface_runoff);

            double startOfDay = Time(year, day, _apsimTimeMinutes);
            double endOfDay = Time(year, day, _apsimTimeMinutes + (int)apsim_timestep);

            double rain = (CRain(endOfDay) - CRain(startOfDay)) * 10.0;

            SCSRunoff(rain, out CN_runoff);
            RemoveFromRainfall(CN_runoff);
            TD_runoff += CN_runoff;
        }

        private void CalculateCoverSurfaceRunoff(ref double coverSurfaceRunoff)
        {
            // cover cn response from perfect   - ML  & dms 7-7-95
            // nb. perfect assumed crop canopy was 1/2 effect of mulch
            // This allows the taller canopies to have less effect on runoff
            // and the cover close to ground to have full effect (jngh)

            //! weight effectiveness of crop canopies
            //    0 (no effect) to 1 (full effect)

            double cover_surface_crop = 0.0;
            double canopyFact;
            for (int crop = 0; crop < num_crops; crop++)
            {
                if (canopy_height[crop] >= 0.0)
                {
                    bool didInterp;
                    canopyFact = MathUtilities.LinearInterpReal(canopy_height[crop], canopy_fact_height, canopy_fact, out didInterp);
                }
                else
                {
                    canopyFact = canopy_fact_default;
                }

                double effective_crop_cover = cover_tot[crop] * canopyFact;
                cover_surface_crop = AddCover(cover_surface_crop, effective_crop_cover);
            }

            // add cover known to affect runoff
            //!    ie residue with canopy shading residue

            coverSurfaceRunoff = AddCover(cover_surface_crop, residue_cover);
        }

        private double AddCover(double cover1, double cover2)
        {
            //!+ Sub-Program Arguments
            //   real       cover1                ! (INPUT) first cover to combine (0-1)
            //   real       cover2                ! (INPUT) second cover to combine (0-1)

            //!+ Purpose
            //!     Combines two covers

            //!+  Definition
            //!     "cover1" and "cover2" are numbers between 0 and 1 which
            //!     indicate what fraction of sunlight is intercepted by the
            //!     foliage of plants.  This function returns a number between
            //!     0 and 1 indicating the fraction of sunlight intercepted
            //!     when "cover1" is combined with "cover2", i.e. both sets of
            //!     plants are present.

            //!+  Mission Statement
            //!     cover as a result of %1 and %2

            double bare;     //! bare proportion (0-1)

            bare = (1.0 - cover1) * (1.0 - cover2);
            return (1.0 - bare);

        }

        private void SCSRunoff(double rain, out double runoff)
        {
            //+  Purpose
            //        calculate runoff using scs curve number method

            // revision of the runoff calculation according to scs curve number
            // cnpd  : fractional avail. soil water weighted over the
            //         hyd.eff. depth  <dms 7-7-95>
            // cn1   : curve number for dry soil
            // cn3   : curve number for wet soil
            // s     : s value from scs equation, transfer to mm scale
            //         = max. pot. retention (~infiltration) (mm)

            // check if hydro_effective_depth applies for eroded profile.

            double[] runoff_wf = new double[n + 1];
            RunoffDepthFactor(out runoff_wf);

            double cnpd = 0.0;
            for (int layer = 0; layer <= n; layer++)
            {
                cnpd = cnpd + (th[layer] - physical.LL15[layer]) / (physical.DUL[layer] - physical.LL15[layer]) * runoff_wf[layer];
            }
            cnpd = MathUtilities.Bound(cnpd, 0.0, 1.0);

            // reduce CN2 for the day due to cover effect

            double cover_fract = MathUtilities.Divide(CoverSurfaceRunoff, CNCov, 0.0);
            cover_fract = MathUtilities.Bound(cover_fract, 0.0, 1.0);

            double cn2_new = CN2Bare - (CNRed * cover_fract);

            cn2_new = MathUtilities.Bound(cn2_new, 0.0, 100.0);

            double cn1 = MathUtilities.Divide(cn2_new, (2.334 - 0.01334 * cn2_new), 0.0);
            double cn3 = MathUtilities.Divide(cn2_new, (0.4036 + 0.005964 * cn2_new), 0.0);
            double cn = cn1 + (cn3 - cn1) * cnpd;

            //! curve number will be decided from scs curve number table ??dms

            double s = 254.0 * (MathUtilities.Divide(100.0, cn, 1000000.0) - 1.0);
            double xpb = rain - 0.2 * s;
            xpb = Math.Max(xpb, 0.0);

            runoff = (xpb * xpb) / (rain + 0.8 * s);
        }

        private void RunoffDepthFactor(out double[] runoff_wf)
        {
            //+  Purpose
            //      Calculate the weighting factor hydraulic effectiveness used
            //      to weight the effect of soil moisture on runoff.

            //+  Local Variables
            double profile_depth;          // current depth of soil profile
                                           // - for when erosion turned on
            double cum_depth;              // cumulative depth (mm)
            double hydrolEffectiveDepth; // hydrologically effective depth for runoff (mm)
            int hydrolEffectiveLayer; // layer number that the effective depth occurs in ()
            int layer;                  // layer counter

            double scale_fact;             // scaling factor for wf function to sum to 1
            double wf_tot;                 // total of wf ()
            double wx;                     //. depth weighting factor for current
                                           //    total depth.
                                           //    intermediate variable for
                                           //    deriving wf
                                           //    (total wfs to current layer)
            double xx;                     // intermediate variable for deriving wf
                                           // total wfs to previous layer

            runoff_wf = new double[n + 1];
            xx = 0.0;
            cum_depth = 0.0;
            wf_tot = 0.0;

            // check if hydro_effective_depth applies for eroded profile.

            profile_depth = MathUtilities.Sum(physical.Thickness);

            hydrolEffectiveDepth = Math.Min(hydrol_effective_depth, profile_depth);

            scale_fact = 1.0 / (1.0 - Math.Exp(-4.16));
            hydrolEffectiveLayer = FindSwimLayer(hydrolEffectiveDepth);

            for (layer = 0; layer <= hydrolEffectiveLayer; layer++)
            {
                cum_depth += physical.Thickness[layer];
                cum_depth = Math.Min(cum_depth, hydrolEffectiveDepth);

                // assume water content to c%hydrol_effective_depth affects runoff
                // sum of wf should = 1 - may need to be bounded? <dms 7-7-95>

                wx = scale_fact * (1.0 - Math.Exp(-4.16 * cum_depth / hydrolEffectiveDepth));
                runoff_wf[layer] = wx - xx;
                xx = wx;

                wf_tot = wf_tot + runoff_wf[layer];
            }

            if (wf_tot < 0.9999 || wf_tot > 1.0001)
                summary.WriteMessage(this, "The value for 'wf_tot' of " + wf_tot.ToString() + " is not near the expected value of 1.0", MessageType.Warning);
        }

        private int FindSwimLayer(double depth)
        {
            double cumDepth = 0.0;
            for (int i = 0; i <= n; i++)
            {
                cumDepth += physical.Thickness[i];
                if (cumDepth > depth)
                    return i;
            }
            return n;
        }

        private double Time(int yy, int dd, int tt)
        {
            //Work out the number of days, in hours, between the start date and the given date
            //Adapted from older code to remove Julian date calculations
            DateTime startDate = DateUtilities.GetDate(start_day, start_year);
            DateTime thisDate = DateUtilities.GetDate(dd, yy);
            return (thisDate - startDate).TotalDays * 24.0 + tt / 60.0; // Convert to hours
        }

        private void PurgeLogInfo(double time, ref double[] SWIMTime, ref double[] SWIMAmt)
        {
            int old_numpairs = SWIMTime.Length;
            int new_start = 0;

            for (int counter = old_numpairs - 1; counter >= 0; counter--)
            {
                if (SWIMTime[counter] <= time)
                {
                    // we have found the oldest record we need to keep
                    new_start = counter;
                    break;
                }
            }

            int new_index = 0;
            for (int counter = new_start; counter < old_numpairs - 1; counter++)
            {
                new_index++;
                SWIMTime[new_index] = SWIMTime[counter];
                SWIMAmt[new_index] = SWIMAmt[counter];
            }
            Array.Resize(ref SWIMTime, new_index);
            Array.Resize(ref SWIMAmt, new_index);
        }

        private void InsertLoginfo(double time,     // min since start
                                   double duration, // min
                                   double amount,   // mm
                                   ref double[] SWIMTime,
                                   ref double[] SWIMAmt)
        {
            bool inserted = false;
            double SAmt = 0.0;
            double FAmt = 0.0;
            double FTime = time + duration / 60.0;
            if (SWIMTime.Length > 0)
            {
                if (time < SWIMTime[0])
                    throw new Exception("log time before start of run");

                SAmt = MathUtilities.LinearInterpReal(time, SWIMTime, SWIMAmt, out inserted);
                FAmt = MathUtilities.LinearInterpReal(FTime, SWIMTime, SWIMAmt, out inserted);

                // Insert starting element placeholder into log
                for (int counter = 0; counter < SWIMTime.Length; counter++)
                {
                    if (MathUtilities.FloatsAreEqual(SWIMTime[counter], time, floatComparisonTolerance))
                    {
                        inserted = true;
                        break;  // There is already a placeholder there
                    }
                    else if (SWIMTime[counter] > time)
                    {
                        Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                        Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                        for (int counter2 = SWIMTime.Length - 1; counter2 > counter; counter2--)
                        {
                            SWIMTime[counter2] = SWIMTime[counter2 - 1];
                            SWIMAmt[counter2] = SWIMAmt[counter2 - 1];
                        }
                        SWIMTime[counter] = time;
                        SWIMAmt[counter] = SAmt;
                        inserted = true;
                        break;
                    }
                }
            }
            if (!inserted)
            {
                // time > last log entry
                Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                SWIMTime[SWIMTime.Length - 1] = time;
                SWIMAmt[SWIMAmt.Length - 1] = SAmt;
            }

            // Insert ending element placeholder into log
            inserted = false;
            for (int counter = 0; counter < SWIMTime.Length; counter++)
            {
                if (MathUtilities.FloatsAreEqual(SWIMTime[counter], FTime, floatComparisonTolerance))
                {
                    inserted = true;
                    break;  // There is already a placeholder there
                }
                else if (SWIMTime[counter] > FTime)
                {
                    Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                    Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                    for (int counter2 = SWIMTime.Length - 1; counter2 > counter; counter2--)
                    {
                        SWIMTime[counter2] = SWIMTime[counter2 - 1];
                        SWIMAmt[counter2] = SWIMAmt[counter2 - 1];
                    }
                    SWIMTime[counter] = FTime;
                    SWIMAmt[counter] = FAmt;
                    inserted = true;
                    break;
                }
            }
            if (!inserted)
            {
                // time > last log entry
                Array.Resize(ref SWIMTime, SWIMTime.Length + 1);
                Array.Resize(ref SWIMAmt, SWIMAmt.Length + 1);
                SWIMTime[SWIMTime.Length - 1] = FTime;
                SWIMAmt[SWIMAmt.Length - 1] = FAmt;
            }

            // Now add extra quantity to each log entry are required

            double avInt = amount / (duration / 60.0);

            for (int counter = 0; counter < SWIMTime.Length; counter++)
            {
                double extra = 0.0;
                if (SWIMTime[counter] > time)
                    extra = avInt * Math.Min(SWIMTime[counter] - time, duration / 60.0);
                SWIMAmt[counter] += extra;
            }
        }

        private void ResetDailyTotals()
        {
            TD_runoff = 0.0;
            TD_rain = 0.0;
            TD_evap = 0.0;
            TD_pevap = 0.0;
            TD_drain = 0.0;
            TD_subsurface_drain = 0.0;

            for (int sol = 0; sol < num_solutes; sol++)
            {
                TD_slssof[sol] = 0.0;
                TD_soldrain[sol] = 0.0;
                for (int node = 0; node <= n + 1; node++)
                    TD_sflow[sol][node] = 0.0;
            }

            for (int node = 0; node <= n + 1; node++)
                TD_wflow[node] = 0.0;

            for (int node = 0; node <= n; node++)
            {
                for (int vegnum = 0; vegnum < num_crops; vegnum++)
                {
                    for (int sol = 0; sol < num_solutes; sol++)
                    {
                        psuptake[sol][vegnum][node] = 0.0;
                    }
                    pwuptake[vegnum][node] = 0.0;
                    pwuptakepot[vegnum][node] = 0.0;
                    _psix[vegnum] = 0.0;
                }
            }
        }

        private void ResetWaterBalance(int wcFlag, ref double[] waterContent)
        {
            for (int i = 0; i <= n; i++)
            {
                if (wcFlag == 1)
                {
                    // water content was supplied in volumetric SW
                    // so calculate matric potential
                    th[i] = waterContent[i];
                    _psi[i] = HP.Suction(i, th[i], _psi, PSIDul, physical.LL15, physical.DUL, physical.SAT);
                }
                else if (wcFlag == 2)
                {
                    // matric potential was supplied
                    // so calculate water content
                    _psi[i] = waterContent[i];
                    th[i] = CalcTheta(i, _psi[i]);
                }
                else
                    throw new Exception("Bad wc_type flag value");
                _p[i] = Pf(_psi[i]);
            }
            _wp = Wpf();
        }

        private double CSol(int solnum, double time)
        {
            //  Purpose
            //        cumulative solute in ug/cm^2
            int numPairs = SWIMSolTime[solnum].Length;
            double[] SAmount = new double[numPairs];
            double[] STime = new double[numPairs];

            if (numPairs > 0)
            {
                for (int counter = 0; counter < numPairs; counter++)
                {
                    SAmount[counter] = SWIMSolAmt[solnum][counter];
                    STime[counter] = SWIMSolTime[solnum][counter];
                }

                // Solute arrays are in kg/ha of added solute.  From swim's equations
                // with everything in cm and ug per g water we convert the output to
                // ug per cm^2 because the cm^2 area and height in cm gives g water.
                // There are 10^9 ug/kg and 10^8 cm^2 per ha therefore we get a
                // conversion factor of 10.

                bool interp;
                return MathUtilities.LinearInterpReal(time, STime, SAmount, out interp) * 10.0;
            }
            else
                return 0.0;
        }

        private double CRain(double time)
        {
            bool interp;
            if (SWIMRainTime.Length > 0)
                return MathUtilities.LinearInterpReal(time, SWIMRainTime, SWIMRainAmt, out interp) / 10.0;
            else
                return 0.0;
        }

        private double CEvap(double time)
        {
            bool interp;
            if (SWIMEvapTime.Length > 0)
                return MathUtilities.LinearInterpReal(time, SWIMEvapTime, SWIMEvapAmt, out interp) / 10.0;
            else
                return 0.0;
        }

        private double Eqrain(double time)
        {
            bool interp;
            if (SWIMEqRainTime.Length > 0)
                return MathUtilities.LinearInterpReal(time, SWIMEqRainTime, SWIMEqRainAmt, out interp);
            else
                return 0.0;
        }

        private double CalculateWaterTable()
        {
            if (PSI != null)
            {
                //   Purpose
                //      Calculate depth of water table from soil surface
                for (int i = 0; i <= n; i++)
                {
                    if (_psi[i] > 0)
                        return (x[i] - _psi[i]) * 10.0;
                }
                // set default value to bottom of soil profile.
                return x[n] * 10.0;
            }
            return 0;
        }

        private void Interp(int node, double tpsi, out double tth, out double thd, out double hklg, out double hklgd)
        {
            //  Purpose
            //   interpolate water characteristics for given potential for a given
            //   node.

            const double dpsi = 0.0001;
            double temp;

            tth = HP.SimpleTheta(node, tpsi);
            temp = HP.SimpleTheta(node, tpsi + dpsi);
            thd = (temp - tth) / Math.Log10((tpsi + dpsi) / tpsi);
            hklg = Math.Log10(HP.SimpleK(node, tpsi, physical.SAT, physical.KS));
            temp = Math.Log10(HP.SimpleK(node, tpsi + dpsi, physical.SAT, physical.KS));
            hklgd = (temp - hklg) / Math.Log10((tpsi + dpsi) / tpsi);
        }

        private double CalcTheta(int node, double suction)
        {
            double theta;
            double thd;
            double hklg;
            double hklgd;

            Interp(node, suction, out theta, out thd, out hklg, out hklgd);
            return theta;
        }

        private bool DoSwim(double timestepStart, double timestep)
        {
            //  Notes
            //     SWIM solves Richards' equation for one dimensional vertical soil water
            //     infiltration and movement.  A surface seal, variable height of surface
            //     ponding, and variable runoff rates are optional.  Deep drainage occurs
            //     under a given matric potential gradient or given potl or zero flux or
            //     seepage.  The method uses a fixed space grid and a sinh transform of
            //     the matric potential, as reported in :
            //     Ross, P.J., 1990.  Efficient numerical methods for infiltration using
            //     Richards' equation.  Water Resources Res. 26, 279-290.

            double timestepRemaining = timestep;
            t = timestepStart;
            bool fail = false;
            int itlim;
            double qmax;

            //     define iteration limit for soln of balance eqns
            if (run_has_started)
            {
                // itlim = 20;
                itlim = max_iterations;
            }
            else
            {
                // this is our first timestep - allow for initial stabilisation
                // itlim = 50;
                itlim = max_iterations + 20;
            }

            //     solve until end of time step
            do
            {
                double dr;
                // call event_send(unknown_module,'swim_timestep_preparation')

                //        calculate next step size_of g%dt

                // Start with first guess as largest size_of possible
                _dt = DTMax;
                if (MathUtilities.FloatsAreEqual(DTMin, DTMax, floatComparisonTolerance))
                    _dt = DTMin;
                else
                {
                    if (!run_has_started)
                    {
                        if (MathUtilities.FloatsAreEqual(DTMin, 0.0, floatComparisonTolerance))
                            _dt = Math.Min(0.01 * (timestepRemaining), 0.25);
                        else
                            _dt = DTMin;
                        ron = 0.0;
                        qmax = 0.0;
                    }
                    else
                    {
                        qmax = Math.Max(0.0, roff);
                        qmax = Math.Max(qmax, res);
                        for (int i = 0; i <= n; i++)
                        {
                            qmax = Math.Max(qmax, qex[i]);
                            // qmax = Math.Max(qmax, qexpot[i]); // this to make steps small when pot is large therefore to
                            // provide accurate pot supply back to crops
                            qmax = Math.Max(qmax, Math.Abs(qs[i]));
                            qmax = Math.Max(qmax, Math.Abs(qssif[i]));
                            qmax = Math.Max(qmax, Math.Abs(qssof[i]));
                            qmax = Math.Max(qmax, Math.Abs(q[i]));
                        }
                        qmax = Math.Max(qmax, Math.Abs(q[n + 1]));
                        if (qmax > 0.0)
                            _dt = MathUtilities.Divide(MaxWaterIncrement, qmax, 0.0);
                    }

                    _dt = Math.Min(_dt, timestepRemaining);

                    double crt = CRain(t);
                    dr = CRain(t + _dt) - crt;

                    double dw1 = (ron == 0.0) ? 0.1 * MaxWaterIncrement : MaxWaterIncrement;

                    double t2 = 0.0;
                    if (dr > 1.1 * dw1)
                    {
                        double t1 = t;
                        for (int i = 0; i < 10; i++)
                        {
                            _dt *= 0.5;
                            t2 = t1 + _dt;
                            dr = CRain(t2) - crt;
                            if (dr < 0.9 * dw1)
                                t1 = t2;
                            else if (dr <= 1.1 * dw1)
                                break;
                        }
                        _dt = t2 - t;
                    }

                    _dt = Math.Min(_dt, DTMax);
                    _dt = Math.Max(_dt, DTMin);
                }

                double dtiny = Math.Max(0.01 * _dt, DTMin);
                //        initialise and take new step
                //        ----------------------------

                double wpold = _wp;
                double old_hmin = _hmin;
                double old_gsurf = gsurf;
                double[] pold = new double[n + 1];
                double[,] cslold = new double[num_solutes, n + 1];
                hold = _h;
                for (int i = 0; i <= n; i++)
                {
                    // save transformed potls and water contents
                    pold[i] = _p[i];
                    thold[i] = th[i];
                    //nh
                    //             psiold[i] = _psi[i];
                    for (int solnum = 0; solnum < num_solutes; solnum++)
                        cslold[solnum, i] = csl[solnum][i];
                }

                double old_time = t;

            //   new step
            //40       continue
            retry:

                t += _dt;
                if (timestepRemaining - _dt < 0.1 * _dt)
                {
                    t = t - _dt + timestepRemaining;
                    _dt = timestepRemaining;
                }

                dr = CRain(t) - CRain(t - _dt);
                ron = dr / _dt; // it could just be rain_intensity

                //cnh
                for (int i = 0; i < num_solutes; i++)
                    rslon[i] = (CSol(i, t) - CSol(i, t - _dt)) / _dt;

                PStat(0, ref resp);

                double deqr = Eqrain(t) - Eqrain(t - _dt);
                if (isbc == 2)
                    HMin(deqr, ref _hmin);
                if (itbc == 2)
                    GSurf(deqr, ref gsurf);
                //cnh
                CheckDemand();

                //call event_send(unknown_module,'pre_swim_timestep')

                // integrate for step _dt

                Solve(itlim, ref fail);

                if (fail)
                {
                    // SWIM failed to find a solution, should reset values to its previous state
                    // and attempt to solve again with a smaller dt

                    ShowDiagnostics(pold);
                    // Reset values
                    t = old_time;
                    _hmin = old_hmin;
                    gsurf = old_gsurf;
                    _wp = wpold;
                    _dt = 0.5 * _dt;
                    _h = hold;
                    for (int i = 0; i <= n; i++)
                    {
                        _p[i] = pold[i];
                        th[i] = thold[i];
                        for (int solnum = 0; solnum < num_solutes; solnum++)
                            csl[solnum][i] = cslold[solnum, i];
                    }

                    //RC   lines for g%th and g%csl added by RCichota, 09/02/2010

                    _dt = 0.5 * _dt;
                    if (_dt == 0)
                        throw new Exception("SWIM failed to find a solution");

                    // Tell user that SWIM is changing dt
                    summary.WriteMessage(this, "ApsimSwim|apswim_swim - Changing dt value from: " + String.Format("{0,15:F3}", _dt * 2.0) + " to: " + String.Format("{0,15:F3}", _dt), MessageType.Diagnostic);
                    if (_dt >= dtiny)
                        goto retry;
                }
                else
                {
                    // update variables
                    TD_runoff += roff * _dt * 10.0;
                    TD_evap += res * _dt * 10.0;
                    TD_drain += q[n + 1] * _dt * 10.0;
                    TD_rain += ron * _dt * 10.0;
                    TD_pevap += resp * _dt * 10.0;
                    TD_subsurface_drain += MathUtilities.Sum(qssof) * _dt * 10.0;
                    for (int node = 0; node <= n + 1; node++)
                        TD_wflow[node] += q[node] * _dt * 10.0;

                    for (int solnum = 0; solnum < num_solutes; solnum++)
                    {
                        // kg    cm ug          g   kg
                        // -- = (--p%x--) p%x hr p%x -- p%x --
                        // ha    hr  g         ha   ug

                        TD_soldrain[solnum] +=
                                  qsl[solnum][n + 1] * _dt
                                  * (1e4) * (1e4)   // cm^2/ha = g/ha
                                  * 1e-9;          // kg/ug

                        for (int node = 0; node <= n + 1; node++)
                        {
                            TD_sflow[solnum][node] += qsl[solnum][node] * _dt * (1e4) * (1e4) * 1e-9;
                            TD_slssof[solnum] += csl[solnum][node] * qssof[node] * _dt * (1e4) * (1e4) * 1e-9;
                        }
                    }

                    //cnh
                    PStat(1, ref resp);
                    PStat(2, ref resp);

                    //cnh
                    // call event_send(unknown_module,'post_swim_timestep')

                }

                // We have now finished our first timestep
                run_has_started = true;
                timestepRemaining -= _dt;
            }
            while (timestepRemaining > 0.0 && !fail);
            return fail;
        }

        private void ShowDiagnostics(double[] pold)
        {
            if (Diagnostics)
            {
                summary.WriteMessage(this, "     APSwim Numerical Diagnostics", MessageType.Diagnostic);
                summary.WriteMessage(this, "     ------------------------------------------------------------------------------", MessageType.Diagnostic);
                summary.WriteMessage(this, "      depth      Theta         psi        K           p          p*", MessageType.Diagnostic);
                summary.WriteMessage(this, "     ------------------------------------------------------------------------------", MessageType.Diagnostic);

                double k;
                double dummy1, dummy2, dummy3, dummy4, dummy5 = 0.0, dummy6 = 0.0;
                for (int layer = 0; layer < x.Length; layer++)
                {
                    Watvar(layer, _p[layer], out dummy1, out dummy2, out dummy3, out dummy4, ref dummy5, out k, ref dummy6);
                    summary.WriteMessage(this, String.Format("     {0,6:F1}         {1,9:F7} {2,10:0.###} {3,10:F3} {4,10:F3} {5,10:F3}",
                                      x[layer] * 10.0,
                                      th[layer],
                                      _psi[layer],
                                      k,
                                      _p[layer],
                                      pold[layer]), MessageType.Diagnostic);
                }
                summary.WriteMessage(this, "     ------------------------------------------------------------------------------", MessageType.Diagnostic);
            }

        }

        private void CheckDemand()
        {
            for (int crop = 0; crop < num_crops; crop++)
                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    double tpsuptake = 0.0;
                    for (int layer = 0; layer <= n; layer++)
                        tpsuptake += Math.Max(psuptake[solnum][crop][layer], 0.0);

                    double demand = Math.Max(solute_demand[crop][solnum] - tpsuptake, 0.0);

                    demand_is_met[crop][solnum] = demand <= 0.0;
                }
        }

        private void HMin(double deqrain, ref double sstorage)
        {
            // Ideally, if timesteps are small we could just use
            // dHmin/dEqr = -1/p%hrc p%x (g%hmin - p%hm0)
            // but because this is really just a linear approximation of the
            // curve for longer timesteps we had better be explicit and
            // calculate the difference from the exponential decay curve.

            if (hrc != 0)
            {
                // first calculate the amount of Energy that must have been
                // applied to reach the current g%hmin.

                double decayFraction = MathUtilities.Divide(_hmin - _hm0, _hm1 - _hm0, 0.0);

                if (MathUtilities.FloatsAreEqual(decayFraction, 0.0, floatComparisonTolerance))
                {
                    // the roughness is totally decayed
                    sstorage = _hm0;
                }
                else
                {
                    double ceqrain = -_hrc * Math.Log(decayFraction);

                    // now add rainfall energy for this timestep
                    if (cover_effects)
                        ceqrain += deqrain * (1.0 - residue_cover);
                    else
                        ceqrain += deqrain;

                    // now calculate new surface storage from new energy
                    sstorage = _hm0 + (_hm1 - _hm0) * Math.Exp(-ceqrain / _hrc);
                }
            }
            //else
            // nih - commented out to keep storage const
            //! sstorage = _hm0;
        }

        private void GSurf(double deqrain, ref double surfcon)
        {
            //     Short Description:
            //     gets soil surface conductance, surfcon

            // Ideally, if timesteps are small we could just use
            // dgsurf/dEqr = -1/grc x (gsurf - g0)
            // but because this is really just a linear approximation of the
            // curve for longer timesteps we had better be explicit and
            // calculate the difference from the exponential decay curve.

            if (grc != 0)
            {
                // first calculate the amount of Energy that must have been
                // applied to reach the current conductance.

                double decayFraction = MathUtilities.Divide(gsurf - _g0, _g1 - _g0, 0.0);

                if (MathUtilities.FloatsAreEqual(decayFraction, 0.0, floatComparisonTolerance))
                {
                    // seal is totally decayed
                    surfcon = _g0;
                }
                else
                {
                    double ceqrain = -_grc * Math.Log(decayFraction);

                    // now add rainfall energy for this timestep
                    if (cover_effects)
                        ceqrain += deqrain * (1.0 - residue_cover);
                    else
                        ceqrain += deqrain;

                    // now calculate new surface storage from new energy
                    surfcon = _g0 + (_g1 - _g0) * Math.Exp(-ceqrain / _grc);
                }
            }
            else
                surfcon = gsurf;
        }

        private void Solve(int itlim, ref bool fail)
        {
            //     Short description:
            //     solves for this time step

            int it = 0;
            double wpold = _wp;
            int iroots = 0;
            // loop until solved or too many iterations or Thomas algorithm fails
            int i1;
            int i2;
            double[] a = new double[n + 1];
            double[] b = new double[n + 1];
            double[] c = new double[n + 1];
            double[] d = new double[n + 1];
            double[] rhs = new double[n + 1];
            double[] dp = new double[n + 1];
            double[] vbp = new double[n + 1];
            PondingData pondingData = new PondingData();

            do
            {
                it++;
                //        get balance eqns
                // LOOK OUT. THE FORTRAN CODE USED ARRAY INDICES STARTING AT -1
                Baleq(it, ref iroots, ref slos, ref csl, out i1, out i2, ref a, ref b, ref c, ref rhs, ref pondingData);
                //   test for convergence to soln
                // nh hey - wpf has no arguments !
                // nh         _wp = wpf(n, _dx, th)
                _wp = Wpf();

                double balerr = ron - roff - q[n + 1] - rex - res + rssf - (_h - hold + _wp - wpold) / _dt;
                double err = 0.0;
                for (int i = i1; i <= i2; i++)
                {
                    double aerr = Math.Abs(rhs[i]);
                    if (err < aerr)
                        err = aerr;
                }

                // switch off iteration for root extraction if err small enough
                if (err < errex * rex && iroots == 0)
                    iroots = 1;
                if (Math.Abs(balerr) < ersoil && err < ernode)
                    fail = false;
                else
                {
                    int neq = i2 - i1 + 1;
                    Thomas(i1, neq, ref a, ref b, ref c, ref rhs, ref d, ref dp, ref pondingData, out fail);
                    _work += neq;
                    //nh            if(fail)go to 90
                    if (fail)
                    {
                        //nh               call warning_error(Err_internal,
                        //nh     :            'swim will reduce timestep to solve water movement')
                        summary.WriteMessage(this, "swim will reduce timestep to avoid error in water balance", MessageType.Diagnostic);
                        break;
                    }

                    fail = true;
                    //           limit step size_of for soil nodesn
                    int i0 = Math.Max(i1, 0);
                    for (int i = i0; i <= i2; i++)
                    {
                        if (dp[i] > dppl)
                            dp[i] = dppl;
                        if (dp[i] < -dpnl)
                            dp[i] = -dpnl;
                    }
                    //           update solution
                    int j = i0;
                    for (int i = i0; i <= i2; i++)
                    {
                        _p[j] += dp[i];
                        if (j > 0 && j < n - 1)
                        {
                            if (MathUtilities.FloatsAreEqual(x[j], x[j + 1], floatComparisonTolerance))
                            {
                                j++;
                                _p[j] = _p[j - 1];
                            }
                        }
                        j++;
                    }
                    if (i1 == -1)
                        _h = Math.Max(0.0, _h + pondingData.v);
                    //_h = Math.Max(0.0, _h + dp[-1]);
                }
            }
            while (fail && it < itlim);

            if (fail)
            {
                summary.WriteMessage(this, clock.Today.ToString("dd_mmm_yyyy"), MessageType.Diagnostic);
                summary.WriteMessage(this, "Maximum iterations reached - swim will reduce timestep", MessageType.Diagnostic);
            }

            //     solve for solute movement
            else
            {
                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    GetSol(solnum, ref a, ref b, ref c, ref d, ref rhs, ref dp, ref vbp, ref pondingData, ref fail);
                    if (fail)
                    {
                        summary.WriteMessage(this, "swim will reduce timestep to solve solute movement", MessageType.Diagnostic);
                        break;
                    }
                }
            }
        }

        private void GetSol(int solnum, ref double[] a, ref double[] b, ref double[] c, ref double[] d, ref double[] rhs, ref double[] c1, ref double[] c2, ref PondingData pondingData, ref bool fail)
        {
            //     Short description:
            //     get and solve solute balance eqns

            //     Constant Values
            const int itmax = 20;
            const int constant_conc = 1;
            const int convection_only = 2;

            //     Determine type of solute BBC to use
            int solute_bbc;
            int j;
            double rslovr;
            bool nonlin = false;
            double wtime = 0.0, wtime1 = 0.0;

            if (ibbc == 1)
                // water table boundary condition
                solute_bbc = constant_conc;
            else if (ibbc == 0 && q[n + 1] < 0)
                // you have a gradient with flow upward
                solute_bbc = constant_conc;
            else
                solute_bbc = convection_only;

            //    surface solute balance - assume evap. (g%res) comes from x0 store
            double rovr = roff + qbp;
            double rinf = q[0] + res;
            if (rinf > Math.Min(ersoil, ernode))
            {
                cslsur[solnum] = (rslon[solnum] + hold * cslsur[solnum] / _dt) / (rovr + rinf + _h / _dt);
                qsl[solnum][0] = rinf * cslsur[solnum];
                rslovr = rovr * cslsur[solnum];
                if (slsur[solnum] > 0.0)
                {
                    if (cslsur[solnum] < slsci[solnum])
                    {
                        if (slsur[solnum] > rinf * _dt * (slsci[solnum] - cslsur[solnum]))
                        {
                            qsl[solnum][0] = rinf * slsci[solnum];
                            slsur[solnum] = slsur[solnum] - rinf * _dt * (slsci[solnum] - cslsur[solnum]);
                        }
                        else
                        {
                            qsl[solnum][0] = rinf * cslsur[solnum] + slsur[solnum] / _dt;
                            slsur[solnum] = 0.0;
                        }
                    }
                    if (cslsur[solnum] < slscr[solnum])
                    {
                        if (slsur[solnum] > rovr * _dt * (slscr[solnum] - cslsur[solnum]))
                        {
                            rslovr = rovr * slscr[solnum];
                            slsur[solnum] = slsur[solnum] - rovr * _dt * (slscr[solnum] - cslsur[solnum]);
                        }
                        else
                        {
                            rslovr = rovr * cslsur[solnum] + slsur[solnum] / _dt;
                            slsur[solnum] = 0.0;
                        }
                        if (slsur[solnum] > _h * (slscr[solnum] - cslsur[solnum]))
                        {
                            slsur[solnum] = slsur[solnum] - _h * (slscr[solnum] - cslsur[solnum]);
                            cslsur[solnum] = slscr[solnum];
                        }
                        else
                        {
                            if (_h > 0.0)
                                cslsur[solnum] = cslsur[solnum] + slsur[solnum] / _h;
                            slsur[solnum] = 0.0;
                        }
                    }
                }
            }
            else
            {
                cslsur[solnum] = 0.0;
                qsl[solnum][0] = 0.0;
                rslovr = 0.0;
            }

            //     get eqn coeffs
            //     get production and storage components
            double thi;
            double exco1;
            //nh      call slprod
            for (int i = 0; i <= n; i++)
            {
                c1[i] = csl[solnum][i];
                thi = th[i];
                //nh         j=indxsl(solnum,i)
                j = i;
                nonlin = false;

                //Peter's CHANGE 21/10/98 to ensure zero exchange is treated as linear
                //         if (p%fip(solnum,j).eq.1.) then
                if ((MathUtilities.FloatsAreEqual(ex[solnum][j], 0.0, floatComparisonTolerance))    //exco=0
                    || (MathUtilities.FloatsAreEqual(fip[solnum][j], 1.0, floatComparisonTolerance)) // fip=1

                    ) // solute amount is zero  2022-01-06 Val Snow added this to avoid crashes when solutes initiated with zero concentraion  || (MathUtilities.FloatsAreEqual(csl[solnum][j], 0.0, 1e-6))
                {
                    //           linear exchange isotherm
                    c2[i] = 1.0;
                    exco1 = ex[solnum][j];
                }
                else
                {
                    //           nonlinear Freundlich exchange isotherm
                    nonlin = true;
                    c2[i] = 0.0;
                    if (c1[i] > 0.0)
                        c2[i] = Math.Pow(c1[i], fip[solnum][i] - 1.0);
                    //``````````````````````````````````````````````````````````````````````````````````````````````````
                    //RC         Changed by RCichota 30/jan/2010
                    exco1 = ex[solnum][j] * c2[i];
                    //            exco1=p%ex(solnum,j)*p%fip(solnum,j)*c2(i)    !<---old code
                    //
                }
                b[i] = (-(thi + exco1) / _dt) * dx[i] - qssof[i];
                //nh     1        apswim_slupf(1,solnum)*g%qex(i)-g%qssof(i)
                for (int crop = 0; crop < num_crops; crop++)
                    b[i] = b[i] - Slupf(crop, solnum) * qr[i][crop];
                //nh     1        p%slupf(solnum)*g%qex(i)
                rhs[i] = -(csl[solnum][i] * ((thold[i] + exco1) / _dt)) * dx[i];
                qsls[solnum][i] = -(csl[solnum][i] * (thold[i] + ex[solnum][j] * c2[i]) / _dt) * dx[i];
            }

            //     get dispersive and convective components
            //        use central diffs in time for convection, backward diffs for rest
            //        use central diffs in space, but for convection may need some
            //        upstream weighting to avoid instability

            for (int i = 1; i <= n; i++) // NOTE: staring from 1 is deliberate this time
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i], floatComparisonTolerance))
                {
                    double w1;
                    double thav = 0.5 * (th[i - 1] + th[i]);
                    double aq = Math.Abs(q[i]);
                    dc[solnum][i] = dcon[solnum] * Math.Pow(thav - DTHC, DTHP) +
                                    Dis * Math.Pow(aq / thav, Disp);
                    double dfac = thav * dc[solnum][i] / (x[i] - x[i - 1]);
                    if (SoluteSpaceWeightingFactor >= 0.5 && SoluteSpaceWeightingFactor <= 1.0)
                    {
                        //              use fixed space weighting on convection
                        w1 = MathUtilities.Sign(2.0 * SoluteSpaceWeightingFactor, q[i]);
                    }
                    else
                    {
                        //              use central diffs for convection if possible, else use
                        //                 just enough upstream weighting to avoid oscillation
                        //                 user may increase acceptable level for central diffs
                        //                 by setting p%slswt < -1
                        double accept = Math.Max(1.0, -SoluteSpaceWeightingFactor);
                        double wt = 0.0;
                        if (aq != 0.0)
                            wt = MathUtilities.Sign(Math.Max(0.0, 1.0 - 2.0 * accept * dfac / aq), q[i]);
                        w1 = 1.0 + wt;
                    }
                    double w2 = 2.0 - w1;

                    //Peter's CHANGE 21/10/98 to remove/restore Crank-Nicolson time weighting
                    //for convection
                    //            fq=.25*g%q(i)
                    //            fqc=fq*(w1*g%csl(solnum,i-1)+w2*g%csl(solnum,i))
                    //            wtime=0.25D0
                    //            wtime1=1.0D0
                    wtime = 0.5;
                    wtime1 = 0.0;
                    double fq = wtime * q[i];
                    double fqc = wtime1 * fq * (w1 * csl[solnum][i - 1] + w2 * csl[solnum][i]);

                    //           get convective component from old time level
                    qsl[solnum][i] = fqc;
                    b[i - 1] = b[i - 1] - dfac - fq * w1;
                    c[i - 1] = dfac - fq * w2;
                    a[i] = dfac + fq * w1;
                    b[i] = b[i] - dfac + fq * w2;
                    rhs[i - 1] = rhs[i - 1] + fqc;
                    rhs[i] = rhs[i] - fqc;
                }
            }

            //     allow for bypass flow
            qslbp[solnum] = 0.0;

            //     impose boundary conditions
            int k;
            if (itbc == 1)
            {
                //        constant concentration
                k = 1;
            }
            else
            {
                k = 0;
                rhs[0] = rhs[0] - qsl[solnum][0];
                if (rinf < -Math.Min(ersoil, ernode))
                {
                    b[0] = b[0] + 0.5 * rinf;
                    rhs[0] = rhs[0] - 0.5 * rinf * csl[solnum][0];
                }
            }

            int neq;
            if (solute_bbc == constant_conc)
            {
                //        constant concentration
                //nh
                csl[solnum][n] = cslgw[solnum];
                //nh
                rhs[n - 1] = rhs[n - 1] - c[n - 1] * csl[solnum][n];
                neq = n;
            }
            else
            {
                //        convection only
                b[n] = b[n] - 0.5 * q[n + 1];
                rhs[n] = rhs[n] + 0.5 * q[n + 1] * csl[solnum][n];
                neq = n + 1;
            }
            //     allow for two nodes at same depth
            j = 0;
            for (int i = 1; i <= n; i++)
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i], floatComparisonTolerance))
                {
                    j = j + 1;
                    a[j] = a[i];
                    b[j] = b[i];
                    rhs[j] = rhs[i];
                    c[j - 1] = c[i - 1];
                }
                else
                {
                    b[j] = b[j] + b[i];
                    rhs[j] = rhs[j] + rhs[i];
                }
            }
            //     save old g%csl(0),g%csl(p%n)
            double csl0 = csl[solnum][0];
            double csln = csl[solnum][n];
            neq = neq - (n - j);
            int itcnt = 0;
        //     solve for concentrations

        loop:
            //nh      call thomas(neq,0,a(k),b(k),c(k),rhs(k),dum,d(k),g%csl(solnum,k),
            //nh     :            dum,fail)
            double[] csltemp = new double[n + 1];
            for (int i = 0; i <= n; i++)
                csltemp[i] = csl[solnum][i];
            Thomas(k, neq, ref a, ref b, ref c, ref rhs, ref d, ref csltemp, ref pondingData, out fail);
            for (int i = 0; i <= n; i++)
                csl[solnum][i] = csltemp[i];
            // nh end subroutine
            itcnt++;
            slwork = slwork + neq;
            if (fail)
                return;
            j = k + neq - 1;
            if (solute_bbc == convection_only)
            {
                csl[solnum][n] = csl[solnum][j];
                j--;
            }
            for (int i = n - 1; i > 0; i--)
            {
                if (!MathUtilities.FloatsAreEqual(x[i], x[i + 1], floatComparisonTolerance))
                {
                    csl[solnum][i] = csl[solnum][j];
                    j--;
                }
                else
                {
                    csl[solnum][i] = csl[solnum][i + 1];
                }
            }

            if (nonlin)
            {
                //        test for convergence
                double dmax = 0.0;
                for (int i = 0; i <= n; i++)
                {
                    double dabs = Math.Abs(csl[solnum][i] - c1[i]);
                    if (dmax < dabs)
                        dmax = dabs;
                }
                if (dmax > slcerr)
                {
                    if (itcnt == itmax)
                    {
                        fail = true;
                        return;
                    }
                    //           keep iterating using Newton-Raphson technique
                    //           next c^fip for Freundlich isotherm is approximated as
                    //              cn^fip=c^fip+p%fip*c^(p%fip-1)*(cn-c)
                    //                    =p%fip*c^(p%fip-1)*cn+(1-p%fip)*c^fip
                    j = 0;
                    for (int i = 0; i <= n; i++)
                    {
                        if (i > 0 && !MathUtilities.FloatsAreEqual(x[i - 1], x[i], floatComparisonTolerance))
                        {
                            if (i > 0)
                                j++;
                        }
                        //cnh               kk=indxsl(solnum,i)
                        int kk = i;
                        if (!MathUtilities.FloatsAreEqual(fip[solnum][i], 1.0, floatComparisonTolerance))
                        {
                            double cp = 0.0;
                            if (csl[solnum][i] > 0.0)
                                cp = Math.Pow(csl[solnum][i], fip[solnum][i] - 1.0);

                            //````````````````````````````````````````````````````````````````````````````````````````````````````````````````
                            //RC      Changed by RCichota (29/Jan/2010), original code is commented out
                            double d1 = cp - c2[i];
                            //                  d1=p%fip(solnum,kk)*(cp-c2(i))
                            //                  d2=(1.-p%fip(solnum,kk))
                            //     :              *(g%csl(solnum,i)*cp-c1(i)*c2(i))
                            c1[i] = csl[solnum][i];
                            c2[i] = cp;
                            b[j] = b[j] - (ex[solnum][kk] / _dt) * d1 * dx[i];
                            //                  rhs(j)=rhs(j)+(p%ex(solnum,kk)/g%dt
                            //     :                            -p%betaex(solnum,kk))
                            //     :                          *d2*p%dx(i)
                            //````````````````````````````````````````````````````````````````````````````````````````````````````````````````
                            // Changes in the calc of d1 are to agree with the calc of exco1 above (no need to multiply by p%fip
                            // If p%fip < 1, the unkown is Cw, and is only used in the calc of b. thus rhs is commented out.
                            //`
                        }
                    }
                    goto loop;
                }
            }

            //     get surface solute balance?
            if (rinf < -Math.Min(ersoil, ernode))
            {
                //        flow out of surface
                //CHANGES 6/11/98 to remove/restore Crank-Nicolson time weighting for convection
                //-----
                //         g%qsl(solnum,0)=.5*rinf*(csl0+g%csl(solnum,0))
                qsl[solnum][0] = 0.5 * rinf * (wtime1 * csl0 + 4.0 * wtime * csl[solnum][0]);

                double rslout = -qsl[solnum][0];
                if (slsur[solnum] > 0.0)
                {
                    //           allow for surface applied solute
                    if (csl[solnum][0] < slsci[solnum])
                    {
                        if (slsur[solnum] > -rinf * _dt * (slsci[solnum] - csl[solnum][0]))
                        {
                            rslout = -rinf * slsci[solnum];
                            slsur[solnum] = slsur[solnum] + rinf * _dt * (slsci[solnum] - csl[solnum][0]);
                        }
                        else
                        {
                            rslout = rslout + slsur[solnum] / _dt;
                            slsur[solnum] = 0.0;
                        }
                    }
                }
                //        get surface solute balance
                cslsur[solnum] = (rslon[solnum] + rslout + hold * cslsur[solnum] / _dt) / (rovr + _h / _dt);
                rslovr = rovr * cslsur[solnum];
            }

            rsloff[solnum] = rslovr - qslbp[solnum];
            //     get solute fluxes
            for (int i = 1; i <= n; i++)
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i], floatComparisonTolerance))
                {
                    double dfac = 0.5 * (th[i - 1] + th[i]) * dc[solnum][i] / (x[i] - x[i - 1]);
                    double aq = Math.Abs(q[i]);
                    double accept = Math.Max(1.0, -SoluteSpaceWeightingFactor);
                    double wt = 0.0;
                    if (aq != 0.0)
                        wt = MathUtilities.Sign(Math.Max(0.0, 1.0 - 2.0 * accept * dfac / aq), q[i]);
                    //Peter's CHANGES 21/10/98 to remove/restore Crank-Nicolson time weighting
                    //for convection
                    //            g%qsl(solnum,i)=g%qsl(solnum,i)
                    //     :                    +.25*g%q(i)*((1.+wt)*g%csl(solnum,i-1)
                    //     :                    +(1.-wt)*g%csl(solnum,i))
                    //     1                    +dfac*(g%csl(solnum,i-1)-g%csl(solnum,i))
                    qsl[solnum][i] = qsl[solnum][i] + wtime * q[i] * ((1.0 + wt) * csl[solnum][i - 1]
                                     + (1.0 - wt) * csl[solnum][i]) + dfac * (csl[solnum][i - 1] - csl[solnum][i]);
                }

            }
            for (int i = 2; i < n; i++)
            {
                if (MathUtilities.FloatsAreEqual(x[i - 1], x[i], floatComparisonTolerance))
                {
                    qsl[solnum][i] = (dx[i] * qsl[solnum][i - 1] + dx[i - 1] * qsl[solnum][i + 1]) / (dx[i - 1] + dx[i]);
                }
            }

            rslex[solnum] = 0.0;
            for (int i = 0; i <= n; i++)
            {
                //nh         j=indxsl(solnum,i)
                j = i;
                double cp = 1.0;
                if (!MathUtilities.FloatsAreEqual(fip[solnum][i], 1.0, floatComparisonTolerance))
                {
                    cp = 0.0;
                    if (csl[solnum][i] > 0.0)
                        cp = Math.Pow(csl[solnum][i], fip[solnum][i] - 1.0);
                }
                cslt[solnum][i] = (th[i] + ex[solnum][j] * cp) * csl[solnum][i];

                for (int crop = 0; crop < num_crops; crop++)
                    rslex[solnum] += qr[i][crop] * csl[solnum][i] * Slupf(crop, solnum);

                qsls[solnum][i] += (csl[solnum][i] * (thold[i] + ex[solnum][j] * cp) / _dt) * dx[i];
            }

            if (solute_bbc == constant_conc)
            {
                //        constant concentration
                //nh         j=indxsl(solnum,p%n)
                j = n;
                qsl[solnum][n + 1] = qsl[solnum][n] - qsls[solnum][n] - qssof[n] * csl[solnum][n];
                //nh     :                  -g%qex(p%n)*g%csl(solnum,p%n)*p%slupf(solnum)
                //nh     :              -g%qex(p%n)*g%csl(solnum,p%n)*apswim_slupf(1,solnum)

                for (int crop = 0; crop < num_crops; crop++)
                    qsl[solnum][n + 1] -= qr[n][crop] * csl[solnum][n] * Slupf(crop, solnum);
            }
            else
            {
                //        convection only
                //CHANGES 6/11/98 to remove/restore Crank-Nicolson time weighting for convection
                //-----
                //         g%qsl(solnum,p%n+1)=.5*g%q(p%n+1)*(csln+g%csl(solnum,p%n))
                qsl[solnum][n + 1] = 0.5 * q[n + 1] * (wtime1 * csln + 4.0 * wtime * csl[solnum][n]);
            }
        }

        private void GetSoluteVariables()
        {
            for (int solnum = 0; solnum < num_solutes; solnum++)
            {
                double[] solute_n = ConcWaterSolute(solnum);
                for (int node = 0; node <= n; node++)
                    csl[solnum][node] = solute_n[node];
            }
        }

        private void GetFlow(string flowName, out double[] flowArray, out bool flowFlag)
        {
            //+  Initial Data Values
            // set to false to start - if match is found it is
            // set to true.
            flowFlag = false;

            //string flowUnits;
            flowArray = new double[n + 1];

            if (flowName == "water")
            {
                flowFlag = true;
                //flowUnits = "(mm)";
                for (int node = 0; node <= n + 1; node++)
                    flowArray[node] = TD_wflow[node];
            }
            else
            {
                for (int solnum = 0; solnum < num_solutes; solnum++)
                {
                    if (solute_names[solnum] == flowName)
                    {
                        for (int node = 0; node <= n + 1; node++)
                            flowArray[node] = TD_sflow[solnum][node];
                        flowFlag = true;
                        //flowUnits = "(kg/ha)";
                        return;
                    }
                }
            }
        }

        private double[] ConcWaterSolute(int solnum)
        {
            double[] concWaterSolute = new double[n + 1]; // Init with zeroes
            double[] solute_n = new double[n + 1]; // solute at each node

            if (solnum >= 0)
            {
                solute_n = (double[])solutes[solnum].kgha.Clone();

                for (int node = 0; node <= n; node++)
                {
                    // Note: Sometimes small numerical errors can leave -ve concentrations.
                    // This will check for -ve or very small values being passed by other modules
                    //  and define the appropriate response:
                    if (solute_n[node] < -(negative_conc_fatal))
                    {
                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6}",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        solute_n[node] = 0.0;
                        throw new Exception("-ve value for solute was passed to SWIM" + mess);
                    }
                    else if (solute_n[node] < -(negative_conc_warn))
                    {
                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6} - Value will be set to zero",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        summary.WriteMessage(this, "'-ve value for solute was passed to SWIM" + mess, MessageType.Warning);

                        solute_n[node] = 0.0;
                    }
                    else if (solute_n[node] < 1e-100)
                    {
                        // Value is REALLY small, no need to tell user,
                        // set value to zero to avoid underflow with reals
                        solute_n[node] = 0.0;
                    }

                    // convert solute from kg/ha to ug/cc soil
                    // ug Sol    kg Sol    ug   ha(node)
                    // ------- = ------- * -- * -------
                    // cc soil   ha(node)  kg   cc soil

                    cslstart[solnum][node] = solute_n[node];
                    solute_n[node] = solute_n[node]
                                     * 1.0e9               // ug/kg
                                     / (dx[node] * 1.0e8); // cc soil/ha

                    concWaterSolute[node] = SolveFreundlich(node, solnum, solute_n[node]);
                }
            }
            else
                throw new Exception("You have asked apswim to use a solute that it does not know about. Number = " + solnum);
            return concWaterSolute;
        }

        private double[] ConcAdsorptionSolute(int solnum)
        {
            //+  Purpose
            //      Calculate the concentration of solute adsorbed (ug/g soil). Note that
            //      this routine is used to calculate output variables and input
            //      variablesand so can be called at any time during the simulation.
            //      It therefore must use a solute profile obtained from the solute's
            //      owner module.  It therefore also follows that this routine cannot
            //      be used for internal calculations of solute concentration during
            //      the process stage etc.
            //+  Changes
            //     30-01-2010 - RCichota - added test for -ve values, causes a fatal error if so

            double[] concAdsorpSolute = new double[n + 1];  // init with zeroes
            double[] solute_n = null; // solute at each node

            if (solnum >= 0)
            {
                for (int node = 0; node <= n; node++)
                {
                    solute_n = (double[])solutes[solnum].kgha.Clone();

                    //````````````````````````````````````````````````````````````````````````````````
                    //RC            Changes by RCichota, 30/Jan/2010
                    // Note: Sometimes small numerical errors can leave -ve concentrations.
                    // This will check for -ve or very small values being passed by other modules
                    //  and define the appropriate response:

                    if (solute_n[node] < -(negative_conc_fatal))
                    {

                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6}",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        throw new Exception("-ve value for solute was passed to SWIM" + Environment.NewLine + mess);
                    }

                    else if (solute_n[node] < -(negative_conc_warn))
                    {
                        string mess = String.Format("   Total {0}({1,3}) = {2,12:G6} - Value will be set to zero",
                                        solute_names[solnum],
                                        node,
                                        solute_n[node]);
                        summary.WriteMessage(this, "'-ve value for solute was passed to SWIM" + Environment.NewLine + mess, MessageType.Warning);

                        solute_n[node] = 0.0;
                    }

                    else if (solute_n[node] < 1.0e-100)
                    {
                        // Value is REALLY small, no need to tell user,
                        // set value to zero to avoid underflow with reals

                        solute_n[node] = 0.0;
                    }

                    // else Value is positive and considerable

                    //````````````````````````````````````````````````````````````````````````````````

                    // convert solute from kg/ha to ug/cc soil
                    // ug Sol    kg Sol    ug   ha(node)
                    // ------- = ------- * -- * -------
                    // cc soil   ha(node)  kg   cc soil

                    solute_n[node] = solute_n[node]   // kg/ha
                                * 1.0e9               // ug/kg
                                / (dx[node] * 1.0e8); // cc soil/ha

                    double concWaterSolute = SolveFreundlich(node, solnum, solute_n[node]);

                    //                  conc_adsorb_solute(node) =
                    //     :              ddivide(solute_n(node)
                    //     :                         - conc_water_solute * g%th(node)
                    //     :                      ,p%rhob(node)
                    //     :                      ,0d0)

                    concAdsorpSolute[node] = ex[solnum][node] * Math.Pow(concWaterSolute, fip[solnum][node]);
                }
            }

            else
                throw new Exception("You have asked apswim to use a solute that it does not know about.");
            return concAdsorpSolute;
        }

        private double SolveFreundlich(int node, int solnum, double Ctot)
        {
            //+  Purpose
            //   Calculate the solute in solution for a given total solute
            //   concentration for a given node.
            //+  Changes
            //   RCichota - 30/Jan/2010 - update name of proc., organize the newton solution
            //                 added tests for -ve and very small values

            //*+  Constant Values
            const int max_iterations = 1000;
            const double tolerance = 1e-10;

            //RC    Changes by RCichota, 30/Jan/2010, updated in 10/Jul/2010
            // Organize solution and add test for no adsorption or linear adsorption

            bool solved = false;
            double Cw;
            double f;
            double dfdCw;
            if (Math.Abs(Ctot) < 1e-100)
            {
                // really small value or zero, solution is zero

                solved = true;
                Cw = 0.0;
            }
            else if (Ctot < 0.0)
            {
                // negative value for Ctot, this should have been catched already
                string mess = String.Format("   Total {0}({1,3}) = {2,12:G6}",
                                           solute_names[solnum],
                                           node,
                                           Ctot);
                throw new Exception("-ve concentration was passed to Freundlich solution" + mess);
            }
            else
            {
                // Ctot is OK, proceed to calculations

                // Check for no adsorption and whether the isotherm is linear
                if (MathUtilities.FloatsAreEqual(ex[solnum][node], 0.0, floatComparisonTolerance))
                {
                    //There is no adsorption:

                    Cw = MathUtilities.Divide(Ctot, th[node], 0.0);
                    solved = true;
                }

                else if (MathUtilities.FloatsAreEqual(fip[solnum][node], 0.0, floatComparisonTolerance))
                {
                    // Full adsorption, solute is immobile:

                    Cw = 0.0;
                    solved = true;
                }

                else if (MathUtilities.FloatsAreEqual(fip[solnum][node], 1.0, floatComparisonTolerance))
                {
                    // Linear adsorption:

                    Cw = MathUtilities.Divide(Ctot, th[node] + ex[solnum][node], 0.0);
                    solved = true;
                }
                else
                {
                    // Non linear isotherm:

                    // take initial guess for Cw
                    Cw = Math.Pow(MathUtilities.Divide(Ctot, (th[node] + ex[solnum][node]), 0.0), (1.0 / fip[solnum][node]));
                    if (Cw < 0.0)            // test added by RCichota 09/Jul/2010
                    {
                        string mess = String.Format("  {0}({1}) = {2,12:G6} - Iteration: 0",
                                            solute_names[solnum],
                                            node,
                                            Cw);
                        throw new Exception("-ve value for Cw on solving Freundlich1" + mess);
                    }

                    // calculate value of isotherm function and the derivative.

                    Freundlich(node, solnum, ref Cw, out f, out dfdCw);

                    double error_amount = f - Ctot;

                    if (Math.Abs(error_amount) < tolerance)
                    {
                        // It is already solved

                        solved = true;
                    }
                    else if (Math.Abs(dfdCw) < 1e-100)
                    {
                        // We are at zero (approximately) so Cw must be zero - this is a solution too

                        Cw = 0.0;
                        solved = true;
                    }
                    //            elseif (dfdCw) .gt. 1d100) then
                    //                derivative is too large, so Cw must be zero - this is a solution too
                    //               Cw = 0d0
                    //               solved = .true.

                    else
                    {
                        // Iterate until a solution is found or max_iterations is reached

                        solved = false;
                        for (int iter = 0; iter < max_iterations; iter++)
                        {

                            // next value for Cw
                            Cw = Cw - MathUtilities.Divide(error_amount, 2 * dfdCw, 0.0);
                            if (Cw < 0.0)             // test added by RCichota 09/Jul/2010
                            {
                                string mess = String.Format("  {0}({1}) = {2,12:G6} - Iteration: {3}",
                                              solute_names[solnum],
                                              node,
                                              Cw,
                                              iter);
                                throw new Exception("-ve value for Cw on solving Freundlich2" + mess);
                            }

                            // calculate new value of isotherm function and derivative.
                            Freundlich(node, solnum, ref Cw, out f, out dfdCw);
                            error_amount = f - Ctot;

                            if (Math.Abs(error_amount) < tolerance)
                            {
                                solved = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (solved)
            {
                //````````````````````````````````````````````````````````````````````````````````
                // Note: Sometimes small numerical errors can leave -ve concentrations.
                // This will evaluate the error and define the appropriate response:
                //RC      Changes by RCichota, 30/Jan/2010

                if (Math.Abs(Cw) < 1e-100)
                {
                    // Value is REALLY small or zero and can be diregarded,
                    //  set value to zero to avoid underflow with reals

                    Cw = 0.0;
                }

                else if (Cw < 0)
                {
                    // Cw is negative, this is a fatal error.
                    string mess = String.Format(" {0}({1,3}) = {2,12:G6}",
                                         solute_names[solnum],
                                         node,
                                         Cw);
                    throw new Exception("-ve value for solute found in adsorption isotherm" + mess);
                    //Cw = 0.0;
                }
                // else Cw is positive and considerable

                //````````````````````````````````````````````````````````````````````````````````

                // Publish the computed value
                return Cw;
            }

            else
            {
                // A solution was not found

                throw new Exception("APSwim failed to solve the freundlich isotherm");
                //return MathUtilities.Divide(Ctot, th[node], 0.0);
            }
        }

        private void Freundlich(int node, int solnum, ref double Cw, out double Ctot, out double dCtot)
        {
            //+  Changes
            //   RCichota - 30/Jan/2010 - implement test for -ve values passed in, test for linear isotherm
            //                  and test for very small values (these were ammended in 10/Jul/2010)

            // first make sure Cw has not been passed negative. Changes by RCichota, 30/jan/2010, ammended 09/Jul/2010

            if (Math.Abs(Cw) < 1e-100)
            {
                // Cw is zero or REALLY small and can be diregarded,
                //  set to zero to avoid underflow with reals

                Cw = 0.0;
                Ctot = 0.0;
                dCtot = th[node];   //if n<1 it actually equals infinity at Cw = zero
                                    //this value will not be used if Cw = 0
            }
            else if (Cw < 0.0)
            {
                // Cw is negative, this is a fatal error.
                string mess = String.Format(" Solution {0}({1,3}) = {2,12:G6}",
                                     solute_names[solnum],
                                     node,
                                     Cw);
                throw new Exception("-ve value has been passed to Freundlich solution" + mess);
                //Ctot = 0.0;
                //dCtot = 0.0;
            }
            else
            {
                // Cw is positive, proceed with the calculations

                if ((MathUtilities.FloatsAreEqual(fip[solnum][node], 1.0, floatComparisonTolerance))
                     || (MathUtilities.FloatsAreEqual(ex[solnum][node], 0.0, floatComparisonTolerance)))
                {
                    // Linear isotherm or no adsorption

                    Ctot = Cw * (th[node] + ex[solnum][node]);
                    dCtot = th[node] + ex[solnum][node];
                }
                else
                {
                    // nonlinear isotherm
                    Ctot = th[node] * Cw + ex[solnum][node] * Math.Pow(Cw, fip[solnum][node]);
                    dCtot = th[node] + ex[solnum][node] * fip[solnum][node] * Math.Pow(Cw, fip[solnum][node] - 1.0);
                }
            }

            //````````````````````````````````````````````````````````````````````````````````
            // Check for very small values, set to zero to avoid underflow with reals

            if (Math.Abs(Ctot) < 1e-100)
                Ctot = 0.0;

            if (Math.Abs(dCtot) < 1e-100)
                dCtot = 0.0;
        }

        private double Wpf()
        {
            //     Short description:
            //     gets water present in profile
            double wpf = 0.0;
            for (int i = 0; i <= n; i++)
            {
                wpf += th[i] * dx[i];
            }
            return wpf;
        }

        private double Pf(double psiValue)
        {
            //     Short description:
            //     returns transform p
            const double psi_0 = -50.0;
            const double psi_1 = psi_0 / 10.0;

            double v = -(psiValue - psi_0) / psi_1;
            if (psiValue < psi_0)
                return Math.Log(v + Math.Sqrt(v * v + 1.0));
            else
                return v;
        }

        private void PStat(int istat, ref double tresp)
        {
            //     Short Description:
            //     gets potl evap. for soil and veg., and root length densities
            //
            //     g%resp,p%slupf and g%csl were renamed to tslupf,trep,tcsl as there were
            //     already variables with those names in common

            if (istat == 0)
            {
                // calc. potl evap.
                double sep;  // soil evaporation demand
                double rep = (CEvap(t) - CEvap(t - _dt)) / _dt;
                if (cover_effects)
                {

                    // Use Soilwat cover effects on evaporation.
                    sep = rep * _dt * CoverEosRedn();
                }
                else
                {
                    sep = rep * _dt * (1.0 - crop_cover);
                }

                // Note: g%pep is passed to swim as total ep for a plant for the
                // entire apsim timestep. so rate will be (CEp = cum EP)
                //   dCEp   Total daily EP     dEo
                //   ---- = -------------- p%x --------
                //   g%dt    Total daily Eo      g%dt

                double start_of_day = Time(year, day, _apsimTimeMinutes);
                double end_of_day = Time(year, day, _apsimTimeMinutes + (int)apsim_timestep);

                double TD_Eo = CEvap(end_of_day) - CEvap(start_of_day);

                for (int j = 0; j < nveg; j++)
                    rtp[j] = MathUtilities.Divide(pep[j], TD_Eo, 0.0) * rep;

                // pot soil evap rate is not linked to apsim timestep
                tresp = sep / _dt;
            }
            else if (istat == 1)
            {
                //         update cumulative transpiration
                for (int i = 0; i < nveg; i++)
                {
                    ctp[i] += rtp[i] * _dt;
                    ct[i] += rt[i] * _dt;
                    //cnh
                    for (int j = 0; j <= n; j++)
                    {
                        pwuptake[i][j] += qr[j][i] * _dt * 10.0;
                        // cm -> mm __/
                        pwuptakepot[i][j] += qrpot[j][i] * _dt * 10.0;
                        // cm -> mm __/
                    }
                }
            }
            else if (istat == 2)
            {
                //        update cumulative solute uptake

                for (int i = 0; i < nveg; i++)
                    for (int j = 0; j <= n; j++)
                        for (int solnum = 0; solnum < num_solutes; solnum++)
                            psuptake[solnum][i][j] += Slupf(i, solnum) * csl[solnum][j] * qr[j][i] / 10.0 * _dt;
                // nh     :                                  slupf[solnum] * csl[solnum][j] * qr[j][i] / 10.0 * _dt
                //   /
                // ppm -> kg/ha
                //        this doesn't make sense....g%csl has already been changed from it
                //        was at the start of the timestep.  need to find a different way
                //        of calculating it.  what about qsl???
                //        or try getting g%csl at start of timestep.
                //        BUT NUMBERS DO ADD UP OK????? does he then update at start of next
                //        timestep??????? !!
            }
        }

        private double Slupf(int crop, int solnum)
        {
            return 0.0;
        }

        private double CoverEosRedn()
        {
            //+  Purpose
            //      Calculate reduction in potential soil evaporation
            //      due to residues on the soil surface.
            //      Approach taken from directly from Soilwat code.

            //---------------------------------------+
            // reduce Eo to that under plant CANOPY                    <DMS June 95>
            //---------------------------------------+

            //  Based on Adams, Arkin & Ritchie (1976) Soil Sci. Soc. Am. J. 40:436-
            //  Reduction in potential soil evaporation under a canopy is determined
            //  the "% shade" (ie cover) of the crop canopy - this should include g%th
            //  green & dead canopy ie. the total canopy cover (but NOT near/on-grou
            //  residues).  From fig. 5 & eqn 2.                       <dms June 95>
            //  Default value for c%canopy_eos_coef = 1.7
            //              ...minimum reduction (at cover =0.0) is 1.0
            //              ...maximum reduction (at cover =1.0) is 0.183.

            double eos_canopy_fract = Math.Exp(-canopy_eos_coef * crop_cover);

            //-----------------------------------------------+
            // reduce Eo under canopy to that under mulch            <DMS June 95>
            //-----------------------------------------------+

            //1a. adjust potential soil evaporation to account for
            //    the effects of surface residue (Adams et al, 1975)
            //    as used in Perfect
            // BUT taking into account that residue can be a mix of
            // residues from various crop types <dms june 95>

            //    [DM. Silburn unpublished data, June 95 ]
            //    <temporary value - will reproduce Adams et al 75 effect>
            //     c%A_to_evap_fact = 0.00022 / 0.0005 = 0.44

            double eos_residue_fract = Math.Pow(1.0 - residue_cover, a_to_evap_fact);


            return eos_canopy_fract * eos_residue_fract;
        }

        private void Watvar(int ix, double tp, out double tpsi, out double psip, out double psipp, out double tth, ref double thp, out double thk, ref double hkp)
        {
            //     Short Description:
            //     calculates water variables from transform value g%p at grid point ix
            //     using cubic interpolation between given values of water content p%wc,
            //     log10 conductivity p%hkl, and their derivatives p%wcd, p%hkld with respect
            //     to log10 suction p%sl
            //
            //     nih - some local variables had the same name as globals so I had
            //     to rename them. I added a g%t (for temp) to start of name for
            //     g%psi, g%hk, g%p, g%th, p%x, p%dx,g%dc

            //     notes

            //         dTheta     dTheta       d(log g%psi)
            //         ------ = ----------  p%x  ---------
            //           dP     d(log g%psi)        d g%p

            //                    dTheta        d g%psi           1
            //                = ----------  p%x  -------  p%x ------------
            //                  d(log g%psi)       d g%p       ln(10).g%psi


            //         dHK          dHK       d(log g%psi)
            //        ------  = ----------  p%x ----------
            //          dP      d(log g%psi)       d g%p

            //                   ln(10).g%hk   d(log(g%hk))     dPsi        1
            //                =  --------- p%x ----------  p%x ------ p%x ----------
            //                        1      d(log(g%psi))     dP     ln(10).g%psi

            //                    g%hk       d(log(g%hk))     dPsi
            //                =  -----  p%x  ----------  p%x  ----
            //                    g%psi      d(log(g%psi))     dP

            //     note:- d(log(y)/p%dx) = 1/y . dy/p%dx
            //
            //     Therefore:-
            //
            //            d(log10(y))/p%dx = d(ln(y)/ln(10))/p%dx
            //                           = 1/ln(10) . d(ln(y))/p%dx
            //                           = 1/ln(10) . 1/y . dy/p%dx


            //     Constant Values
            const double al10 = 2.3025850929940457;
            const double vcon1 = 7.28e-9;
            const double vcon2 = 7.26e-7;

            double thd, hklg, hklgd;

            Trans(tp, out tpsi, out psip, out psipp);

            Interp(ix, tpsi, out tth, out thd, out hklg, out hklgd);

            thk = Math.Exp(al10 * hklg);

            if (tpsi != 0.0)
            {
                thp = (thd * psip) / (al10 * tpsi);
                hkp = (thk * hklgd * psip) / tpsi;
            }

            double thsat = physical.SAT[ix];  // NOTE: this assumes that the wettest p%wc is
                                              //! first in the pairs of log suction vs p%wc

            // EJZ - this was in the fortran source, but is clearly futile
            //if (thsat == 0.0)
            //    thsat = _sat[ix];

            if (VC)
            {
                //        add vapour conductivity hkv
                double phi = thsat / 0.93 - tth;
                double hkv = vcon1 * phi * Math.Exp(vcon2 * tpsi);
                thk = thk + hkv;
                hkp = hkp + hkv * (vcon2 * psip - thp / phi);
            }
        }

        private void Trans(double p, out double psi, out double psip, out double psipp)
        {
            //     Short description:
            //     gets psi and its partial derivitives

            // Constants
            const double psi_0 = -50e0;
            const double psi_1 = psi_0 / 10.0;

            if (p < 0.0)
            {
                double ep = Math.Exp(p);
                double emp = 1.0 / ep;
                double sinhp = 0.5 * (ep - emp);
                double coshp = 0.5 * (ep + emp);
                double v = psi_1 * sinhp;
                psi = psi_0 - v;
                psip = -psi_1 * coshp;
                psipp = -v;
            }
            else
            {
                psi = psi_0 - psi_1 * p;
                psip = -psi_1;
                psipp = 0.0;
            }
        }

        private void Thomas(int istart, int n, ref double[] a, ref double[] b, ref double[] c, ref double[] rhs, ref double[] d, ref double[] v, ref PondingData pondingData, out bool fail)
        {
            //     Short description:
            //     Thomas algorithm for solving tridiagonal system of eqns

            fail = true; // Indicate failure if we return early

            double piv = b[istart];
            if (istart == -1)
                piv = pondingData.b;
            if (piv == 0.0)
                return;
            if (istart == -1)
                pondingData.v = pondingData.rhs / piv;
            else
                v[istart] = rhs[istart] / piv;
            for (int i = istart + 1; i < istart + n; i++)
            {
                if (i == 0)
                    d[i] = pondingData.c / piv;
                else
                    d[i] = c[i - 1] / piv;
                piv = b[i] - a[i] * d[i];
                if (piv == 0.0)
                    return;
                if (i == 0)
                    v[i] = (rhs[i] - a[i] * pondingData.v) / piv;
                else
                    v[i] = (rhs[i] - a[i] * v[i - 1]) / piv;
            }

            for (int i = istart + n - 2; i >= istart; i--)
            {
                if (i == -1)
                    pondingData.v = pondingData.v - d[i + 1] * v[i + 1];
                else
                    v[i] = v[i] - d[i + 1] * v[i + 1];
            }

            fail = false;
        }

        private void Baleq(int it, ref int iroots, ref double[] tslos, ref double[][] tcsl, out int ibegin, out int iend, ref double[] a, ref double[] b, ref double[] c, ref double[] rhs, ref PondingData pondingData)
        {
            //     Short Description:
            //     gets coefficient matrix and rhs for Newton soln of balance eqns
            //
            //     Some variables had the same name as some global variables and so
            //     these were renamed (by prefixing with g%t - for temp)
            //     this include p%isol, g%csl, p%slos

            const double hcon = 7.0e-7;
            const double hair = 0.5;

            double[] psip = new double[n + 1];
            double[] psipp = new double[n + 1];
            double[] thp = new double[n + 1];
            double[] hkp = new double[n + 1];
            double[] qsp = new double[n + 1];
            double[] qp1 = new double[n + 2];
            double[] qp2 = new double[n + 2];
            double[] psios = new double[n + 1];
            double[,] qexp = new double[3, n + 1];
            double[] qdrain = new double[n + 1];
            double[] qdrainpsi = new double[n + 1];
            double[] qssofp = new double[n + 1];
            double v1;

            //   initialise for first iteration
            if (it == 1)
            {
                ifirst = 0;
                ilast = n;
                if (itbc == 2 && hold > 0.0)
                    ifirst = -1;
                if (ibbc == 0)
                    gr = bbc_value;

                if (ibbc == 1)
                {
                    _psi[n] = bbc_value;
                    _p[n] = Pf(_psi[n]);
                }
            }

            //   get soil water variables and their derivatives
            for (int i = 0; i <= n; i++)
                Watvar(i, _p[i], out _psi[i], out psip[i], out psipp[i], out th[i], ref thp[i], out hk[i], ref hkp[i]);

            //   check boundary potls
            if (itbc == 0 && isbc == 0 && _psi[0] > 0.0)
            {
                //        infinite conductance and no ponding allowed
                _psi[0] = 0.0;
                _p[0] = Pf(_psi[0]);
                Watvar(0, _p[0], out v1, out psip[0], out psipp[0], out th[0], ref thp[0], out hk[0], ref hkp[0]);
            }
            if (ibbc == 3 && _psi[n] > bbc_value)
            {
                //        seepage at bottom boundary
                _psi[n] = bbc_value;
                _p[n] = Pf(_psi[n]);
                Watvar(n, _p[n], out v1, out psip[n], out psipp[n], out th[n], ref thp[n], out hk[n], ref hkp[n]);
            }

            //   get fluxes between nodes
            double absgf = Math.Abs(gf);
            double w1, w2;
            double deltap;
            double deltax;
            double skd;
            double hkdp1;
            double hkdp2;
            double hsoil;
            for (int i = 1; i <= n; i++)
            {
                if (!MathUtilities.FloatsAreEqual(x[i - 1], x[i], floatComparisonTolerance))
                {
                    deltax = x[i] - x[i - 1];
                    deltap = _p[i] - _p[i - 1];
                    double hkd1 = hk[i - 1] * psip[i - 1];
                    double hkd2 = hk[i] * psip[i];
                    hkdp1 = hk[i - 1] * psipp[i - 1] + hkp[i - 1] * psip[i - 1];
                    hkdp2 = hk[i] * psipp[i] + hkp[i] * psip[i];
                    skd = hkd1 + hkd2;
                    if (SpaceWeightingFactor >= 0.5 && SpaceWeightingFactor <= 1.0)
                    {
                        //              use fixed space weighting on gravity flow
                        w1 = MathUtilities.Sign(2.0 * SpaceWeightingFactor, gf);
                    }
                    else
                    {
                        //              use central diffs for gravity flow if possible, else use
                        //                 just enough upstream weighting to avoid instability
                        //                 user may increase acceptable level for central diffs
                        //                 by setting p%swt < -1
                        double accept = Math.Max(1.0, -SpaceWeightingFactor);
                        double wt = 0.0;
                        //               if(absgf.ne.0..and.hkp(i).ne.0.)then
                        double gfhkp = gf * hkp[i];
                        if (gfhkp != 0.0)
                        {
                            if (it == 1)
                            {
                                //                     value=1.-accept*(skd+(g%p(i)-g%p(i-1))*hkdp2)/(absgf*deltax*hkp(i))
                                double value = 1.0 - accept * (skd) / (Math.Abs(gfhkp) * deltax);
                                //                     value=min(1d0,value)
                                swta[i] = MathUtilities.Sign(Math.Max(0.0, value), gfhkp);
                            }
                            wt = swta[i];
                        }

                        w1 = 1.0 + wt;
                    }
                    w2 = 2.0 - w1;

                    if ((w1 > 2.0) || (w1 < 0.0))
                        summary.WriteMessage(this, "bad space weighting factor", MessageType.Warning);

                    q[i] = -0.5 * (skd * deltap / deltax - gf * (w1 * hk[i - 1] + w2 * hk[i]));
                    qp1[i] = -0.5 * ((hkdp1 * deltap - skd) / deltax - gf * w1 * hkp[i - 1]);
                    qp2[i] = -0.5 * ((hkdp2 * deltap + skd) / deltax - gf * w2 * hkp[i]);

                    _swf[i] = w1;
                }
            }
            //   get fluxes to storage
            for (int i = 0; i <= n; i++)
            {
                qs[i] = (th[i] - thold[i]) * dx[i] / _dt;
                qsp[i] = thp[i] * dx[i] / _dt;
            }

            rex = 0.0;
            for (int i = 0; i <= n; i++)
                rex = rex + qex[i];

            //   NIH  get subsurface fluxes
            Drain(out qdrain, out qdrainpsi);

            rssf = 0.0;
            for (int i = 0; i <= n; i++)
            {
                qssif[i] = SubSurfaceInFlow[i] / 10.0 / 24.0; // assumes mm and daily timestep - need something better !!!!
                qssof[i] = qdrain[i]; // Add outflow calc here later
                qssofp[i] = qdrainpsi[i] * psip[i];
                rssf += qssif[i] - qssof[i];
            }

            //   get soil surface fluxes, taking account of top boundary condition
            //
            double respsi;
            double roffd;
            if (itbc == 0)
            {
                //       infinite conductance
                ifirst = 0;
                if (_psi[0] < 0.0)
                {
                    hsoil = Math.Max(hair, Math.Exp(hcon * _psi[0]));
                    res = resp * (hsoil - hair) / (1.0 - hair);
                    respsi = resp * hcon * hsoil / (1.0 - hair);
                }
                else
                {
                    res = resp;
                    respsi = 0.0;
                }

                if (isbc == 0)
                {
                    //           no ponding allowed
                    _h = 0.0;
                    double q0 = ron - res + hold / _dt;

                    if (_psi[0] < 0.0 || q0 < qs[0] + qex[0] + q[1] - qssif[0] + qssof[0])
                    {
                        q[0] = q0;
                        qp2[0] = -respsi * psip[0];
                        roff = 0.0;
                        roffd = 0.0;
                    }
                    else
                    {
                        //              const zero potl
                        ifirst = 1;
                        q[0] = qs[0] + qex[0] + q[1] - qssif[0] + qssof[0];
                        roff = q0 - q[0];
                        roffd = -qp2[1];
                    }
                }
                else
                {
                    //           runoff zero or given by a function
                    if (_psi[0] < 0.0)
                    {
                        _h = 0.0;
                        roff = 0.0;
                        q[0] = ron - res + hold / _dt;
                        qp2[0] = -respsi * psip[0];
                    }
                    else
                    {
                        _h = _psi[0];
                        roff = 0.0;
                        roffd = 0.0;
                        if (isbc == 2)
                            CalculateRunoff(t, _h, out roff, out roffd);
                        q[0] = ron - roff - res - (_h - hold) / _dt;
                        qp2[0] = (-roffd - respsi - 1.0 / _dt) * psip[0];
                    }
                }
            }

            if (itbc == 1)
            {
                //       const potl
                ifirst = 1;
                if (_psi[0] < 0.0)
                {
                    hsoil = Math.Exp(hcon * _psi[0]);
                    res = resp * (hsoil - hair) / (1.0 - hair);
                }
                else
                {
                    res = resp;
                }
                _h = Math.Max(_psi[0], 0.0);
                q[0] = qs[0] + qex[0] + q[1] - qssif[0] + qssof[0];
                //        flow to source of potl treated as "runoff" (but no bypass flow)
                roff = ron - res - (_h - hold) / _dt - q[0];
            }
            else if (itbc == 2)
            {
                //       conductance given by a function
                double g_, gh;
                double q0 = ron - resp + hold / _dt;
                if (isbc == 0)
                {
                    //           no ponding allowed
                    ifirst = 0;
                    _h = 0.0;
                    SCond(t, _h, out g_, out gh);

                    if (q0 > -g_ * _psi[0])
                    {
                        res = resp;
                        respsi = 0.0;
                        q[0] = -g_ * _psi[0];
                        qp2[0] = -g_ * psip[0];
                        roff = q0 - q[0];
                        roffd = -qp2[0];
                    }
                    else
                    {
                        hsoil = Math.Exp(hcon * _psi[0]);
                        res = resp * (hsoil - hair) / (1.0 - hair);
                        respsi = resp * hcon * hsoil / (1.0 - hair);
                        q0 = ron - res + hold / _dt;
                        q[0] = q0;
                        qp2[0] = -respsi * psip[0];
                        roff = 0.0;
                    }
                }
                else
                {
                    //           runoff zero or given by a function
                    SCond(t, _h, out g_, out gh);
                    if (q0 > -g_ * _psi[0])
                    {
                        //              initialise _h if necessary
                        if (ifirst == 0)
                            _h = Math.Max(_psi[0], 0.0);
                        ifirst = -1;
                        res = resp;
                        roff = 0.0;
                        roffd = 0.0;
                        if (isbc == 2 && _h > 0.0)
                            CalculateRunoff(t, _h, out roff, out roffd);
                        q[0] = g_ * (_h - _psi[0]);
                        qp1[0] = g_ + gh * (_h - _psi[0]);
                        qp2[0] = -g_ * psip[0];
                        // WE MAY NEED TO HANDLE THE -1 INDICES SOMEHOW (though I'm not sure they are ever used)
                        pondingData.rhs = -(ron - roff - res - q[0] - (_h - hold) / _dt);
                        pondingData.b = -roffd - qp1[0] - 1.0 / _dt;
                        pondingData.c = -qp2[0];
                        //rhs[-1] = -(ron - roff - res - q[0] - (_h - hold) / _dt);
                        //b[-1] = -roffd - qp1[0] - 1.0 / _dt;
                        //c[-1] = -qp2[0];
                    }
                    else
                    {
                        ifirst = 0;
                        _h = 0.0;
                        roff = 0.0;
                        hsoil = Math.Exp(hcon * _psi[0]);
                        res = resp * (hsoil - hair) / (1.0 - hair);
                        respsi = resp * hcon * hsoil / (1.0 - hair);
                        q[0] = ron - res + hold / _dt;
                        qp2[0] = -respsi * psip[0];
                    }
                }
            }
            //     bypass flow?
            qbp = 0.0;
            //qbpd = 0.0;
            //double qbpp = 0.0;
            //double qbps = 0.0;
            //double qbpsp = 0.0;

            //   bottom boundary condition
            if (ibbc == 0)
            {
                //       zero matric potl gradient
                q[n + 1] = (gf + gr) * hk[n];
                qp1[n + 1] = (gf + gr) * hkp[n];
            }
            else if (ibbc == 1)
            {
                //       const potl
                ilast = n - 1;
                q[n + 1] = q[n] - qs[n] - qex[n] + qssif[n] - qssof[n];
            }
            else if (ibbc == 2)
            {
                //       zero flux
                q[n + 1] = 0.0;
                qp1[n + 1] = 0.0;
            }
            else if (ibbc == 3)
            {
                //       seepage
                //nh added to allow seepage to user potential at bbc
                if (_psi[n] >= bbc_value)
                {
                    q[n + 1] = q[n] - qs[n] - qex[n] + qssif[n] - qssof[n];
                    if (q[n + 1] >= 0.0)
                    {
                        ilast = n - 1;
                        //qbpd = 0.0;
                    }
                    else
                    {
                        ilast = n;
                    }
                }
                if (ilast == n)
                {
                    q[n + 1] = 0.0;
                    qp1[n + 1] = 0.0;
                }
            }
            //    get Newton-Raphson equations
            int i1 = Math.Max(ifirst, 0);
            int k = i1 - 1;
            bool xidif = true;
            for (int i = i1; i <= ilast; i++)
            {
                //        allow for two nodes at same depth
                bool xipdif = true;
                if (xidif)
                {
                    k = k + 1;
                    int j = i + 1;
                    //           j is next different node, k is equation
                    if (i > 0 && i < n - 1)
                    {
                        if (MathUtilities.FloatsAreEqual(x[i], x[i + 1], floatComparisonTolerance))
                        {
                            xipdif = false;
                            j = i + 2;
                            q[i + 1] = ((x[j] - x[i]) * q[i] + (x[i] - x[i - 1]) * q[j]) / (x[j] - x[i - 1]);
                        }
                    }
                    rhs[k] = -(q[i] - q[j]);
                    a[k] = qp1[i];
                    b[k] = qp2[i] - qp1[j];
                    c[k] = -qp2[j];
                }
                rhs[k] = rhs[k] + qs[i] + qex[i] - qssif[i] + qssof[i];
                b[k] = b[k] - qsp[i] - qssofp[i];

                if (iroots == 0)
                {
                    //            a(k)=a(k)-qexp(1,i)
                    b[k] = b[k] - qexp[1, i];
                    //            c(k)=c(k)-qexp(3,i)
                }
                else
                {
                    iroots = 2;
                }
                xidif = xipdif;
            }

            ibegin = ifirst;
            iend = k;
        }

        private void SCond(double ttt, double tth, out double g_, out double gh)
        {
            //     Short Description:
            //     gets soil surface conductance g and derivative gh
            //
            //     g%t was renamed to ttt as g%t already exists in common
            //     g%h was renamed to tth as g%h already exists in common
            g_ = gsurf;
            gh = 0.0;
        }

        private void CalculateRunoff(double t, double h, out double roff, out double roffh)
        {
            //     Short Description:
            //     gets runoff rate

            if (h > _hmin)
            {
                double v = roff0 * Math.Pow(h - _hmin, roff1 - 1.0);
                roff = v * (h - _hmin);
                roffh = roff1 * v;
            }
            else
            {
                roff = 0.0;
                roffh = 0.0;
            }
        }

        private void Drain(out double[] qdrain, out double[] qdrainpsi)
        {
            //     Short Description:
            //     gets flow rate into drain
            //     All units are mm and days

            //     Constant Values
            const double dpsi = 0.01;

            qdrain = new double[n + 1];
            qdrainpsi = new double[n + 1];
            double wt_above_drain;
            double wt_above_drain2;
            double[] qdrain2 = new double[n + 1];

            if (subsurfaceDrain != null)
            {
                int drain_node = SoilUtilities.LayerIndexOfClosestDepth(physical.Thickness, subsurfaceDrain.DrainDepth);

                double d = subsurfaceDrain.ImpermDepth - subsurfaceDrain.DrainDepth;
                if (_psi[drain_node] > 0)
                    wt_above_drain = _psi[drain_node] * 10.0;
                else
                    wt_above_drain = 0.0;

                double q = Hooghoudt(d, wt_above_drain, subsurfaceDrain.DrainSpacing, subsurfaceDrain.DrainRadius, subsurfaceDrain.Klat);

                qdrain[drain_node] = q / 10.0 / 24.0;

                if (_psi[drain_node] + dpsi > 0.0)
                    wt_above_drain2 = (_psi[drain_node] + dpsi) * 10.0;
                else
                    wt_above_drain2 = 0.0;

                double q2 = Hooghoudt(d, wt_above_drain2, subsurfaceDrain.DrainSpacing, subsurfaceDrain.DrainRadius, subsurfaceDrain.Klat);

                qdrain2[drain_node] = q2 / 10.0 / 24.0;

                qdrainpsi[drain_node] = (qdrain2[drain_node] - qdrain[drain_node]) / dpsi;
            }
        }

        private double Hooghoudt(double d, double m, double L, double r, double Ke)
        {
            //  Purpose
            //       Drainage loss to subsurface drain using Hooghoudts drainage equation. (mm/d)


            const double C = 1.0;                 // ratio of flux between drains to flux midway between drains.
                                                  // value of 1.0 usually used as a simplification.
                                                  //double q;           // flux into drains (mm/s)
            double de;          // effective d to correct for convergence near the drain. (mm)
            double alpha;       // intermediate variable in de calculation

            if (d / L <= 0)
                de = 0.0;
            else if (d / L < 0.3)
            {
                alpha = 3.55 - 1.6 * (d / L) + 2 * (d / L) * (d / L);
                de = d / (1.0 + d / L * (8.0 / Math.PI * Math.Log(d / r) - alpha));
            }
            else
            {
                de = L * Math.PI / (8.0 * Math.Log(L / r) - 1.15);
            }

            return (8.0 * Ke * de * m + 4 * Ke * m * m) / (C * L * L);
        }
    }
}
