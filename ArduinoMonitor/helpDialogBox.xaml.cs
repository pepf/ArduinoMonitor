using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ArduinoMonitor
{
    public partial class helpDialogBox : Window
    {
        public helpDialogBox()
        {
            InitializeComponent();
            helpText.Text = ArduinoMonitor.Resources.moreHelp;
        }


        private void close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
