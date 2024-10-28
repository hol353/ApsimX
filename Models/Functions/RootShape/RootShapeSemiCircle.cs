using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Organs;

namespace Models.Functions.RootShape
{
    /// <summary>
    /// This model calculates the proportion of each soil layer occupided by roots.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Root))]
    public class RootShapeSemiCircle : RootShapeCylinder
    {
        /// <summary>Calculates the proportion of the layer's volume occupied by roots.</summary>
        public double CalcRootProportion(ZoneState zone, int layer)
        {
            var physical = zone.Soil.FindChild<Soils.IPhysical>();

            double top = layer == 0 ? 0 : MathUtilities.Sum(physical.Thickness, 0, layer - 1);
            double bottom = top + physical.Thickness[layer];

            if (zone.Depth < top)
                return 0;

            double rootArea = CalcRootAreaSemiCircleMaize(zone, top, bottom, zone.RightDist);    // Right side
            rootArea += CalcRootAreaSemiCircleMaize(zone, top, bottom, zone.LeftDist);    // Left Side

            double soilArea = (zone.RightDist + zone.LeftDist) * (bottom - top);
            return Math.Max(0.0, MathUtilities.Divide(rootArea, soilArea, 0.0));
        }

        /// <summary>
        /// Calculate proportion of soil volume occupied by root in each layer.
        /// </summary>
        /// <param name="zone">What is a ZoneState?</param>
        public override void CalcRootVolumeProportionInLayers(ZoneState zone)
        {
            var physical = zone.Soil.FindChild<Soils.IPhysical>();
            for (int i = 0; i < physical.Thickness.Length; i++)
                zone.RootProportionVolume[i] = CalcRootProportion(zone, i);
        }

        private double CalcRootAreaSemiCircleMaize(IRootGeometryData zone, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0)
            {
                return 0.0;
            }

            // get the area occupied by roots in a semi-circular section between top and bottom
            double SDepth, areaLayer;

            // intersection of roots and Section
            if (zone.RootFront <= hDist)
                SDepth = 0.0;
            else
                SDepth = Math.Sqrt(Math.Pow(zone.RootFront, 2) - Math.Pow(hDist, 2));

            // Rectangle - SDepth past bottom of this area
            if (SDepth >= bottom)
                areaLayer = (bottom - top) * hDist;
            else               // roots Past top
            {
                double Theta = 2 * Math.Acos(MathUtilities.Divide(Math.Max(top, SDepth), zone.RootFront, 0));
                double topArea = (Math.Pow(zone.RootFront, 2) / 2.0 * (Theta - Math.Sin(Theta))) / 2.0;

                // bottom down
                double bottomArea = 0;
                if (zone.RootFront > bottom)
                {
                    Theta = 2 * Math.Acos(bottom / zone.RootFront);
                    bottomArea = (Math.Pow(zone.RootFront, 2) / 2.0 * (Theta - Math.Sin(Theta))) / 2.0;
                }
                // rectangle
                if (SDepth > top) topArea += (SDepth - top) * hDist;
                areaLayer = topArea - bottomArea;
            }
            return areaLayer;
        }
    }
}
