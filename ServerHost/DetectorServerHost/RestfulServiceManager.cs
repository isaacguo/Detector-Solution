using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DetectorServerHost
{
    public class RestfulServiceManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private DetectorManager detectorManager;
        public DetectorManager DetectorManager
        {
            get { return detectorManager; }
            set { detectorManager = value; }
        }

        ServiceHost _serviceHost = null;
        public void StartService()
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
            }
            MyDetectorService mds = new MyDetectorService();
            mds.DetectorManager = this.detectorManager;
            _serviceHost = new ServiceHost(mds);
            try
            {
                _serviceHost.Open();
            }
            catch (Exception e)
            {
                log.Error(e);
                throw;
            }
        }
        public void StopService()
        {
            try
            {
                if (_serviceHost != null)
                {
                    _serviceHost.Close();
                    _serviceHost = null;
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }
    }
}
