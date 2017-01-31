using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;
using System.Collections;

namespace SLI.Logger
{
    public interface ILogger
    {
        void PublishMessage(SLI_Message message);
        void Close();
    }

    public class Logger : ILogger
    {
        #region Events
        public static event EventHandler<ErrorMessageEventArgs> OnErrorEvent;
        #endregion

        #region Implementation Configuration Holder
        private LoggerConfiguration _logConfig = null;
        private Dictionary<SeverityLevels, BaseLoggerConfig> levelConfiguration = new Dictionary<SeverityLevels, BaseLoggerConfig>();
        #endregion

        #region Dictionary to be lock using tasks
        private ConcurrentDictionary<int, SLI_Message> debugDictionary = null;
        private ConcurrentDictionary<int, SLI_Message> errorDictionary = null;
        #endregion

        #region Properties
        private IConfigurationRoot Configuration { get; set; }
        #endregion

        #region Param
        private static Timer _writeTime;
        private static volatile int dldnow;
        private Dictionary<SeverityLevels, Queue<string>> severityfilequeue = new Dictionary<SeverityLevels, Queue<string>>();
        #endregion

        #region Ctor
        public Logger()
        {
            GetLoggerConfiguration();
            _writeTime = new Timer(new TimerCallback(WriteToAllFile), dldnow, _logConfig.TimeUntilFileWrite, _logConfig.TimeUntilFileWrite);
        }
        #endregion

        #region PublicMethods  

        public void PublishMessage(SLI_Message _message)
        {
            bool writeSucceeded;
            int dictionarySize = AddToDictionary(_message.GetHashCode(), _message);
            //check if dictionary size is larger than configuration max entries
            if (dictionarySize >= _logConfig.MaxEntriesInDictionary)
            {
                writeSucceeded = WriteToFile(_message.Level);
                if (writeSucceeded)
                    EmptyDictionary(_message.Level);
            }
        }
        public void Close()
        {
            WriteToAllFile(null);
        }
        #endregion

