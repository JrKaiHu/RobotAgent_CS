using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace RobotAgent_CS
{
    class LaserReader
    {
        private SerialPort m_SerialPort;
        public bool bIsLaserCheckOk = false;

        public delegate void ReceiveLaserDataEventHandler();
        public event ReceiveLaserDataEventHandler ReceiveLaserDataEvent;

        public bool m_bIsEnable;
        private double m_flDiffLimit;
        private int m_nBaudRate;
        private int m_nDataBits;
        private string m_strParity;
        private string m_strStopBits;
        private string m_strPortName;

        public LaserReader(ComboboxItem laserCBItem)
        {

            XDocument myXDoc = XDocument.Load("common.xml");

            XElement xeLasers = myXDoc.Root.Element("lasers");

            m_bIsEnable = int.Parse(xeLasers.Attribute("IsEnable").Value) == 1 ? true : false;

            if (m_bIsEnable)
            {

                IEnumerable<XElement> allLasers = xeLasers.Descendants("laser");

                foreach (XElement laser in allLasers)
                {

                    if (laserCBItem.strVendor.Equals(laser.Attribute("Vendor").Value) &&
                         laserCBItem.strType.Equals(laser.Attribute("Type").Value))
                    {

                        m_flDiffLimit = float.Parse(laser.Element("DIFFERENCE_LIMIT").Value);
                        m_nBaudRate = int.Parse(laser.Element("BAUD_RATE").Value);
                        m_nDataBits = int.Parse(laser.Element("DATA_BITS").Value);
                        m_strParity = laser.Element("PARITY").Value;
                        m_strStopBits = laser.Element("STOP_BITS").Value;
                        m_strPortName = laser.Element("PORT_NAME").Value;
                    }
                }

                m_SerialPort = new SerialPort();
                m_SerialPort.BaudRate = m_nBaudRate;
                m_SerialPort.DataBits = m_nDataBits;

                if (m_strParity.Equals("EVEN")) m_SerialPort.Parity = Parity.Even;
                else if (m_strParity.Equals("ODD")) m_SerialPort.Parity = Parity.Odd;
                else m_SerialPort.Parity = Parity.None;

                if (m_strStopBits.Equals("NONE")) m_SerialPort.StopBits = StopBits.None;
                else if (m_strStopBits.Equals("ONE")) m_SerialPort.StopBits = StopBits.One;
                else if (m_strStopBits.Equals("ONEPOINTFIVE")) m_SerialPort.StopBits = StopBits.OnePointFive;
                else m_SerialPort.StopBits = StopBits.Two;

                m_SerialPort.PortName = m_strPortName;

                m_SerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                try
                {

                    if (m_SerialPort.IsOpen) m_SerialPort.Close();

                    m_SerialPort.Open();

                    //serialPort.ReadTimeout = 100;
                }
                catch (Exception ex)
                {

                    m_bIsEnable = false;
                    Console.WriteLine(ex.ToString());
                    //MessageBox.Show(serialPort.PortName + "\r\n" + ex.Message);  // non-existent or disappeared
                }
            }
        }

        public void CloseLaser()
        {

            m_SerialPort.Close();
        }

        public void ReadFromLaser()
        {

            string readCmd = "M0\r";
            Byte[] sendBytes = ASCIIEncoding.ASCII.GetBytes(readCmd);

            if (m_SerialPort.IsOpen)
            {

                try
                {

                    m_SerialPort.Write(sendBytes, 0, sendBytes.Length);
                }
                catch (IOException ex)
                {

                    MessageBox.Show(m_SerialPort.PortName + "\r\n" + ex.Message);    // disappeared
                }
            }
            else MessageBox.Show(m_SerialPort.PortName + " is disconnected.");
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {

            Thread.Sleep(100);

            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();

            Console.WriteLine(indata);

            int idx = indata.IndexOf(",");
            int idx2 = indata.LastIndexOf(",");

            string str1 = indata.Substring(idx + 1, idx2 - idx - 1);
            string str2 = indata.Substring(idx2 + 1, indata.Length - idx2 - 3);

            bIsLaserCheckOk = false;

            if (str1.IndexOf("99.998") == -1 && str2.IndexOf("99.998") == -1)
            {

                float temp1 = Math.Abs(float.Parse(str1));
                float temp2 = Math.Abs(float.Parse(str2));

                if (Math.Abs(temp1 - temp2) < m_flDiffLimit) bIsLaserCheckOk = true;
            }

            if (ReceiveLaserDataEvent != null) ReceiveLaserDataEvent();
        }
    }
}
