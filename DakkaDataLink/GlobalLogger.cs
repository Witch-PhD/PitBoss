using System.IO;

namespace DakkaDataLink
{
    internal class GlobalLogger
    {
        private static GlobalLogger? m_instance;
        public static GlobalLogger Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new GlobalLogger();
                    m_instance.dataManager = DisplayManager.Instance;
                }
                return m_instance;
            }
            private set { }
        }
        private GlobalLogger()
        {
            logQueue = new Queue<string>();
            loggerThread = new Thread(loggerThreadTask);
            loggerThread.IsBackground = true;
            loggerThread.Start();
        }

        private DisplayManager dataManager;
        private Thread loggerThread;

        private static readonly object queueLock = new object();
        private Queue<string> logQueue;
        private bool m_RunLogger = true;

        private void loggerThreadTask()
        {
            using (StreamWriter outputFile = new StreamWriter(dataManager.userOptions.LoggerFilePath))
            {
                outputFile.Write(""); // Clear the log file.
            }
            while (m_RunLogger)
            {
                lock (queueLock)
                {
                    try
                    {
                        using (StreamWriter outputFile = new StreamWriter(dataManager.userOptions.LoggerFilePath, true))
                        {
                            while (logQueue.Count > 0)
                            {
                                string nextLogLine = logQueue.Dequeue();
                                outputFile.WriteLine(nextLogLine);
                            }
                        }
                    }
                    catch (Exception e) // This should only occur if two instance of Pit Boss are running on the same machine.
                    {
                        Console.WriteLine($"GlobalLogger.loggerThreadTask {e.GetType()}: {e.Message} \n\tLOGGING TO FILE WILL NOW CEASE.");
                    }
                }
                Thread.Sleep(250);
            }
        }

        public static void Shutdown()
        {
            m_instance.m_RunLogger = false;
            bool joined = m_instance.loggerThread.Join(2000);
            //Thread.Sleep(750);
            m_instance.FlushLog();
        }

        private void FlushLog()
        {
            lock (queueLock)
            {
                using (StreamWriter outputFile = new StreamWriter(dataManager.userOptions.LoggerFilePath, true))
                {
                    while (logQueue.Count > 0)
                    {
                        string nextLogLine = logQueue.Dequeue();
                        outputFile.WriteLine(nextLogLine);
                    }
                }
            }
        }

        public static void Log(string message, bool appendTimestamp = true)
        {
            lock (queueLock)
            {
                if (appendTimestamp)
                {
                    DateTime dateTime = DateTime.Now;
                    string formattedMonth = string.Format("{0:D2}", dateTime.Month);
                    string formattedDay = string.Format("{0:D2}", dateTime.Day);
                    string formattedHour = string.Format("{0:D2}", dateTime.Hour);
                    string formattedMin = string.Format("{0:D2}", dateTime.Minute);
                    string formattedSec = string.Format("{0:D2}", dateTime.Second);
                    string formattedMilli = string.Format("{0:D3}", dateTime.Millisecond);
                    string timeStampedLog = $"[{dateTime.Year}-{formattedMonth}-{formattedDay}]({formattedHour}:{formattedMin}:{formattedSec}.{formattedMilli})  {message}";
                    Instance.logQueue.Enqueue(timeStampedLog);
                    Console.WriteLine(timeStampedLog);
                }
                else
                {
                    Instance.logQueue.Enqueue(message);
                    Console.WriteLine(message);
                }
            }
        }
    }
}
