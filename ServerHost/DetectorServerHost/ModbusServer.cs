﻿/*
 * Erstellt mit SharpDevelop.
 * www.rossmann-engineering.de
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using DetectorServerHost.Model;

namespace DetectorServerHost
{
    /// <summary>
    /// Modbus Protocol informations.
    /// </summary>
    public class ModbusProtocol
    {

        public DateTime timeStamp;
        public bool request;
        public bool response;
        public Int16 transactionIdentifier;
        public Int16 protocolIdentifier;
        public Int16 length;
        public byte unitIdentifier;
        public byte functionCode;
        public Int16 startingAdress;
        public Int16 startingAddressRead;
        public Int16 startingAddressWrite;
        public Int16 quantity;
        public Int16 quantityRead;
        public Int16 quantityWrite;
        public byte byteCount;
        public byte exceptionCode;
        public byte errorCode;
        public UInt16[] receiveCoilValues;
        public UInt16[] receiveRegisterValues;
        public Int16[] sendRegisterValues;
        public bool[] sendCoilValues;
        public byte[] receiveInputData;
    }



    struct NetworkConnectionParameter
    {
        public NetworkStream stream;        //For TCP-Connection only
        public Byte[] bytes;
        public int portIn;                  //For UDP-Connection only
        public IPAddress ipAddressIn;       //For UDP-Connection only
    }


    internal class TCPHandler
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public delegate void DataChanged(object networkConnectionParameter);
        public event DataChanged dataChanged;

        public delegate void NumberOfClientsChanged();
        public event NumberOfClientsChanged numberOfClientsChanged;

        TcpListener server = null;
        NetworkConnectionParameter networkConnectionParameter;

        public List<Client> tcpClientLastRequestList = new List<Client>();

        public int NumberOfConnectedClients { get; set; }

        public TCPHandler(int port)
        {

            try
            {
                IPAddress localAddr = IPAddress.Any;
                server = new TcpListener(localAddr, port);
                server.Start();
                server.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
            }
            catch (SocketException se)
            {
                log.Error(se);
                log.Error(se.Message + Environment.NewLine + se.StackTrace);
            }
           
        }

        private void AcceptTcpClientCallback(IAsyncResult asyncResult)
        {
            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient = server.EndAcceptTcpClient(asyncResult);
                tcpClient.ReceiveTimeout = 4000;
            }
            catch (Exception) { }
            try
            {
                server.BeginAcceptTcpClient(AcceptTcpClientCallback, null);
                Client client = new Client(tcpClient);
                NetworkStream networkStream = client.NetworkStream;
                networkStream.ReadTimeout = 4000;
                networkStream.BeginRead(client.Buffer, 0, client.Buffer.Length, ReadCallback, client);
            }
            catch (Exception) { }
        }

        private int GetAndCleanNumberOfConnectedClients(Client client)
        {
            int i = 0;
            bool objetExists = false;
            foreach (Client clientLoop in tcpClientLastRequestList)
            {
                if (client.Equals(clientLoop))
                    objetExists = true;
            }
            try
            {
                tcpClientLastRequestList.RemoveAll(delegate(Client c)
                    {
                        return ((DateTime.Now.Ticks - c.Ticks) > 40000000);
                    }

                    );
            }
            catch (Exception) { }
            if (!objetExists)
                tcpClientLastRequestList.Add(client);


            return tcpClientLastRequestList.Count;
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            Client client = asyncResult.AsyncState as Client;
            client.Ticks = DateTime.Now.Ticks;
            NumberOfConnectedClients = GetAndCleanNumberOfConnectedClients(client);
            if (numberOfClientsChanged != null)
                numberOfClientsChanged();
            if (client != null)
            {
                int read;
                NetworkStream networkStream = null;
                try
                {
                    networkStream = client.NetworkStream;

                    read = networkStream.EndRead(asyncResult);
                }
                catch (Exception ex)
                {
                    return;
                }


                if (read == 0)
                {
                    //OnClientDisconnected(client.TcpClient);
                    //connectedClients.Remove(client);
                    return;
                }
                byte[] data = new byte[read];
                Buffer.BlockCopy(client.Buffer, 0, data, 0, read);
                networkConnectionParameter.bytes = data;
                networkConnectionParameter.stream = networkStream;
                IPEndPoint ipEndPoint = ((IPEndPoint)client.TcpClient.Client.RemoteEndPoint);
                networkConnectionParameter.ipAddressIn = ipEndPoint.Address;
                networkConnectionParameter.portIn = ipEndPoint.Port;

                if (dataChanged != null)
                    dataChanged(networkConnectionParameter);
                try
                {
                    networkStream.BeginRead(client.Buffer, 0, client.Buffer.Length, ReadCallback, client);
                }
                catch (Exception)
                {
                }
            }
        }

        public void Disconnect()
        {
            try
            {
                foreach (Client clientLoop in tcpClientLastRequestList)
                {
                    clientLoop.NetworkStream.Close(00);
                }
            }
            catch (Exception) { }
            server.Stop();

        }


        internal class Client
        {
            private readonly TcpClient tcpClient;
            private readonly byte[] buffer;
            public long Ticks { get; set; }

            public Client(TcpClient tcpClient)
            {
                this.tcpClient = tcpClient;
                int bufferSize = tcpClient.ReceiveBufferSize;
                buffer = new byte[bufferSize];
            }

            public TcpClient TcpClient
            {
                get { return tcpClient; }
            }

            public byte[] Buffer
            {
                get { return buffer; }
            }

            public NetworkStream NetworkStream
            {
                get
                {

                    return tcpClient.GetStream();

                }
            }
        }
    }

    /// <summary>
    /// Modbus TCP Server.
    /// </summary>
    public class ModbusServer
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        Int32 port = 502;
        ModbusProtocol receiveData;
        ModbusProtocol sendData = new ModbusProtocol();
        Byte[] bytes = new Byte[2100];
        public Int16[] holdingRegisters = new Int16[65535];
        public Int16[] inputRegisters = new Int16[65535];
        public bool[] coils = new bool[65535];
        public bool[] discreteInputs = new bool[65535];
        private int numberOfConnections = 0;
        private bool udpFlag;
        private int portIn;
        private IPAddress ipAddressIn;
        private UdpClient udpClient;
        private IPEndPoint iPEndPoint;
        private NetworkStream stream;
        private TCPHandler tcpHandler;
        Thread listenerThread;
        Thread clientConnectionThread;
        private ModbusProtocol[] modbusLogData = new ModbusProtocol[100];
        public bool FunctionCode1Disabled { get; set; }
        public bool FunctionCode2Disabled { get; set; }
        public bool FunctionCode3Disabled { get; set; }
        public bool FunctionCode4Disabled { get; set; }
        public bool FunctionCode5Disabled { get; set; }
        public bool FunctionCode6Disabled { get; set; }
        public bool FunctionCode15Disabled { get; set; }
        public bool FunctionCode16Disabled { get; set; }
        public bool FunctionCode23Disabled { get; set; }
        public bool PortChanged { get; set; }


        public delegate void CoilsChanged();
        public event CoilsChanged coilsChanged;

        public delegate void HoldingRegistersChanged();
        public event HoldingRegistersChanged holdingRegistersChanged;

        public delegate void NumberOfConnectedClientsChanged();
        public event NumberOfConnectedClientsChanged numberOfConnectedClientsChanged;

        public delegate void LogDataChanged();
        public event LogDataChanged logDataChanged;

        public delegate void EIOExtensionFrameReceived( object sender,EIOExtensionProtocol frame);
        public event EIOExtensionFrameReceived OnEIOExtensionFrameReceived;

        public void Listen()
        {
            listenerThread = new Thread(ListenerThread);
            listenerThread.Start();
        }

        public void StopListening()
        {
            try
            {
                tcpHandler.Disconnect();
                listenerThread.Abort();
            }
            catch (Exception) { }
            listenerThread.Join();
            try
            {
                clientConnectionThread.Abort();
            }
            catch (Exception) { }
        }

        private void ListenerThread()
        {
            if (!udpFlag)
            {
                if (udpClient != null)
                {
                    try
                    {
                        udpClient.Close();
                    }
                    catch (Exception) { }
                }
                tcpHandler = new TCPHandler(port);
                tcpHandler.dataChanged += new TCPHandler.DataChanged(ProcessReceivedData);
                tcpHandler.numberOfClientsChanged += new TCPHandler.NumberOfClientsChanged(numberOfClientsChanged);
            }
            else
                while (true)
                {
                    if (udpClient == null | PortChanged)
                    {
                        udpClient = new UdpClient(port);
                        udpClient.Client.ReceiveTimeout = 1000;
                        iPEndPoint = new IPEndPoint(IPAddress.Any, port);
                        PortChanged = false;
                    }
                    if (tcpHandler != null)
                        tcpHandler.Disconnect();
                    try
                    {
                        bytes = udpClient.Receive(ref iPEndPoint);
                        portIn = iPEndPoint.Port;
                        NetworkConnectionParameter networkConnectionParameter = new NetworkConnectionParameter();
                        networkConnectionParameter.bytes = bytes;
                        ipAddressIn = iPEndPoint.Address;
                        networkConnectionParameter.portIn = portIn;
                        networkConnectionParameter.ipAddressIn = ipAddressIn;
                        ParameterizedThreadStart pts = new ParameterizedThreadStart(this.ProcessReceivedData);
                        Thread processDataThread = new Thread(pts);
                        processDataThread.Start(networkConnectionParameter);

                    }
                    catch (Exception)
                    {

                    }
                }
        }

        private void numberOfClientsChanged()
        {
            numberOfConnections = tcpHandler.NumberOfConnectedClients;
            if (numberOfConnectedClientsChanged != null)
                numberOfConnectedClientsChanged();
        }

        private void ProcessReceivedData(object networkConnectionParameter)
        {

            try
            {
                NetworkConnectionParameter ncp = (NetworkConnectionParameter)networkConnectionParameter;

                Byte[] bytes = ((NetworkConnectionParameter)networkConnectionParameter).bytes;

                log.Info("On ProcessReceivedData:" + BitConverter.ToString(bytes).Replace("-", " "));

                NetworkStream stream = ((NetworkConnectionParameter)networkConnectionParameter).stream;
                int portIn = ((NetworkConnectionParameter)networkConnectionParameter).portIn;
                IPAddress ipAddressIn = ((NetworkConnectionParameter)networkConnectionParameter).ipAddressIn;

                log.Info("Remote IP:" + ipAddressIn.ToString());
                log.Info("Remote Port:" + portIn.ToString());

                var receiveDataThread = new ModbusProtocol();
                var sendDataThread = new ModbusProtocol();


                #region Modbus Parse Code
                //Int16[] wordData = new Int16[1];
                //byte[] byteData = new byte[2];
                //receiveDataThread.timeStamp = DateTime.Now;
                //receiveDataThread.request = true;

                ////Lese Transaction identifier
                //byteData[1] = bytes[0];
                //byteData[0] = bytes[1];
                //Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //receiveDataThread.transactionIdentifier = wordData[0];

                ////Lese Protocol identifier
                //byteData[1] = bytes[2];
                //byteData[0] = bytes[3];
                //Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //receiveDataThread.protocolIdentifier = wordData[0];

                ////Lese length
                //byteData[1] = bytes[4];
                //byteData[0] = bytes[5];
                //Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //receiveDataThread.length = wordData[0];

                ////Lese unit identifier
                //receiveDataThread.unitIdentifier = bytes[6];

                //// Lese function code
                //receiveDataThread.functionCode = bytes[7];

                //receiveDataThread.receiveInputData = new byte[bytes[8]];
                //if(bytes.Length-9!=bytes[8])
                //{
                //    //send requiring bytes
                //    log.Error("sending requiring data");
                //    this.SendData(stream);

                //    return;
                //}
                //Array.Copy(bytes, 9, receiveDataThread.receiveInputData, 0, bytes[8]);


                //// Lese starting address 
                //byteData[1] = bytes[8];
                //byteData[0] = bytes[9];
                //Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //receiveDataThread.startingAdress = wordData[0];

                //if (receiveDataThread.functionCode ==1 )
                //{
                //    // Lese quantity
                //    byteData[1] = bytes[10];
                //    byteData[0] = bytes[11];
                //    Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //    receiveDataThread.quantity = wordData[0];
                //}
                //if (receiveDataThread.functionCode == 5)
                //{
                //    receiveDataThread.receiveCoilValues = new ushort[1];
                //    // Lese Value
                //    byteData[1] = bytes[10];
                //    byteData[0] = bytes[11];
                //    Buffer.BlockCopy(byteData, 0, receiveDataThread.receiveCoilValues, 0, 2);
                //}
                //if (receiveDataThread.functionCode == 6)
                //{
                //    receiveDataThread.receiveRegisterValues = new ushort[1];
                //    // Lese Value
                //    byteData[1] = bytes[10];
                //    byteData[0] = bytes[11];
                //    Buffer.BlockCopy(byteData, 0, receiveDataThread.receiveRegisterValues, 0, 2);
                //}
                //if (receiveDataThread.functionCode == 15)
                //{
                //    // Lese quantity
                //    byteData[1] = bytes[10];
                //    byteData[0] = bytes[11];
                //    Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //    receiveDataThread.quantity = wordData[0];

                //    receiveDataThread.byteCount = bytes[12];

                //    if ((receiveDataThread.byteCount % 2) != 0)
                //        receiveDataThread.receiveCoilValues = new ushort[receiveDataThread.byteCount / 2 + 1];
                //    else
                //        receiveDataThread.receiveCoilValues = new ushort[receiveDataThread.byteCount / 2];
                //    // Lese Value
                //    Buffer.BlockCopy(bytes, 13, receiveDataThread.receiveCoilValues, 0, receiveDataThread.byteCount);
                //}
                //if (receiveDataThread.functionCode == 16)
                //{
                //    // Lese quantity
                //    byteData[1] = bytes[10];
                //    byteData[0] = bytes[11];
                //    Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //    receiveDataThread.quantity = wordData[0];

                //    receiveDataThread.byteCount = bytes[12];
                //    receiveDataThread.receiveRegisterValues = new ushort[receiveDataThread.quantity];
                //    for (int i = 0; i < receiveDataThread.quantity; i++)
                //    {
                //        // Lese Value
                //        byteData[1] = bytes[13+i*2];
                //        byteData[0] = bytes[14+i*2];
                //        Buffer.BlockCopy(byteData, 0, receiveDataThread.receiveRegisterValues, i * 2, 2);
                //    }

                //}
                //if (receiveDataThread.functionCode == 23)
                //{
                //    // Lese starting Address Read
                //    byteData[1] = bytes[8];
                //    byteData[0] = bytes[9];
                //    Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //    receiveDataThread.startingAddressRead = wordData[0];
                //    // Lese quantity Read
                //    byteData[1] = bytes[10];
                //    byteData[0] = bytes[11];
                //    Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //    receiveDataThread.quantityRead = wordData[0];
                //    // Lese starting Address Write
                //    byteData[1] = bytes[12];
                //    byteData[0] = bytes[13];
                //    Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //    receiveDataThread.startingAddressWrite = wordData[0];
                //    // Lese quantity Write
                //    byteData[1] = bytes[14];
                //    byteData[0] = bytes[15];
                //    Buffer.BlockCopy(byteData, 0, wordData, 0, 2);
                //    receiveDataThread.quantityWrite = wordData[0];

                //    receiveDataThread.byteCount = bytes[16];
                //    receiveDataThread.receiveRegisterValues = new ushort[receiveDataThread.quantityWrite];
                //    for (int i = 0; i < receiveDataThread.quantityWrite; i++)
                //    {
                //        // Lese Value
                //        byteData[1] = bytes[17 + i * 2];
                //        byteData[0] = bytes[18 + i * 2];
                //        Buffer.BlockCopy(byteData, 0, receiveDataThread.receiveRegisterValues, i * 2, 2);
                //    }
                //}


                //this.sendAnswer();
                //this.CreateLogData(receiveDataThread, sendDataThread);
                #endregion

                #region EIO Parse Code

                EIOExtensionProtocol eioExtensionFrame;
                if (EIOParser.TryParseEIOExtension(bytes, out eioExtensionFrame))
                {
                    eioExtensionFrame.IPAddress = ipAddressIn;
                    eioExtensionFrame.Port = portIn;
                }
                else
                {
                    log.Error("On ProcessReceivedData: Received data is not a valid EIOExtensionProtocol Frame");
                    return;
                }


                #endregion // EIO Parse Code



                if (OnEIOExtensionFrameReceived != null)
                    OnEIOExtensionFrameReceived(this, eioExtensionFrame);
            }
            catch (Exception e)
            {
                log.Error(e);
                return;
            }
            
        }

        

        private void CreateAnswer(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {

            switch (receiveData.functionCode)
            {
                // Read Coils
                case 1:
                    if (!FunctionCode1Disabled)
                    {
                        //this.ReadCoils(receiveData, sendData, stream, portIn, ipAddressIn);
                    }
                    else
                    {
                        sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                        sendData.exceptionCode = 1;
                        sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                    }
                    break;
                //// Read Input Registers
                //case 2:
                //    if (!FunctionCode2Disabled)
                //        this.ReadDiscreteInputs(receiveData, sendData, stream, portIn, ipAddressIn);
                //    else
                //    {
                //        sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //        sendData.exceptionCode = 1;
                //        sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //    }

                //    break;
                //// Read Holding Registers
                //case 3:
                //    if (!FunctionCode3Disabled)
                //        this.ReadHoldingRegisters(receiveData, sendData, stream, portIn, ipAddressIn);
                //    else
                //    {
                //        sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //        sendData.exceptionCode = 1;
                //        sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //    }

                //    break;
                //// Read Input Registers
                //case 4:
                //    if (!FunctionCode4Disabled)
                //        this.ReadInputRegisters(receiveData, sendData, stream, portIn, ipAddressIn);
                //    else
                //    {
                //        sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //        sendData.exceptionCode = 1;
                //        sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //    }

                //    break;
                //// Write single coil
                //case 5:
                //    if (!FunctionCode5Disabled)
                //        this.WriteSingleCoil(receiveData, sendData, stream, portIn, ipAddressIn);
                //    else
                //    {
                //        sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //        sendData.exceptionCode = 1;
                //        sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //    }

                //    break;
                //// Write single register
                //case 6:
                //    if (!FunctionCode6Disabled)
                //        this.WriteSingleRegister(receiveData, sendData, stream, portIn, ipAddressIn);
                //    else
                //    {
                //        sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //        sendData.exceptionCode = 1;
                //        sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //    }

                //        break;
                //// Write Multiple coils
                //case 15:
                //        if (!FunctionCode15Disabled)
                //            this.WriteMultipleCoils(receiveData, sendData, stream, portIn, ipAddressIn);
                //        else
                //        {
                //            sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //            sendData.exceptionCode = 1;
                //            sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //        }

                //        break;
                //// Write Multiple registers
                //case 16:
                //        if (!FunctionCode16Disabled)
                //            this.WriteMultipleRegisters(receiveData, sendData, stream, portIn, ipAddressIn);
                //        else
                //        {
                //            sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //            sendData.exceptionCode = 1;
                //            sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //        }

                //        break;
                //// Error: Function Code not supported
                //case 23:
                //        if (!FunctionCode23Disabled)
                //            this.ReadWriteMultipleRegisters(receiveData, sendData, stream, portIn, ipAddressIn);
                //        else
                //        {
                //            sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                //            sendData.exceptionCode = 1;
                //            sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                //        }

                //        break;
                // Error: Function Code not supported
                default: sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                    sendData.exceptionCode = 1;
                    sendException(sendData.errorCode, sendData.exceptionCode, receiveData, sendData, stream, portIn, ipAddressIn);
                    break;
            }
            sendData.timeStamp = DateTime.Now;
        }

        private void ReadCoils(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            if ((receiveData.quantity < 1) | (receiveData.quantity > 0x07D0))  //Invalid quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if ((receiveData.startingAdress + 1 + receiveData.quantity) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            if ((receiveData.quantity % 8) == 0)
                sendData.byteCount = (byte)(receiveData.quantity / 8);
            else
                sendData.byteCount = (byte)(receiveData.quantity / 8 + 1);

            sendData.sendCoilValues = new bool[receiveData.quantity];
            Array.Copy(coils, receiveData.startingAdress + 1, sendData.sendCoilValues, 0, receiveData.quantity);

            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[9 + sendData.byteCount];
                Byte[] byteData = new byte[2];

                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;

                //ByteCount
                data[8] = sendData.byteCount;

                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendCoilValues = null;
                }

                if (sendData.sendCoilValues != null)
                    for (int i = 0; i < (sendData.byteCount); i++)
                    {
                        byteData = new byte[2];
                        for (int j = 0; j < 8; j++)
                        {

                            byte boolValue;
                            if (sendData.sendCoilValues[i * 8 + j] == true)
                                boolValue = 1;
                            else
                                boolValue = 0;
                            byteData[1] = (byte)((byteData[1]) | (boolValue << j));
                            if ((i * 8 + j + 1) >= sendData.sendCoilValues.Length)
                                break;
                        }
                        data[9 + i] = byteData[1];
                    }
                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
            }

        }

        private void ReadDiscreteInputs(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            if ((receiveData.quantity < 1) | (receiveData.quantity > 0x07D0))  //Invalid quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if ((receiveData.startingAdress + 1 + receiveData.quantity) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            if ((receiveData.quantity % 8) == 0)
                sendData.byteCount = (byte)(receiveData.quantity / 8);
            else
                sendData.byteCount = (byte)(receiveData.quantity / 8 + 1);

            sendData.sendCoilValues = new bool[receiveData.quantity];
            Array.Copy(discreteInputs, receiveData.startingAdress + 1, sendData.sendCoilValues, 0, receiveData.quantity);

            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[9 + sendData.byteCount];
                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;

                //ByteCount
                data[8] = sendData.byteCount;


                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendCoilValues = null;
                }

                if (sendData.sendCoilValues != null)
                    for (int i = 0; i < (sendData.byteCount); i++)
                    {
                        byteData = new byte[2];
                        for (int j = 0; j < 8; j++)
                        {

                            byte boolValue;
                            if (sendData.sendCoilValues[i * 8 + j] == true)
                                boolValue = 1;
                            else
                                boolValue = 0;
                            byteData[1] = (byte)((byteData[1]) | (boolValue << j));
                            if ((i * 8 + j + 1) >= sendData.sendCoilValues.Length)
                                break;
                        }
                        data[9 + i] = byteData[1];
                    }

                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
            }
        }

        private void ReadHoldingRegisters(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            if ((receiveData.quantity < 1) | (receiveData.quantity > 0x007D))  //Invalid quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if ((receiveData.startingAdress + 1 + receiveData.quantity) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            sendData.byteCount = (byte)(2 * receiveData.quantity);
            sendData.sendRegisterValues = new Int16[receiveData.quantity];
            Buffer.BlockCopy(holdingRegisters, receiveData.startingAdress * 2 + 2, sendData.sendRegisterValues, 0, receiveData.quantity * 2);

            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = (short)(0x03 + sendData.byteCount);

            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[9 + sendData.byteCount];
                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;

                //ByteCount
                data[8] = sendData.byteCount;

                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendRegisterValues = null;
                }


                if (sendData.sendRegisterValues != null)
                    for (int i = 0; i < (sendData.byteCount / 2); i++)
                    {
                        byteData = BitConverter.GetBytes((Int16)sendData.sendRegisterValues[i]);
                        data[9 + i * 2] = byteData[1];
                        data[10 + i * 2] = byteData[0];
                    }
                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
            }
        }

        private void ReadInputRegisters(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            if ((receiveData.quantity < 1) | (receiveData.quantity > 0x007D))  //Invalid quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if ((receiveData.startingAdress + 1 + receiveData.quantity) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            sendData.byteCount = (byte)(2 * receiveData.quantity);
            sendData.sendRegisterValues = new Int16[receiveData.quantity];
            Buffer.BlockCopy(inputRegisters, receiveData.startingAdress * 2 + 2, sendData.sendRegisterValues, 0, receiveData.quantity * 2);

            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = (short)(0x03 + sendData.byteCount);

            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[9 + sendData.byteCount];
                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;

                //ByteCount
                data[8] = sendData.byteCount;


                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendRegisterValues = null;
                }


                if (sendData.sendRegisterValues != null)
                    for (int i = 0; i < (sendData.byteCount / 2); i++)
                    {
                        byteData = BitConverter.GetBytes((Int16)sendData.sendRegisterValues[i]);
                        data[9 + i * 2] = byteData[1];
                        data[10 + i * 2] = byteData[0];
                    }
                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
            }
        }

        private void WriteSingleCoil(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            sendData.startingAdress = receiveData.startingAdress;
            sendData.receiveCoilValues = receiveData.receiveCoilValues;
            if ((receiveData.receiveCoilValues[0] != 0x0000) & (receiveData.receiveCoilValues[0] != 0xFF00))  //Invalid Value
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if ((receiveData.startingAdress + 1) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            if (receiveData.receiveCoilValues[0] == 0xFF00)
            {
                coils[receiveData.startingAdress + 1] = true;
            }
            if (receiveData.receiveCoilValues[0] == 0x0000)
            {
                coils[receiveData.startingAdress + 1] = false;
            }
            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = 0x06;
            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[12];

                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;



                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendRegisterValues = null;
                }
                else
                {
                    byteData = BitConverter.GetBytes((int)receiveData.startingAdress);
                    data[8] = byteData[1];
                    data[9] = byteData[0];
                    byteData = BitConverter.GetBytes((int)receiveData.receiveCoilValues[0]);
                    data[10] = byteData[1];
                    data[11] = byteData[0];
                }


                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
                if (coilsChanged != null)
                    coilsChanged();
            }
        }

        private void WriteSingleRegister(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            sendData.startingAdress = receiveData.startingAdress;
            sendData.receiveRegisterValues = receiveData.receiveRegisterValues;

            if ((receiveData.receiveRegisterValues[0] < 0x0000) | (receiveData.receiveRegisterValues[0] > 0xFFFF))  //Invalid Value
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if ((receiveData.startingAdress + 1) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            holdingRegisters[receiveData.startingAdress + 1] = unchecked((short)receiveData.receiveRegisterValues[0]);
            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = 0x06;
            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[12];

                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);


                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;



                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendRegisterValues = null;
                }
                else
                {
                    byteData = BitConverter.GetBytes((int)receiveData.startingAdress);
                    data[8] = byteData[1];
                    data[9] = byteData[0];
                    byteData = BitConverter.GetBytes((int)receiveData.receiveRegisterValues[0]);
                    data[10] = byteData[1];
                    data[11] = byteData[0];
                }


                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
                if (holdingRegistersChanged != null)
                    holdingRegistersChanged();
            }
        }


        private void WriteMultipleCoils(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            sendData.startingAdress = receiveData.startingAdress;
            sendData.quantity = receiveData.quantity;

            if ((receiveData.quantity == 0x0000) | (receiveData.quantity > 0x07B0))  //Invalid Quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if (((int)receiveData.startingAdress + 1 + (int)receiveData.quantity) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            for (int i = 0; i < receiveData.quantity; i++)
            {
                int shift = i % 16;
                /*                if ((i == receiveData.quantity - 1) & (receiveData.quantity % 2 != 0))
                                {
                                    if (shift < 8)
                                        shift = shift + 8;
                                    else
                                        shift = shift - 8;
                                }*/
                int mask = 0x1;
                mask = mask << (shift);
                if ((receiveData.receiveCoilValues[i / 16] & (ushort)mask) == 0)
                    coils[receiveData.startingAdress + i + 1] = false;
                else
                    coils[receiveData.startingAdress + i + 1] = true;

            }
            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = 0x06;
            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[12];

                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;



                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendRegisterValues = null;
                }
                else
                {
                    byteData = BitConverter.GetBytes((int)receiveData.startingAdress);
                    data[8] = byteData[1];
                    data[9] = byteData[0];
                    byteData = BitConverter.GetBytes((int)receiveData.quantity);
                    data[10] = byteData[1];
                    data[11] = byteData[0];
                }


                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
                if (coilsChanged != null)
                    coilsChanged();
            }
        }

        private void WriteMultipleRegisters(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            sendData.startingAdress = receiveData.startingAdress;
            sendData.quantity = receiveData.quantity;

            if ((receiveData.quantity == 0x0000) | (receiveData.quantity > 0x07B0))  //Invalid Quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if (((int)receiveData.startingAdress + 1 + (int)receiveData.quantity) > 65535)    //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            for (int i = 0; i < receiveData.quantity; i++)
            {
                holdingRegisters[receiveData.startingAdress + i + 1] = unchecked((short)receiveData.receiveRegisterValues[i]);
            }
            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = 0x06;
            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[12];

                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;



                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendRegisterValues = null;
                }
                else
                {
                    byteData = BitConverter.GetBytes((int)receiveData.startingAdress);
                    data[8] = byteData[1];
                    data[9] = byteData[0];
                    byteData = BitConverter.GetBytes((int)receiveData.quantity);
                    data[10] = byteData[1];
                    data[11] = byteData[0];
                }


                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
                if (holdingRegistersChanged != null)
                    holdingRegistersChanged();
            }
        }

        private void ReadWriteMultipleRegisters(ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.functionCode = receiveData.functionCode;
            sendData.sendRegisterValues = new Int16[receiveData.quantityRead];
            Buffer.BlockCopy(holdingRegisters, receiveData.startingAddressRead * 2 + 2, sendData.sendRegisterValues, 0, receiveData.quantityRead * 2);


            if ((receiveData.quantityRead <= 0x0001) | (receiveData.quantityRead >= 0x007D) | (receiveData.quantityWrite <= 0x0001) | (receiveData.quantityWrite >= 0x0079) | (receiveData.byteCount != (receiveData.quantityWrite * 2)))  //Invalid Quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 3;
            }
            if ((((int)receiveData.startingAddressRead + 1 + (int)receiveData.quantityRead) > 65535) | (((int)receiveData.startingAddressWrite + 1 + (int)receiveData.quantityWrite) > 65535))   //Invalid Starting adress or Starting address + quantity
            {
                sendData.errorCode = (byte)(receiveData.functionCode + 0x80);
                sendData.exceptionCode = 2;
            }
            for (int i = 0; i < receiveData.quantityWrite; i++)
            {
                holdingRegisters[receiveData.startingAddressWrite + i + 1] = unchecked((short)receiveData.receiveRegisterValues[i]);
            }
            sendData.byteCount = (byte)(2 * receiveData.quantityRead);
            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = Convert.ToInt16(3 + 2 * receiveData.quantityRead);
            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[9 + sendData.byteCount];

                Byte[] byteData = new byte[2];

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;

                //Function Code
                data[7] = sendData.functionCode;

                //ByteCount
                data[8] = sendData.byteCount;

                sendData.byteCount = receiveData.byteCount;

                if (sendData.exceptionCode > 0)
                {
                    data[7] = sendData.errorCode;
                    data[8] = sendData.exceptionCode;
                    sendData.sendRegisterValues = null;
                }
                else
                {
                    if (sendData.sendRegisterValues != null)
                        for (int i = 0; i < (sendData.byteCount / 2); i++)
                        {
                            byteData = BitConverter.GetBytes((Int16)sendData.sendRegisterValues[i]);
                            data[9 + i * 2] = byteData[1];
                            data[10 + i * 2] = byteData[0];
                        }

                }


                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
                if (holdingRegistersChanged != null)
                    holdingRegistersChanged();
            }
        }

        private void sendException(int errorCode, int exceptionCode, ModbusProtocol receiveData, ModbusProtocol sendData, NetworkStream stream, int portIn, IPAddress ipAddressIn)
        {
            sendData.response = true;

            sendData.transactionIdentifier = receiveData.transactionIdentifier;
            sendData.protocolIdentifier = receiveData.protocolIdentifier;

            sendData.unitIdentifier = receiveData.unitIdentifier;
            sendData.errorCode = (byte)errorCode;
            sendData.exceptionCode = (byte)exceptionCode;

            if (sendData.exceptionCode > 0)
                sendData.length = 0x03;
            else
                sendData.length = (short)(0x03 + sendData.byteCount);

            if (true)
            {
                Byte[] data;
                if (sendData.exceptionCode > 0)
                    data = new byte[9];
                else
                    data = new byte[9 + sendData.byteCount];
                Byte[] byteData = new byte[2];
                sendData.length = (byte)(data.Length - 6);

                //Send Transaction identifier
                byteData = BitConverter.GetBytes((int)sendData.transactionIdentifier);
                data[0] = byteData[1];
                data[1] = byteData[0];

                //Send Protocol identifier
                byteData = BitConverter.GetBytes((int)sendData.protocolIdentifier);
                data[2] = byteData[1];
                data[3] = byteData[0];

                //Send length
                byteData = BitConverter.GetBytes((int)sendData.length);
                data[4] = byteData[1];
                data[5] = byteData[0];

                //Unit Identifier
                data[6] = sendData.unitIdentifier;


                data[7] = sendData.errorCode;
                data[8] = sendData.exceptionCode;


                try
                {
                    if (udpFlag)
                    {
                        //UdpClient udpClient = new UdpClient();
                        IPEndPoint endPoint = new IPEndPoint(ipAddressIn, portIn);
                        udpClient.Send(data, data.Length, endPoint);

                    }
                    else
                        stream.Write(data, 0, data.Length);
                }
                catch (Exception) { }
            }
        }

        private void CreateLogData(ModbusProtocol receiveData, ModbusProtocol sendData)
        {
            for (int i = 0; i < 98; i++)
            {
                modbusLogData[99 - i] = modbusLogData[99 - i - 2];

            }
            modbusLogData[0] = receiveData;
            modbusLogData[1] = sendData;

        }


        public int NumberOfConnections
        {
            get
            {
                return numberOfConnections;
            }
        }

        public ModbusProtocol[] ModbusLogData
        {
            get
            {
                return modbusLogData;
            }
        }

        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;


            }
        }

        public bool UDPFlag
        {
            get
            {
                return udpFlag;
            }
            set
            {
                udpFlag = value;
            }
        }

    }
}
