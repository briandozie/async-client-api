using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace RemoteServer
{
    [ServiceContract]
    public interface RemoteServerInterface
    {
        [OperationContract]
        [FaultContract(typeof(ServerFault))]
        Job Download();

        [OperationContract]
        [FaultContract(typeof(ServerFault))]
        void Remove(Job job);

        [OperationContract]
        [FaultContract(typeof(ServerFault))]
        bool Upload(Job job);

        [OperationContract]
        [FaultContract(typeof(ServerFault))]
        bool JobAvailable();
    }

    [DataContract]
    public class ServerFault
    {
        private string reason;

        [DataMember]
        public string Reason
        {
            get { return reason; }
            set { reason = value; }
        }
    }
}
