using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using DetectorServerHost.Model;

namespace DetectorServerHost
{
    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single, IncludeExceptionDetailInFaults=true)]
    public class MyDetectorService : IDetectorService
    {
        private DetectorManager detectorManager;

        public DetectorManager DetectorManager
        {
            get { return detectorManager; }
            set { detectorManager = value; }
        }


        #region IDetectorService Members

        public Detector RefreshDetectorStatus(string DateTime,string UserAccountID, string DetectorID, string Status)
        {
            detectorManager.SetDetectorState(UserAccountID, DetectorID, (DetectorStatus)Enum.Parse(typeof(DetectorStatus), Status,true));
            return detectorManager.GetDetectors(UserAccountID).Where(d => d.DetectorID == DetectorID).First();
        }

        public Detector[] GetDetectors(string UserAccountID)
        {
            return detectorManager.GetDetectors(UserAccountID);
        }

        #endregion
    }
}
