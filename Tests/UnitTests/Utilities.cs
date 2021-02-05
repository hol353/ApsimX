
namespace UnitTests
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.GrazPlan;
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Reflection;

    [SetUpFixture]
    public class Utilities
    {
        private static string tempPath;
        [OneTimeTearDown]
        public static void TearDown()
        {
            try
            {
                Directory.Delete(tempPath, true);
            }
            catch
            {

            }
        }

        [OneTimeSetUp]
        public static void OneTimeSetUp()
        {
            tempPath = Path.Combine(Path.GetTempPath(), $"apsimx-unittests-{Guid.NewGuid().ToString()}");
            Environment.SetEnvironmentVariable("TMP", tempPath);
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
        }

        /// <summary>
        /// Event handler for a job runner's <see cref="IJobRunner.AllJobsCompleted"/> event.
        /// Asserts that the job ran successfully.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        public static void EnsureJobRanGreen(object sender, JobCompleteArguments args)
        {
            if (args.ExceptionThrowByJob != null)
                throw new Exception(string.Format("Exception was thrown when running via {0}, when we expected no error to be thrown.", sender.GetType().Name), args.ExceptionThrowByJob);
        }

        /// <summary>Call an event in a model</summary>
        public static void CallEvent(object model, string eventName, object[] arguments = null)
        {
            MethodInfo eventToInvoke = model.GetType().GetMethod("On" + eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (eventToInvoke != null)
            {
                if (arguments == null)
                    arguments = new object[] { model, new EventArgs() };
                eventToInvoke.Invoke(model, arguments);
            }
        }

        /// <summary>Call an event in a model and all child models.</summary>
        public static void CallEventAll(IModel model, string eventName, object[] arguments = null)
        {
            CallEvent(model, eventName, arguments);
            foreach (IModel descendant in model.FindAllDescendants())
                CallEvent(descendant, eventName, arguments);
        }

        /// <summary>ResolveLinks in a model</summary>
        public static void ResolveLinks(IModel model)
        {
            model.ParentAllDescendants();
            var links = new Links();
            links.Resolve(model, true, true);
        }

        /// <summary>Inject a link into a model</summary>
        public static void InjectLink(object model, string linkFieldName, object linkFieldValue)
        {
            ReflectionUtilities.SetValueOfFieldOrProperty(linkFieldName, model, linkFieldValue);
        }


        /// <summary>Convert a SQLite table to a string.</summary>
        public static string TableToString(string fileName, string tableName, IEnumerable<string> fieldNames = null)
        {
            SQLite database = new SQLite();
            database.OpenDatabase(fileName, true);
            var st = TableToString(database, tableName, fieldNames);
            database.CloseDatabase();
            return st;
        }

        /// <summary>Convert a SQLite table to a string.</summary>
        public static string TableToString(IDatabaseConnection database, string tableName, IEnumerable<string> fieldNames = null)
        {
            string sql = "SELECT ";
            if (fieldNames == null)
                sql += "*";
            else
            {
                bool first = true;
                foreach (string fieldName in fieldNames)
                {
                    if (first)
                        first = false;
                    else
                        sql += ",";
                    sql += fieldName;
                }
            }
            sql += " FROM [" + tableName + "]";
            var orderByFieldNames = new List<string>();
            if (database.GetColumnNames(tableName).Contains("CheckpointID"))
                orderByFieldNames.Add("[CheckpointID]");
            if (database.GetColumnNames(tableName).Contains("SimulationID"))
                orderByFieldNames.Add("[SimulationID]");
            if (database.GetColumnNames(tableName).Contains("Clock.Today"))
                orderByFieldNames.Add("[Clock.Today]");
            if (orderByFieldNames.Count > 0)
                sql += " ORDER BY " + StringUtilities.BuildString(orderByFieldNames.ToArray(), ",");
            DataTable data = database.ExecuteQuery(sql);
            return TableToString(data);
        }

        /// <summary>Convert a SQLite query to a string.</summary>
        public static string TableToStringUsingSQL(IDatabaseConnection database, string sql)
        {
            var data = database.ExecuteQuery(sql);
            return TableToString(data);
        }

        /// <summary>Convert a DataTable to a string.</summary>
        public static string TableToString(DataTable data)
        {
            StringWriter writer = new StringWriter();
            DataTableUtilities.DataTableToText(data, 0, ",", true, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Runs models.exe on the given sims and passes along the given command line arguments.
        /// Returns StdOut of Models.exe.
        /// </summary>
        /// <param name="sims">Simulations to be run.</param>
        /// <param name="arguments">Command line arguments to be passed to Models.exe.</param>
        public static string RunModels(Simulations sims, string arguments)
        {
            sims.FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            sims.Write(sims.FileName);
            string pathToModels = typeof(IModel).Assembly.Location;
            return RunModels(sims.FileName + " " + arguments);
        }

        public static string RunModels(string arguments)
        {
            string pathToModels = typeof(IModel).Assembly.Location;

            ProcessUtilities.ProcessWithRedirectedOutput proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(pathToModels, arguments, Path.GetTempPath(), true);
            proc.WaitForExit();

            if (proc.ExitCode != 0)
                throw new Exception(proc.StdOut);

            return proc.StdOut;
        }

        /// <summary>
        /// Returns a lightweight skeleton simulation which can be run.
        /// </summary>
        public static Simulations GetRunnableSim()
        {
            Simulations sims = new Simulations()
            {
                FileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx"),
                Children = new List<IModel>()
                {
                    new DataStore(),
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(2017, 1, 1),
                                EndDate = new DateTime(2017, 1, 10) // January 10
                            },
                            new Summary(),
                            new Zone()
                            {
                                Area = 1,
                                Children = new List<IModel>()
                                {
                                    new Models.Report()
                                    {
                                        VariableNames = new string[]
                                        {
                                            "[Clock].Today.DayOfYear as n"
                                        },
                                        EventNames = new string[]
                                        {
                                            "[Clock].DoReport"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            sims.ParentAllDescendants();
            sims.Write(sims.FileName);
            return sims;
        }

        public static Simulations GetSimpleExperiment()
        {
            Simulations result = ReadFromResource<Simulations>("UnitTests.Resources.SimpleExperiment.apsimx", out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            return result;
        }

        public static T ReadFromResource<T>(string resourceName, out List<Exception> creationExceptions) where T : IModel
        {
            string json = ReflectionUtilities.GetResourceAsString(resourceName);
            return FileFormat.ReadFromString<T>(json, out creationExceptions);
        }

        /// <summary>
        /// Call OnCreated in a model and all child models.
        /// </summary>
        /// <param name="model"></param>
        public static void CallOnCreated(IModel model)
        {
            model.OnCreated();
            foreach (var child in model.Children)
                CallOnCreated(child);
        }
    }
}
