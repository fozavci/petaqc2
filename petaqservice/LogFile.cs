using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PetaqService
{
    public class LogFile 
    {
        public string logFolderName;
        public string logFileName;
        public string filesFolderName;
        public bool keepReading = true;
        public string socketId { get; private set; }

        public LogFile(string socketId) 
        {
            // set main session log path
            logFolderName = Path.Combine("Logs", Program.logDate);
            logFileName = Path.Combine(logFolderName, socketId + ".txt");

            // set a store path for files
            filesFolderName = Path.Combine("Files", Program.logDate, socketId);

            try
            {

                // check and create the main session log and files folders
                if (! Directory.Exists(logFolderName))
                {
                    Directory.CreateDirectory(logFolderName);
                    //Console.WriteLine("Creating {0} folder.", logFolderName);
                }
                if (!Directory.Exists(filesFolderName))
                {
                    Directory.CreateDirectory(filesFolderName);
                    //Console.WriteLine("Creating {0} folder.", filesFolderName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Session log folder couldn't be created for {0}.", e);
            }
            finally
            {
                var fileStream = new FileStream(logFileName, FileMode.OpenOrCreate) ;
                Console.WriteLine("Log file {0} created.", Path.Combine(logFolderName, socketId + ".txt"));
                fileStream.Close(); 
            }

        }

        public DirectoryInfo CreateFolder(string f)
        {
            DirectoryInfo di = null;

            return di;
        }
        public void Write(string data)
        {
            // opening file as a stream to write the data
            var fileStream = new FileStream(logFileName, FileMode.Append);
            StreamWriter outputStream = new StreamWriter(fileStream);
            outputStream.WriteLine(data);
            outputStream.Close();
            fileStream.Close();
        }

        public void Read()
        {
            // setting events for the file updates to read 
            var wh = new AutoResetEvent(false);
            var fsw = new FileSystemWatcher(".");
            fsw.Filter = "file-to-read";
            fsw.EnableRaisingEvents = true;
            fsw.Changed += (s, e) => wh.Set();

            // set the file to read
            var fileStream = new FileStream(logFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            // start reading the file untill keepReading reaches false
            using (var inputStream = new StreamReader(fileStream))
            {
                var s = "";
                while (keepReading)
                {
                    s = inputStream.ReadLine();
                    if (s != null)
                        Console.WriteLine(s);
                    else
                        wh.WaitOne(1000);
                }
                
            }

            wh.Close();
            fileStream.Close();
            return;
        }

    }
}
