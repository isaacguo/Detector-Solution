using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using DetectorServerHost.Model;

namespace DetectorServerHost
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IDetectorService
    {
        [OperationContract]
        [WebInvoke(
        BodyStyle = WebMessageBodyStyle.Bare,
        Method = "POST",
        RequestFormat = WebMessageFormat.Xml,
        ResponseFormat = WebMessageFormat.Xml,
        UriTemplate = "RefreshDetectorStatus/{DateTime}/{UserAccountID}/{DetectorID}/{Status}")]
        Detector RefreshDetectorStatus(string DateTime,string UserAccountID, string DetectorID, string Status);

        [OperationContract]
        [WebGet(
        BodyStyle = WebMessageBodyStyle.Bare,
        RequestFormat = WebMessageFormat.Xml,
        ResponseFormat = WebMessageFormat.Xml,
        UriTemplate="GetDetectorState/{UserAccountID}")]
        Detector[] GetDetectors(string UserAccountID);
    }

    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    // You can add XSD files into the project. After building the project, you can directly use the data types defined there, with the namespace "DetectorService.ContractType".
   
}
