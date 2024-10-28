using APSIM.Shared.Utilities;
using APSIM.Shared.Documentation;
using APSIM.Documentation.Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ModelsGraph = Models.Graph;
using DocGraph = APSIM.Shared.Documentation.Graph;
using DocGraphPage = APSIM.Shared.Documentation.GraphPage;
using Newtonsoft.Json.Linq;
using System.Data;
using System.IO;
using System.Reflection;
using Models;
using APSIM.Interop.Mapping;
using Models.Mapping;

namespace UnitTests.Documentation
{
    /// <summary>This is a test class for the Autodocumentation system</summary>
    [TestFixture]
    public class DocumentationTests
    {

        static List<string> FILES = new List<string>{"Wheat", "Barley", "Potato", "OilPalm", "MicroClimate", "Nutrient", "SCRUM", "SWIM", "AgPasture", "Report", "Manager"};

        static bool REGENERATE_FILES = false;

        /// <summary>
        /// This runs through a set of stored apsimx files and generates documentation for them, both validation and tutorials.
        /// If the documentation structure is changed, or some of the crop models are changed drastically, this will run false.
        /// If the stored tagged need to be recreated (due to an indended change), set REGENERATE_FILES to true and run the test.
        /// </summary>
        [Test]
        public void TestDocumentationStructure()
        {
            if (REGENERATE_FILES)
                GenerateComparisionJSONs();

            foreach (string file in FILES)
            {
                //read in our base test that we'll use for this
                string json = ReflectionUtilities.GetResourceAsString("UnitTests.Documentation.TestFiles."+file+".apsimx");
                Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

                if (file == "Report" || file == "Manager" || file == "Morris")
                    sims.FileName = "/Examples/Tutorials/"+file+".apsimx";
                else
                    sims.FileName = "/Tests/Validation/"+file+".apsimx";

                List<ITag> actualTags = AutoDocumentation.Document(sims);

                string savedJSON = ReflectionUtilities.GetResourceAsString("UnitTests.Documentation.TestFiles."+file+".json");
                List<ITag> expectedTags = GetTags(savedJSON);

                MatchTagStructure(expectedTags, actualTags);
            }
        }

        /// <summary>
        /// This function creates each of the example doc structure jsons that the test TestDocumentationStructure uses to compare against.
        /// This needs to be run again if the documentation structure code is changed in a way that causes the structure of docs to change.
        /// </summary>
        public void GenerateComparisionJSONs()
        {
            foreach (string file in FILES)
            {
                string json = ReflectionUtilities.GetResourceAsString("UnitTests.Documentation.TestFiles."+file+".apsimx");
                Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

                if (file == "Report" || file == "Manager")
                    sims.FileName = "/Examples/Tutorials/"+file+".apsimx";
                else
                    sims.FileName = "/Tests/Validation/"+file+".apsimx";

                List<ITag> actualTags = AutoDocumentation.Document(sims);

                //use this to recreate the json file for an apsimx doc if changes to it's structure are made.
                string newJSON = GetJSON(actualTags);
                string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string fileName =Path.GetFullPath(Path.Combine(binDirectory, "..", "..", "..", "Tests", "UnitTests", "Documentation", "TestFiles", file+".json"));
                File.WriteAllText(fileName, newJSON);
            }
        }

        public void MatchTagStructure(List<ITag> expectedTags, List<ITag> actualTags)
        {
            Assert.That(actualTags.Count, Is.EqualTo(expectedTags.Count));

            for (int i = 0; i < expectedTags.Count; i++) {
                ITag expected = expectedTags[i];
                ITag actual = actualTags[i];
                Assert.That(expected.GetType() == actual.GetType());
                if (expected.GetType() == typeof(Section))
                {
                    Assert.That((actual as Section).Title, Is.EqualTo((expected as Section).Title));
                    MatchTagStructure((expected as Section).Children, (actual as Section).Children);
                }
                else if (expected.GetType() == typeof(Paragraph))
                {
                    string textE = (expected as Paragraph).text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");
                    string textA = (actual as Paragraph).text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");
                    Assert.That(textA, Is.EqualTo(textE));
                }
            }
        }
        public string GetJSON(List<ITag> tags) {
            return "[" + TagsToJSON(tags) + "]";
        }

        public string TagsToJSON(List<ITag> tags) {
            string output = "";

            for(int i = 0; i < tags.Count; i++) {
                ITag tag = tags[i];
                string type = tag.GetType().ToString();
                string text = "";
                string children = "";

                if (tag.GetType() == typeof(Section)) {
                    text = (tag as Section).Title;
                    children = TagsToJSON((tag as Section).Children);
                } else if (tag.GetType() == typeof(Paragraph)) {
                    text = (tag as Paragraph).text;
                } else if (tag.GetType() == typeof(Table)) {
                    text = (tag as Table).Style;
                } else if (tag.GetType() == typeof(DocGraphPage)) {
                    List<ITag> childGraphs = new List<ITag>();
                    foreach(IGraph iGraph in (tag as DocGraphPage).Graphs)
                        childGraphs.Add(iGraph as DocGraph);
                    children = TagsToJSON(childGraphs);
                } else if (tag.GetType() == typeof(DocGraph)) {
                    text = (tag as DocGraph).Title;
                }
                text = text.Replace("\r", "").Replace("\n", "").Replace("\"", "").Replace("\\", "");

                string objectString = "";
                objectString += "{\"type\": \""+type+"\"";
                if (text.Length > 0)
                    objectString += ", \"text\": \""+text+"\"";
                if (children.Length > 0)
                    objectString += ", \"children\": ["+children+"]";
                objectString += "}";

                if (i < tags.Count-1)
                    objectString += ",";

                output += objectString;
            }
            return output;
        }

        public List<ITag> GetTags(string json) {
            JArray obj = JArray.Parse(json);
            return JSONToTags(obj);
        }

        public List<ITag> JSONToTags(JArray array) {

            List<ITag> output = new List<ITag>();

            for(int i = 0; i < array.Count; i++) {
                JToken obj = array[i];
                if (obj["type"].ToString() == typeof(Section).ToString()) {
                    output.Add(new Section(obj["text"].ToString(), JSONToTags(obj["children"] as JArray)));

                } else if (obj["type"].ToString() == typeof(Paragraph).ToString()) {
                    output.Add(new Paragraph(obj["text"].ToString()));

                } else if (obj["type"].ToString() == typeof(Table).ToString()) {
                    output.Add(new Table(new DataTable(), style: obj["text"].ToString()));

                } else if (obj["type"].ToString() == typeof(DocGraphPage).ToString()) {
                    List<ITag> childGraphs = JSONToTags(obj["children"] as JArray);
                    List<IGraph> childIGraphs = new List<IGraph>();
                    foreach(DocGraph graph in childGraphs)
                        childIGraphs.Add(graph as IGraph);
                    output.Add(new DocGraphPage(childIGraphs));

                } else if (obj["type"].ToString() == typeof(DocGraph).ToString()) {
                    output.Add(new DocGraph(obj["text"].ToString(), "", null, null, null, null));

                }  else if (obj["type"].ToString() == typeof(MapTag).ToString()) {
                    Coordinate center = new Coordinate(0, 0);
                    output.Add(new MapTag(center, 0, new List<Coordinate>()));
                }
            }

            return output;
        }
    }
}