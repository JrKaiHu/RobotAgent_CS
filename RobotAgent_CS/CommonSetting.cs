using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;

namespace RobotAgent_CS
{
    class CommonSetting
    {

        // for XML +
        private string m_XmlFilePath;
        private XDocument m_MyXDoc;
        // for XML -

        // Network +
        private string m_StrCPIPAddr;
        private int m_nCPPortNum;
        private string m_StrArmIPAddr;
        private int m_nArmPortNum;
        private int m_nWaitTimeOut;
        // Network -                

        // Camera +
        // Camera -            

        public CommonSetting()
        {

            m_XmlFilePath = System.Environment.CurrentDirectory + "\\common.xml";
            m_MyXDoc = XDocument.Load(m_XmlFilePath);

            // Network +

            XElement netNode = m_MyXDoc.Root.Element("network_settings");

            m_StrCPIPAddr = netNode.Element("CP_IP_ADDRESS").Value;
            m_nCPPortNum = int.Parse(netNode.Element("CP_PORT").Value);
            m_StrArmIPAddr = netNode.Element("ARM_IP_ADDRESS").Value;
            m_nArmPortNum = int.Parse(netNode.Element("ARM_PORT").Value);
            m_nWaitTimeOut = int.Parse(netNode.Element("ARM_WAIT_TIMEOUT").Value);

            // Network -
        }

        public List<ComboboxItem> GetDeviceVendorAndType(string strDeviceType)
        {

            IEnumerable<XElement> SubDevices;
            SubDevices = m_MyXDoc.Root.Element(strDeviceType + "s").Descendants(strDeviceType);

            List<ComboboxItem> subDevCBList = new List<ComboboxItem>();

            foreach (XElement subDevice in SubDevices)
                subDevCBList.Add(new ComboboxItem(subDevice.Attribute("Vendor").Value, subDevice.Attribute("Type").Value));

            return subDevCBList;
        }

        // Network +
        public string _strArmIPAddr
        {

            get
            {
                return m_StrArmIPAddr;
            }
        }

        public int _ArmPortNum
        {

            get
            {
                return m_nArmPortNum;
            }
        }

        public string _strCPIPAddr
        {
            get
            {
                return m_StrCPIPAddr;
            }
        }

        public int _CPPortNum
        {

            get
            {
                return m_nCPPortNum;
            }
        }
        public int _nWaitTimeOut
        {
            get
            {
                return m_nWaitTimeOut;
            }
        }
        // Network -

        // Camera +
        // Camera -
    }
}
