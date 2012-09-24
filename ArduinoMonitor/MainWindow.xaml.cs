using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.IO;


namespace ArduinoMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SerialPort arduinoPort;
        public Regex serialpattern;
        private SignalCollection signals;
        private Point dragPoint;
        public TranslateTransform pan;
        public ScaleTransform scale;
        public ViewParams view;
        public bool isConnected = false;
        
        private string[] ports;

        public MainWindow()
        {
            view = new ViewParams();
            InitializeComponent();
            resetTransform();
            signals = new SignalCollection(this);
            ports = SerialPort.GetPortNames();
            this.MouseLeftButtonDown += new MouseButtonEventHandler(dragWindow);
            
            Console.WriteLine("ports:");
            foreach(string port in ports){
                comportList.Items.Add(port.ToString());
                Console.WriteLine(port);
            }
            arduinoPort = new SerialPort();
            arduinoPort.Parity = Parity.None;
            arduinoPort.StopBits = StopBits.One;
            arduinoPort.DataBits = 8;
            arduinoPort.BaudRate = 9600;
            arduinoPort.ReadTimeout = 200;
            if (comportList.Items.Count > 0)
            {
                arduinoPort.PortName = comportList.Items[0].ToString();
            }
            else
            {
                Console.WriteLine("No ports available");
                connectButton.IsEnabled = false;
            }


        }

        void dragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        //Connecting arduino
        public void arduinoConnect(object sender, RoutedEventArgs e)
        {
            string content = connectButton.Content.ToString();
            if (content.Equals("Disconnect")) //Means disconnect
            {
                signals.closePort();
                connectButton.Content = "Connect";
                connectButton.IsEnabled = true;
                ExportBtn.IsEnabled = false;
                isConnected = false;
                return;
            }
            
            connectButton.Content = "Connecting";
            connectButton.IsEnabled = false;
            comportList.IsEnabled = false;
            try
            {
                signals.arduinoPort = arduinoPort;
                signals.openPort();
                connectButton.Content = "Disconnect";
                connectButton.IsEnabled = true;
                ExportBtn.IsEnabled = true;
                isConnected = true;
            }
            catch (Exception except)
            {
                Console.WriteLine(except.ToString());
                connectButton.Content = "Connect";
                connectButton.IsEnabled = true;
                comportList.IsEnabled = true;
                isConnected = false;
            }
        }

        //Change comport
        private void changePort(object sender, SelectionChangedEventArgs e)
        {
            if (comportList.SelectedItem != null)
            {
                arduinoPort.PortName = comportList.SelectedItem.ToString();
            }
            connectButton.IsEnabled = true;
        }

        //Refresh comports
        private void refreshPorts(object sender, RoutedEventArgs e)
        {
            for (int i=0; i<comportList.Items.Count; i++)
            {
                    comportList.Items.RemoveAt(i);
            }
            ports = SerialPort.GetPortNames();
            Console.WriteLine("ports:");
            foreach (string port in ports)
            {
                comportList.Items.Add(port.ToString());
                Console.WriteLine(port);
            }
        }
        
        //MouseEnter: Display specific coordinates
        private void specific(object sender, MouseEventArgs e)
        {
            Canvas plot = mainWindow.plot;
            Point mousepos = e.GetPosition(plot);
            signals.drawmouse = true;
            signals.mousePosition = mousepos;
            signals.updatePlot();
            //Console.WriteLine(mousepos.X.ToString() + " - " + mousepos.Y.ToString());
        }

        //MouseLeave: Stop drawing
        private void disableDraw(object sender, MouseEventArgs e)
        {
            signals.drawmouse = false;
        }

        //Fire when adjusting grid resolution
        private void GridresChanged(object sender, TextChangedEventArgs e)
        {
            TextBox control = (TextBox)sender;
            control.Foreground = Brushes.Black;
            try
            {
                int value = int.Parse(control.Text);
                if (value < 10) { throw new System.FormatException("Can't be lower then 10"); }
                if (control.Name == "gridXres")
                {
                    signals.xRes = value;
                }
                else if (control.Name == "gridYres")
                {
                    signals.yRes = value;
                }

                signals.updatePlot();

            }
            catch (System.FormatException excep)
            {
                control.Foreground = Brushes.Red;
                Console.WriteLine(excep.Message);
            }
            catch (NullReferenceException) { }

        }

        //Zoom in and zoom out
        private void scrollViewbox(object sender, MouseWheelEventArgs e)
        {
            ScaleTransform scale = new ScaleTransform();
            ScaleTransform oldscale = this.scale;
            
            double delta = (double)e.Delta / 1000;
            Point cursor = e.GetPosition(bgplot);
            Console.WriteLine(cursor.ToString());
            Matrix m = this.scale.Value;
            //Event that scrolls the viewbox, to zoom in on the graph
            double zF = 1 + delta;
            if (e.Delta > 0)
            {
                Console.WriteLine("Zooming the viewbox in with " + e.Delta.ToString());
                m.ScaleAtPrepend(zF, zF, cursor.X, cursor.Y);
            }
            else
            {
                Console.WriteLine("Zooming the viewbox out with " + e.Delta.ToString());
                m.ScaleAtPrepend(zF, zF, cursor.X, cursor.Y);
            }

            scale.ScaleX = m.M11;
            scale.ScaleY = m.M22;
            this.pan.X += m.OffsetX;
            this.pan.Y += m.OffsetY;
            this.scale = scale; //write back to class property
            updateTransform(this.pan);
        }

        //Drag event
        private void drag(object sender, MouseButtonEventArgs e)
        {
            Viewbox viewbox = (Viewbox) sender;
            dragPoint = e.GetPosition(viewbox);
            //Change cursor
            Cursor closedhand = new Cursor(new System.IO.MemoryStream(ArduinoMonitor.Resources.closedhand));
            plot.Cursor = closedhand;
        }

        //Stop dragging; Calculate the amount of drag etc.
        private void dragstop(object sender, MouseButtonEventArgs e)
        {
            Viewbox viewbox = (Viewbox)sender;
            Point newpoint = e.GetPosition(viewbox);
            //calculate movement
            double dx = newpoint.X - dragPoint.X;
            double dy = 0;
           // double dy = newpoint.Y - dragPoint.Y;
            Console.WriteLine("stop drag, moved X:" + dx.ToString() + " - Y:" + dy.ToString());
            TranslateTransform oldpan = this.pan;
            TranslateTransform pan = new TranslateTransform();
            pan.X = oldpan.X +(dx) *(1/scale.ScaleX);
            pan.Y = oldpan.Y + (dy) * (1 / scale.ScaleY);
            this.pan = pan; //Write back to class property
            updateTransform(this.pan);
            plot.Cursor = Cursors.Cross;
        }
        
        //Update rendertransform property
        private void updateTransform(TranslateTransform pan,bool scaleStrokes = true)
        {
            TransformGroup transform = new TransformGroup();
            transform.Children.Add(pan);
            transform.Children.Add(scale);
            plot.RenderTransform = transform;
            view.XMIN = -1*pan.X;
            view.XMAX = view.XMIN + plot.Width/scale.ScaleX;
            //labels.RenderTransform = transform;
            if (scaleStrokes)
            {
                signals.scaleSignalStrokes(scale);
                signals.updateLabels();
            }
        }

        //Reset view to basic view
        private void resetView(object sender, RoutedEventArgs e)
        {
            resetTransform();
            horizontalZoomslider.Value = 100;
        }
        
        //Reset graph transform
        private void resetTransform()
        {
            //Add transformgroup to plot
            double yscale = plot.Height / view.YMAX;
            double xscale = plot.Width / view.XMAX;
            scale = new ScaleTransform(xscale, -yscale, 0,0);
            pan = new TranslateTransform(0, plot.Height);
            updateTransform(this.pan,false);
        }

        //Export sensor data to a .csv file
        private void exportData(object sender, RoutedEventArgs e)
        {
            FileStream fs;
            // Displays a SaveFileDialog so the user can save the export
            SaveFileDialog exportDialog = new SaveFileDialog();
            exportDialog.Filter = "Comma-seperated value|*.csv";
            exportDialog.Title = "Export graph data";
            exportDialog.ShowDialog();
            if (exportDialog.FileName != "")
            {
                // Saves the export via a FileStream created by the OpenFile method.
                if ((fs = (FileStream)exportDialog.OpenFile()) != null)
                {
                    // Code to write the stream goes here.
                    Exporter exporter = new Exporter(signals, exportDialog.FileName);
                    exporter.writeFileStream(fs);
                    fs.Close();
                }
            }

            
        }

        //Panning options
        private void panPreview(object sender, MouseEventArgs e)
        {
            Viewbox viewbox = (Viewbox)sender;
            if (e.RightButton == MouseButtonState.Pressed)
            { //Means user is panning the canvas, provide updated view
                Point newpoint = e.GetPosition(viewbox);
                //calculate movement
                double dx = newpoint.X - dragPoint.X;
                //double dy = newpoint.Y - dragPoint.Y;
                double dy = 0;
                TranslateTransform oldpan = this.pan;
                TranslateTransform newpan = new TranslateTransform();
                newpan.X = oldpan.X + (dx) * (1 / scale.ScaleX);
                newpan.Y = oldpan.Y + (dy) * (1 / scale.ScaleY);              

                updateTransform(newpan);
            }
        }

        //Open up a screen with extra help
        private void moreHelp(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            helpDialogBox dlg = new helpDialogBox(mainWindow);

            // Configure the dialog box
            dlg.Owner = this;

            // Open the dialog box modally
            dlg.Show();
        }

        //Change xmax
        private void xmaxChange(object sender, TextChangedEventArgs e)
        {
            TextBox control = (TextBox)sender;
            control.Foreground = Brushes.Black;
            try
            {
                    int value = int.Parse(control.Text);
                    if (value < 500) { throw new System.FormatException("Can't be lower then 500"); }
                    control.Text = "500";
                    view.XMAX = value;
                    signals.updatePlot();

             }
            catch (System.FormatException excep)
            {
                control.Foreground = Brushes.Red;
                Console.WriteLine(excep.Message);
            }
            catch (NullReferenceException) { }
            
            //Adjust view
            try
            {
                double xscale = plot.Width / view.XMAX;
                scale = new ScaleTransform(xscale, this.scale.ScaleY, 0, 0);
                updateTransform(this.pan, false);
            }
            catch (NullReferenceException) { }
        }


        private void closeWindow(object sender, RoutedEventArgs e)
        {
            mainWindow.Close();
        }

        private void minWindow(object sender, RoutedEventArgs e)
        {
            mainWindow.WindowState = System.Windows.WindowState.Minimized;
            Button btn = (Button)sender;
        }

        //changescale according to zoom slider
        private void vertZoomslider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slide = (Slider)sender;
            double percentage = slide.Value/100;
            try
            {
                double xscale = plot.Width / view.XMAX / percentage;
                this.scale.ScaleX = xscale;
            }
            catch (Exception) { }
        }

        //Clears the current graphs
        private void cleargraphBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (Signal signal in signals.signals)
            {
                signal.clear();
            }
        }


    }
}