        #region Private Methods
        private Queue<string> InitializeQueue(int _amountoffiles, string _filepath, int maxfilesize)
        {
            Queue<string> _emptyfilepathqueue = new Queue<string>(_amountoffiles);
            Queue<string> _existfilepathqueue = new Queue<string>(_amountoffiles);
            string _extension = Path.GetExtension(_filepath);
            for (int i = -1; i < _amountoffiles - 1; i++)
            {
                string newfilepath = _filepath;
                newfilepath = newfilepath.Replace(_extension, "_" + (i + 1) + _extension);
                if (File.Exists(newfilepath) && new System.IO.FileInfo(newfilepath).Length >= maxfilesize)
                    _existfilepathqueue.Enqueue(newfilepath);
                else
                    break;

            }
            //all log file are full at initilize
            if (_existfilepathqueue.Count == _amountoffiles)
            {
                return _existfilepathqueue;
            }

            else
            {
                for (int i = _existfilepathqueue.Count - 1; i < _amountoffiles - 1; i++)
                {
                    string newfilepath = _filepath;
                    newfilepath = newfilepath.Replace(_extension, "_" + (i + 1) + _extension);
                    _emptyfilepathqueue.Enqueue(newfilepath);

                }
                while (_existfilepathqueue.Count != 0)
                    _emptyfilepathqueue.Enqueue(_existfilepathqueue.Dequeue());
                return _emptyfilepathqueue;
            }
        }
        private void GetLoggerConfiguration()
        {
            try
            {
                Configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("AppSettings.json").Build();
                //Get Logger Configuration
                _logConfig = new LoggerConfiguration();
                Configuration.GetSection("LogSettings:LogGlobalConfiguration").Bind(_logConfig);
                //Get level preferences
                foreach (SeverityLevels obj in Enum.GetValues(typeof(SeverityLevels)))
                {
                    BaseLoggerConfig tempObj = new BaseLoggerConfig();
                    Configuration.GetSection("LogSettings:" + "Log" + obj.ToString()).Bind(tempObj);
                    if (tempObj.Implemented)
                    {
                        switch (obj)
                        {

                            case SeverityLevels.Debug:
                                {
                                    levelConfiguration.Add(SeverityLevels.Debug, new ManyFilesConfig());
                                    Configuration.GetSection("LogSettings:LogDebug").Bind(levelConfiguration[SeverityLevels.Debug]);
                                    debugDictionary = new ConcurrentDictionary<int, SLI_Message>();
                                    break;
                                }
                            case SeverityLevels.Error:
                                {
                                    levelConfiguration.Add(SeverityLevels.Error, new OneFileConfig());
                                    Configuration.GetSection("LogSettings:LogError").Bind(levelConfiguration[SeverityLevels.Error]);
                                    errorDictionary = new ConcurrentDictionary<int, SLI_Message>();
                                    break;
                                }
                            case SeverityLevels.All:
                            case SeverityLevels.Fatal:
                            case SeverityLevels.Info:
                            case SeverityLevels.OFF:
                            case SeverityLevels.Trace:
                            case SeverityLevels.Trace_INT:
                            case SeverityLevels.WARN:
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method:GetLoggerConfiguration -- Message:" + ex.Message);
            }
        }
        private void WriteToAllFile(object state)
        {
            _writeTime.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            foreach (KeyValuePair<SeverityLevels, BaseLoggerConfig> obj in levelConfiguration)
            {
                WriteToFile(obj.Key);
            }
            _writeTime.Change(_logConfig.TimeUntilFileWrite, _logConfig.TimeUntilFileWrite);
        }

        private void EmptyDictionary(SeverityLevels level)
        {
            switch (level)
            {
                case SeverityLevels.Debug:
                    {
                        debugDictionary.Clear();
                        break;
                    }
                case SeverityLevels.Error:
                    {
                        errorDictionary.Clear();
                        break;
                    }
                case SeverityLevels.All:
                case SeverityLevels.Fatal:
                case SeverityLevels.Info:
                case SeverityLevels.OFF:
                case SeverityLevels.Trace:
                case SeverityLevels.Trace_INT:
                case SeverityLevels.WARN:
                default:
                    break;
            }
        }

        private int AddToDictionary(int _hashcode, SLI_Message temp)
        {
            try
            {
                int dictionarySize = 0;
                switch (temp.Level)
                {

                    case SeverityLevels.Debug:
                        {
                            bool added;
                            int numberoftries = 0;
                            do
                            {

                                added = debugDictionary.TryAdd(_hashcode, temp);
                                if (++numberoftries > 99)
                                    throw new Exception("Add tuple was not success", new Exception(SeverityLevels.Debug.ToString()));
                            }
                            while (!added);
                            dictionarySize = debugDictionary.Count;
                            break;
                        }
                    case SeverityLevels.Error:
                        {
                            bool added;
                            int numberoftries = 0;
                            do
                            {
                                added = errorDictionary.TryAdd(_hashcode, temp);
                                if (++numberoftries > 99)
                                    throw new Exception("Add tuple was not success", new Exception(SeverityLevels.Error.ToString()));
                            }
                            while (!added);
                            Logger.OnErrorEvent(null, new ErrorMessageEventArgs { Message = temp.Message });
                            dictionarySize = errorDictionary.Count;
                            break;
                        }
                    case SeverityLevels.All:
                    case SeverityLevels.Fatal:
                    case SeverityLevels.Info:
                    case SeverityLevels.OFF:
                    case SeverityLevels.Trace:
                    case SeverityLevels.Trace_INT:
                    case SeverityLevels.WARN:
                    default:
                        break;
                }
                return dictionarySize;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method:AddToDictionary -- Message:" + ex.Message);
                switch ((SeverityLevels)Enum.Parse(typeof(SeverityLevels), ex.InnerException.Message))
                {
                    case SeverityLevels.Debug:
                        return debugDictionary.Count;
                    case SeverityLevels.Error:
                        return errorDictionary.Count;
                    case SeverityLevels.All:
                    case SeverityLevels.Fatal:
                    case SeverityLevels.Info:
                    case SeverityLevels.OFF:
                    case SeverityLevels.Trace:
                    case SeverityLevels.Trace_INT:
                    case SeverityLevels.WARN:
                    default:
                        return 0;
                }
            }
        }

        private bool WriteToFile(SeverityLevels _severity)
        {

            try
            {
                StringBuilder messageToWrite = new StringBuilder();
                MemoryStream stream = new MemoryStream();
                switch (_severity)
                {
                    case SeverityLevels.Debug:
                        {
                            long fileSize;
                            if (severityfilequeue.Count == 0)
                                severityfilequeue.Add(SeverityLevels.Debug, new Queue<string>());
                            if (((Queue<string>)severityfilequeue[SeverityLevels.Debug]).Count() == 0)
                            {
                                severityfilequeue[SeverityLevels.Debug] = InitializeQueue(((ManyFilesConfig)levelConfiguration[_severity]).AmountOfFiles, levelConfiguration[_severity].FilePath, ((ManyFilesConfig)levelConfiguration[_severity]).SizeThresholdInBytes);
                            }
                            if (debugDictionary.Count > 0)
                            {
                                string filepath = string.Empty;
                                filepath = severityfilequeue[SeverityLevels.Debug].Peek();
                                if (File.Exists(filepath))
                                {
                                    fileSize = new FileInfo(filepath).Length;
                                    if (fileSize >= ((ManyFilesConfig)levelConfiguration[_severity]).SizeThresholdInBytes)
                                        File.Create(filepath);
                                }
                                else
                                    File.Create(filepath);
                                PrepareMessages(ref messageToWrite, _severity);
                                using (StreamWriter file = File.AppendText(filepath))
                                {
                                    file.WriteLine(messageToWrite);
                                }
                                fileSize = new FileInfo(filepath).Length;
                                if (fileSize > ((ManyFilesConfig)levelConfiguration[_severity]).SizeThresholdInBytes)
                                {
                                    severityfilequeue[SeverityLevels.Debug].Enqueue(severityfilequeue[SeverityLevels.Debug].Dequeue());
                                }
                            }
                        }
                        break;
                    case SeverityLevels.Error:
                        {
                            if (errorDictionary.Count > 0)
                            {
                                PrepareMessages(ref messageToWrite, _severity);
                                using (StreamWriter file = File.AppendText(levelConfiguration[_severity].FilePath))
                                {
                                    file.WriteLine(messageToWrite);
                                }
                            }
                            break;
                        }
                    case SeverityLevels.All:
                    case SeverityLevels.Fatal:
                    case SeverityLevels.Info:
                    case SeverityLevels.OFF:
                    case SeverityLevels.Trace:
                    case SeverityLevels.Trace_INT:
                    case SeverityLevels.WARN:
                    default:
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Method:WriteToFile -- Message:" + ex.Message);
                return false;
            }
        }

        private void PrepareMessages(ref StringBuilder messageToWrite, SeverityLevels _severity)
        {
            switch (_severity)
            {
                case SeverityLevels.Debug:
                    {
                        messageToWrite.Append("----------------------------------");
                        foreach (KeyValuePair<int, SLI_Message> item in debugDictionary)
                        {
                            messageToWrite.Append(Environment.NewLine)
                            .Append("SourceTime: " + item.Value.Time)
                            .Append(Environment.NewLine)
                            .Append("LoggerTime: " + item.Value.Time)
                            .Append(Environment.NewLine)
                            .Append("SeverityLevels: " + item.Value.Level)
                            .Append(Environment.NewLine)
                            .Append("Message: " + item.Value.Message);
                            if (!String.IsNullOrEmpty(item.Value.AdditonalMessage))
                            {
                                messageToWrite.Append(Environment.NewLine)
                                .Append("AdditonalMessage: " + item.Value.AdditonalMessage);
                            }
                            if (!String.IsNullOrEmpty(item.Value.ExtraString))
                            {
                                messageToWrite.Append(Environment.NewLine)
                                .Append("ExtraString: " + item.Value.ExtraString);
                            }
                            messageToWrite.Append(Environment.NewLine)
                            .Append("MachineName: " + Environment.MachineName)
                            .Append(Environment.NewLine)
                            .Append("----------------------------------");
                        }
                        break;
                    }
                case SeverityLevels.Error:
                    {
                        messageToWrite.Append("----------------------------------");
                        foreach (KeyValuePair<int, SLI_Message> item in errorDictionary)
                        {
                            messageToWrite.Append(Environment.NewLine)
                            .Append("SourceTime: " + item.Value.Time)
                            .Append(Environment.NewLine)
                            .Append("LoggerTime: " + item.Value.Time)
                            .Append(Environment.NewLine)
                            .Append("SeverityLevels: " + item.Value.Level)
                            .Append(Environment.NewLine)
                            .Append("Message: " + item.Value.Message);
                            if (!String.IsNullOrEmpty(item.Value.AdditonalMessage))
                            {
                                messageToWrite.Append(Environment.NewLine)
                                .Append("AdditonalMessage: " + item.Value.AdditonalMessage);
                            }
                            if (!String.IsNullOrEmpty(item.Value.ExtraString))
                            {
                                messageToWrite.Append(Environment.NewLine)
                                .Append("ExtraString: " + item.Value.ExtraString);
                            }
                            messageToWrite.Append(Environment.NewLine)
                            .Append("MachineName: " + Environment.MachineName)
                            .Append(Environment.NewLine)
                            .Append("----------------------------------");
                        }
                        break;
                    }
                case SeverityLevels.All:
                case SeverityLevels.Fatal:
                case SeverityLevels.Info:
                case SeverityLevels.OFF:
                case SeverityLevels.Trace:
                case SeverityLevels.Trace_INT:
                case SeverityLevels.WARN:
                default:
                    break;
            }
        }
        #endregion
    }
}
