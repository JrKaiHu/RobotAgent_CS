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
    class BarcodeReader
    {
        private SerialPort m_SerialPort;

        public string m_strSerialNumber;
        public string m_strMacAddress;

        public delegate void ReceiveBRDataEventHandler();
        public event ReceiveBRDataEventHandler ReceiveBRDataEvent;

        public bool m_bIsEnable;
        public bool m_bIsTopBarcode;
        public int m_nReadTimeOut;
        private int m_nBaudRate;
        private int m_nDataBits;
        private string m_strParity;
        private string m_strStopBits;
        private string m_strPortName;

        public BarcodeReader(ComboboxItem barcodeCBItem)
        {

            XDocument myXDoc = XDocument.Load("common.xml");

            XElement xeBarcodes = myXDoc.Root.Element("barcodes");

            m_bIsEnable = int.Parse(xeBarcodes.Attribute("IsEnable").Value) == 1 ? true : false;

            if (m_bIsEnable)
            {

                IEnumerable<XElement> allBarcodes = xeBarcodes.Descendants("barcode");

                foreach (XElement barcode in allBarcodes)
                {

                    if (barcodeCBItem.strVendor.Equals(barcode.Attribute("Vendor").Value) &&
                         barcodeCBItem.strType.Equals(barcode.Attribute("Type").Value))
                    {

                        m_nReadTimeOut = int.Parse(barcode.Element("READ_TIMEOUT").Value);

                        int nIsTopBarcode = int.Parse(barcode.Element("IS_TOP_BARCODE").Value);
                        m_bIsTopBarcode = nIsTopBarcode == 1 ? true : false;

                        m_nBaudRate = int.Parse(barcode.Element("BAUD_RATE").Value);
                        m_nDataBits = int.Parse(barcode.Element("DATA_BITS").Value);
                        m_strParity = barcode.Element("PARITY").Value;
                        m_strStopBits = barcode.Element("STOP_BITS").Value;
                        m_strPortName = barcode.Element("PORT_NAME").Value;
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
                }
                catch (Exception ex)
                {

                    m_bIsEnable = false;
                    Console.WriteLine(ex.ToString());
                    //MessageBox.Show(serialPort.PortName + "\r\n" + ex.Message);
                }
            }
        }

        public void CloseBR()
        {

            m_SerialPort.Close();
        }

        public void ReadFromBR()
        {

            string lon = "\x02LON\x03";
            Byte[] sendBytes = ASCIIEncoding.ASCII.GetBytes(lon);

            if (m_SerialPort.IsOpen)
            {

                try
                {

                    m_SerialPort.Write(sendBytes, 0, sendBytes.Length);
                }
                catch (IOException ex)
                {

                    MessageBox.Show(m_SerialPort.PortName + "\r\n" + ex.Message);
                }
            }
            else MessageBox.Show(m_SerialPort.PortName + " is disconnected.");
        }

        public void ShutDownBR()
        {

            string loff = "\x02LOFF\x03";
            Byte[] sendBytes = ASCIIEncoding.ASCII.GetBytes(loff);


            if (m_SerialPort.IsOpen)
            {

                try
                {

                    m_SerialPort.Write(sendBytes, 0, sendBytes.Length);
                }
                catch (IOException ex)
                {

                    MessageBox.Show(m_SerialPort.PortName + "\r\n" + ex.Message);
                }
            }
            else MessageBox.Show(m_SerialPort.PortName + " is disconnected.");
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {

            Thread.Sleep(100);

            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();

            if (indata.Equals("ERROR\r")) return;

            int idx = indata.IndexOf(",");
            m_strSerialNumber = indata.Substring(0, idx);
            m_strMacAddress = indata.Substring(idx + 1, indata.Length - idx - 2);

            if (ReceiveBRDataEvent != null) ReceiveBRDataEvent();
        }
    }
}
