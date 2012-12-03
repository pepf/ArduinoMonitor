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
        private Point outdragPoint;
        private Point indragPoint;
        private Point bardragPoint;
        private int barPos, inPos, outPos; //store old position of the scrollbar, in and outpoint
        private Rectangle dragged;
        public TranslateTransform pan;
        public ScaleTransform scale;
        public ViewParams view;
        public bool isConnected = false;
        public double anchorIn, anchorOut = 0;
        
        private string[] ports;

        public MainWindow()
        {
            view = new ViewParams();
            InitializeComponent();
            scale = new ScaleTransform(1, 1, 0, 0);
            pan = new TranslateTransform(0, plot.Height);
            signals = new SignalCollection(this);
                signals.scaleSignalStrokes(scale);
                signals.updateLabels();
            resetTransform();
            ports = SerialPort.GetPortNames();
            
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
            //Change cursor
            Cursor closedhand = new Cursor(new System.IO.MemoryStream(ArduinoMonitor.Resources.closedhand));
            dragBar.Cursor = closedhand;
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


            }
            catch (System.FormatException excep)
            {
                control.Foreground = Brushes.Red;
                Console.WriteLine(excep.Message);
            }
            catch (NullReferenceException) { }

        }
   
        //Reset view to basic view
        private void resetView(object sender, RoutedEventArgs e)
        {
            resetTransform();
        }
        
        //Reset graph transform
        public void resetTransform(Boolean useSlider = false)
        {
            Rect rectangleBounds = new Rect();
            rectangleBounds = plot.RenderTransform.TransformBounds(new Rect(0, 0, plot.Width, plot.Height));


            //Add transformgroup to plot
            double yscale = plot.Height / view.YMAX; //YMAX is maximum plot value received
            double xscale = plot.Width / view.XMAX; //XMAX is total ammount of plotted points
            Matrix m = new Matrix(1, 0, 0, 1, 0, 0);
            if (useSlider)
            {
                double maxVal = zoomBar.ActualWidth - outPoint.Width;
                double outP = Canvas.GetLeft(outPoint); //points position relative to the scrollbar
                double inP = Canvas.GetLeft(inPoint);
                double delta = (outP-inP);
                double factor = (maxVal/delta) * xscale;
                double mappedinP = (inP / maxVal) * view.XMAX;

                anchorOut = (outP / maxVal) * view.XMAX;
                anchorIn = (inP / maxVal) * view.XMAX;
                double center = (anchorOut +anchorIn)/2;

                m.Translate(-anchorIn, 0); //Move graph to inpoint
                m.ScaleAt(factor, -yscale,0,0); //scale around the inpoint, with a factor so that outpoint is 600px further away
                m.Translate(0, plot.Height); //to compensate the flipped graph, move it back down
            }
                scale = new ScaleTransform(m.M11, m.M22, 0, 0); //save scale factors in a scaletransform for reference
                signals.scaleSignalStrokes(scale); //Scale the plotlines to compensate for canvas scaling
                MatrixTransform matrixTrans = new MatrixTransform(m); //Create matrixtransform
                plot.RenderTransform = matrixTrans; //Apply to canvas
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

        //Close the window
        private void closeWindow(object sender, RoutedEventArgs e)
        {
            mainWindow.Close();
        }

        //Minimize Window
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
                //updateTransform(this.pan, true);
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

        //MouseDown Drag in- or outPoint 
        private void dragZoom_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            Rectangle point = (Rectangle)sender;
            Mouse.Capture(point, CaptureMode.Element);
            dragged = point;
            point.Fill = Brushes.DimGray;
            if (point.Name == "inPoint") { indragPoint = e.GetPosition(zoomBar); }
            else if (point.Name == "zoomBar_Bar") { //Code to deal with draggin the bar itself
                bardragPoint = e.GetPosition(zoomBar);
                barPos = (int) Canvas.GetLeft(zoomBar_Bar);
                inPos = (int)Canvas.GetLeft(inPoint);
                outPos = (int)Canvas.GetLeft(outPoint);
            }
            else { outdragPoint = e.GetPosition(zoomBar); }
            //Change cursor
            Cursor closedhand = new Cursor(new System.IO.MemoryStream(ArduinoMonitor.Resources.closedhand));
            point.Cursor = closedhand;


        }

        //MouseUp Drag in- or outPoint
        private void dragZoom_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Rectangle point = (Rectangle)sender;
            point.Fill = Brushes.WhiteSmoke;
            dragged = null;
            Mouse.Capture(null);
        }

        private void dragZoom_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && dragged != null)
            {
                Rectangle point = (Rectangle)sender;
                Point newPoint = e.GetPosition(zoomBar);
                if (newPoint.X < 10) { newPoint.X = 10; }
                if (newPoint.X > zoomBar.ActualWidth -10) { newPoint.X = zoomBar.ActualWidth-10; }
                if (dragged.Name == "inPoint") {
                    if (newPoint.X < Canvas.GetLeft(outPoint) - point.Width/2)
                    {
                        Canvas.SetLeft(dragged, newPoint.X - 10);

                        
                    }
                }
                else if (dragged.Name == "zoomBar_Bar")
                {
                    double deltaX = newPoint.X - bardragPoint.X;
                    double barLeft = barPos + deltaX;
                    double inLeft = inPos + deltaX;
                    double outLeft = outPos + deltaX;
                    if (barLeft < 0) { barLeft = 0; inLeft = 0; outLeft = dragged.Width; }
                    if (barLeft + dragged.Width > zoomBar.ActualWidth) { barLeft = zoomBar.ActualWidth - dragged.Width; inLeft = zoomBar.ActualWidth - dragged.Width; outLeft = zoomBar.ActualWidth - 20; }
                    Canvas.SetLeft(dragged, barLeft);
                    Canvas.SetLeft(inPoint, inLeft);
                    Canvas.SetLeft(outPoint, outLeft);

                }
                else
                { //outpoint
                    if (newPoint.X > Canvas.GetLeft(inPoint) + point.Width)
                    {
                        Canvas.SetLeft(dragged, newPoint.X - 10);
                    }
                }

                if (dragged.Name == "inPoint" || dragged.Name == "outPoint")
                { //if draggin in and outpoints, update bar along
                    Canvas.SetLeft(zoomBar_Bar, Canvas.GetLeft(inPoint));
                    zoomBar_Bar.Width = Canvas.GetLeft(outPoint) - Canvas.GetLeft(inPoint);
                }
            }
            
        }
        private void dragZoom_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle point = (Rectangle)sender;
            point.Fill = Brushes.Gray;
            //Change cursor
            Cursor closedhand = new Cursor(new System.IO.MemoryStream(ArduinoMonitor.Resources.openhand));
            point.Cursor = closedhand;
        }

        private void dragZoom_MouseLeave(object sender, MouseEventArgs e)
        {
            Rectangle point = (Rectangle)sender;
            if (point.Name == "zoomBar_Bar") { point.Fill = Brushes.CadetBlue; }
            else
            {
                point.Fill = Brushes.WhiteSmoke;
            }
            dragged = null;
            Mouse.Capture(null);
        }

        private void dragBar_MouseEnter(object sender, MouseEventArgs e)
        {
            Cursor openhand = new Cursor(new System.IO.MemoryStream(ArduinoMonitor.Resources.openhand));
            dragBar.Cursor = openhand;
        }

        private void zoomBar_Bar_MouseUp(object sender, MouseButtonEventArgs e)
        { //End
            Rectangle point = (Rectangle)sender;
            point.Fill = Brushes.CadetBlue;
            dragged = null;
            Mouse.Capture(null);
            Cursor open = new Cursor(new System.IO.MemoryStream(ArduinoMonitor.Resources.openhand));
            point.Cursor = open;
        }

        private void zoomBar_Bar_Initialized(object sender, EventArgs e)
        { //Make rectangle stretch between in and out points
            Rectangle bar = (Rectangle)sender;
            Canvas.SetLeft(bar,inPoint.Width);
            bar.Width = Canvas.GetLeft(outPoint) - Canvas.GetLeft(inPoint) - inPoint.Width;
        }


    }
}
