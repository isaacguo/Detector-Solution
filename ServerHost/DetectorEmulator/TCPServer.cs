using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DetectorEmulator
{
    public class TCPServer
    {
        private bool isListening;

        public bool IsListening
        {
            get { return isListening; }
            set { isListening = value; }
        }


        public delegate void ExceptionData(string message, string stacktrace);
        public event ExceptionData OnException;
        public delegate void SendingData(string data);
    public event SendingData OnSendingData;


        private string serverAddress;
        public string ServerAddress
        {
            get { return serverAddress; }
            set { serverAddress = value; }
        }
        private string port;
        public string Port
        {
            get { return port; }
            set { port = value; }
        }

        private string returnBytes;
        public string ReturnBytes
        {
            get { return returnBytes; }
            set { returnBytes = value; }
        }



        TcpListener myList = null;
        Socket s = null;

    public void SendData(string ipaddress,int port)
        {
            Socket tcpSynCl;
            string ip = ipaddress;
            tcpSynCl = new Socket(IPAddress.Parse(ip).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSynCl.Connect(new IPEndPoint(IPAddress.Parse(ip), port));

            byte[] responseBytes = new byte[10];
            responseBytes[1] = Convert.ToByte(0);
            responseBytes[5] = Convert.ToByte(4);
            responseBytes[6] = Convert.ToByte(1);
            responseBytes[7] = Convert.ToByte(1);
            responseBytes[8] = Convert.ToByte(1);
            responseBytes[9] = Convert.ToByte(ReturnBytes);
            if (OnSendingData != null)
            {
                OnSendingData(BitConverter.ToString(responseBytes).Replace("-", " "));
            }
            tcpSynCl.Send(responseBytes);

            /* clean up */
            tcpSynCl.Close();
        }

    public void SendEIOData(string ipaddress, int port, string data)
    {

        Socket tcpSynCl;
        string ip = ipaddress;
        tcpSynCl = new Socket(IPAddress.Parse(ip).AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        tcpSynCl.Connect(new IPEndPoint(IPAddress.Parse(ip), port));

        byte[] responseBytes = new byte[15];
        data = data.Replace(" ", "");
     
        responseBytes = StringToByteArray(data);
        responseBytes[11] = Convert.ToByte(ReturnBytes);

        if (OnSendingData != null)
        {
            OnSendingData(BitConverter.ToString(responseBytes).Replace("-", " "));
        }
        tcpSynCl.Send(responseBytes);

        /* clean up */
        tcpSynCl.Close();
    }

    public static byte[] StringToByteArray(string hex) {
    return Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();
}

        public void StartListening()
        {

            try
            {
                while (true)
                {

                    IPAddress ipAd = IPAddress.Parse(serverAddress);

                    /* Initializes the Listener */
                    myList = new TcpListener(ipAd, Int32.Parse(port));

                    /* Start Listeneting at the specified port */
                    myList.Start();
                    this.isListening = true;

                    s = myList.AcceptSocket();

                    byte[] b = new byte[100];
                    int k = s.Receive(b);

                    byte[] receivedBytes = b.Take(k).ToArray();
                    //int bits = Convert.ToInt16(receivedBytes[receivedBytes.Length - 1]);

                    byte[] responseBytes = new byte[10];
                    responseBytes[1] = Convert.ToByte(0);
                    responseBytes[5] = Convert.ToByte(4);
                    responseBytes[6] = Convert.ToByte(1);
                    responseBytes[7] = Convert.ToByte(1);
                    responseBytes[8] = Convert.ToByte(1);
                    responseBytes[9] = Convert.ToByte(ReturnBytes);
                    if (OnSendingData!=null)
                    {
                        OnSendingData(BitConverter.ToString(responseBytes).Replace("-", " "));
                    }
                    s.Send(responseBytes);


                    /* clean up */
                    s.Close();
                    myList.Stop();
                    this.isListening = false;
                }
            }

            catch (SocketException se)
            {
                if (se.ErrorCode != 10004)
                {
                    if (OnException != null)
                    {
                        OnException(se.Message, se.StackTrace);
                    }
                }

            }
            catch (Exception e)
            {

                if (OnException != null)
                {
                    OnException(e.Message, e.StackTrace);
                }
            }
            finally
            {
                StopListening();
            }

        }
        public void StopListening()
        {
            try
            {
                if (s != null)
                {
                    s.Close();
                }
                myList.Server.Close();

            }
            catch (Exception)
            {
            }
            finally
            {
                myList.Stop();
            }

        }
    }
}
