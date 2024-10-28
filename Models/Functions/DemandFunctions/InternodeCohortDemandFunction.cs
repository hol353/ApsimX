using System;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Struct;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Calculate individual internode demand base on age and maxSize.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class InternodeCohortDemandFunction : Model, IFunction
    {
        /// <summary>YinBetaFunction Constructor</summary>
        public InternodeCohortDemandFunction()
        {
            Lmax = 1.0;
            te = 1.0;
            tm = 1.0;
        }
        /// <summary>Lmax</summary>
        [Description("Lmax")]
        public double Lmax { get; set; }
        /// <summary>te</summary>
        [Description("te")]
        public double te { get; set; }
        /// <summary>The tm</summary>
        [Description("tm")]
        public double tm { get; set; }

        /// <summary>The leaf </summary>
        [Link]
        Leaf Leaf = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double sinkStrength = 0;
            foreach (LeafCohort L in Leaf.Leaves)
            {
                if (L.IsAppeared)
                {
                    double maxSinkStrength = Lmax * ((2 * te - tm) / (te * (te - tm))) *
                        Math.Pow((tm / te), (tm / (te - tm)));
                    double result = maxSinkStrength *
                        ((te - L.Age) / (te - tm)) *
                        Math.Pow((L.Age / tm), (tm / (te - tm)));


                    if (L.Age > te)
                    {
                        sinkStrength += 0;
                    }
                    else
                    {
                        sinkStrength += result;
                    }
                }
            }
            return Structure.TotalStemPopn * sinkStrength;
        }
    }
}
