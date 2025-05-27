using DakkaDataLink_Dedicated;

namespace DDL_DedicatedServer_Linux
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DataManager? dataManager = DataManager.Instance;
            GlobalLogger logger = GlobalLogger.Instance;
            try
            {
                GlobalLogger.Log($"Dakka Data Link v{DdlConstants.VERSION_STRING} Dedicated Server starting.");
                dataManager.StartUdpServer();
                Console.WriteLine("Type Quit to quit.");
                string? KillString = "";
                while (KillString != "Quit")
                {
                    KillString = Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Log($"Unhandled exception: {ex.GetType()} {ex.Message}");
            }
            finally
            {
                dataManager.StopUdp();
                GlobalLogger.Shutdown();
            }
        }
    }
}
