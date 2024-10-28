using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace APSIM.Shared.Utilities
{
    /// <summary>
    /// Various math utilities.
    /// </summary>
    public class MathUtilities
    {
        /// <summary>
        /// A constant tolerance used by many utilities.
        /// </summary>
        private const double tolerance = 0.00001;

        /// <summary>
        /// A constant double value denoting a missing value
        /// </summary>
        public const double MissingValue = 999999;

        /// <summary>
        /// Return true if the true floating point numbers are equal
        /// </summary>
        public static bool FloatsAreEqual(double value1, double value2)
        {
            return FloatsAreEqual(value1, value2, tolerance);
        }
        
        /// <summary>
        /// Return true if the true floating point numbers are equal within the given tolerance
        /// </summary>
        public static bool FloatsAreEqual(double value1, double value2, double tolerance)
        {
            if (tolerance == 0)
                return value1 == value2;
            return (System.Math.Abs(value1 - value2) < tolerance);
        }
        
        /// <summary>
        /// Return true if the true if value 1 is greater than value 2
        /// </summary>
        public static bool IsGreaterThan(double value1, double value2)
        {
            return (value1 - value2) > tolerance;
        }

        /// <summary>
        /// Return true if the true if value 1 is greater than or equal to value 2
        /// </summary>
        public static bool IsGreaterThanOrEqual(double value1, double value2)
        {
            return (value1 - value2) >= tolerance || FloatsAreEqual(value1, value2);
        }

        /// <summary>
        /// Return true if the true if value 1 is less than value 2
        /// </summary>
        public static bool IsLessThan(double value1, double value2)
            {
            return (value2 - value1) > tolerance;
            }

        /// <summary>
        /// Return true if the true if value 1 is less than or equal to value 2
        /// </summary>
        public static bool IsLessThanOrEqual(double value1, double value2)
        {
            return (value2 - value1) >= tolerance || FloatsAreEqual(value1, value2);
        }

        /// <summary>
        /// Returns true iff a value is less than 0.
        /// </summary>
        /// <param name="value">The value to test.</param>
        public static bool IsNegative(double value)
        {
            return IsLessThan(value, 0);
        }

        /// <summary>
        /// Returns true iff a value is greater than 0.
        /// </summary>
        /// <param name="value">The value to test.</param>
        public static bool IsPositive(double value)
        {
            return IsGreaterThan(value, 0);
        }

        /// <summary>
        /// Round the specified value to zero if within the given tolerance
        /// </summary>
        public static double RoundToZero(double value, double tolerance)
            {
                return (System.Math.Abs(value) <= tolerance) ? 0.0 : value;
            }
        
        /// <summary>
        ///Round the specified value to zero if within 1x10e-15 of zero
        /// </summary>
        public static double RoundToZero(double value)
        {
            return RoundToZero(value, 1.0e-15);
        }
        
        /// <summary>
        /// Perform a stepwise multiply of the values in value 1 with the values in value2.
        /// Returns an array of the same size as value 1 and value 2
        /// </summary>
        public static double[] Multiply(double[] value1, double[] value2)
        {
            double[] results = new double[value1.Length];
            if (value1.Length == value2.Length)
            {
                results = new double[value1.Length];
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue || value2[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = (value1[iIndex] * value2[iIndex]);
                }
            }
            return results;
        }
        
        /// <summary>
        /// Multiply all values in value 1 with the value2.
        /// Returns an array of the same size as value 1
        /// </summary>
        public static double[] Multiply_Value(double[] value1, double value2)
        {
            double[] results = null;
            results = new double[value1.Length];
            for (int iIndex = 0; iIndex < value1.Length; iIndex++)
            {
                if (value1[iIndex] == MissingValue)
                    results[iIndex] = MissingValue;
                else
                    results[iIndex] = (value1[iIndex] * value2);
            }
            return results;
        }
        
        /// <summary>
        /// Perform a stepwise divide of the values in value 1 with the values in value2.
        /// Returns an array of the same size as value 1 and value 2
        /// </summary>
        public static double[] Divide(double[] value1, double[] value2, double errVal=0.0)
        {
            double[] results = null;
            if (value1.Length == value2.Length)
            {
                results = new double[value1.Length];
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue || value2[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = MathUtilities.Divide(value1[iIndex], value2[iIndex], errVal);
                }
            }
            return results;
        }
        
        /// <summary>
        /// Divide all values in value 1 with the value2.
        /// Returns an array of the same size as value 1
        /// </summary>
        public static double[] Divide_Value(double[] value1, double value2)
        {
            double[] results = new double[value1.Length];
            //Avoid divide by zero problems
            if (value2 != 0)
            {
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = (value1[iIndex] / value2);
                }
            }
            else
            {
                results = value1;
            }
            return results;
        }

        /// <summary>
        /// Divide value1 by value2. On error, the value errVal will be returned.
        /// </summary>
        public static double Divide(double value1, double value2, double errVal)
        {
            double returnValue = value1 / value2;
            if (double.IsInfinity(returnValue) || double.IsNaN(returnValue))
                return errVal;
            return returnValue;
        }

        /// <summary>
        /// Add value2 to all values in value 1
        /// Returns an array of the same size as value 1
        /// </summary>
        public static double[] AddValue(double[] value1, double value2)
        {
            double[] results = new double[value1.Length];
            for (int iIndex = 0; iIndex < value1.Length; iIndex++)
            {
                if (value1[iIndex] == MissingValue)
                    results[iIndex] = MissingValue;
                else
                    results[iIndex] = (value1[iIndex] + value2);
            }
            return results;
        }

        /// <summary>
        /// Perform a stepwise addition of the values in value 1 with the values in value2.
        /// Returns an array of the same size as value 1 and value 2
        /// </summary>
        public static double[] Add(IReadOnlyList<double> value1, IReadOnlyList<double> value2)
        {
            double[] results = null;
            if (value1.Count == value2.Count)
            {
                results = new double[value1.Count];
                for (int iIndex = 0; iIndex < value1.Count; iIndex++)
                {
                    if (value1[iIndex] == MissingValue || value2[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = (value1[iIndex] + value2[iIndex]);
                }
            }
            return results;
        }

        /// <summary>
        /// Perform a stepwise subtraction of the values in value 1 with the values in value2.
        /// Returns an array of the same size as value 1 and value 2
        /// </summary>
        public static double[] Subtract(double[] value1, double[] value2)
        {
            double[] results = null;
            if (value1.Length == value2.Length)
            {
                results = new double[value1.Length];
                for (int iIndex = 0; iIndex < value1.Length; iIndex++)
                {
                    if (value1[iIndex] == MissingValue || value2[iIndex] == MissingValue)
                        results[iIndex] = MissingValue;
                    else
                        results[iIndex] = (value1[iIndex] - value2[iIndex]);
                }
            }
            return results;
        }

        /// <summary>
        /// Subtract value2 from all values in value 1
        /// Returns an array of the same size as value 1
        /// </summary>
        public static double[] Subtract_Value(double[] value1, double value2)
        {
            double[] results = new double[value1.Length];
            for (int iIndex = 0; iIndex < value1.Length; iIndex++)
            {
                if (value1[iIndex] == MissingValue)
                    results[iIndex] = MissingValue;
                else
                    results[iIndex] = (value1[iIndex] - value2);
            }
            return results;
        }
        /// <summary>
        /// Sum an array of doubles 
        /// </summary>
        public static double Sum(IEnumerable<double> values)
        {
            double result = 0.0;
            if (values != null)
                foreach (var value in values)
                    if (!double.IsNaN(value))
                        result += value;
            return result;
        }

        /// <summary>
        /// Sum an array of doubles 
        /// </summary>
        public static double Sum(IEnumerable<int> values)
        {
            int result = 0;
            if (values != null)
                foreach (var value in values)
                    result += value;
            return result;
        }

        /// <summary>
        /// Product of an array of doubles 
        /// </summary>
        public static double Prod(IEnumerable Values)
        {
            double prod = 1.0;
            foreach (object Value in Values)
            {
                if (Value != null && !double.IsNaN(Convert.ToDouble(Value, CultureInfo.InvariantCulture)))
                {
                    prod *= Convert.ToDouble(Value, CultureInfo.InvariantCulture);
                }
            }
            return prod;
        }

        /// <summary>
        /// Average an array of doubles 
        /// </summary>
        public static double Average(IEnumerable Values)
        {
            if (Values == null)
                return 0;
            double Sum = 0.0;
            int Count = 0;
            foreach (object Value in Values)
            {
                if (Value != null && !double.IsNaN(Convert.ToDouble(Value, CultureInfo.InvariantCulture)))
                {
                    Sum += Convert.ToDouble(Value, CultureInfo.InvariantCulture);
                    Count++;
                }
            }
            if (Count > 0)
                return Sum / Count;
            else
                return 0.0;
        }

        /// <summary>
        /// Return a running average for the specified values.
        /// </summary>
        public static IList<double> RunningAverage(IList<double> values)
        {
            List<double> returnValues = new List<double>();

            double sum = 0.0;
            int count = 0;
            foreach (double value in values)
            {
                if (!double.IsNaN(value))
                {
                    sum += value;
                    count++;
                    returnValues.Add(sum / count);
                }
            }
            return returnValues;
        }

        /// <summary>
        /// Sum an array of numbers starting at startIndex up to (but not including) endIndex
        /// beginning with an initial value
        /// </summary>
        public static double Sum(IEnumerable Values, int iStartIndex, int iEndIndex,
                                double dInitialValue)
        {
            double result = dInitialValue;
            if (iStartIndex < 0)
                throw new Exception("MathUtilities.Sum: End index or start index is out of range");
            int iIndex = 0;
            foreach (double Value in Values)
            {
                if (iIndex >= iEndIndex)
                    return result;
                if (iIndex >= iStartIndex && Value != MissingValue)
                    result += Value;
                iIndex++;
            }

            return result;
        }

        /// <summary>
        /// Sum an array of numbers starting at startIndex up to endIndex (inclusive)
        /// </summary>
        public static double Sum(IEnumerable values, int startIndex, int endIndex)
        {
            double result = 0.0;
            if (startIndex > endIndex)
                throw new Exception("MathUtilities.Sum: Start index is greater than end index");
            if (startIndex < 0 || endIndex >= (values as Array).Length)
                throw new Exception("MathUtilities.Sum: End index or start index is out of range");
            int index = -1;
            foreach (double value in values)
            {
                index++;
                if (index >= startIndex && value != MissingValue)
                    result += value;
                if (index == endIndex)
                    break;
            }

            return result;
        }

        /// <summary>
        /// Returns the difference between X and Y if the difference is positive, otherwise returns 0 if negative.
        /// Is based on the Fortran DIM function
        /// </summary>
        /// <returns></returns>
        static public double PositiveDifference(double X, double Y)
        {
            double difference = X - Y;
            if (difference > 0)
                return difference;
            else
                return 0;

        }

        /// <summary>
        ///Linearly interpolates a value y for a given value x and a given
        ///set of xy co-ordinates.
        ///When x lies outside the x range_of, y is set to the boundary condition.
        ///Returns true for Did_interpolate if interpolation was necessary.
        /// </summary>
        public static double LinearInterpReal(double dX, double[] dXCoordinate, double[] dYCoordinate, out bool bDidInterpolate)
        {
            bDidInterpolate = false;
            if (dXCoordinate == null || dYCoordinate == null)
                return 0;

            // find where x lies in the x coordinate
            if (dXCoordinate.Length == 0 || dYCoordinate.Length == 0 || dXCoordinate.Length != dYCoordinate.Length)
                throw new Exception("MathUtilities.LinearInterpReal: Lengths of passed in arrays are incorrect");

            int pos = Array.BinarySearch(dXCoordinate, dX);
            if (pos == -1)
            {
                if (dXCoordinate.Length > 1 && dXCoordinate[0] > dXCoordinate[1])
                    throw new Exception("Linear interp x-series out of order (must be in ascending order)");
                return dYCoordinate[0];  // off the bottom
            }
            else if (pos >= 0)
                return dYCoordinate[pos];   // exact match
            else if (pos < 0)
                pos = ~pos;
            
            if (pos == dXCoordinate.Length)
                return dYCoordinate[dXCoordinate.Length - 1];  // off the top
            
            // pos should now point to the next largest value - interpolate
            return (dYCoordinate[pos] - dYCoordinate[pos - 1]) / (dXCoordinate[pos] - dXCoordinate[pos - 1]) * (dX - dXCoordinate[pos - 1]) + dYCoordinate[pos - 1];
        }

        /// <summary>
        /// Ensure that dValue is within the specified lower and upper limits.
        /// </summary>
        /// <param name="dValue"></param>
        /// <param name="dLowerLimit"></param>
        /// <param name="dUpperLimit"></param>
        /// <returns></returns>
        static public double Constrain(double dValue, double dLowerLimit, double dUpperLimit)
        {
            double dConstrainedValue = 0.0;
            dConstrainedValue = System.Math.Min(dUpperLimit, System.Math.Max(dLowerLimit, dValue));
            return dConstrainedValue;
        }

        /// <summary>
        /// Ensure that all values in dValues are within the specified lower and upper limits.
        /// </summary>
        /// <param name="dValues"></param>
        /// <param name="dLowerLimits"></param>
        /// <param name="dUpperLimits"></param>
        /// <returns></returns>
        static public double[] Constrain(double[] dValues, double[] dLowerLimits, double[] dUpperLimits)
        {
            double[] Values = new double[dValues.Length];
            for (int i = 0; i < dValues.Length; i++)
                Values[i] = System.Math.Min(dUpperLimits[i], System.Math.Max(dLowerLimits[i], dValues[i]));
            return Values;
        }

        /// <summary>
        /// Round the specified number to the specified number of decimal places.
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="NumDecPlaces"></param>
        /// <returns></returns>
        static public double Round(double Value, int NumDecPlaces)
         {
             // rounds properly rather than the System.Math.round function.
             // e.g. 3.4 becomes 3.0
             //      3.5 becomes 4.0
            double Multiplier = System.Math.Pow(10.0, NumDecPlaces);  // gives 1 or 10 or 100 for decplaces=0, 1, or 2 etc
            Value = System.Math.Truncate(Value * Multiplier + 0.5);
            return Value / Multiplier;
        }

        /// <summary>
        /// Round the specified number to the specified number of decimal places.
        /// This allows numbers less than 1 to be rounded to the nearest sig fig.
        /// Uses Demical to maintain correctness.
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="NumDecPlaces"></param>
        /// <returns></returns>
        static public double RoundSignificant(double Value, int NumDecPlaces)
        {
            if (Value == 0) 
                return 0;

            Decimal v = (Decimal)Value;
            bool isNegative = false;
            if (Value < 0)
            {
                isNegative = true;
                v = Math.Abs(v);
            }

            int count = 0;
            while(v < 1)
            {
                v = v * 10;
                count += 1;
            }

            v = Decimal.Round(v, NumDecPlaces);

            while (count > 0)
            {
                v = v * (Decimal)0.1;
                count -= 1;
            }

            if (isNegative)
                return -(double)v;
            else
                return (double)v;
        }

        /// <summary>
        /// Round all values in Values to the specified number of decimal places.
        /// </summary>
        /// <param name="Values"></param>
        /// <param name="NumDecPlaces"></param>
        /// <returns></returns>
        static public double[] Round(double[] Values, int NumDecPlaces)
        {
            // rounds properly rather than the System.Math.round function.
            // e.g. 3.4 becomes 3.0
            //      3.5 becomes 4.0
            for (int i = 0; i != Values.Length; i++)
                Values[i] = Round(Values[i], NumDecPlaces);
            return Values;
        }

        /// <summary>
        /// Zero the specified array.
        /// </summary>
        /// <param name="arr">The array to be zeroed</param>
        static public void Zero(double[] arr)
        {
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = 0;
                }
            }
        }

        /// <summary>
        /// Zero the specified 2-D array.
        /// </summary>
        /// <param name="arr">The array to be zeroed</param>
        static public void Zero(double[,] arr)
        {
            if (arr != null)
            {
                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    for (int j = 0; j < arr.GetLength(1); j++)
                    {
                        arr[i, j] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Zero the specified 3-D array.
        /// </summary>
        /// <param name="arr">The array to be zeroed</param>
        static public void Zero(double[,,] arr)
        {
            if (arr != null)
            {
                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    for (int j = 0; j < arr.GetLength(1); j++)
                    {
                        for (int k = 0; k < arr.GetLength(2); k++)
                        {
                            arr[i, j, k] = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Zero the specified 4-D array.
        /// </summary>
        /// <param name="arr">The array to be zeroed</param>
        static public void Zero(double[, , ,] arr)
        {
            if (arr != null)
            {
                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    for (int j = 0; j < arr.GetLength(1); j++)
                    {
                        for (int k = 0; k < arr.GetLength(2); k++)
                        {
                            for (int l = 0; l < arr.GetLength(2); l++) 
                            {
                                arr[i, j, k, l] = 0;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Zero the specified 5-D array.
        /// </summary>
        /// <param name="arr">The array to be zeroed</param>
        static public void Zero(double[, , , ,] arr)
        {
            if (arr != null)
            {
                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    for (int j = 0; j < arr.GetLength(1); j++)
                    {
                        for (int k = 0; k < arr.GetLength(2); k++) 
                        {
                            for (int l = 0; l < arr.GetLength(2); l++) 
                            {
                                for (int m = 0; m < arr.GetLength(3); m++)
                                {
                                    arr[i, j, k, l, m] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reverse the contents of the specified array.
        /// </summary>
        static public double[] Reverse(double[] Values)
        {
            double[] ReturnValues = new double[Values.Length];

            int Index = 0;
            for (int Layer = Values.Length - 1; Layer >= 0; Layer--)
            {
                ReturnValues[Index] = Values[Layer];
                Index++;
            }
            return ReturnValues;
        }

        /// <summary>
        /// Returns true if there are values in the specified array that aren't missing values.
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        static public bool ValuesInArray(double[] Values)
        {
            if (Values != null)
            {
                foreach (double Value in Values)
                {
                    if (Value != MissingValue && !double.IsNaN(Value))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if there are non blank values in the specified array
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        static public bool ValuesInArray(string[] Values)
        {
            if (Values != null)
            {
                foreach (string Value in Values)
                {
                    if (!string.IsNullOrEmpty(Value))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if there are values in the specified array that aren't missing values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        static public bool ValuesInArray(IEnumerable values)
        {
            if (values != null)
            {
                IEnumerator e = values.GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current is double)
                    {
                        double value = Convert.ToDouble(e.Current, System.Globalization.CultureInfo.InvariantCulture);
                        if (value != MissingValue && !double.IsNaN(value))
                            return true;
                    }
                    else if (e.Current is string)
                    {
                        if (e.Current as string != "")
                            return true;
                    }
                    else if (e.Current is DateTime)
                    {
                        DateTime d = (DateTime)e.Current;
                        if (d != DateTime.MinValue && d != DateTime.MaxValue)
                            return true;
                    }
                    else
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Replace missing values with 'replacementValue'
        /// </summary>
        /// <param name="values">The values to search through.</param>
        /// <param name="replacementValue">The value to use as the replacement.</param>
        public static void ReplaceMissingValues(double[] values, double replacementValue)
        {
            if (values != null)
                for (int i = 0; i < values.Length; i++)
                    if (double.IsNaN(values[i]))
                        values[i] = replacementValue;
        }

        /// <summary>
        /// Convert an array of strings to an array of doubles
        /// </summary>
        static public double[] StringsToDoubles(IList Values)
        {
            if (Values == null)
                return new double[0];

            double[] ReturnValues = new double[Values.Count];

            for (int Index = 0; Index != Values.Count; Index++)
            {
                if (Values[Index].ToString() == "" || Values[Index].ToString() == "NaN")
                    ReturnValues[Index] = MissingValue;
                else
                    ReturnValues[Index] = Convert.ToDouble(Values[Index], System.Globalization.CultureInfo.InvariantCulture);
            }
            return ReturnValues;
        }

        /// <summary>
        /// Convert an array of strings to an array of integers
        /// </summary>
        static public int[] StringsToIntegers(IList Values)
        {
            int[] ReturnValues = new int[Values.Count];

            for (int Index = 0; Index != Values.Count; Index++)
            {
                if (Values[Index].ToString() == "" || Values[Index].ToString() == "NaN")
                    ReturnValues[Index] = int.MinValue;
                else
                    ReturnValues[Index] = Convert.ToInt32(Values[Index], CultureInfo.InvariantCulture);
            }
            return ReturnValues;
        }

        /// <summary>
        /// Calculate the percentile value from the sorted array of values
        /// </summary>
        /// <param name="sequence">Array of sorted values</param>
        /// <param name="pctile">The percentile 0-1</param>
        /// <returns>The percentile value</returns>
        public static double Percentile(double[] sequence, double pctile)
        {
            int n;

            n = sequence.Length;    //count the number of useful values

            if ((n <= 1) || (pctile < 0.0) || (pctile > 1.0))
                return double.NaN;
            else if (pctile == 1.0)
            {
                return sequence[n - 1];
            }
            else
            {
                double[] sortedSequence = (double[]) sequence.Clone();
                Array.Sort(sortedSequence);
                int i = Convert.ToInt32(Math.Truncate(pctile * (n - 1)), CultureInfo.InvariantCulture);       //Otherwise interpolate between the
                double z = pctile * (n - 1) - i;                                //appropriate array elements
                return sortedSequence[i] * (1.0 - z) + sortedSequence[i + 1] * z;
            }
        }

        /// <summary>
        /// Return an array of numbers of the specified size that represents the
        /// y axis on a probability distribution graph.
        /// </summary>
        /// <param name="NumPoints"></param>
        /// <param name="Exceed"></param>
        /// <returns></returns>
        static public double[] ProbabilityDistribution(int NumPoints, bool Exceed)
        {
            double[] Probability = new double[NumPoints];

            for (int x = 1; x <= NumPoints; x++)
                Probability[x - 1] = (x - 0.5) / NumPoints * 100;

            if (Exceed)
                Array.Reverse(Probability);
            return Probability;
        }

        /// <summary>
        /// A class encapsulating regression statistics.
        /// </summary>
        [Serializable]
        public class RegrStats
        {
            /// <summary>
            /// Name of the variable being analysed
            /// </summary>
            public string Name;

            /// <summary>
            /// Number of observations
            /// </summary>
            public int n;

            /// <summary>
            /// The slope
            /// </summary>
            public double Slope;

            /// <summary>
            /// The intercept
            /// </summary>
            public double Intercept;

            /// <summary>
            /// Standard error of the slope
            /// </summary>
            public double SEslope;

            /// <summary>
            /// Standard error of the intercept
            /// </summary>
            public double SEintercept;

            /// <summary>
            /// The R squared
            /// </summary>
            public double R2;

            /// <summary>
            /// The root mean squared error
            /// </summary>
            public double RMSE;

            /// <summary>
            /// Nash-Sutcliff efficiency
            /// </summary>
            public double NSE;

            /// <summary>
            /// Mean error
            /// </summary>
            public double ME;

            /// <summary>
            /// Mean absolute error
            /// </summary>
            public double MAE;

            /// <summary>
            /// Root mean square error to Standard deviation Ratio
            /// </summary>
            public double RSR;

            /// <summary>
            /// Root mean square error to (observed) Mean Ratio
            /// </summary>
            public double RMR;
        };

        /// <summary>
        /// Calculate regression statistics for the given x and y values.
        /// </summary>
        /// <param name="name">Name of variable being analysed.</param>
        /// <param name="X">Collection of X values.</param>
        /// <param name="Y">Collection of Y values.</param>
        /// <returns></returns>
        static public RegrStats CalcRegressionStats(string name, IEnumerable Y, IEnumerable X)
        {
            RegrStats stats = new RegrStats();
            double SumX = 0;
            double SumY = 0;
            double SumXY = 0;
            double SumX2 = 0;
            double SumY2 = 0;
            double CSSX, CSSXY;
            double Xbar, Ybar;
            double TSS;
            double REGSS;
            double RESIDSS, RESIDSSM;
            double S2;
            double SumOfSquaredResiduals = 0;   //SUM i=1->n  ((P(i) - O(i)) ^ 2)
            double SumOfResiduals = 0;          //SUM i=1->n   (P(i) - O(i))
            double SumOfAbsResiduals = 0;       //SUM i=1->n  |(P(i) - O(i))|
            double SumOfSquaredSD = 0;          //SUM i=1->n  ((O(i) - Omean) ^ 2)

            stats.Name = name;
            stats.n = 0;
            stats.Slope = 0.0;
            stats.Intercept = 0.0;
            stats.SEslope = 0.0;
            stats.SEintercept = 0.0;
            stats.R2 = 0.0;
            stats.RMSE = 0.0;

            int Num_points = 0;
            IEnumerator xEnum = X.GetEnumerator();
            IEnumerator yEnum = Y.GetEnumerator();
            while (xEnum.MoveNext() && yEnum.MoveNext())
            {
                if (xEnum.Current.GetType() != typeof(double) ||
                    yEnum.Current.GetType() != typeof(double))
                    return null;
                double xValue = Convert.ToDouble(xEnum.Current, System.Globalization.CultureInfo.InvariantCulture);
                double yValue = Convert.ToDouble(yEnum.Current, System.Globalization.CultureInfo.InvariantCulture);
                if (!double.IsNaN(xValue) && !double.IsNaN(yValue))
                {
                    SumX = SumX + xValue;
                    SumX2 = SumX2 + xValue * xValue;       // SS for X
                    SumY = SumY + yValue;
                    SumY2 = SumY2 + yValue * yValue;       // SS for y
                    SumXY = SumXY + xValue * yValue;       // SS for products

                    SumOfSquaredResiduals += Math.Pow(yValue - xValue, 2);
                    SumOfResiduals += yValue - xValue;
                    SumOfAbsResiduals += Math.Abs(yValue - xValue);

                    Num_points++;
                }
            }
            if (Num_points == 0)
                return null;
            Xbar = SumX / Num_points;
            Ybar = SumY / Num_points;

            xEnum.Reset();
            yEnum.Reset();
            while (xEnum.MoveNext() && yEnum.MoveNext())
            {
                double xValue = Convert.ToDouble(xEnum.Current, System.Globalization.CultureInfo.InvariantCulture);
                double yValue = Convert.ToDouble(yEnum.Current, System.Globalization.CultureInfo.InvariantCulture);
                if (!double.IsNaN(xValue) && !double.IsNaN(yValue))
                {
                    SumOfSquaredSD += Math.Pow(xValue - Xbar, 2);
                }
            }

            CSSXY = SumXY - SumX * SumY / Num_points;     // Corrected SS for products
            CSSX = SumX2 - SumX * SumX / Num_points;      // Corrected SS for X
            stats.n = Num_points;
            stats.Slope = CSSXY / CSSX;                   // Calculate slope
            stats.Intercept = Ybar - stats.Slope * Xbar;  // Calculate intercept

            TSS = SumY2 - SumY * SumY / Num_points;       // Corrected SS for Y = Sum((y-ybar)^2)
            REGSS = stats.Slope * CSSXY;                  // SS due to regression = Sum((yest-ybar)^2)
            RESIDSS = TSS - REGSS;                        // SS about the regression = Sum((y-yest)^2)

            if (Num_points > 2)                           // MUST HAVE MORE THAN TWO POINTS FOR REG
                RESIDSSM = RESIDSS / (Num_points - 2);    // Residual mean SS, variance of residual
            else
                RESIDSSM = 0.0;

            stats.RMSE = Math.Sqrt(SumOfSquaredResiduals / stats.n); // Root mean square error
            stats.R2 = 1.0 - (RESIDSS / TSS);                        // Unadjusted R2 calculated from SS
            S2 = RESIDSSM;                                           // Resid. MSS is estimate of variance
            // about the regression
            stats.SEslope = System.Math.Sqrt(S2) / System.Math.Sqrt(CSSX);              // Standard errors estimated from S2 & CSSX
            stats.SEintercept = System.Math.Sqrt(S2) * System.Math.Sqrt(SumX2 / (Num_points * CSSX));

            stats.NSE = 1.0 - SumOfSquaredResiduals / SumOfSquaredSD;     // Nash-Sutcliff efficiency
            stats.ME =  1.0 / (double)stats.n * SumOfResiduals;           // Mean error
            stats.MAE = 1.0 / (double)stats.n * SumOfAbsResiduals;        // Mean Absolute Error
            stats.RSR = stats.RMSE / Math.Sqrt((1.0 / (stats.n - 1)) * SumOfSquaredSD);         // Root mean square error to Standard deviation Ratio
            stats.RMR = stats.RMSE / Xbar;         // Root mean square error to Mean Ratio

            return stats;
        }

        /// <summary>
        /// Return the time elapsed in hours between the specified sun angle
        ///  from 90^o^ in am and pm. +ve above the horizon, -ve below the horizon.
        /// </summary>
        /// \param DayOfYear The day of year
        /// \param SunAngle 
        /// \parblock 
        /// Angle to measure time between such as twilight (deg).
        /// angular distance between 90 deg and end of twilight - altitude of sun. +ve up, -ve down.
        /// Civil twilight ends after sunset or begins before sunrise when the solar depression angle is 6deg;. e.g SunAngle = -6deg;
        /// Nautical twilight : 12deg;
        /// Astronomical twilight : 18deg;
        /// \endparblock
        /// \param Latitude Latitude to calculate the day length (deg;)
        /// \return Day length in hours between the specified sun angle from 90deg; in am and pm.
        static public double DayLength(double DayOfYear, double SunAngle, double Latitude)
        {
            //+ Constant Values
            const double aeqnox = 79.25;   //  average day number of autumnal equinox
            const double pi = 3.14159265359;
            const double dg2rdn = (2.0 * pi) / 360.0; // convert degrees to radians
            const double decsol = 23.45116 * dg2rdn; // amplitude of declination of sun
            //   - declination of sun at solstices.
            // cm says here that the maximum
            // declination is 23.45116 or 23 degrees
            // 27 minutes.
            // I have seen else_where that it should
            // be 23 degrees 26 minutes 30 seconds -
            // 23.44167
            const double dy2rdn = (2.0 * pi) / 365.25; // convert days to radians
            const double rdn2hr = 24.0 / (2.0 * pi); // convert radians to hours

            //+ Local Variables
            double alt;// twilight altitude limited to max/min
            //   sun altitudes end of twilight
            //   - altitude of sun. (radians)
            double altmn;// altitude of sun at midnight
            double altmx;// altitude of sun at midday
            double clcd;// cos of latitude * cos of declination
            double coshra;// cos of hour angle - angle between the
            //   sun and the meridian.
            double dec;// declination of sun in radians - this
            //   is the angular distance at solar
            //   noon between the sun and the equator.
            double hrangl;// hour angle - angle between the sun
            //   and the meridian (radians).
            double hrlt;// day_length in hours
            double latrn;// latitude in radians
            double slsd;// sin of latitude * sin of declination
            double sun_alt;// angular distance between
            // sunset and end of twilight - altitude
            // of sun. (radians)
            // Twilight is defined as the interval
            // between sunrise or sunset and the
            // time when the true centre of the sun
            // is 6 degrees below the horizon.
            // Sunrise or sunset is defined as when
            // the true centre of the sun is 50'
            // below the horizon.

            sun_alt = SunAngle * dg2rdn;

            // calculate daylangth in hours by getting the
            // solar declination (radians) from the day of year, then using
            // the sin and cos of the latitude.

            // declination ranges from -.41 to .41 (summer and winter solstices)

            dec = decsol * System.Math.Sin(dy2rdn * (DayOfYear - aeqnox));

            // get the max and min altitude of sun for today and limit
            // the twilight altitude between these.

            if (FloatsAreEqual(System.Math.Abs(Latitude), 90.0))
            {
                coshra = Sign(1.0, -dec) * Sign(1.0, Latitude);
            }
            else
            {
                latrn = Latitude * dg2rdn;
                slsd = System.Math.Sin(latrn) * System.Math.Sin(dec);
                clcd = System.Math.Cos(latrn) * System.Math.Cos(dec);

                altmn = System.Math.Asin(System.Math.Min(System.Math.Max(slsd - clcd, -1.0), 1.0));
                altmx = System.Math.Asin(System.Math.Min(System.Math.Max(slsd + clcd, -1.0), 1.0));
                alt = System.Math.Min(System.Math.Max(sun_alt, altmn), altmx);

                // get cos of the hour angle
                coshra = (System.Math.Sin(alt) - slsd) / clcd;
                coshra = System.Math.Min(System.Math.Max(coshra, -1.0), 1.0);
            }

            // now get the hour angle and the hours of light
            hrangl = System.Math.Acos(coshra);
            hrlt = hrangl * rdn2hr * 2.0;
            return hrlt;
        }

        /// <summary>
        /// Transfer of sign - from FORTRAN.
        ///The result is of the same type and kind as a. Its value is the abs(a) of a,
        ///if b is greater than or equal positive zero; and -abs(a), if b is less than
        ///or equal to negative zero.
        ///Example a = sign (30,-2) ! a is assigned the value -30
        /// </summary>
        static public double Sign(double a, double b)
        {
            if (b >= 0)
                return System.Math.Abs(a);
            else
                return -System.Math.Abs(a);
        }

        /// <summary>
        /// Return the minimum value
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        public static double Min(IEnumerable Values)
        {
            double Minimum = 9999999;
            foreach (object Value in Values)
            {
                double value;
                if (Value is double)
                    value = (double)Value;
                else
                    value = Convert.ToDouble(Value);

                if (Value != null && !double.IsNaN(value))
                    Minimum = Math.Min(value, Minimum);
            }
            if (Minimum == 9999999)
                return double.NaN;
            return Minimum;
        }

        /// <summary>
        /// Return the maximum value
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        public static double Max(IEnumerable Values)
        {
            double Maximum = -9999999;
            foreach (object Value in Values)
            {
                if (Value == null)
                    continue;

                double value;
                if (Value is double)
                    value = (double)Value;
                else
                    value = Convert.ToDouble(Value);

                if (!double.IsNaN(value))
                    Maximum = Math.Max(value, Maximum);
            }
            if (Maximum == -9999999)
                return double.NaN;
            return Maximum;
        }

        /// <summary>
        /// Ensure that x is between x1 and x2.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <returns></returns>
        public static double Bound(double x, double x1, double x2)
        {
            return System.Math.Min(System.Math.Max(x, x1), x2);
        }

        /// <summary>
        /// Get the min/max values of a list of x/y pairs. Ignores values
        /// where the x or y value at a particular index is NaN.
        /// </summary>
        /// <param name="x">X values.</param>
        /// <param name="y">Y values.</param>
        /// <param name="minX">Smallest x value.</param>
        /// <param name="maxX">Largest x value.</param>
        /// <param name="minY">Smallest y value.</param>
        /// <param name="maxY">Largest y value.</param>
        public static void GetBounds(IEnumerable<double> x, IEnumerable<double> y, out double minX, out double maxX, out double minY, out double maxY)
        {
            if (!(x.Any() && y.Any()))
                throw new ArgumentException("x is empty");
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach ((double xi, double yi) in x.Zip(y, (xi, yi) => (xi, yi)))
            {
                if (!double.IsNaN(xi) && !double.IsNaN(yi))
                {
                    minX = Math.Min(minX, xi);
                    minY = Math.Min(minY, yi);
                    maxX = Math.Max(maxX, xi);
                    maxY = Math.Max(maxY, yi);
                }
            }
        }

        /// <summary>
        /// Create an array of values all containing 'Value'
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="NumValues"></param>
        /// <returns></returns>
        public static double[] CreateArrayOfValues(double Value, int NumValues)
        {
            double[] Values = new double[NumValues];
            for (int i = 0; i < NumValues; i++)
                Values[i] = Value;
            return Values;
        }

        /// <summary>
        /// Return true if 'value1 is greater than value2 within the specified number of 
        /// decimal places
        /// </summary>
        /// <param name="Value1"></param>
        /// <param name="Value2"></param>
        /// <param name="NumDecPlaces"></param>
        /// <returns></returns>
        static public bool GreaterThan(double Value1, double Value2, int NumDecPlaces)
        {
            double Multiplier = System.Math.Pow(10.0, NumDecPlaces);  // gives 1 or 10 or 100 for decplaces=0, 1, or 2 etc
            Value1 = System.Math.Truncate(Value1 * Multiplier + 0.5);
            Value2 = System.Math.Truncate(Value2 * Multiplier + 0.5);
            return (Value1 > Value2);
        }

        /// <summary>
        /// Return true if 'value1 is less than value2 within the specified number of 
        /// decimal places
        /// </summary>
        /// <param name="Value1"></param>
        /// <param name="Value2"></param>
        /// <param name="NumDecPlaces"></param>
        /// <returns></returns>
        static public bool LessThan(double Value1, double Value2, int NumDecPlaces)
        {
            double Multiplier = System.Math.Pow(10.0, NumDecPlaces);  // gives 1 or 10 or 100 for decplaces=0, 1, or 2 etc
            Value1 = System.Math.Truncate(Value1 * Multiplier + 0.5);
            Value2 = System.Math.Truncate(Value2 * Multiplier + 0.5);
            return (Value1 < Value2);
        }

        /// <summary>
        /// Return true if the specified string can be converted to a double.
        /// </summary>
        /// <param name="StringValue"></param>
        /// <returns></returns>
        static public bool IsNumerical(string StringValue)
        {
            double Value;
            if (StringValue != "" && !Double.TryParse(StringValue, out Value))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Return true if all specified strings can be converted to a double.
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        static public bool IsNumerical(string[] Values)
        {
            foreach (string Value in Values)
                if (!IsNumerical(Value))
                    return false;
            return true;
        }

        /// <summary>
        /// Return true the specified string can be converted to a double given the US culture
        /// </summary>
        /// <param name="StringValue"></param>
        /// <returns></returns>
        static public bool IsNumericalenUS(string StringValue)
        {
            double Value;
            if (StringValue != "" && !Double.TryParse(StringValue, NumberStyles.Any, new CultureInfo("en-US"), out Value))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Return true if all specified strings can be converted to a double given the US culture
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        static public bool IsNumericalenUS(string[] Values)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                if (!IsNumericalenUS(Values[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Return an array of values where the missing values have been removed.
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        static public double[] RemoveMissingValuesFromBottom(double[] Values)
        {
            if (Values == null) return null;
            // Find the last non missing value.
            int i;
            for (i = Values.Length - 1; i >= 0; i--)
            {
                if (Values[i] != MissingValue && !double.IsNaN(Values[i]))
                    break;
            }
            if (i < 0)
                return new double[0];
            double[] ReturnValues = new double[i + 1];
            for (int j = 0; j <= i; j++)
                ReturnValues[j] = Values[j];
            return ReturnValues;
        }

        /// <summary>
        /// Return an array of values where the missing values have been removed.
        /// </summary>
        /// <param name="Values"></param>
        /// <returns></returns>
        static public string[] RemoveMissingValuesFromBottom(string[] Values)
        {
            if (Values == null) return null;
            // Find the last non missing value.
            int i;
            for (i = Values.Length - 1; i >= 0; i--)
            {
                if (Values[i] != "")
                    break;
            }
            if (i < 0)
                return new string[0];
            string[] ReturnValues = new string[i + 1];
            for (int j = 0; j <= i; j++)
                ReturnValues[j] = Values[j];
            return ReturnValues;
        }

        /// <summary>
        /// Remove a value from the specified array.
        /// </summary>
        /// <param name="Values"></param>
        /// <param name="Index"></param>
        /// <returns></returns>
        static public double[] RemoveValueAt(double[] Values, int Index)
        {
            List<double> NewValues = new List<double>();
            for (int i = 0; i < Values.Length; i++)
            {
                if (i != Index)
                    NewValues.Add(Values[i]);
            }
            return NewValues.ToArray();
        }

        /// <summary>
        /// Make sure the specified array is of the specified length. Will pad
        /// with double.NaN to make it the required length.
        /// </summary>
        /// <param name="values">The array of values to resize.</param>
        /// <param name="length">The new size of the array.</param>
        /// <returns>The new array.</returns>
        public static double[] FixArrayLength(double[] values, int length)
        {
            if (values.Length != length)
            {
                int i = values.Length;
                Array.Resize(ref values, length);
                while (i < length)
                {
                    values[i] = double.NaN;
                    i++;
                }
            }
            return values;
        }

        /// <summary>Return the last value that isn't a missing value.</summary>
        /// <param name="Values">The values.</param>
        /// <returns></returns>
        public static double LastValue(double[] Values)
        {
            if (Values == null) return double.NaN;
            for (int i = Values.Length - 1; i >= 0; i--)
                if (!double.IsNaN(Values[i]))
                    return Values[i];
            return 0;
        }

        /// <summary>Return the second last value that isn't a missing value.</summary>
        /// <param name="Values">The values.</param>
        /// <returns></returns>
        public static double SecondLastValue(double[] Values)
        {
            bool foundLastValue = false;
            if (Values == null) return double.NaN;
            for (int i = Values.Length - 1; i >= 0; i--)
            {
                if (!double.IsNaN(Values[i]))
                {
                    if (foundLastValue)
                        return Values[i];
                    else
                        foundLastValue = true;
                }
            }

            return 0;
        }

        /// <summary>
        /// Convert the list of doubles to strings.
        /// </summary>
        /// <param name="DoubleValues"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string[] DoublesToStrings(IList DoubleValues, string format = null)
        {
            string[] Values = new string[DoubleValues.Count];
            for (int i = 0; i < DoubleValues.Count; i++)
            {
                if (format != null)
                {
                    Values[i] = ((double)DoubleValues[i]).ToString(format);
                }
                else
                {
                    Values[i] = ((double)DoubleValues[i]).ToString();
                }
            }
            return Values;
        }

        /// <summary>
        /// Convert the list of doubles to single precision floats.
        /// </summary>
        /// <param name="DoubleValues"></param>
        /// <returns></returns>
        public static float[] DoublesToSingles(double[] DoubleValues)
        {
            float[] Values = new float[DoubleValues.Length];
            for (int i = 0; i < DoubleValues.Length; i++)
                Values[i] = Convert.ToSingle(DoubleValues[i], CultureInfo.InvariantCulture);
            return Values;
        }
        
        /// <summary>
        /// Return x * x
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Sqr(double x) { return x * x; }

        /// <summary>
        /// Return Ln Gamma
        /// </summary>
        /// <param name="xx"></param>
        /// <returns></returns>
        public static double LnGamma(double xx)
        {
            double x = xx - 1.0;
            double tmp = x + 5.5;
            tmp = (x + 0.5) * System.Math.Log(tmp) - tmp;
            double ser = 1.0 + 76.18009173 / (x + 1.0) - 86.50532033 / (x + 2.0) + 24.01409822 / (x + 3.0)
             - 1.231739516 / (x + 4.0) + 0.120858003e-2 / (x + 5.0) - 0.536382e-5 / (x + 6.0);
            return tmp + System.Math.Log(2.50662827465 * ser);
        }

        /// <summary>
        /// Return Gamma
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Gamma(double x)
        {
            // From http://rosettacode.org/wiki/Gamma_function#Java
        
            double[] p = {0.99999999999980993, 676.5203681218851, -1259.1392167224028,
                      771.32342877765313, -176.61502916214059, 12.507343278686905,
                      -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7};
            int g = 7;
            if (x < 0.5) return System.Math.PI / (System.Math.Sin(System.Math.PI * x) * Gamma(1 - x));

            x -= 1;
            double a = p[0];
            double t = x + g + 0.5;
            for (int i = 1; i < p.Length; i++)
            {
                a += p[i] / (x + i);
            }

            return System.Math.Sqrt(2 * System.Math.PI) * System.Math.Pow(t, x + 0.5) * System.Math.Exp(-t) * a;
        }

        /// <summary>
        /// Return true if the two lists are equal
        /// </summary>
        /// <param name="L1"></param>
        /// <param name="L2"></param>
        /// <returns></returns>
        static public bool AreEqual(IList<double> L1, IList<double> L2)
        {
            if (L1 == null && L2 == null)
            {
                return true;
            }
            else if ((L1 == null && L2 != null || (L1 != null && L2 == null)))
            {
                return false;
            }

            if (L1.Count != L2.Count)
            {
                return false;
            }

            for (int i = 0; i < L1.Count; i++)
            {
                if (double.IsNaN(L1[i]) && double.IsNaN(L2[i]))
                {
                }
                else if (!FloatsAreEqual(L1[i], L2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Return true if the two lists are equal
        /// </summary>
        /// <param name="L1"></param>
        /// <param name="L2"></param>
        /// <returns></returns>
        static public bool AreEqual(IList<string> L1, IList<string> L2)
        {
            if (L1 == null && L2 == null)
            {
                return true;
            }
            else if (L1 == null || L2 == null)
            {
                return false;
            }
            else if (L1.Count != L2.Count)
            {
                return false;
            }

            for (int i = 0; i < L1.Count; i++)
            {
                if (!L1[i].Equals(L2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Perform an insertion sort (stable sort) on the specified list.
        /// </summary>
        public static void StableSort<T>(IList<T> list, Comparison<T> comparison)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (comparison == null)
                throw new ArgumentNullException("comparison");

            int count = list.Count;
            for (int j = 1; j < count; j++)
            {
                T key = list[j];

                int i = j - 1;
                for (; i >= 0 && comparison(list[i], key) > 0; i--)
                {
                    list[i + 1] = list[i];
                }
                list[i + 1] = key;
            }
        }

        /// <summary>
        /// A structure for holding time series stats.
        /// </summary>
        public class Stats
        {
            /// <summary>Gets the residual.</summary>
            /// <value>The residual.</value>
            public double Residual { get { return PredictedMean - ObservedMean; } }
            /// <summary>Gets the s ds.</summary>
            /// <value>The s ds.</value>
            public double SDs { get { return System.Math.Sqrt((1.0 / Count) * Y_YSquared); } }
            /// <summary>Gets the s dm.</summary>
            /// <value>The s dm.</value>
            public double SDm { get { return System.Math.Sqrt((1.0 / Count) * X_XSquared); } }
            /// <summary>Gets the r.</summary>
            /// <value>The r.</value>
            public double r { get { return (1.0 / Count) * Y_YxX_X / (SDs * SDm); } }
            /// <summary>Gets the r2.</summary>
            /// <value>The r2.</value>
            public double R2 { get { return System.Math.Pow(r, 2); } }
            /// <summary>Gets the LCS.</summary>
            /// <value>The LCS.</value>
            public double LCS { get { return 2.0 * SDs * SDm * (1.0 - r); } }
            /// <summary>Gets the SDSD.</summary>
            /// <value>The SDSD.</value>
            public double SDSD { get { return System.Math.Pow(SDs - SDm, 2.0); } }
            /// <summary>Gets the sb.</summary>
            /// <value>The sb.</value>
            public double SB { get { return System.Math.Pow(PredictedMean - ObservedMean, 2); } }
            /// <summary>Gets the MSD.</summary>
            /// <value>The MSD.</value>
            public double MSD { get { return SB + SDSD + LCS; } }
            /// <summary>Gets the RMSD.</summary>
            /// <value>The RMSD.</value>
            public double RMSD { get { return System.Math.Sqrt(MSD); } }
            /// <summary>Gets the percent.</summary>
            /// <value>The percent.</value>
            public double Percent { get { return (RMSD / ObservedMean)*100; } }


            // Low level pre calculations.
            /// <summary>The observed mean</summary>
            public double ObservedMean;
            /// <summary>The predicted mean</summary>
            public double PredictedMean;
            /// <summary>The x_ x squared</summary>
            public double X_XSquared; // sum of (observed - observedmean) ^ 2
            /// <summary>The y_ y squared</summary>
            public double Y_YSquared; // sum of (predicted - predictedmean) ^ 2
            /// <summary>The y_ yx x_ x</summary>
            public double Y_YxX_X;    // sum of (predicted - predictedmean) * (observed - observedmean)
            /// <summary>The count</summary>
            public int Count;
        }

        /// <summary>
        /// Calculate stats on the specified column.
        /// </summary>
        public static Stats CalcTimeSeriesStats(double[] observed, double[] predicted)
        {
            if (observed.Length != predicted.Length)
                throw new Exception("The number of observed points does not match the number of predicted points in CalcTimeSeriesStats");

            Stats stats = new Stats();
            stats.Count = observed.Length;
            stats.ObservedMean = Average(observed);
            stats.PredictedMean = Average(predicted);

            for (int i = 0; i < stats.Count; i++)
            {
                stats.Y_YSquared += System.Math.Pow(observed[i] - stats.ObservedMean, 2);
                stats.X_XSquared += System.Math.Pow(predicted[i] - stats.PredictedMean, 2);
                stats.Y_YxX_X += (predicted[i] - stats.PredictedMean) * (observed[i] - stats.ObservedMean);
            }
           
            return stats;
        }

        /// <summary>
        /// Calculate the population standard deviation.
        /// </summary>
        /// <param name="values">List of values.</param>
        /// <returns>Population standard deviation.</returns>
        public static double StandardDeviation(IEnumerable<double> values)
        {
            double sumOfDerivation = 0;
            int count = 0;
            foreach (double value in values)
            {
                sumOfDerivation += (value) * (value);
                count++;
            }

            double mean = Sum(values) / count;
            double sumOfDerivationAverage = sumOfDerivation / count;
            return System.Math.Sqrt(sumOfDerivationAverage - (mean * mean));
        }

        /// <summary>
        /// Calculate the sample standard deviation.
        /// </summary>
        /// <param name="values">List of values.</param>
        /// <returns>Sample standard deviation.</returns>
        public static double SampleStandardDeviation(IEnumerable<double> values)
        {
            double mean = values.Sum() / values.Count();
            double sigma = 0;
            foreach (double value in values)
                sigma += Math.Pow(value - mean, 2);

            // In case of division by zero (only 1 element in list), return 0.
            sigma = Divide(sigma, values.Count() - 1, 0);
            sigma = Math.Sqrt(sigma);
            return sigma;
        }

        /// <summary>Cumulates the specified values.</summary>
        /// <param name="values">The values.</param>
        /// <returns>The cumulated values</returns>
        public static IEnumerable<double> Cumulative(IEnumerable<double> values)
        {
            if (values == null)
                return null;

            List<double> newValues = new List<double>();
            double sumSoFar = 0.0;
            foreach (double value in values)
            {
                sumSoFar += value;
                newValues.Add(sumSoFar);
            }

            return newValues;
        }


        /// <summary>
        /// From: http://stackoverflow.com/questions/545703/combination-of-listlistint
        /// </summary>
        public static List<List<T>> AllCombinationsOf<T>(List<T>[] sets, bool reverse = false)
        {
            // need array bounds checking etc for production
            var combinations = new List<List<T>>();

            // prime the data
            if (sets.Length > 0)
            {
                foreach (var value in sets[0])
                    combinations.Add(new List<T> { value });

                foreach (var set in sets.Skip(1))
                    combinations = AddExtraSet(combinations, set, reverse);
            }
            return combinations;
        }

        private static List<List<T>> AddExtraSet<T>
             (List<List<T>> combinations, List<T> set, bool reverse)
        {
            if (reverse)
                return (from combination in combinations
                       from value in set
                       select new List<T>(combination) { value }).ToList();
            else
                return (from value in set
                        from combination in combinations
                        select new List<T>(combination) { value }).ToList();
        }

        /// <summary>
        /// Performs a floating point-safe search for the specified value in a
        /// list of doubles. Returns the 0-based index of the item in the list,
        /// or -1 if not found.
        /// </summary>
        /// <param name="items">List to search.</param>
        /// <param name="value">Item to search for.</param>
        public static int SafeIndexOf(IEnumerable<double> items, double value)
        {
            int i = 0;
            foreach (double item in items)
            {
                if (FloatsAreEqual(item, value))
                    return i;
                i++;
            }
            return -1;
        }

        /// <summary>
        /// Format a value to a specified number of significant figures.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="digits">The number of digits to format to.</param>
        public static string FormatSignificantDigits(double value, int digits)
        {
            string returnStr;

            // get the number of digits before the decimal place
            int decPos = value.ToString().IndexOf(".");

            // then round the number up based on the number of decimal places we can have
            // ie 62.3422623  should become 62.3423  if decPos = 2, then round dvalue to 4 decimal places
            //    3642.45678  should become 3642.57  if decPos = 4, then round dvalue to 2 decimal places
            //    3642        should become 3642.00  if decPos = 0, then round dvalue to 2 decimal places
            if (decPos > 0)
                returnStr = Math.Round(value, digits - decPos).ToString();
            else
                returnStr = Math.Round(value, digits).ToString();

            if (returnStr.Length >= (digits + 1))
            {
                // new trucate the string to get the correct number of digits
                returnStr = returnStr.Substring(0, digits + 1);
            }
            else
                returnStr = returnStr.PadRight(7, '0');
            
            // add a space so that we always have something
            return returnStr;
        }

        /// <summary>Changes all missing values in an array to a valid value.</summary>
        /// <param name="values">The values to in fill.</param>
        /// <param name="numValues">The number of values that should exist.</param>
        /// <param name="defaultValue">The value to use if can't find any other value.</param>
        public static double[] FillMissingValues(double[] values, int numValues, double defaultValue)
        {
            int numOriginalValues = 0;
            if (values != null)
                numOriginalValues = values.Length;
            Array.Resize(ref values, numValues);
            for (int i = 0; i < numValues; i++)
            {
                if (i >= numOriginalValues || double.IsNaN(values[i]))
                {
                    double validValue;
                    if (i == 0)
                        validValue = FindFirstValueInArray(values);
                    else
                        validValue = values[i - 1];

                    if (double.IsNaN(validValue))
                        values[i] = defaultValue;
                    else
                        values[i] = validValue;
                }
            }
            return values;
        }

        /// <summary>
        /// Ensure an array is the correct size.
        /// </summary>
        /// <param name="arr">The array.</param>
        /// <param name="numValues">The required size.</param>
        /// <returns>The new array of the correct size.</returns>
        public static double[] SetArrayOfCorrectSize(double[] arr, int numValues)
        {
            if (arr == null)
                arr = Enumerable.Repeat(double.NaN, numValues).ToArray();
            else
            {
                int bottomIndexOfValues = arr.Length;
                Array.Resize(ref arr, numValues);
                for (int i = bottomIndexOfValues; i < numValues; i++)
                    arr[i] = double.NaN;
            }

            return arr;
        }          

        /// <summary>
        /// Ensure an array is the correct size.
        /// </summary>
        /// <param name="arr">The array.</param>
        /// <param name="numValues">The required size.</param>
        /// <returns>The new array of the correct size.</returns>
        public static string[] SetArrayOfCorrectSize(string[] arr, int numValues)
        {
            Array.Resize(ref arr, numValues);
            return arr;
        }         

        /// <summary>
        /// Find the first non NaN value in the array.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private static double FindFirstValueInArray(double[] values)
        {
            for (int j = 0; j < values.Length; j++)
                if (!double.IsNaN(values[j]))
                    return values[j];

            return double.NaN;
        }
     
    }
}
