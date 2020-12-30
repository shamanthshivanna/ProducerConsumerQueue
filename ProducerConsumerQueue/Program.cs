using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProducerConsumerQueue.Model.Interfaces;
using ProducerConsumerQueue.Queueing;
using ProducerConsumerQueue.Model;
using System.Timers;
using System.IO;


namespace ProducerConsumerQueue
{
    class Program
    {

        //This will send objects to generic Queue
        private static ProcessorQueue<IQueueItem, IQueueResponse> QueueProcessor;

        //This will hold our base directory path
        private static string FileBasePath = AppDomain.CurrentDomain.BaseDirectory;
        private static string FileName = "InputStimulus.txt";

        // Main method or entry point of our application
        static void Main (string[] args)
            {
            QueueProcessor = new ProcessorQueue<IQueueItem, IQueueResponse>(Processor());
            SetTimerToCloseApp();
            ProcessInput();
            Console.ReadLine();
        }

        // ProcessInput methos will take all the necessary input from the user
        private static void ProcessInput()
        {
            try
            {
                var payloads = GetCsvData();
                if (payloads != null && payloads.Count > 0)
                {
                    var strMessage = "To Continue (y/n), to exit: (ctrl + c)? ";
                    Console.Write(strMessage);
                    var key = Console.ReadLine();
                    if (key.ToLower() != "y")
                    {
                        // set default to 'n' so we can ask more times to run or not in while loop below.
                        key = "n";
                    }

                    switch (key.ToLower())
                    {
                        case "y":
                            ProcessData(payloads);
                            break;
                        case "n":
                            while (key.ToLower() == "n")
                            {
                                Console.Write(strMessage);
                                key = Console.ReadLine();
                                if (key.ToLower() != "y")
                                    key = "n";
                            }
                            if (key.ToLower() == "y")
                                ProcessData(payloads);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// ProcessData method will process all the entries we get from the csv file and send to queue.
        /// </summary>
        /// <param name="payloads"></param>
        private static void ProcessData(List<CsvPayloadDataModel> payloads)


        {
            try
            {
                var maxLimitToProcessObject = System.Configuration.ConfigurationManager.AppSettings["maxLimitToProcessedObjectPerSecond"] == null ? 10 : Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["maxLimitToProcessedObjectPerSecond"].ToString());
                var recordProcessed = 1;
                var startTime = DateTime.UtcNow;
                foreach (var item in payloads)
                {
                    if (string.IsNullOrEmpty(item.Type))
                        continue;

                    if (recordProcessed > maxLimitToProcessObject)
                    {
                        int totalRemaintime = (int)((DateTime.UtcNow - startTime).TotalMilliseconds);
                        if (totalRemaintime <= 1000)
                        {
                            Task.Delay(1000 - totalRemaintime).Wait();
                        }
                        recordProcessed = 1;
                        startTime = DateTime.UtcNow;
                    }

                 

                    switch (item.Type.ToLower())
                    {
                        case "file":
                            QueueProcessor.QueueFileItem(new FileQueueModel(item.Payload)).ConfigureAwait(false);
                            break;
                        case "console":
                            QueueProcessor.QueueConsoleItem(new ConsoleQueueModel(item.Payload)).ConfigureAwait(false);
                            break;
                        default:
                            break;
                    }
                    recordProcessed++;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Processor will identify which functions should be call according to Queue Type.
        /// </summary>
        /// <returns></returns>
        private static Func<IQueueItem, IQueueResponse> Processor()
        {
            return new Func<IQueueItem, IQueueResponse>(new Program().ProcessQueueItem);
        }

        /// <summary>
        /// ProcessQueueItem will manage which function should be call for which type. (File, Console).
        /// </summary>
        /// <param name="queueItem"></param>
        /// <returns></returns>
        private IQueueResponse ProcessQueueItem(IQueueItem queueItem)
        {
            IQueueResponse queueResponse;
            var fileModel = queueItem as FileQueueModel;
            if (fileModel == null)
            {
                var consoleModel = queueItem as ConsoleQueueModel;
                queueResponse = this.ProcessConsole(consoleModel);
            }
            else
            {
                queueResponse = this.ProcessFile(fileModel);
            }
            return queueResponse;
        }

        /// <summary>
        /// This is our main function for 'File' type. this is write payout to file.
        /// </summary>
        /// <param name="fileItem"></param>
        /// <returns></returns>
        private FileQueueResponseModel ProcessFile(FileQueueModel fileItem)
        {
            FileQueueResponseModel result = new FileQueueResponseModel();
            try
            {
                if (!string.IsNullOrEmpty(fileItem.Message))
                {
                    File.AppendAllText(Path.Combine(FileBasePath, FileName), string.Format("{0}{1}", fileItem.Message, Environment.NewLine));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// This is our main function for 'Console' type. this is write payout to console application screen.
        /// </summary>
        /// <param name="consoleItem"></param>
        /// <returns></returns>
        private ConsoleQueueResponseModel ProcessConsole(ConsoleQueueModel consoleItem)
        {
            ConsoleQueueResponseModel result = new ConsoleQueueResponseModel();
            try
            {
                Console.WriteLine(consoleItem.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return result;
        }

        private static List<CsvPayloadDataModel> GetCsvData()
        {
            List<CsvPayloadDataModel> payloads = new List<CsvPayloadDataModel>();
            try
            {
                var csvFileName = System.Configuration.ConfigurationManager.AppSettings["csvFileName"] != null ? System.Configuration.ConfigurationManager.AppSettings["csvFileName"].ToString() : "";
                var csvFilePath = Path.Combine(FileBasePath, "csv", csvFileName);
                if (File.Exists(csvFilePath))
                {
                    Console.WriteLine($"Getting data from csv file: {csvFilePath}");
                    var data = File.ReadAllLines(csvFilePath);
                    payloads = (from queueData in data
                                let q = queueData.Split(',')
                                select new CsvPayloadDataModel()
                                {
                                    Type = q[0],
                                    Payload = q[1]
                                }).ToList();

                    Console.WriteLine($"Total records.: {payloads.Count()}");
                }
                else
                {
                    Console.WriteLine($"File not exists. {csvFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return payloads;
        }

        /// <summary>
        /// This will manage to shutdown the app after specify time. if 0 then won't auto close.
        /// </summary>
        private static void SetTimerToCloseApp()
        {
            try
            {
                var second = System.Configuration.ConfigurationManager.AppSettings["secondToCloseApp"] == null ? 10 : Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["secondToCloseApp"].ToString());
                if (second > 0)
                {
                    Console.WriteLine($"Application will shutdown in {second} seconds.");
                    System.Timers.Timer timer = new System.Timers.Timer(second * 1000);
                    timer.Elapsed += TimerTick;
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Will shutdown the app.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void TimerTick(Object obj, ElapsedEventArgs e)
        {
            Console.WriteLine("Exiting");
            Environment.Exit(0);
        }

    }
}
