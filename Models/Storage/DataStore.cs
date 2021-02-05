namespace Models.Storage
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// # [Name]
    /// A storage service for reading and writing to/from a database.
    /// </summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.DataStoreView.glade")]
    [PresenterName("UserInterface.Presenters.DataStorePresenter")]
    [ValidParent(ParentType = typeof(Simulations))]
    public class DataStore : Model, IDataStore, IDisposable
    {
        /// <summary>A database connection</summary>
        [NonSerialized]
        private IDatabaseConnection connection = null;

        [NonSerialized]
        private DataStoreReader dbReader = new DataStoreReader();

        [NonSerialized]
        private DataStoreWriter dbWriter = new DataStoreWriter();

        [JsonIgnore]
        private string fileName;

        /// <summary>
        /// Selector for the database type. Set in the constructors.
        /// </summary>
        public bool useFirebird { get; set; } = false;

        /// <summary>
        /// Returns the file name of the .db file.
        /// Returns CustomFileName if it has been given; will fallback to
        /// fileName otherwise.
        /// </summary>
        [JsonIgnore]
        public string FileName
        {
            get
            {
                return string.IsNullOrWhiteSpace(CustomFileName) ? fileName : CustomFileName;
            }
            set
            {
                fileName = value;
            }
        }

        /// <summary>
        /// Allows the user to override the .db file location.
        /// </summary>
        public string CustomFileName { get; set; } = null;

        /// <summary>Get a reader to perform read operations on the datastore.</summary>
        public IStorageReader Reader { get { return dbReader; } }

        /// <summary>Get a writer to perform write operations on the datastore.</summary>
        public IStorageWriter Writer { get { return dbWriter; } }

        /// <summary>Constructor</summary>
        public DataStore()
        {
        }

        /// <summary>Constructor</summary>
        public DataStore(string fileNameToUse)
        {
            FileName = fileNameToUse;
            SQLite database = new SQLite();
            database.OpenDatabase(fileName, true);
            connection = database;
            dbReader.SetConnection(connection);
            dbWriter.SetConnection(connection);
        }

        /// <summary>Constructor</summary>
        public DataStore(IDatabaseConnection db)
        {
            connection = db;
            dbReader.SetConnection(connection);
            dbWriter.SetConnection(connection);
        }

        /// <summary>
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~DataStore()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed = false;

        /// <summary>Dispose method</summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
            Close();
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    // component.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                Close();

                // Note disposing has been done.
                disposed = true;
            }
        }

        /// <summary>Object has been created.</summary>
        public override void OnCreated()
        {
            if (connection == null)
                Open();
        }
        
        /// <summary>
        /// Updates the file name of the database file, based on the file name
        /// of the parent Simulations object.
        /// </summary>
        public void UpdateFileName()
        {
            string extension = useFirebird ? ".fdb" : ".db";

            Simulations simulations = FindAncestor<Simulations>();

            // If we have been cloned prior to a run, then we won't be able to locate
            // the simulations object. In this situation we can fallback to using the
            // parent simulation's filename (which should be the same anyway).
            Simulation simulation = FindAncestor<Simulation>();

            if (simulations != null && simulations.FileName != null)
                FileName = Path.ChangeExtension(simulations.FileName, extension);
            else if (simulation != null && simulation.FileName != null)
                FileName = Path.ChangeExtension(simulation.FileName, extension);
            else
                FileName = ":memory:";
        }

        /// <summary>Open the database.</summary>
        public void Open()
        {
            if (FileName == null)
                UpdateFileName();

            if (useFirebird)
                connection = new Firebird();
            else
                connection = new SQLite();

            connection.OpenDatabase(FileName, readOnly: false);

            Exception caughtException = null;
            try
            {
                dbReader?.SetConnection(connection);
            }
            catch (Exception e)
            {
                caughtException = e;
            }
            try
            {
                dbWriter?.SetConnection(connection);
            }
            catch (Exception e)
            {
                caughtException = e;
            }
            if (caughtException != null)
                throw caughtException;
        }

        /// <summary>Close the database.</summary>
        public void Close()
        {
            if (connection != null)
            {
                connection.CloseDatabase();
                connection = null;
            }
        }

        /// <summary>
        /// Add a select based view to the data table for SQLite datastores
        /// </summary>
        /// <param name="name">name of the view</param>
        /// <param name="selectSQL">select SQL statement</param>
        public void AddView(string name, string selectSQL)
        {
            if (connection is SQLite)
            {
                if (connection.ViewExists(name))
                {
                    connection.ExecuteNonQuery($"DROP VIEW {name}");
                }
                connection.ExecuteNonQuery($"CREATE VIEW {name} AS {selectSQL}");
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
