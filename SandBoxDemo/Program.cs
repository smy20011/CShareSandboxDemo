using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Runtime.Remoting;
using System.Reflection;
namespace SandBoxDemo
{
    class Program
    {
        class SandBoxer: MarshalByRefObject
        {
            static string typeName = "SandboxTest.SandboxTest";
            public String assemblyName { get; set; }
            public static SandBoxer BuildSandbox(string assemblyName)
            {
                PermissionSet permSet = new PermissionSet(PermissionState.None);
                // Only allow execution
                permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                // Underlaying exvironment
                StrongName fullTrustAssembly = typeof(SandBoxer).Assembly.Evidence.GetHostEvidence<StrongName>();

                AppDomainSetup adSetup = new AppDomainSetup();
                adSetup.ApplicationBase = Path.GetFullPath(".");

                AppDomain domain = AppDomain.CreateDomain("Sandbox", null, adSetup, permSet, fullTrustAssembly);

                // Create Sandboxer in this domain
                ObjectHandle handler = Activator.CreateInstanceFrom(
                    domain, typeof(SandBoxer).Assembly.ManifestModule.FullyQualifiedName,
                    typeof(SandBoxer).FullName);

                SandBoxer handle = (SandBoxer)handler.Unwrap();
                handle.assemblyName = assemblyName;
                return handle;
            }

            // Currently all function call parameters are integer and return integer
            public int Execute(string commandName, int[] arguments)
            {
                try
                {
                    (new PermissionSet(PermissionState.Unrestricted)).Assert();
                    MethodInfo target = Assembly.LoadFrom(assemblyName).GetType(typeName).GetMethod(commandName);
                    CodeAccessPermission.RevertAssert();
                    int result = (int)target.Invoke(null, arguments.Cast<Object>().ToArray());
                    return result;
                }
                catch (Exception ex)
                {
                    // Release permission requirement to print error
                    (new PermissionSet(PermissionState.Unrestricted)).Assert();
                    if (ex is TargetInvocationException)
                    {
                        Console.WriteLine("Exception caught: {0}", (ex as TargetInvocationException).InnerException.GetType().Name);
                    }
                    else
                    {
                        Console.WriteLine("Exception caught: {0}", ex.ToString());
                    }
                    CodeAccessPermission.RevertAssert();
                }
                return 0;
            }
            // Get status of sandbox environment
            public string Status()
            {
                if (String.IsNullOrWhiteSpace(assemblyName))
                {
                    return "Assembly: None";
                }
                return "Assembly: " + assemblyName;
            }
        }
        class REPL
        {
            private static string LoadCommand = "load";
            private static string ExitCommand = "exit";
            SandBoxer container = new SandBoxer();
            bool finished = false;

            // REPL loop, accepts a stream reader
            public void Loop()
            {
                while (!finished)
                {
                    // Write the commend header
                    Console.Write("(cmd {0}) ", container.Status());
                    String command = Console.ReadLine();
                    Exec(command);
                }
            }

            public void Exec(String command)
            {
                if (String.IsNullOrWhiteSpace(command))
                {
                    return;
                }

                string[] commandSplit = command.Split(' ');
                string   commandName = commandSplit[0];
                string[] commandParams = commandSplit.Skip(1).ToArray();

                if (commandName == LoadCommand)
                {
                    container = SandBoxer.BuildSandbox(commandParams[0]);
                    return;
                }

                if (commandName == ExitCommand)
                {
                    finished = true;
                    return;
                }

                // Convert all params to int
                int[] functionCallParams = commandParams.Select(Int32.Parse).ToArray();
                int result = container.Execute(commandName, functionCallParams);
                Console.WriteLine("Exection Result is: {0}", result);
            }
        }
        static void Main(string[] args)
        {
            REPL repl = new REPL();
            repl.Loop();
        }
    }
}
