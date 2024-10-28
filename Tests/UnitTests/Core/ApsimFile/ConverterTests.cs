using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnitTests.Core.ApsimFile
{
    /// <summary>This is a test class for the .apsimx file converter.</summary>
    [TestFixture]
    public class ConverterTests
    {
        /// <summary>Test version 1</summary>
        [Test]
        public void Version1()
        {
            string fromXML = "<Simulation Version=\"0\">" +
                             "  <Graph>" +
                             "    <Series>" +
                             "      <X>" +
                             "        <TableName>HarvestReport</TableName>" +
                             "        <FieldName>Maize.Population</FieldName>" +
                             "      </X>" +
                             "      <Y>" +
                             "        <TableName>HarvestReport</TableName>" +
                             "        <FieldName>GrainWt</FieldName>" +
                             "      </Y>" +
                             "    </Series>" +
                             "  </Graph>" +
                             "</Simulation>";

            var converter = Converter.DoConvert(fromXML, 1);
            Assert.That(converter.DidConvert, Is.True);

            string toXML = "<Simulation Version=\"1\">" +
                             "<Graph>" +
                               "<Series>" +
                                 "<TableName>HarvestReport</TableName>" +
                                 "<XFieldName>Maize.Population</XFieldName>" +
                                 "<YFieldName>GrainWt</YFieldName>" +
                               "</Series>" +
                             "</Graph>" +
                           "</Simulation>";
            Assert.That(converter.RootXml.OuterXml, Is.EqualTo(toXML));
        }

        /// <summary>Test version 2</summary>
        [Test]
        public void Version2()
        {
            string fromXML = "<Simulation Version=\"0\">" +
                             "  <Cultivar>" +
                             "    <Alias>Cultivar1</Alias>" +
                             "    <Alias>Cultivar2</Alias>" +
                             "  </Cultivar>" +
                             "</Simulation>";

            var converter = Converter.DoConvert(fromXML, 2);
            Assert.That(converter.DidConvert, Is.True);

            string toXML = "<Simulation Version=\"2\">" +
                             "<Cultivar>" +
                                 "<Alias>" +
                                   "<Name>Cultivar1</Name>" +
                                 "</Alias>" +
                                 "<Alias>" +
                                   "<Name>Cultivar2</Name>" +
                                 "</Alias>" +
                             "</Cultivar>" +
                           "</Simulation>";
            Assert.That(converter.RootXml.OuterXml, Is.EqualTo(toXML));
        }

        /// <summary>Test version 7</summary>
        [Test]
        public void Version7()
        {
            string fromXML = "<Simulation Version=\"6\">\r\n" +
                             "  <Manager>\r\n" +
                             "    <Code><![CDATA[using System;\r\n" +
                             "using Models.Core;\r\n" +
                             "using Models.PMF;\r\n" +
                             "namespace Models\r\n" +
                             "{\r\n" +
                             "    [Serializable]\r\n" +
                             "    public class Script : Model\r\n" +
                             "    {\r\n" +
                             "        [Link] Clock Clock;\r\n" +
                             "        [Link] Fertiliser Fertiliser;\r\n" +
                             "        [Link] Summary Summary;\r\n" +
                             "        private void OnDoManagement(object sender, EventArgs e)\r\n" +
                             "        {\r\n" +
                             "            accumulatedRain.Update();\r\n" +
                             "            if (DateUtilities.WithinDates(StartDate, Clock.Today, EndDate) &&\r\n" +
                             "                Soil.SoilWater.ESW > MinESW &&\r\n" +
                             "                accumulatedRain.Sum > MinRain)\r\n" +
                             "            {\r\n" +
                             "                Wheat.Sow(population: Population, cultivar: CultivarName, depth: SowingDepth, rowSpacing: RowSpacing);\r\n" +
                             "            }\r\n" +
                             "        }\r\n" +
                             "    }\r\n" +
                             "}\r\n" +
                             "]]></Code>\r\n" +
                             "      </Manager>\r\n" +
                             "   <Report>\r\n" +
                                   "<Name>Report</Name>\r\n" +
                                   "<VariableNames>\r\n" +
                                      "<string>[Clock].Today</string>\r\n" +
                                      "<string>[MySoil].SoilWater.ESW</string>\r\n" +
                                   "</VariableNames>\r\n" +
                             "   </Report>\r\n" +
                " </Simulation>\r\n";

            var converter = Converter.DoConvert(fromXML, 7);
            Assert.That(converter.DidConvert, Is.True);

            string expected = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.Version7.Expected.txt");
            Assert.That(converter.RootXml.OuterXml, Is.EqualTo(expected));
        }

        /// <summary>Test version 10</summary>
        [Test]
        public void Version10()
        {
            string fromXML = "<Simulation Version=\"9\">\r\n" +
                             "   <GenericOrgan>\r\n" +
                                   "<Name>Stem</Name>\r\n" +
                             "   </GenericOrgan>\r\n" +
                             " </Simulation>\r\n";

            var converter = Converter.DoConvert(fromXML, 10);
            Assert.That(converter.DidConvert, Is.True);

            string toXML = "<Simulation Version=\"10\">" +
                             "<GenericOrgan>" +
                               "<Name>Stem</Name>" +
                               "<Constant>" +
                                 "<Name>NRetranslocationFactor</Name>" +
                                 "<FixedValue>0.0</FixedValue>" +
                               "</Constant>" +
                               "<Constant>" +
                                 "<Name>NitrogenDemandSwitch</Name>" +
                                 "<FixedValue>1.0</FixedValue>" +
                               "</Constant>" +
                               "<Constant>" +
                                 "<Name>DMReallocationFactor</Name>" +
                                 "<FixedValue>0.0</FixedValue>" +
                               "</Constant>" +
                               "<Constant>" +
                                 "<Name>DMRetranslocationFactor</Name>" +
                                 "<FixedValue>0.0</FixedValue>" +
                               "</Constant>" +
                               "<VariableReference>" +
                                 "<Name>CriticalNConc</Name>" +
                                 "<VariableName>[Stem].MinimumNConc.Value()</VariableName>" +
                               "</VariableReference>" +
                             "</GenericOrgan>" +
                            "</Simulation>";
            Assert.That(converter.RootXml.OuterXml, Is.EqualTo(toXML));
        }

        /// <summary>Test version 11</summary>
        [Test]
        public void Version11()
        {
            string fromXML = "<Simulation Version=\"10\">\r\n" +
                             "  <Manager>\r\n" +
                             "    <Code><![CDATA[using System;\r\n" +
                             "using Models.Core;\r\n" +
                             "using Models.PMF;\r\n" +
                             "Wheat.NonStructuralDemand + xyz\r\n" +
                             "Wheat.TotalNonStructuralDemand + xyz\r\n" +
                             "]]></Code>\r\n" +
                             "      </Manager>\r\n" +
                             "   <Report>\r\n" +
                                   "<Name>Report</Name>\r\n" +
                                   "<VariableNames>\r\n" +
                                      "<string>[Wheat].NonStructural</string>\r\n" +
                                      "<string>[Wheat].NonStructural.Wt</string>\r\n" +
                                   "</VariableNames>\r\n" +
                             "   </Report>\r\n" +
                             "  <Graph>" +
                             "    <Series>" +
                             "      <XFieldName>Observed.Wheat.AboveGround.NonStructural.Wt</XFieldName>\r\n" +
                             "      <YFieldName>Predicted.Wheat.AboveGround.Wt</YFieldName>\r\n" +
                             "    </Series>" +
                             "  </Graph>" +
                             "  <VariableReference>" +
                             "    <Name>WSC</Name>" +
                             "    <IncludeInDocumentation>true</IncludeInDocumentation>" +
                             "    <VariableName>[Stem].Live.NonStructural.Wt</VariableName>" +
                             "  </VariableReference>" +
                             "  <LinearInterpolationFunction>" +
                             "    <Name>WaterStressEffect</Name>" +
                             "    <XYPairs>" +
                             "      <Name>XYPairs</Name>" +
                             "      <IncludeInDocumentation>true</IncludeInDocumentation>" +
                             "      <X>" +
                             "        <double>0.5</double>" +
                             "        <double>1</double>" +
                             "      </X>" +
                             "      <Y>" +
                             "        <double>0.1</double>" +
                             "        <double>1</double>" +
                             "      </Y>" +
                             "    </XYPairs>" +
                             "    <IncludeInDocumentation>true</IncludeInDocumentation>" +
                             "    <XProperty>[Stem].Live.NonStructural.Wt</XProperty>" +
                             "  </LinearInterpolationFunction>" +
                             "  <NonStructuralNReallocated>" +
                             "    <Value>1</Value>" +
                             "  </NonStructuralNReallocated>" +
                             "</Simulation>\r\n";

            var converter = Converter.DoConvert(fromXML, 11);
            Assert.That(converter.DidConvert, Is.True);

            string toXML = "<Simulation Version=\"11\">" +
                             "<Manager>" +
                               "<Code><![CDATA[using System;\r\n" +
                               "using Models.Core;\r\n" +
                               "using Models.PMF;\r\n" +
                               "Wheat.StorageDemand + xyz\r\n" +
                               "Wheat.TotalStorageDemand + xyz\r\n" +
                               "]]></Code>" +
                             "</Manager>" +
                             "<Report>" +
                               "<Name>Report</Name>" +
                               "<VariableNames>" +
                                  "<string>[Wheat].Storage</string>" +
                                  "<string>[Wheat].Storage.Wt</string>" +
                               "</VariableNames>" +
                             "</Report>" +
                             "<Graph>" +
                               "<Series>" +
                                 "<XFieldName>Observed.Wheat.AboveGround.Storage.Wt</XFieldName>" +
                                 "<YFieldName>Predicted.Wheat.AboveGround.Wt</YFieldName>" +
                               "</Series>" +
                             "</Graph>" +
                             "<VariableReference>" +
                               "<Name>WSC</Name>" +
                               "<IncludeInDocumentation>true</IncludeInDocumentation>" +
                               "<VariableName>[Stem].Live.Storage.Wt</VariableName>" +
                             "</VariableReference>" +
                             "<LinearInterpolationFunction>" +
                               "<Name>WaterStressEffect</Name>" +
                               "<XYPairs>" +
                                 "<Name>XYPairs</Name>" +
                                 "<IncludeInDocumentation>true</IncludeInDocumentation>" +
                                 "<X>" +
                                   "<double>0.5</double>" +
                                   "<double>1</double>" +
                                 "</X>" +
                                 "<Y>" +
                                   "<double>0.1</double>" +
                                   "<double>1</double>" +
                                 "</Y>" +
                               "</XYPairs>" +
                               "<IncludeInDocumentation>true</IncludeInDocumentation>" +
                               "<XProperty>[Stem].Live.Storage.Wt</XProperty>" +
                             "</LinearInterpolationFunction>" +
                             "<StorageNReallocated>" +
                               "<Value>1</Value>" +
                             "</StorageNReallocated>" +
                           "</Simulation>";
            Assert.That(converter.RootXml.OuterXml, Is.EqualTo(toXML));
        }

        public void Version9()
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());

            string fileName = Path.Combine(Path.GetTempPath(), "TestConverter.db");
            File.Delete(fileName);
            SQLite connection = new SQLite();
            connection.OpenDatabase(fileName, false);
            try
            {
                connection.ExecuteNonQuery("CREATE TABLE Simulations (ID INTEGER PRIMARY KEY ASC, Name TEXT COLLATE NOCASE)");
                connection.ExecuteNonQuery("CREATE TABLE Messages (SimulationID INTEGER, ComponentName TEXT, Date TEXT, Message TEXT, MessageType INTEGER)");
                connection.ExecuteNonQuery("CREATE TABLE _Units (TableName TEXT, ColumnHeading TEXT, Units TEXT)");
                connection.ExecuteNonQuery("CREATE TABLE Report (Col1 TEXT, Col2 TEXT, Col3 TEXT)");

                string fromXML = "<Simulation Version=\"8\"/>";

                var converter = Converter.DoConvert(fromXML, 9);
                Assert.That(converter.DidConvert, Is.True);

                DataTable tableData = connection.ExecuteQuery("SELECT * FROM sqlite_master");
                string[] tableNames = DataTableUtilities.GetColumnAsStrings(tableData, "Name", CultureInfo.InvariantCulture);
                Assert.That(tableNames, Is.EqualTo(new string[] { "_Simulations", "_Messages", "_Units", "Report" } ));
            }
            finally
            {
                connection.CloseDatabase();
                File.Delete(fileName);
            }
        }

        [Test]
        public void Version32()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion32 before.xml");
            string expectedXml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion32 after.xml");

            var converter = Converter.DoConvert(xml, 33);
            Assert.That(converter.DidConvert, Is.True);

            using (StringWriter writer = new StringWriter())
            {
                converter.RootXml.Save(writer);
                Assert.That(writer.ToString(), Is.EqualTo(expectedXml));
            }
        }

        [Test]
        public void Version59()
        {
            string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion59 before.json");
            string expectedXml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion59 after.json");

            var converter = Converter.DoConvert(xml, 59);
            Assert.That(converter.DidConvert, Is.True);

            using (StringWriter writer = new StringWriter())
            {
                writer.Write(converter.Root.ToString());
                Assert.That(writer.ToString(), Is.EqualTo(expectedXml));
            }
        }

        [Test]
        public void Version60()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion60 before.json");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion60 after.json");

            var converter = Converter.DoConvert(json, 60);
            Assert.That(converter.DidConvert, Is.True);

            using (StringWriter writer = new StringWriter())
            {
                writer.Write(converter.Root.ToString());
                Assert.That(writer.ToString(), Is.EqualTo(expectedJson));
            }
        }

        [Test]
        public void Version63()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion63 before.json");
            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTestsVersion63 after.json");

            var converter = Converter.DoConvert(json, 63);
            Assert.That(converter.DidConvert, Is.True);

            using (StringWriter writer = new StringWriter())
            {
                writer.Write(converter.Root.ToString());
                Assert.That(writer.ToString(), Is.EqualTo(expectedJson));
            }
        }

        [Test]
        public void Version164() //no this is not a typo, this is test 164
        {
            string beforeJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.CoverterTest164FileBefore.apsimx");
            ConverterReturnType converter = FileFormat.ReadFromString<Simulations>(beforeJSON, null, true, null);
            Simulations actualModel = converter.NewModel as Simulations;
            Assert.That(converter.DidConvert, Is.True);

            string afterJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.CoverterTest164FileAfter.apsimx");
            converter = FileFormat.ReadFromString<Simulations>(afterJSON, null, true, null);
            Simulations expectedModel = converter.NewModel as Simulations;

            string actual = FileFormat.WriteToString(actualModel);
            string expected = FileFormat.WriteToString(expectedModel);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Version172()
        {
            string beforeJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.CoverterTest172FileBefore.apsimx");
            ConverterReturnType converter = FileFormat.ReadFromString<Simulations>(beforeJSON, null, true, null);
            Simulations actualModel = converter.NewModel as Simulations;
            Assert.That(converter.DidConvert, Is.True);

            string afterJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.CoverterTest172FileAfter.apsimx");
            converter = FileFormat.ReadFromString<Simulations>(afterJSON, null, true, null);
            Simulations expectedModel = converter.NewModel as Simulations;

            string actual = FileFormat.WriteToString(actualModel);
            string expected = FileFormat.WriteToString(expectedModel);

            Assert.That(actual, Is.EqualTo(expected));

            Assert.Pass();
        }

        /// <summary>
        /// Arguably this doesn't even belong in the converter.
        /// Nonetheless, it's not working properly at the moment so
        /// I'm just going to fix the problem and add a test. See here:
        /// https://github.com/APSIMInitiative/ApsimX/issues/6270.
        /// </summary>
        [Test]
        public void TestAddingInitialWater()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.Soil.json");
            ConverterReturnType result = Converter.DoConvert(json, Converter.LatestVersion);
            IEnumerable<JObject> initialWaters = JsonUtilities.Children(result.Root).Where(c => string.Equals(c["Name"].ToString(), "Initial water", StringComparison.InvariantCultureIgnoreCase));
            Assert.That(initialWaters.Count(), Is.EqualTo(1));
            Assert.That(initialWaters.First()["$type"].ToString(), Is.EqualTo("Models.Soils.Sample, Models"));
        }

        [Test]
        public void TestSoluteRearrangeConverter()
        {
            JObject organic = new JObject()
            {
                ["$type"] = "Models.Soils.Organic, Models",
                ["Thickness"] = new JArray(new double[] { 100, 200}),
                ["OC"] = new JArray(new double[] { 2, 1 }),
                ["OCUnits"] = "Total"
            };
            JObject sample = new JObject()
            {
                ["$type"] = "Models.Soils.Sample, Models",
                ["Thickness"] = new JArray(new double[] { 100, 200 }),
                ["OC"] = new JArray(new double[] { double.NaN, 0.9 })
            };
            var oc = Converter.GetValues(new JObject[] { organic, sample }, "OC", 1.0, null, null, null);

            Assert.That(oc.Item1, Is.EqualTo(new double[] { 2.0, 1.0 }));
            Assert.That(oc.Item2, Is.EqualTo("Total"));
            Assert.That(oc.Item3, Is.EqualTo(new double[] { 100.0, 200.0 }));
        }

        [Test]
        public void TestUpgradeSoil()
        {
          string xml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.Soil.apsim");
          ConverterReturnType result = Converter.DoConvert(xml, Converter.LatestVersion);
          Assert.That(JsonUtilities.ChildWithName(result.Root, "Temperature"), Is.Not.Null);
          Assert.That(JsonUtilities.ChildWithName(result.Root, "Nutrient"), Is.Not.Null);
        }

        [Test]
        public void TestEnsure183_Removes_GraphsFromUnderExperiment()
        {
            string beforeJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTest183FileBefore.apsimx");
            ConverterReturnType converter = FileFormat.ReadFromString<Simulations>(beforeJSON, null, true, null);
            Simulations actualModel = converter.NewModel as Simulations;
            Assert.That(converter.DidConvert, Is.True);

            string afterJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.ConverterTest183FileAfter.apsimx");
            converter = FileFormat.ReadFromString<Simulations>(afterJSON, null, true, null);
            Simulations expectedModel = converter.NewModel as Simulations;

            string actual = FileFormat.WriteToString(actualModel);
            string expected = FileFormat.WriteToString(expectedModel);

            Assert.That(actual, Is.EqualTo(expected));

            Assert.Pass();
        }
    }
}
