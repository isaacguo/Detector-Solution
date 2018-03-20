using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DetectorServerHost
{

    public partial class MainForm : Form
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        ModbusServer easyModbusTCPServer = null;
        DetectorManager detectorManager = null;
        RestfulServiceManager serviceManager = null;
        Thread webServiceThread;

        public MainForm()
        {
            InitializeComponent();
            tbTCPListeningPort.Text = ConfigurationManager.AppSettings["TCPListeningPort"];
            tbWebServerPort.Text = ConfigurationManager.AppSettings["WebServerPort"];
            tbIPAddress.Text = GetIPAddress();

            detectorManager = new DetectorManager(ConfigurationManager.AppSettings["ConfigurationFile"]);

        }

        private string GetIPAddress()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
        bool locked;
        void easyModbusTCPServer_logDataChanged()
        {
            if (this.InvokeRequired)
            {
                if (!locked)
                {
                    lock (this)
                    {
                        locked = true;
                        try
                        {
                            InvokeCallback d = new InvokeCallback(easyModbusTCPServer_logDataChanged);
                            this.Invoke(d);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            else
            {
                var arrP = easyModbusTCPServer.ModbusLogData;
                if (arrP.Length > 1)
                {
                    ModbusProtocol mp = arrP[0];
                    byte[] data = mp.receiveInputData;
                    if (data.Length > 0)
                    {
                        var status = Convert.ToString(data[0], 2).PadLeft(4, '0');
                        lbDetectorStatus.Text = status;
                        for (int i = 1; i <= status.Length; i++)
                        {
                            detectorManager.SetDetectorState("1", (status.Length - i + 1).ToString(), (DetectorStatus)Int32.Parse(status[i - 1].ToString()));
                        }
                    }
                }
                locked = false;
            }
        }


        delegate void InvokeCallback();
        bool LockNumberOfConnectionsChanged = false;
        delegate void numberOfConnectionsCallback();
        void easyModbusTCPServer_numberOfConnectedClientsChanged()
        {
            if (this.InvokeRequired & !LockNumberOfConnectionsChanged)
            {
                {
                    lock (this)
                    {
                        LockNumberOfConnectionsChanged = true;
                        InvokeCallback d = new InvokeCallback(easyModbusTCPServer_numberOfConnectedClientsChanged);
                        try
                        {
                            this.Invoke(d);
                        }
                        catch (Exception) { }
                        finally
                        {
                            LockNumberOfConnectionsChanged = false;
                        }
                    }
                }
            }
            else
            {
                try
                {
                    //lbNumberOfDetectors.Text = easyModbusTCPServer.NumberOfConnections.ToString();
                }
                catch (Exception)
                { }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (easyModbusTCPServer != null)
            {
                easyModbusTCPServer.StopListening();
            }
            if (serviceManager != null)
            {
                serviceManager.StopService();
            }

            Environment.Exit(0);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            log.Info("Application started");
            btnStart.Text = Properties.Resources.ButtonStart;

        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (Properties.Resources.ButtonStart == btnStart.Text)
            {
                try
                {
                    easyModbusTCPServer = new ModbusServer();
                    easyModbusTCPServer.Port = Int32.Parse(tbTCPListeningPort.Text);
                    easyModbusTCPServer.Listen();
                    easyModbusTCPServer.OnEIOExtensionFrameReceived += easyModbusTCPServer_OnEIOExtensionFrameReceived;

                    serviceManager = new RestfulServiceManager();
                    serviceManager.DetectorManager = detectorManager;

                    webServiceThread = new Thread(() => {
                        serviceManager.StartService();                    
                    });
                    webServiceThread.Start();


                    lbWebServerStatus.Text = "运行中";
                    log.Info("Modbus Server started");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
                    easyModbusTCPServer.StopListening();
                    serviceManager.StopService();
                    log.Error(ex);
                }

                btnStart.Text = Properties.Resources.ButtonStop;
            }
            else
            {
                btnStart.Text = Properties.Resources.ButtonStart;
                easyModbusTCPServer.StopListening();
                webServiceThread.Abort();
                serviceManager.StopService();
                lbWebServerStatus.Text = "已停止";
            }

        }
        delegate void InvokeCallback2(object o, Model.EIOExtensionProtocol frame);
        void easyModbusTCPServer_OnEIOExtensionFrameReceived(object sender, Model.EIOExtensionProtocol frame)
        {
            if (this.InvokeRequired)
            {
                if (!locked)
                {
                    lock (this)
                    {
                        locked = true;
                        try
                        {
                            InvokeCallback2 d = new InvokeCallback2(easyModbusTCPServer_OnEIOExtensionFrameReceived);
                            this.Invoke(d, new object[] { this, frame });
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            else
            {
                log.Info("OnEIOExtensionFrameReceive: Processing EIO data");
                byte data = frame.EIOFrame.EIOData[0];

                var status = Convert.ToString(data, 2).PadLeft(4, '0');
                lbDetectorStatus.Text = status;
                for (int i = 1; i <= status.Length; i++)
                {
                    detectorManager.SetDetectorState("1", (status.Length - i + 1).ToString(), (DetectorStatus)Int32.Parse(status[i - 1].ToString()));
                }
                log.Info("OnEIOExtensionFrameReceive: Processing EIO data finished");
                locked = false;
            }
        }
    }
}
