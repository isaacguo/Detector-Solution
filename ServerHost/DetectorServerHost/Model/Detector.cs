using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DetectorServerHost.Model
{
    [DataContract]
    public class Detector
    {

        private string detectorID;
        [DataMember]
        public string DetectorID
        {
            get { return detectorID; }
            set { detectorID = value; }
        }

        private string status;
        [DataMember]
        public string Status
        {
            get { return status; }
            set { status = value; }
        }
        private string userAccountID;
        [DataMember]
        public string UserAccountID
        {
            get { return userAccountID; }
            set { userAccountID = value; }
        }
        private string detectorDescription;
        [DataMember]
        public string DetectorDescription
        {
            get { return detectorDescription; }
            set { detectorDescription = value; }
        }



    }
}
