using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;

namespace RobotAgent_CS
{
    public partial class Form1 : Form
    {

        // for UI +
        private PictureBox m_ImagePB;
        // for UI -

        // For Server +   
        private Socket serverSocket;
        private Socket connServerSocket;

        private Thread StartServerThread;
        private Thread ReceiveCPDataThread;

        private StateObject ServerState;
        // Thread signal.
        private static ManualResetEvent acceptDoneEvent = new ManualResetEvent(false);
        private static ManualResetEvent sendAckToCPDoneEvent = new ManualResetEvent(false);

        private bool m_bIsCPConnected = false;
        // For Server -

        // For Client +
        private Socket clientSocket;

        private Thread StartClientThread;
        private Thread ReceiveARMDataThread;

        private StateObject ClientState;
        // Thread signal.      
        public static ManualResetEvent connectDoneEvent = new ManualResetEvent(false);
        public static ManualResetEvent sendCmdToArmDoneEvent = new ManualResetEvent(false);
        public static AutoResetEvent waitArmAckDoneEvent = new AutoResetEvent(false);

        private string m_strResultFromARM;

        private bool m_bIsArmConnected = false;
        // For Client -

        // for barcode reader +
        List<ComboboxItem> m_BarcodeCBItemList;
        private BarcodeReader m_BarcodeReader;
        private bool m_bIsReadBarcodeOK = true;
        private string m_strSerialNumber;
        private string m_strMacAddress;
        private static AutoResetEvent waitReadBarcodeDoneEvent = new AutoResetEvent(false);
        // for barcode reader -

        // for laser reader +
        List<ComboboxItem> m_LaserCBItemList;
        private LaserReader m_LaserReader;
        private bool m_bIsLaserCheckOk = true;
        private static AutoResetEvent waitReadLaserDoneEvent = new AutoResetEvent(false);
        // for laser reader -

        // for camera device +
        List<ComboboxItem> m_CameraCBItemList;
        private CameraDevice m_CameraDevice;
        // for camera device - 

        // for common setting +
        private CommonSetting m_CommonSetting;
        // for common setting -     

        // for rich box limit line number +
        private int m_nMsgRBLine = 0;
        private int m_nLogRBLine = 0;
        // for rich box limit line number -                           

        private delegate void LogAppendDelegate(Color color, string text);

        public void LogAppend(Color color, string text)
        {

            logRichTB.AppendText("");
            logRichTB.SelectionColor = color;
            logRichTB.AppendText(text);

            m_nLogRBLine = Regex.Split(logRichTB.Text, "\n").Length;
            if (m_nLogRBLine == 100) logRichTB.Clear();
        }

        private delegate void MsgAppendDelegate(Color color, string text);

        public void MsgAppend(Color color, string text)
        {

            msgRichTB.AppendText("");
            msgRichTB.SelectionColor = color;
            msgRichTB.AppendText(text);

            m_nMsgRBLine = Regex.Split(msgRichTB.Text, "\n").Length;
            if (m_nMsgRBLine == 100) msgRichTB.Clear();
        }

        // For Server +        

        public void StartServerFunc()
        {

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com". 
            //IPHostEntry ipHostInfo    = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress   ipAddress     = ipHostInfo.AddressList[0];
            IPAddress ipAddress = IPAddress.Parse(m_CommonSetting._strCPIPAddr);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, m_CommonSetting._CPPortNum);

