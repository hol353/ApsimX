namespace APSIMRunner
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.Script;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using System.Timers;
    using static Models.Core.Run.JobRunnerMultiProcess;

    class Program
    {
        /// <summary>Main program</summary>
        static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length < 2)
                    throw new Exception("Usage: APSIMRunner.exe pipeWriteHandle pipeReadHandle");

                // Get read and write pipe handles
                // Note: Roles are now reversed from how the other process is passing the handles in
                string pipeWriteHandle = args[0];
                string pipeReadHandle = args[1];

                // Let in for debugging purposes.
                //while (pipeReadHandle != null) 
                //    Thread.Sleep(500);

                // Add hook for manager assembly resolve method.
                AppDomain.CurrentDomain.AssemblyResolve += ScriptCompiler.ResolveManagerAssemblies;

                // Create 2 anonymous pipes (read and write) for duplex communications
                // (each pipe is one-way)
                using (var pipeRead = new AnonymousPipeClientStream(PipeDirection.In, pipeReadHandle))
                using (var pipeWrite = new AnonymousPipeClientStream(PipeDirection.Out, pipeWriteHandle))
                {
                    Client client = new Client(pipeRead, pipeWrite);
                    client.Run();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ScriptCompiler.ResolveManagerAssemblies;
            }
            return 0;
        }
    }
}