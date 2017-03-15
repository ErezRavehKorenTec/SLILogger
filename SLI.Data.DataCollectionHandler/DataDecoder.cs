using SLI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Sockets;

namespace SLI.Data.DataCollectionHandler
{

    public class DataDecoder
    {
        public TcpClient CurrentClient { get; set; }
        private List<IData> DecodedObjects = new List<IData>();
        public DataDecoder()
        {
        }
        public List<IData> Decode(byte[] data)
        {
            try
            {
            string datareceived = Encoding.ASCII.GetString(data);
            OgeroutMsg _allReceiveData = JsonConvert.DeserializeObject<OgeroutMsg>(datareceived);
                switch (_allReceiveData.ogerout_msg_type)
                {
                    case "error":
                        DataCollectionHandler._logger.PublishMessage(new Logger.SLI_Message($"Error Code:{_allReceiveData.payload.error_reason}, Error Reason:{_allReceiveData.payload.error_reason},Error Time:{ ParseTimestampToDateTime(_allReceiveData.payload.timestamp)}", Logger.SeverityLevels.Debug, DateTime.Now));
                        break;
                    case "sensor":
                        DecodedObjects.Add(new DefaultData(_allReceiveData.payload.name, _allReceiveData.payload.sample, (DataType)_allReceiveData.payload.type, ParseTimestampToDateTime(_allReceiveData.payload.timestamp)));
                        break;
                    case "keepalive":
                    DataCollectionHandler.ClientList[CurrentClient.Client.LocalEndPoint.ToString()] = ParseTimestampToDateTime(_allReceiveData.payload.timestamp);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                DataCollectionHandler._logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
            }
            return DecodedObjects;

        }

        private DateTime ParseTimestampToDateTime(string timestamp)
        {
            try
            {

            DateTime value =  new DateTime(
             int.Parse(timestamp.Substring(4, 4)),
             int.Parse(timestamp.Substring(2, 2)),
             int.Parse(timestamp.Substring(0, 2)),
             int.Parse(timestamp.Substring(8, 2)),
             int.Parse(timestamp.Substring(10, 2)),
             int.Parse(timestamp.Substring(12, 2)),
             int.Parse(timestamp.Substring(14, 3)));
            return value;
            }
            catch (Exception ex )
            {
                DataCollectionHandler._logger.PublishMessage(new Logger.SLI_Message(ex.Message, Logger.SeverityLevels.Error, DateTime.Now));
                return DateTime.Now;
            }
        }
    }
}
