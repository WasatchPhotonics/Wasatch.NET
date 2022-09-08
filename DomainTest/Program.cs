using System;
using System.Reflection;
using WasatchNET;

/// <summary>
/// Test WasatchNET's resource disposal.
/// </summary>
/// <remarks>
/// This program allows WasatchNET maintainers to test how well WasatchNET
/// classes "clean up" after themselves when they are destroyed (including
/// all singletons).  In particular, this can be handy to run from inside
/// WinDbg to catch upwelling CLR exceptions which you may not see from
/// WinFormDemo, etc.
/// </remarks>
/// <see cref="https://knowledge.ni.com/KnowledgeArticleDetails?id=kA00Z000000PAR3SAO"/>
namespace DomainTest
{
    public class TestClass : MarshalByRefObject
    {
        public void callConstructor()
        {
            Driver driver = Driver.getInstance();
            Logger logger = driver.logger;
            driver.openAllSpectrometers();
            int count = driver.getNumberOfSpectrometers();
            Console.WriteLine($"found {count} spectrometers");
            logger.close();
            driver.closeAllSpectrometers();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string assemblyName = Assembly.GetExecutingAssembly().Location;
            string typeName     = "DomainTest.TestClass";
            string domainName   = "Test Domain";

            Console.WriteLine($"assemblyName = [{assemblyName}]");
            Console.WriteLine($"typeName     = [{typeName}]");
            Console.WriteLine($"domainName   = [{domainName}]");

            // this is a good time to attach WinDbg
            pause();

            Console.WriteLine("Creating domain");
            AppDomain appDomain = AppDomain.CreateDomain(domainName);

            Console.WriteLine($"Instantiating {typeName} in {assemblyName}");
            TestClass testClass = (TestClass)appDomain.CreateInstanceFromAndUnwrap(
                assemblyName, typeName);

            Console.WriteLine($"Testing Wasatch.NET");
            testClass.callConstructor();

            // This should actually Dispose of singletons like Driver, Logger etc
            Console.WriteLine($"Unloading {domainName}");
            AppDomain.Unload(appDomain);

            Console.WriteLine("Done.");
            pause();
        }

        static void pause()
        {
            Console.WriteLine("Press return to continue...");
            Console.ReadLine();
        }
    }
}
