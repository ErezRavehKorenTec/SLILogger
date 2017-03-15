using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using SLI.Common;
using System.Text;

namespace SLI.Data.DataCollectionHandler
{
    public class DataCollectionHandler
    {
        #region [Param]
        private bool _isDataConnectionActive;
        private bool _isKeepAliveConnectionActive;
        private Action<IData> _dataIsReady;
        private DataHandlerConfiguration _dataHandlerConfig = null;
        private DataDecoder _decoder;
        public static Logger.Logger _logger;
        private static Timer _writeTime;
        private static volatile int dldnow;
        #endregion

        #region [Properties]
        private IConfigurationRoot Configuration { get; set; }
        public static Dictionary<string, DateTime> ClientList { get; set; }
        private static TcpListener Listener { get; set; }
        private static TcpListener KeepAliveListiner { get; set; }
        #endregion

        #region [Ctor]
        public DataCollectionHandler()
        {
            _decoder = new DataDecoder();
        }
        #endregion

        #region [Public Methods]
        public void Init()
        {
            try
            {
                ClientList = new Dictionary<string, DateTime>();
                Configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("AppSettings.json").Build();
                _dataHandlerConfig = new DataHandlerConfiguration();
                Configuration.GetSection("DataCollectionHandler").Bind(_dataHandlerConfig);
                _writeTime = new Timer(new TimerCallback(CheckAliveClients), dldnow, _dataHandlerConfig.TimeToTrigerKeepAliveCheck, _dataHandlerConfig.TimeToTrigerKeepAliveCheck);
            }
            catch (Exception ex)
            {
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }

        private void CheckAliveClients(object state)
        {
            DateTime tempval;
            _writeTime.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            foreach (KeyValuePair<string, DateTime> obj in ClientList)
            {
                tempval = obj.Value;
                if (tempval.AddMilliseconds(_dataHandlerConfig.TimeToTrigerKeepAliveCheck) < DateTime.UtcNow)
                {
                    _logger.PublishMessage(new Logger.SLI_Message($"Client:{obj.Key} was disconnected", Logger.SeverityLevels.Error, DateTime.Now));
                }
            }
            _writeTime.Change(_dataHandlerConfig.TimeToTrigerKeepAliveCheck, _dataHandlerConfig.TimeToTrigerKeepAliveCheck);
        }

        public void StopListening()
        {
            _isKeepAliveConnectionActive = false;
            _isDataConnectionActive = false;
            Listener.Stop();
        }
        public async void StartListen(Logger.Logger logger)
        {
            try
            {
                _logger = logger;
                Init();
                if (DataCollectionHandler.Listener == null)
                {
                    _isDataConnectionActive = true;
                    Listener = new TcpListener(IPAddress.Any, _dataHandlerConfig.DataHandlerListeningPort);
                    Listener.Start();
                    await AcceptDataConnections();
                }
                if (DataCollectionHandler.KeepAliveListiner == null)
                {
                    _isKeepAliveConnectionActive = true;
                    KeepAliveListiner = new TcpListener(IPAddress.Any, _dataHandlerConfig.KeepAliveListeningPort);
                    KeepAliveListiner.Start();
                    await AcceptKeepAliveConnectionConnections();
                }
            }
            catch (Exception ex)
            {
                _isKeepAliveConnectionActive = false;
                _isDataConnectionActive = false;
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }

        private async Task AcceptKeepAliveConnectionConnections()
        {
            try
            {
                TcpClient client = await KeepAliveListiner.AcceptTcpClientAsync();
                ClientList.Add(client.Client.LocalEndPoint.ToString(), DateTime.UtcNow);
                while (_isKeepAliveConnectionActive)
                {
                    await KeepAliveClientProccessThread(client);
                }
            }
            catch (Exception ex)
            {
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }

        private async Task AcceptDataConnections()
        {
            try
            {
                TcpClient client = await Listener.AcceptTcpClientAsync();
                while (_isDataConnectionActive)
                {
                    await DataClientProccessThread(client);
                }
            }
            catch (Exception ex)
            {
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }
        public void SubscribeForData(Action<IData> callback)
        {
            _dataIsReady += callback;
        }
        #endregion

        #region [Private Methods]
        private async Task DataClientProccessThread(TcpClient client)
        {
            try
            {
                NetworkStream networkStream = client.GetStream();
                byte[] data = new byte[10000];
                int size = await networkStream.ReadAsync(data, 0, data.Length);
                _decoder.CurrentClient = client;
                List<IData> _decodeData = _decoder.Decode(data);
                if (_decodeData.Count > 0)
                {
                    foreach (var decData in _decodeData)
                    {
                        _dataIsReady?.Invoke(decData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }
        private async Task KeepAliveClientProccessThread(TcpClient client)
        {
            try
            {
                NetworkStream networkStream = client.GetStream();
                byte[] data = new byte[10000];
                int size = await networkStream.ReadAsync(data, 0, data.Length);
                _decoder.CurrentClient = client;
                _decoder.Decode(data);
            }
            catch (Exception ex)
            {
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }
        #endregion
    }
}
