using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetectorServerHost.Model
{
    public class User
    {
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string userAccount;
        public string UserAccount
        {
            get { return userAccount; }
            set { userAccount = value; }
        }
        private List<DetectorServerHost.Model.Detector> detectors;
        public List<DetectorServerHost.Model.Detector> Detectors
        {
            get { return detectors; }
            set { detectors = value; }
        }
    }
}
