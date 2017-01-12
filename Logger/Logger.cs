using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using Logger.Enums;
using System.Collections.Concurrent;
using Logger.LoggerMapping;
using System.Threading;
using System.Text;

namespace Logger
{
    public class Logger
    {
        #region Implementation Configuration Holder
        private LoggerConfiguration _logConfig = null;
        private Dictionary<Severity, BaseLoggerConfig> levelConfiguration = new Dictionary<Severity, BaseLoggerConfig>();
        #endregion

        #region Dictionary to be lock using tasks

        private ConcurrentDictionary<DateTime, DataToStore> debugDictionary = null;
        private ConcurrentDictionary<DateTime, DataToStore> errorDictionary = null;
        #endregion

        #region Properties
        private IConfigurationRoot Configuration { get; set; }
        #endregion

        #region Param
        private static Logger _logger = null;
        private static Timer _writeTime;
        private static volatile int dldnow;
        #endregion

        #region Ctor
        private Logger()
        {
            GetLoggerConfiguration();
            _writeTime = new Timer(new TimerCallback(WriteToAllFile), dldnow, _logConfig.TimeUntilFileWrite, _logConfig.TimeUntilFileWrite);

        }
        #endregion

        private void WriteToAllFile(object state)
        {
            _writeTime.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            foreach (KeyValuePair<Severity, BaseLoggerConfig> obj in levelConfiguration)
            {
                WriteToFile(obj.Key);
            }
            _writeTime.Change(_logConfig.TimeUntilFileWrite, _logConfig.TimeUntilFileWrite);
        }
        public static Logger GetInstance()
        {
            if (_logger == null)
                _logger = new Logger();
            return _logger;
        }
        private void GetLoggerConfiguration()
        {
            Configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("AppSettings.json").Build();
            //Get Logger Configuration
            _logConfig = new LoggerConfiguration();
            Configuration.GetSection("LogSettings:LogGlobalConfiguration").Bind(_logConfig);
            //Get level preferences
            foreach (Severity obj in Enum.GetValues(typeof(Severity)))
            {
                BaseLoggerConfig tempObj = new BaseLoggerConfig();
                Configuration.GetSection("LogSettings:" + "Log" + obj.ToString()).Bind(tempObj);
                if (tempObj.Implemented)
                {
                    switch (obj)
                    {
                        case Severity.All:
                            break;
                        case Severity.Debug:
                            {
                                levelConfiguration.Add(Severity.Debug, new DebugConfig());
                                Configuration.GetSection("LogSettings:LogDebug").Bind(levelConfiguration[Severity.Debug]);
                                debugDictionary = new ConcurrentDictionary<DateTime, DataToStore>();
                                break;
                            }
                        case Severity.Error:
                            {
                                levelConfiguration.Add(Severity.Error, new ErrorConfig());
                                Configuration.GetSection("LogSettings:LogError").Bind(levelConfiguration[Severity.Error]);
                                errorDictionary = new ConcurrentDictionary<DateTime, DataToStore>();
                                break;
                            }
                        case Severity.Fatal:
                            break;
                        case Severity.Info:
                            break;
                        case Severity.OFF:
                            break;
                        case Severity.Trace:
                            break;
                        case Severity.Trace_INT:
                            break;
                        case Severity.WARN:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void WriteToLog(string _key, Severity _severity, string _message, string _extraString = "", string _additonalMessage = "")
        {
            int dictionarySize = AddToDictionary(DateTime.Now, new DataToStore() { SourceTime = DateTime.Now.ToString(), LoggerTime = DateTime.Now.ToString(), Key = _key, LogSeverity = _severity, Message = _message, AdditonalMessage = _additonalMessage, ExtraString = _extraString, MachineName = Environment.MachineName });
            //check if dictionary size is larger than configuration max entries
            if (dictionarySize >= _logConfig.MaxEntriesInDictionary)
                //check if require to flush all dictionaries
                if (_logConfig.FlushAllDictionaryAtOnce)
                    WriteToAllFile(null);
                else
                    WriteToFile(_severity);
        }
        private int AddToDictionary(DateTime now, DataToStore temp)
        {
            int dictionarySize = 0;
            switch (temp.LogSeverity)
            {
                case Severity.All:
                    break;
                case Severity.Debug:
                    {
                        debugDictionary.TryAdd(now, temp);
                        dictionarySize = debugDictionary.Count;
                        break;
                    }
                case Severity.Error:
                    {
                        errorDictionary.TryAdd(now, temp);
                        dictionarySize = errorDictionary.Count;
                        break;
                    }
                case Severity.Fatal:
                    break;
                case Severity.Info:
                    break;
                case Severity.OFF:
                    break;
                case Severity.Trace:
                    break;
                case Severity.Trace_INT:
                    break;
                case Severity.WARN:
                    break;
                default:
                    break;
            }
            return dictionarySize;
        }
        private void WriteToFile(Severity _severity)
        {
            StringBuilder messageToWrite = new StringBuilder();
            MemoryStream stream = new MemoryStream();
            switch (_severity)
            {
                case Severity.All:
                    break;
                case Severity.Debug:
                    {
                        if (debugDictionary.Count > 0)
                        {
                            long fileSize = 0;
                            string extension = Path.GetExtension(levelConfiguration[_severity].FilePath);
                            string filepath = string.Empty;
                            PrepareMessages(ref messageToWrite, _severity);
                            for (int i = -1; i < ((DebugConfig)levelConfiguration[_severity]).AmountOfFiles; i++)
                            {
                                filepath = levelConfiguration[_severity].FilePath;// + "_" + (i+1);
                                filepath = filepath.Replace(extension, "_" + (i + 1) + extension);
                                if (File.Exists(filepath))
                                {
                                    fileSize = new System.IO.FileInfo(filepath).Length;
                                    if (fileSize < ((DebugConfig)levelConfiguration[_severity]).SizeThresholdInBytes)
                                        break;
                                    else
                                        continue;

                                }
                                else
                                    break;
                            }
                            if((!filepath.Contains("100")) || (filepath.Contains("100") && fileSize < ((DebugConfig)levelConfiguration[_severity]).SizeThresholdInBytes*1.1))
                                using (StreamWriter file = File.AppendText(filepath))
                                {
                                    file.WriteLine(messageToWrite);
                                }
                        }
                        break;
                    }
                case Severity.Error:
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
                case Severity.Fatal:
                    break;
                case Severity.Info:
                    break;
                case Severity.OFF:
                    break;
                case Severity.Trace:
                    break;
                case Severity.Trace_INT:
                    break;
                case Severity.WARN:
                    break;
                default:
                    break;
            }
        }

        private void PrepareMessages(ref StringBuilder messageToWrite, Severity _severity)
        {
            switch (_severity)
            {
                case Severity.All:
                    break;
                case Severity.Debug:
                    {
                        messageToWrite.Append("----------------------------------");
                        foreach (KeyValuePair<DateTime, DataToStore> item in debugDictionary)
                        {
                            messageToWrite.Append(Environment.NewLine)
                            .Append("SourceTime: " + item.Key)
                            .Append(Environment.NewLine)
                            .Append("LoggerTime: " + item.Key)
                            .Append(Environment.NewLine)
                            .Append("Key: " + item.Value.Key)
                            .Append(Environment.NewLine)
                            .Append("Severity: " + item.Value.LogSeverity)
                            .Append(Environment.NewLine)
                            .Append("Message: " + item.Value.Message)
                            .Append(Environment.NewLine)
                            .Append("AdditonalMessage: " + item.Value.AdditonalMessage)
                            .Append(Environment.NewLine)
                            .Append("ExtraString: " + item.Value.ExtraString)
                            .Append(Environment.NewLine)
                            .Append("MachineName: " + item.Value.MachineName)
                            .Append(Environment.NewLine)
                            .Append("----------------------------------");
                        }
                        break;
                    }
                case Severity.Error:
                    {
                        messageToWrite.Append("----------------------------------");
                        foreach (KeyValuePair<DateTime, DataToStore> item in errorDictionary)
                        {
                            messageToWrite.Append(Environment.NewLine)
                            .Append("SourceTime: " + item.Key)
                            .Append(Environment.NewLine)
                            .Append("LoggerTime: " + item.Key)
                            .Append(Environment.NewLine)
                            .Append("Key: " + item.Value.Key)
                            .Append(Environment.NewLine)
                            .Append("Severity: " + item.Value.LogSeverity)
                            .Append(Environment.NewLine)
                            .Append("Message: " + item.Value.Message)
                            .Append(Environment.NewLine)
                            .Append("MachineName: " + item.Value.MachineName)
                            .Append(Environment.NewLine)
                            .Append("----------------------------------");
                        }
                        break;
                    }
                case Severity.Fatal:
                    break;
                case Severity.Info:
                    break;
                case Severity.OFF:
                    break;
                case Severity.Trace:
                    break;
                case Severity.Trace_INT:
                    break;
                case Severity.WARN:
                    break;
                default:
                    break;
            }


        }
    }
}
