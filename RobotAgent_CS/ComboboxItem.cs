using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobotAgent_CS
{
    class ComboboxItem
    {
        public string strVendor;
        public string strType;

        public ComboboxItem(string strVendor, string strType)
        {
            this.strVendor = strVendor;
            this.strType = strType;
        }

        public override string ToString()
        {
            return strVendor + " : " + strType;
        }
    }
}