            // Create a TCP/IP socket.
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {

                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(10);

                m_bIsCPConnected = false;

                while (!m_bIsCPConnected)
                {

                    // Set the event to nonsignaled state.
                    acceptDoneEvent.Reset();

                    // Start an asynchronous socket to listen for connections.
                    serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

                    // Wait until a connection is made before continuing.
                    acceptDoneEvent.WaitOne();

                    break;
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {

            // Signal the main thread to continue.
            acceptDoneEvent.Set();
            m_bIsCPConnected = true;

            // Get the socket that handles the client request.
            connServerSocket = serverSocket.EndAccept(ar);

            LogAppendDelegate la = new LogAppendDelegate(LogAppend);
            logRichTB.Invoke(la, Color.Black, "[ " + DateTime.Now.ToString("HH:mm:ss") + " ] " + "Success to connect CP...\r\n");

            // Create the server state object.
            ServerState = new StateObject();
            ServerState.workSocket = connServerSocket;

            ReceiveCPDataThread = new Thread(recCPDataThreadFunc);
            ReceiveCPDataThread.Start();
        }

        private void recCPDataThreadFunc()
        {

            connServerSocket.BeginReceive(ServerState.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(RecCPDataCallback), ServerState);
        }

        public void RecCPDataCallback(IAsyncResult ar)
        {

            String content = String.Empty;

            try
            {

                // Read data from the client socket. 
                int bytesRead = connServerSocket.EndReceive(ar);

                if (bytesRead > 0)
                {

                    ServerState.sb.Append(Encoding.ASCII.GetString(ServerState.buffer, 0, bytesRead));

                    content = ServerState.sb.ToString();
                    if (content.IndexOf("\r\n") > -1)
                    {

                        MsgAppendDelegate la = new MsgAppendDelegate(MsgAppend);
                        msgRichTB.Invoke(la, Color.Green, "[ " + DateTime.Now.ToString("HH:mm:ss ") + "CP to RA ] " + content);

                        if (m_bIsArmConnected) ProcessCPCmdFromCP(content);
                        else
                        {

                            SendAckToCP("66,ERROR\r\n");
                            sendAckToCPDoneEvent.WaitOne();
                        }

                        ServerState.sb.Length = 0;
                        connServerSocket.BeginReceive(ServerState.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(RecCPDataCallback), ServerState);
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        private int ProcessCPCmdFromCP(string strCmdFromCP)
        {

            waitArmAckDoneEvent.Reset();

            // query "Is there board in DUT?"
            if (strCmdFromCP.Equals("QUERY,DT01,DI2\r\n"))
            {

                SendCmdToArm(strCmdFromCP);
                sendCmdToArmDoneEvent.WaitOne();

                if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                {

                    SendAckToCP("66,ERROR\r\n");
                    sendAckToCPDoneEvent.WaitOne();
                    return -1;
                }

                SendAckToCP(m_strResultFromARM);
                sendAckToCPDoneEvent.WaitOne();
            }
            // query "SN and MAC"
            else if (strCmdFromCP.Equals("QUERY,DT01,SN=?,MAC=?\r\n"))
            {

                // Snapshot to detect whether the board is ready or not +
                bool bIsBoardReady = true;
                // Snapshot to detect whether the board is ready or not -

                if (bIsBoardReady)
                {

                    if (m_BarcodeReader.m_bIsTopBarcode)
                    {

                        SendCmdToArm("BARCODE TOP\r\n");
                        sendCmdToArmDoneEvent.WaitOne();

                        if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                        {

                            SendAckToCP("66,ERROR\r\n");
                            sendAckToCPDoneEvent.WaitOne();
                            return -1;
                        }

                        if (m_strResultFromARM.Equals("READ BARCODE\r\n"))
                        {

                            // read barcode +
                            if (m_BarcodeReader.m_bIsEnable)
                            {

                                m_BarcodeReader.ReadFromBR();

                                if (!waitReadBarcodeDoneEvent.WaitOne(m_BarcodeReader.m_nReadTimeOut, true))
                                {

                                    m_bIsReadBarcodeOK = false;
                                    m_BarcodeReader.ShutDownBR();
                                }
                                else
                                {
                                    m_bIsReadBarcodeOK = true;
                                    m_strSerialNumber = m_BarcodeReader.m_strSerialNumber;
                                    m_strMacAddress = m_BarcodeReader.m_strMacAddress;
                                }
                            }
                            // read barcode -

                            if (m_bIsReadBarcodeOK)
                            {

                                SendCmdToArm("SNOK\r\n");
                                sendCmdToArmDoneEvent.WaitOne();

                                if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                                {

                                    SendAckToCP("66,ERROR\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                    return -1;
                                }

                                if (m_strResultFromARM.Equals("PICK OK\r\n"))
                                {

                                    // send result to CP
                                    SendAckToCP("SNOK," + m_strSerialNumber + "," + m_strMacAddress + "\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                }
                                else
                                {

                                    // send result to CP
                                    SendAckToCP("BR0001,Barcode Not Read\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                }
                            }
                            else
                            {

                                SendCmdToArm("SNNG\r\n");
                                sendCmdToArmDoneEvent.WaitOne();

                                if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                                {

                                    SendAckToCP("66,ERROR\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                    return -1;
                                }

                                if (m_strResultFromARM.Equals("AT HOME\r\n"))
                                {

                                    // send result to CP
                                    SendAckToCP("BR0001,Barcode Not Read\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                }
                            }
                        }
                    }
                    else
                    {

                        SendCmdToArm("BARCODE BOTTOM\r\n");
                        sendCmdToArmDoneEvent.WaitOne();

                        if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                        {

                            SendAckToCP("66,ERROR\r\n");
                            sendAckToCPDoneEvent.WaitOne();
                            return -1;
                        }

                        if (m_strResultFromARM.Equals("READ BARCODE\r\n"))
                        {

                            // read barcode +
                            if (m_BarcodeReader.m_bIsEnable)
                            {

                                m_BarcodeReader.ReadFromBR();

                                m_BarcodeReader.ReadFromBR();

                                if (!waitReadBarcodeDoneEvent.WaitOne(10 * 1000, true))
                                {

                                    m_bIsReadBarcodeOK = false;
                                    m_BarcodeReader.ShutDownBR();
                                }
                                else
                                {
                                    m_bIsReadBarcodeOK = true;
                                    m_strSerialNumber = m_BarcodeReader.m_strSerialNumber;
                                    m_strMacAddress = m_BarcodeReader.m_strMacAddress;
                                }
                            }
                            // read barcode -

                            if (m_bIsReadBarcodeOK)
                            {

                                SendCmdToArm("SNOK\r\n");
                                sendCmdToArmDoneEvent.WaitOne();

                                if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                                {

                                    SendAckToCP("66,ERROR\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                    return -1;
                                }

                                if (m_strResultFromARM.Equals("PICK OK\r\n"))
                                {

                                    // send result to CP
                                    SendAckToCP("SNOK," + m_strSerialNumber + "," + m_strMacAddress + "\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                }
                            }
                            else
                            {

                                SendCmdToArm("SNNG\r\n");
                                sendCmdToArmDoneEvent.WaitOne();

                                if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                                {

                                    SendAckToCP("66,ERROR\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                    return -1;
                                }

                                if (m_strResultFromARM.Equals("AT HOME\r\n"))
                                {

                                    // send result to CP
                                    SendAckToCP("BR0001,Barcode Not Read\r\n");
                                    sendAckToCPDoneEvent.WaitOne();
                                }
                            }
                        }
                        else
                        {

                            // send result to CP
                            SendAckToCP("BR0001,Barcode Not Read\r\n");
                            sendAckToCPDoneEvent.WaitOne();
                        }
                    }
                }
                else
                {

                    SendAckToCP("CV0001,DUT is not ready\r\n");
                    sendAckToCPDoneEvent.WaitOne();
                }
            }
            // Move instrution
            else if (strCmdFromCP.IndexOf("MOVE") >= 0)
            {

                SendCmdToArm(strCmdFromCP);
                sendCmdToArmDoneEvent.WaitOne();

                if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                {

                    SendAckToCP("66,ERROR\r\n");
                    sendAckToCPDoneEvent.WaitOne();
                    return -1;
                }

                if (strCmdFromCP.IndexOf("PS01") >= 0 || strCmdFromCP.IndexOf("FL01") >= 0)
                {

                    // MOVE,TSXX,PS01
                    // MOVE,TSXX,FL01

                    // send result to CP
                    SendAckToCP(m_strResultFromARM);
                    sendAckToCPDoneEvent.WaitOne();
                }
                else
                {

                    // MOVE,DT01,TSXX
                    // MOVE,TSXX,TSXX                    

                    if (m_strResultFromARM.Equals("LASER FT\r\n"))
                    {

                        // read laser and check the two side defference value +
                        if (m_LaserReader.m_bIsEnable)
                        {

                            m_LaserReader.ReadFromLaser();
                            waitReadLaserDoneEvent.WaitOne();
                            this.m_bIsLaserCheckOk = m_LaserReader.bIsLaserCheckOk;
                        }
                        // read laser and check the two side defference value -

                        if (m_bIsLaserCheckOk)
                        {

                            SendCmdToArm("LASER PASS\r\n");
                            sendCmdToArmDoneEvent.WaitOne();
                        }
                        else
                        {

                            SendCmdToArm("LASER FAIL\r\n");
                            sendCmdToArmDoneEvent.WaitOne();
                        }

                        if (!waitArmAckDoneEvent.WaitOne(m_CommonSetting._nWaitTimeOut, true))
                        {

                            SendAckToCP("66,ERROR\r\n");
                            sendAckToCPDoneEvent.WaitOne();
                            return -1;
                        }
                    }

                    // send result to CP
                    SendAckToCP(m_strResultFromARM);
                    sendAckToCPDoneEvent.WaitOne();
                }
            }
            else
            {

                // send result to CP
                SendAckToCP("98,ERROR\r\n");
                sendAckToCPDoneEvent.WaitOne();
            }

            return 0;
        }

        private void SendAckToCP(String data)
        {

            MsgAppendDelegate la = new MsgAppendDelegate(MsgAppend);
            msgRichTB.Invoke(la, Color.Red, "[ " + DateTime.Now.ToString("HH:mm:ss ") + "RA to CP ] " + data);

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            connServerSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendAckToCPCallback), null);
        }

        private void SendAckToCPCallback(IAsyncResult ar)
        {

            try
            {

                // Complete sending the data to the remote device.
                int bytesSent = connServerSocket.EndSend(ar);

                // Signal that all bytes have been sent.
                sendAckToCPDoneEvent.Set();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        // For Server -

        // For Client +    
        public void StartClientFunc()
        {

            // Connect to a remote device.
            try
            {

                IPAddress ipAddress = IPAddress.Parse(m_CommonSetting._strArmIPAddr);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, m_CommonSetting._ArmPortNum);

                // Create a TCP/IP socket.
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                m_bIsArmConnected = false;

                while (!m_bIsArmConnected)
                {

                    connectDoneEvent.Reset();

                    clientSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), clientSocket);

                    connectDoneEvent.WaitOne();
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {

            try
            {

                // Complete the connection.
                clientSocket.EndConnect(ar);

                m_bIsArmConnected = true;

                LogAppendDelegate la = new LogAppendDelegate(LogAppend);
                logRichTB.Invoke(la, Color.Black, "[ " + DateTime.Now.ToString("HH:mm:ss") + " ] " + "Success to connect ARM...\r\n");

                ReceiveARMDataThread = new Thread(recARMDataThreadFunc);
                ReceiveARMDataThread.Start();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
                m_bIsArmConnected = false;
            }

            connectDoneEvent.Set();
        }

        private void SendCmdToArm(String data)
        {

            MsgAppendDelegate la = new MsgAppendDelegate(MsgAppend);
            msgRichTB.Invoke(la, Color.Blue, "[ " + DateTime.Now.ToString("HH:mm:ss ") + "RA to RO ] " + data);

            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            clientSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendToServerCallback), null);
        }

        private void SendToServerCallback(IAsyncResult ar)
        {

            try
            {

                // Complete sending the data to the remote device.
                int bytesSent = clientSocket.EndSend(ar);

                // Signal that all bytes have been sent.
                sendCmdToArmDoneEvent.Set();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        private void recARMDataThreadFunc()
        {

            try
            {

                // Create the client state object.
                ClientState = new StateObject();
                ClientState.workSocket = clientSocket;

                // Begin receiving the data from the remote device.
                clientSocket.BeginReceive(ClientState.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(RecARMDataCallback), ClientState);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        public void RecARMDataCallback(IAsyncResult ar)
        {

            String content = String.Empty;

            try
            {

                // Read data from the client socket. 
                int bytesRead = clientSocket.EndReceive(ar);

                if (bytesRead > 0)
                {

                    ClientState.sb.Append(Encoding.ASCII.GetString(ClientState.buffer, 0, bytesRead));

                    content = ClientState.sb.ToString();
                    if (content.IndexOf("\r\n") > -1)
                    {

                        MsgAppendDelegate la = new MsgAppendDelegate(MsgAppend);
                        msgRichTB.Invoke(la, Color.Black, "[ " + DateTime.Now.ToString("HH:mm:ss ") + "RO to RA ] " + content);

                        m_strResultFromARM = content;
                        waitArmAckDoneEvent.Set();
                        ClientState.sb.Length = 0;
                        clientSocket.BeginReceive(ClientState.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(RecARMDataCallback), ClientState);
                    }
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        // For Client -

        public Form1()
        {

            InitializeComponent();
        }

        public System.Drawing.Point SetComponentLoc(int x, int y, int nBlockEdge)
        {

            return new System.Drawing.Point(x * nBlockEdge, y * nBlockEdge);
        }

        public System.Drawing.Size SetComponentSize(int w, int h, int nBlockEdge)
        {

            return new System.Drawing.Size(w * nBlockEdge, h * nBlockEdge);
        }

        public void SetUpUI()
        {

            barcodeCombo.Items.Clear();
            m_BarcodeCBItemList = m_CommonSetting.GetDeviceVendorAndType("barcode");
            foreach (ComboboxItem barcodeCBItem in m_BarcodeCBItemList)
                barcodeCombo.Items.Add(barcodeCBItem);

            laserCombo.Items.Clear();
            m_LaserCBItemList = m_CommonSetting.GetDeviceVendorAndType("laser");
            foreach (ComboboxItem laserCBItem in m_LaserCBItemList)
                laserCombo.Items.Add(laserCBItem);

            cameraCombo.Items.Clear();
            m_CameraCBItemList = m_CommonSetting.GetDeviceVendorAndType("camera");
            foreach (ComboboxItem cameraCBItem in m_CameraCBItemList)
                cameraCombo.Items.Add(cameraCBItem);


            // get screen rect 
            Rectangle resolution = Screen.PrimaryScreen.Bounds;

            // set form position and size
            Location = new Point(resolution.Width / 20, resolution.Height / 20);
            Size = new Size(resolution.Width * 9 / 10, resolution.Height * 9 / 10);

            int nBlockEdge = base.ClientSize.Width / 60;

            // label +
            barcodeLabel.Location = SetComponentLoc(2, 3, nBlockEdge);
            barcodeLabel.Size = SetComponentSize(4, 1, nBlockEdge);
            barcodeCombo.Location = SetComponentLoc(2, 4, nBlockEdge);
            barcodeCombo.Size = SetComponentSize(8, 1, nBlockEdge);

            laserLabel.Location = SetComponentLoc(2, 7, nBlockEdge);
            laserLabel.Size = SetComponentSize(4, 1, nBlockEdge);
            laserCombo.Location = SetComponentLoc(2, 8, nBlockEdge);
            laserCombo.Size = SetComponentSize(8, 1, nBlockEdge);

            cameraLabel.Location = SetComponentLoc(2, 11, nBlockEdge);
            cameraLabel.Size = SetComponentSize(4, 1, nBlockEdge);
            cameraCombo.Location = SetComponentLoc(2, 12, nBlockEdge);
            cameraCombo.Size = SetComponentSize(8, 1, nBlockEdge);

            // combobox -

            // picture box +            
            imgLabel.Location = SetComponentLoc(12, 3, nBlockEdge);
            imgLabel.Size = SetComponentSize(4, 1, nBlockEdge);

            m_ImagePB = new PictureBox(33 * nBlockEdge, 22 * nBlockEdge);
            m_ImagePB.Border = System.Windows.Forms.BorderStyle.FixedSingle;
            m_ImagePB.Name = "ImagePicBox";
            m_ImagePB.Picture = "";
            m_ImagePB.Location = SetComponentLoc(12, 4, nBlockEdge);
            m_ImagePB.Size = new System.Drawing.Size(33 * nBlockEdge + 2, 22 * nBlockEdge + 2);
            m_ImagePB.TabIndex = 9;
            m_ImagePB.Picture = "1.jpg";
            Controls.Add(this.m_ImagePB);
            // picture box -

            brLabel.Location = SetComponentLoc(12, 28, nBlockEdge);
            brLabel.Size = SetComponentSize(4, 1, nBlockEdge);
            brTB.Location = SetComponentLoc(12, 29, nBlockEdge);
            brTB.Size = SetComponentSize(33, 1, nBlockEdge);

            msgLabel.Location = SetComponentLoc(47, 3, nBlockEdge);
            msgLabel.Size = SetComponentSize(4, 1, nBlockEdge);
            msgRichTB.Location = SetComponentLoc(47, 4, nBlockEdge);
            msgRichTB.Size = SetComponentSize(12, 12, nBlockEdge);

            logLabel.Location = SetComponentLoc(47, 17, nBlockEdge);
            logLabel.Size = SetComponentSize(4, 1, nBlockEdge);
            logRichTB.Location = SetComponentLoc(47, 18, nBlockEdge);
            logRichTB.Size = SetComponentSize(12, 12, nBlockEdge);

            laserCombo.SelectedIndex = 0;
            barcodeCombo.SelectedIndex = 0;
            cameraCombo.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            // Initialization Section +
            m_CommonSetting = new CommonSetting();
            // Initialization Section -

            SetUpUI();

            StartServerThread = new Thread(StartServerFunc);
            StartServerThread.Start();

            StartClientThread = new Thread(StartClientFunc);
            StartClientThread.Start();

            // start timer +

            timer_IsArmConnected.Enabled = true;
            timer_IsCPConnected.Enabled = true;
            timer_UpdateCameraBuffer.Enabled = true;

            // start timer -
        }

        // for barcode receive data +

        private void ReceiveBRDataFunc()
        {

            waitReadBarcodeDoneEvent.Set();
        }

        // for barcode receive data -

        // for laser receive data  +

        private void ReceiveLaserDataFunc()
        {

            waitReadLaserDoneEvent.Set();
        }

        // for laser receive data  -

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {

            m_CameraDevice.CloseCamera();

            acceptDoneEvent.Set();
            connectDoneEvent.Set();
        }

        // for timer callback function +

        private void timer_IsArmConnected_Tick(object sender, EventArgs e)
        {

            bool part1 = clientSocket.Poll(1000, SelectMode.SelectRead);
            bool part2 = (clientSocket.Available == 0);

            if (m_bIsArmConnected)
            {

                if (part1 || !part2)
                {

                    // Console.WriteLine("connection unavailable");
                    if (!StartClientThread.IsAlive)
                    {

                        StartClientThread = new Thread(StartClientFunc);
                        StartClientThread.Start();
                    }
                }
            }
        }

        private void timer_IsCPConnected_Tick(object sender, EventArgs e)
        {

            if (m_bIsCPConnected)
            {

                bool part1 = connServerSocket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (connServerSocket.Available == 0);

                if (part1 || !part2)
                {

                    // Console.WriteLine("connection unavailable");
                    if (!StartServerThread.IsAlive)
                    {

                        serverSocket.Close();
                        StartServerThread = new Thread(StartServerFunc);
                        StartServerThread.Start();
                    }
                }
            }
        }

        private void timer_UpdateCameraBuffer_Tick(object sender, EventArgs e)
        {

            if (m_CameraDevice.m_bIsConnect) m_CameraDevice.UpdateCameraBuffer();
        }

        // for timer callback function -

        // for combobox callback function +

        private void barcodeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {

            int index = ((ComboBox)sender).SelectedIndex;

            if (m_BarcodeReader != null)
            {

                m_BarcodeReader.CloseBR();
                m_BarcodeReader = null;
                GC.Collect();
            }

            m_BarcodeReader = new BarcodeReader(m_BarcodeCBItemList[index]);
            m_BarcodeReader.ReceiveBRDataEvent += new Form1().ReceiveBRDataFunc;
            if (!m_BarcodeReader.m_bIsEnable)
            {

                LogAppendDelegate la = new LogAppendDelegate(LogAppend);
                logRichTB.Invoke(la, Color.Red, "[ " + DateTime.Now.ToString("HH:mm:ss") + " ] " + "Failed to open barcode...\r\n");
            }
        }

        private void laserCombo_SelectedIndexChanged(object sender, EventArgs e)
        {

            int index = ((ComboBox)sender).SelectedIndex;

            if (m_LaserReader != null)
            {

                m_LaserReader.CloseLaser();
                m_LaserReader = null;
                GC.Collect();
            }

            m_LaserReader = new LaserReader(m_LaserCBItemList[index]);
            m_LaserReader.ReceiveLaserDataEvent += new Form1().ReceiveLaserDataFunc;
            if (!m_LaserReader.m_bIsEnable)
            {

                LogAppendDelegate la = new LogAppendDelegate(LogAppend);
                logRichTB.Invoke(la, Color.Red, "[ " + DateTime.Now.ToString("HH:mm:ss") + " ] " + "Failed to open laser...\r\n");
            }
        }

        private void cameraCombo_SelectedIndexChanged(object sender, EventArgs e)
        {

            int index = ((ComboBox)sender).SelectedIndex;

            if (m_CameraDevice != null)
            {

                m_CameraDevice.CloseCamera();
                m_CameraDevice = null;
                GC.Collect();
            }

            m_CameraDevice = new CameraDevice(m_CameraCBItemList[index], m_ImagePB);
            if ((!m_CameraDevice.m_bIsEnable) || (!m_CameraDevice.m_bIsConnect))
            {

                LogAppendDelegate la = new LogAppendDelegate(LogAppend);
                logRichTB.Invoke(la, Color.Red, "[ " + DateTime.Now.ToString("HH:mm:ss") + " ] " + "Failed to open camera...\r\n");
            }
        }

        // for combobox callback function -

        private void m_takeSnapshot_Click(object sender, EventArgs e)
        {

            if (m_CameraDevice.m_bIsConnect) m_CameraDevice.SaveImage();
        }
    }

    // State object for reading client data asynchronously
    public class StateObject
    {

        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}
