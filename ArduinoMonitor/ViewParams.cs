using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArduinoMonitor
{
    public class ViewParams
    {
        public double MIN,MAX,XMIN, XMAX, YMIN, YMAX, FACTOR, LINESCALE = 0;
        public ViewParams()
        {
            MIN = 0;
            MAX = 1024;
            XMIN = 0;
            XMAX = 2000;
            YMIN = 0;
            YMAX = 1024;
            FACTOR = 1;
            LINESCALE = 1;
        }
    }
}
