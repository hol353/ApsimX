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
    public class RootShapeSemiEllipse : Model, IRootShape
    {
        /// <summary>The Root Angle</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("Degree")]
        private readonly IFunction RootAngle = null;

        /// <summary>The Root Angle for which soil LL values were estimated</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("Degree")]
        private readonly IFunction RootAngleBase = null;

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
                double rootArea, rootAreaBaseUnlimited, rootAreaUnlimited, llModifer;

                if (RootAngleBase != null && RootAngleBase.Value() != RootAngle.Value())
                {
                    // Root area for the base and current root angle when not limited by adjacent rows
                    rootAreaBaseUnlimited = CalcRootAreaSemiEllipse(zone, RootAngleBase.Value(), top, bottom, 10000);   // Right side
                    rootAreaBaseUnlimited += CalcRootAreaSemiEllipse(zone, RootAngleBase.Value(), top, bottom, 10000);   // Left Side
                    rootAreaUnlimited = CalcRootAreaSemiEllipse(zone, RootAngle.Value(), top, bottom, 10000);   // Right side
                    rootAreaUnlimited += CalcRootAreaSemiEllipse(zone, RootAngle.Value(), top, bottom, 10000);   // Left Side
                    llModifer = MathUtilities.Divide(rootAreaUnlimited, rootAreaBaseUnlimited, 1);
                }
                else
                {
                    llModifer = 1;
                }

                rootArea = CalcRootAreaSemiEllipse(zone, RootAngle.Value(), top, bottom, zone.RightDist);   // Right side
                rootArea += CalcRootAreaSemiEllipse(zone, RootAngle.Value(), top, bottom, zone.LeftDist);   // Left Side
                zone.RootArea += rootArea / 1e6;

                double soilArea = (zone.RightDist + zone.LeftDist) * (bottom - top);
                prop = Math.Max(0.0, MathUtilities.Divide(rootArea, soilArea, 0.0));

                zone.RootProportions[layer] = prop;
                zone.LLModifier[layer] = llModifer;
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

        private double CalcRootAreaSemiEllipse(IRootGeometryData zone, double rootAngle, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0 || zone.RootFront <= top)
            {
                return 0.0;
            }

            double meanDepth, layerThick, rootLength, sowDepth, layerArea, a;

            sowDepth = zone.plant.SowingData.Depth;
            double bottomNew = Math.Min(bottom, zone.RootFront);
            double topNew = Math.Max(top, sowDepth);

            zone.RootSpread = zone.RootLength * Math.Tan(DegToRad(rootAngle));   // Semi minor axis

            meanDepth = Math.Max(0.5 * (bottomNew + topNew) - sowDepth, 1); // 1mm is added to assure germination occurs.
            layerThick = Math.Max(bottomNew - topNew, 1);
            rootLength = Math.Max(zone.RootLength, 1);

            a = Math.Pow(meanDepth - 0.5 * rootLength, 2) / Math.Pow(0.5 * rootLength, 2);
            double hDistNew = Math.Min(hDist, Math.Sqrt(MathUtilities.Bound(Math.Pow(zone.RootSpread, 2) * (1 - a), 0, 100000)));
            layerArea = layerThick * hDistNew;
            return layerArea;
        }
    }
}
