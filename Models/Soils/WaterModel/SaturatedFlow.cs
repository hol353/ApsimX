using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.WaterModel
{

    /// <summary>
    /// When water content in any layer is below SAT but above DUL, a fraction of the water drains to the next
    /// deepest layer each day.
    ///
    /// Flux = SWCON x (SW - DUL)
    ///
    /// Infiltration or water movement into any layer that exceeds the saturation capacity of the layer automatically
    /// cascades to the next layer.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(WaterBalance))]
    public class SaturatedFlowModel : Model
    {
        /// <summary>The water movement model.</summary>
        [Link]
        private WaterBalance soil = null;

        /// <summary>Access the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>Amount of water (mm) backed up.</summary>
        [JsonIgnore]
        public double backedUpSurface { get; private set; }

        /// <summary>Perform the movement of water.</summary>
        public double[] Values
        {
            get
            {
                backedUpSurface = 0.0;

                double[] SW = soil.Water;
                double[] DUL = MathUtilities.Multiply(soilPhysical.DUL, soilPhysical.Thickness);
                double[] SAT = MathUtilities.Multiply(soilPhysical.SAT, soilPhysical.Thickness);

                double w_in = 0.0;   // water coming into layer (mm)
                double w_out;        // water going out of layer (mm)
                double[] flux = new double[soilPhysical.Thickness.Length];
                double[] newSWmm = new double[soilPhysical.Thickness.Length];
                for (int i = 0; i < soilPhysical.Thickness.Length; i++)
                {
                    double w_tot = SW[i] + w_in;

                    // Calculate EXCESS Amount (above SAT)

                    // get excess water above saturation & then water left
                    // to drain between sat and dul.  Only this water is
                    // subject to swcon. The excess is not - treated as a
                    // bucket model. (mm)

                    double w_excess;               // amount above saturation(overflow)(mm)
                    if (w_tot > SAT[i])
                    {
                        w_excess = w_tot - SAT[i];
                        w_tot = SAT[i];
                    }
                    else
                        w_excess = 0.0;

                    // Calculate water draining by gravity (mm) (between SAT and DUL)
                    double w_drain;
                    if (w_tot > DUL[i])
                        w_drain = (w_tot - DUL[i]) * soil.SWCON[i];
                    else
                        w_drain = 0.0;

                    // Calculate EXCESS Flow and DRAIN Flow (combined into Flux)
                    // if there is EXCESS Amount,
                    if (w_excess > 0.0)
                    {
                        if (soilPhysical.KS == null || soilPhysical.KS.Length == 0)
                        {
                            //! all this excess goes on down
                            w_out = w_excess + w_drain;
                            flux[i] = w_out;
                        }
                        else
                        {
                            // Calculate amount of water to backup and push down
                            // Firstly top up this layer (to saturation)
                            double add = Math.Min(w_excess, w_drain);
                            w_excess = w_excess - add;
                            newSWmm[i] = SAT[i] - w_drain + add;

                            // partition between flow back up and flow down
                            // 'excessDown' is the amount above saturation(overflow) that moves down (mm)
                            double excess_down = Math.Min(soilPhysical.KS[i] - w_drain, w_excess);
                            double backup = w_excess - excess_down;

                            w_out = excess_down + w_drain;
                            flux[i] = w_out;

                            // Starting from the layer above the current layer,
                            // Move up to the surface, layer by layer and use the
                            // backup to fill the space still remaining between
                            // the new sw_dep (that you calculated on the way down)
                            // and sat for that layer. Once the backup runs out
                            // it will keep going but you will be adding 0.
                            if (i > 0)
                            {
                                for (int j = i - 1; j >= 0; j--)
                                {
                                    flux[j] = flux[j] - backup;
                                    add = Math.Min(SAT[j] - newSWmm[j], backup);
                                    newSWmm[j] = newSWmm[j] + add;
                                    backup = backup - add;
                                }
                            }

                            backedUpSurface = backedUpSurface + backup;
                        }
                    }
                    else
                    {
                        // there is no EXCESS Amount so only do DRAIN Flow
                        w_out = w_drain;
                        flux[i] = w_drain;
                        newSWmm[i] = SW[i] + w_in - w_out;
                    }

                    // drainage out of this layer goes into next layer down
                    w_in = w_out;
                }

                return flux;
            }
        }

    }
}
