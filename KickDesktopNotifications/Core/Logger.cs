using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KickDesktopNotifications.Core
{
    public class Logger : SingletonFactory<Logger>, Singleton
    {
        private string _name;
        private StreamWriter sw;
        private readonly object _lock = new object();
        
        public Logger()
        {
#if DEBUG
            _name = DateTime.Now.ToString("dd_mm_yyyy HH_mm")+".log";
#endif
        }

        ~Logger()
        {
#if DEBUG
            lock (_lock)
            {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                }
            }
#endif
        }

        public void WriteLine(string message)
        {
#if DEBUG
            lock (_lock)
            {
                try
                {
                    if (sw == null)
                    {
                        String FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KickNotify");
                        Directory.CreateDirectory(FilePath);
                        // Open with FileShare.ReadWrite to allow multiple processes/instances to write
                        var fileStream = new FileStream(
                            Path.Combine(FilePath, _name), 
                            FileMode.Append, 
                            FileAccess.Write, 
                            FileShare.ReadWrite
                        );
                        sw = new StreamWriter(fileStream);
                        sw.AutoFlush = true; // Auto-flush to ensure logs are written immediately
                    }
                    sw.WriteLine(message);
                }
                catch
                {
                    // Silently fail if logging fails
                }
            }
#endif
        }

        public StreamWriter Writer { 
            get { 
#if DEBUG
                if(sw == null)
                {
                    String FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KickNotify");
                    Directory.CreateDirectory(FilePath);
                    // Open with FileShare.ReadWrite to allow multiple processes/instances to write
                    var fileStream = new FileStream(
                        Path.Combine(FilePath, _name), 
                        FileMode.Append, 
                        FileAccess.Write, 
                        FileShare.ReadWrite
                    );
                    sw = new StreamWriter(fileStream);
                    sw.AutoFlush = true; // Auto-flush to ensure logs are written immediately
                }
                return sw;
#else
                return null;
#endif
            } 
        }
    }
}
