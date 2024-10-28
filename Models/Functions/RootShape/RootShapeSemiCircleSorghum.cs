using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Organs;

namespace Models.Functions.RootShape
{
    /// <summary>
    /// This model calculates the proportion of each soil layer occupided by roots.
    /// The formula used for the circle is wrong as it does not account for the coordinate of the centre!
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Root))]
    public class RootShapeSemiCircleSorghum : Model, IRootShape
    {
        /// <summary>Calculates the root area for a layer of soil</summary>
        public void CalcRootProportionInLayers(IRootGeometryData zone)
        {
            var physical = zone.Soil.FindChild<Soils.IPhysical>();
            zone.RootArea = 0;
            for (int layer = 0; layer < physical.Thickness.Length; layer++)
            {
                double prop;
                double top = layer == 0 ? 0 : MathUtilities.Sum(physical.Thickness, 0, layer - 1);
                double bottom = top + physical.Thickness[layer];
                double rootArea;

                if (zone.Depth < top)
                {
                    prop = 0;
                }
                else
                {
                    rootArea = CalcRootAreaSemiCircleSorghum(zone, top, bottom, zone.RightDist);    // Right side
                    rootArea += CalcRootAreaSemiCircleSorghum(zone, top, bottom, zone.LeftDist);    // Left Side
                    zone.RootArea += rootArea / 1e6;

                    double soilArea = (zone.RightDist + zone.LeftDist) * (bottom - top);
                    prop = Math.Max(0.0, MathUtilities.Divide(rootArea, soilArea, 0.0));
                }
                zone.RootProportions[layer] = prop;
            }
        }

        /// <summary>
        /// Calculate proportion of soil volume occupied by root in each layer.
        /// </summary>
        /// <param name="zone">What is a ZoneState?</param>
        public void CalcRootVolumeProportionInLayers(ZoneState zone)
        {
            zone.RootProportionVolume = zone.RootProportions;
        }

        private double DegToRad(double degs)
        {
            return degs * Math.PI / 180.0;
        }

        private double CalcRootAreaSemiCircleSorghum(IRootGeometryData zone, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0)
            {
                return 0.0;
            }

            double depth, depthInLayer;

            zone.RootSpread = zone.RootFront * Math.Tan(DegToRad(45.0));   //Semi minor axis

            if (zone.RootFront >= bottom)
            {
                depth = (bottom - top) / 2.0 + top;
                depthInLayer = bottom - top;
            }
            else
            {
                depth = (zone.RootFront - top) / 2.0 + top;
                depthInLayer = zone.RootFront - top;
            }
            double xDist = zone.RootSpread * Math.Sqrt(1 - (Math.Pow(depth, 2) / Math.Pow(zone.RootFront, 2)));

            return Math.Min(depthInLayer * xDist, depthInLayer * hDist);
        }
    }
}
