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
        private bool _isConnectionActive;
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
            catch(Exception ex)
            {
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }

        private void CheckAliveClients(object state)
        {
            _writeTime.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            foreach (KeyValuePair<string, DateTime> obj in ClientList)
            {
                if (obj.Value.AddMilliseconds(_dataHandlerConfig.TimeToTrigerKeepAliveCheck)< DateTime.Now)
                {
                    _logger.PublishMessage(new Logger.SLI_Message(string.Format("Client:{0} was disconnected", obj.Key), Logger.SeverityLevels.Error, DateTime.Now));
                }
            }
            _writeTime.Change(_dataHandlerConfig.TimeToTrigerKeepAliveCheck, _dataHandlerConfig.TimeToTrigerKeepAliveCheck);
        }

        public void StopListening()
        {
            _isConnectionActive = false;
            Listener.Stop();
        }
        public async void StartListen(Logger.Logger logger)
        {
            try
            {
                _logger = logger;
                Init();
                _isConnectionActive = true;
                if (DataCollectionHandler.Listener != null)
                    return;
                Listener = new TcpListener(IPAddress.Any, _dataHandlerConfig.DataHandlerListeningPort);
                Listener.Start();
                await AcceptConnections();
            }
            catch (Exception ex)
            {
                _logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
        }
        private async Task AcceptConnections()
        {
            try
            {
                while (_isConnectionActive)
                {
                    TcpClient client = await Listener.AcceptTcpClientAsync();
                    ClientList.Add(client.Client.LocalEndPoint.ToString(), DateTime.UtcNow);
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
        #endregion
    }
}
