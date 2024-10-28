using System;
using System.Linq;
using System.Collections.Generic;
using Models.Core;
using Newtonsoft.Json;

namespace Models.Agroforestry
{
    /// <summary>
    /// The APSIM AgroforestrySystem model calculates interactions between trees and neighbouring crop or pasture zones.  The model is therefore derived from the Zone class within APSIM and includes child zones to simulate soil and plant processes within the system.  It obtains information from a tree model within its scope (ie a child) and uses information about the tree structure (such as height and canopy dimensions) to calculate microclimate impacts on its child zones.  Below-ground interactions between trees and crops or pastures are calculated by the APSIM SoilArbitrator model.
    /// 
    /// Windbreaks are simulated using an approach [Huthetal2002] that calculates windspeeds in the lee of windbreaks as a function distance (described in terms of multiples of tree heights) and windbreak optical porosity.
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class AgroforestrySystem : Zone
    {

        /// <summary>
        /// The reduction in wind as a fraction.
        /// </summary>
        [Units("0-1")]
        [JsonIgnore]
        public double Urel { get; set; }

        /// <summary>
        /// A list containing forestry information for each zone.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<IModel> ZoneList;

        /// <summary>
        /// Fraction of rainfall intercepted by canopy
        /// </summary>
        [Description("Fraction of rainfall incepted within the tree canopy (0-1)")]
        public double RainfallInterceptionFraction { get; set; }

        /// <summary>
        /// Width of the tree rain shadow in terms of tree heights
        /// </summary>
        [Description("Width of tree rainfall shadow (H)")]
        public double RainShaddowWidth { get; set; }

        /// <summary>
        /// Return the area of the zone.
        /// </summary>
        [JsonIgnore]
        public override double Area
        {
            get
            {
                double A = 0;
                foreach (Zone Z in this.FindAllChildren<Zone>())
                    A += Z.Area;
                return A;
            }
            set
            {
            }
        }

        /// <summary>
        /// A pointer to the tree model.
        /// </summary>
        [JsonIgnore]
        public TreeProxy tree = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            tree = FindChild<TreeProxy>();
            ZoneList = FindAllChildren<Zone>().ToList();
        }

        /// <summary>
        /// Passthrough for child nodes that need information from the tree.
        /// Saves having to query the simulation for the node location all the time.
        /// </summary>
        /// <param name="z">The zone.</param>
        /// <returns></returns>
        public double GetDistanceFromTrees(Zone z)
        {
            return tree.GetDistanceFromTrees(z);
        }

        /// <summary>
        /// Return the %Wind Reduction for a given zone
        /// </summary>
        /// <param name="z">Zone</param>
        /// <returns>%Wind Reduction</returns>
        public double GetWindReduction(Zone z)
        {
            foreach (Zone zone in ZoneList)
                if (zone == z)
                {
                    double UrelMin = Math.Max(0.0, 1.14 * 0.40 - 0.16); // 0.4 is porosity, will be dynamic in the future

                    if (tree.heightToday < 1)
                        Urel = 1;
                    else
                    {
                        tree.H = GetDistanceFromTrees(z) / tree.heightToday;
                        if (tree.H < 6)
                            Urel = UrelMin + (1 - UrelMin) / 2 - tree.H / 6 * (1 - UrelMin) / 2;
                        else if (tree.H < 6.1)
                            Urel = UrelMin;
                        else
                            Urel = UrelMin + (1 - UrelMin) / (1 + 0.000928 * Math.Exp(12.9372 * Math.Pow((tree.H - 6), -0.26953)));
                    }
                    return Urel;
                }
            throw new ApsimXException(this, "Could not find zone called " + z.Name);
        }


        /// <summary>
        /// Return the %Radiation Reduction for a given zone
        /// </summary>
        /// <param name="z">Zone</param>
        /// <returns>%Radiation Reduction</returns>
        public double GetRadiationReduction(Zone z)
        {

            if (GetDistanceFromTrees(z) > 0.0)
                    return 1.0-tree.GetShade(z)/100.0;
            else
                    return 1.0;

        }
    }
}
