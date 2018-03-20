using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Xml;

namespace DetectorServerHost
{
    public enum DetectorStatus
    {
        Normal,
        Alarmed
    }
    public class DetectorManager
    {
        public string _configurationFile;
        XDocument xdoc;
        List<Model.User> UserAccounts;

        public DetectorManager(string configurationFile)
        {
            _configurationFile = configurationFile;
            using (FileStream fs = new FileStream(configurationFile, FileMode.Open))
            using (XmlReader xmlRdr = new XmlTextReader(fs))
            {
                xdoc = XDocument.Load(xmlRdr);
            }
            this.UserAccounts = LoadUser();
        }
        public void SetDetectorState(string userAccountID,string detectorID, DetectorStatus status)
        {
            var userAccount = UserAccounts.Where(r => r.UserAccount == userAccountID).FirstOrDefault();
            var detector = userAccount.Detectors.Where(d => d.DetectorID == detectorID).FirstOrDefault();
            detector.Status = status.ToString();
            Save();
        }
	public Model.Detector[] GetDetectors(string userAccoundID)
        {
            return UserAccounts.Where(r => r.UserAccount == userAccoundID).FirstOrDefault().Detectors.ToArray();
        }
        public List<DetectorServerHost.Model.User> LoadUser()
        {
            return (xdoc.Element("UserAccounts").Elements("UserAccount").Select(r =>
            {
                return new Model.User { UserAccount = (string)r.Attribute("ID"), Name = (string)r.Attribute("Name"), Detectors = r.Element("Detectors").Elements("Detector").Select(d => { return new Model.Detector { DetectorID = (string)d.Attribute("ID"), Status = (string)d.Attribute("Status"), DetectorDescription = d.Element("Description").Value }; }).ToList() };
            })).ToList();
        }
        public void Save()
        {
            XDocument xdoc = new XDocument(new XElement("UserAccounts",
                UserAccounts.Select(
                        ua =>
                        {
                            return new XElement("UserAccount",
                                new XAttribute("ID", ua.UserAccount),
                                new XAttribute("Name", ua.Name),
                                new XElement("Detectors",
                                ua.Detectors.Select(detector =>
                                {
                                    return new XElement("Detector",
                                        new XAttribute("ID", detector.DetectorID),
                                        new XAttribute("Status", detector.Status),
                                        new XElement("Description", detector.DetectorDescription));
                                }))
                                );
                        })));

            xdoc.Save(_configurationFile);
        }
    }
}
