using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Organs;

namespace Models.Functions.RootShape
{
    /// <summary>
    /// This model calculates the proportion of each soil layer occupided by roots.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Root))]
    public class RootShapeCylinder : Model, IRootShape
    {
        /// <summary>Calculates the root area for a layer of soil</summary>
        public void CalcRootProportionInLayers(IRootGeometryData zone)
        {
            var physical = zone.Soil.FindChild<Soils.IPhysical>();
            zone.RootArea = (zone.RightDist + zone.LeftDist) * zone.Depth / 1e6;
            for (int layer = 0; layer < physical.Thickness.Length; layer++)
            {
                double prop;
                double top = layer == 0 ? 0 : MathUtilities.Sum(physical.Thickness, 0, layer - 1);

                if (zone.Depth < top)
                {
                    prop = 0;
                }
                else
                {
                    prop = SoilUtilities.ProportionThroughLayer(physical.Thickness, layer, zone.Depth);
                }
                zone.RootProportions[layer] = prop;
            }
        }

        /// <summary>
        /// Calculate proportion of soil volume occupied by root in each layer.
        /// </summary>
        /// <param name="zone">What is a ZoneState?</param>
        public virtual void CalcRootVolumeProportionInLayers(ZoneState zone)
        {
            zone.RootProportionVolume = zone.RootProportions;
        }
    }
}
