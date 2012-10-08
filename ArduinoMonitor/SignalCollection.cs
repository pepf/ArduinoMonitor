using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using System.Threading;

namespace ArduinoMonitor
{
    class SignalCollection
    {
        public List<Signal> signals;
        private delegate void MethodDelegate();
        private delegate void drawWorkerDelegate();
        private delegate void updatePlotDelegate();
        private MethodDelegate ReadSerialData;
        public SerialPort arduinoPort;
        //private int timeIndex = 0;
        private Dispatcher dataDispatcher;
        private MainWindow window;
        private List<SolidColorBrush> brushes;
        private List<gridLine> horizontals; //horizontal plot lines
        private List<gridLine> verticals; //vertical plot lines
        private gridLine plotlineH;
        private gridLine plotlineV;
        private ViewParams view;

        public int yRes = 50;
        public int xRes = 50;
        private double factor = 1;
        public bool drawmouse = false;
        public Point mousePosition;


        //Constructor
        public SignalCollection(MainWindow uiWindow)
        {
            brushes = new List<SolidColorBrush>();
            brushes.Add(Brushes.MediumVioletRed);
            brushes.Add(Brushes.LightBlue);
            brushes.Add(Brushes.BurlyWood);
            brushes.Add(Brushes.LightPink);
            brushes.Add(Brushes.Yellow);
            brushes.Add(Brushes.LightGreen);
            brushes.Add(Brushes.Gold);
            window = uiWindow;
            dataDispatcher = window.Dispatcher;
            signals = new List<Signal>(0);
            this.view = uiWindow.view; //set initial view params
            //view.XMAX = 2000;
            view.YMIN = 0;
            view.YMAX = 1024;
            view.XMIN = 0;
            view.LINESCALE = 1;
            horizontals = new List<gridLine>(10);
            verticals = new List<gridLine>(10);
            plotlineH = createHLine(0,false);
            plotlineH.hideLabel();
            plotlineV = createVLine(0,false);
            plotlineV.hideLabel();
            preparePlot();

            drawWorkerDelegate w = drawWorker;
            w.BeginInvoke(null, null);

            //addSignal(true);
        }

        //Method to actually open serial port for communication
        public void openPort() 
        {
            arduinoPort.Open();
            arduinoPort.DiscardInBuffer();
            // Delegate for DoUpdate.
            ReadSerialData = new MethodDelegate(DoUpdate);
            arduinoPort.DataReceived += new SerialDataReceivedEventHandler(arduinoPort_DataReceived);
        }

        //Close port on disconnect
        public void closePort()
        {
            try
            {
                arduinoPort.DiscardInBuffer();
                arduinoPort.Close();
            }
            catch (Exception)
            {

            }
            ReadSerialData = null;
            arduinoPort.DataReceived -= arduinoPort_DataReceived;
        }

        //Add new signal to the collection
        private void addSignal(bool fake=false)
        {
            Signal signal;
            if (fake == true)
            {
                signal = new Signal(window,true);
            }
            else
            {
                signal = new Signal(window);
            }
            Random random = new Random();
            int randomNumber = random.Next(0, brushes.Count);
            signal.setStroke(brushes[randomNumber]);
            brushes.Remove(brushes[randomNumber]); //no double colors
            signals.Add(signal);
            Console.WriteLine("Signal: " + signals.Count);
            //Make checkbox
            CheckBox signalcheck = new CheckBox();
            signal.check = signalcheck;
            signal.name = "Signal " + signals.Count().ToString();
            signalcheck.Content = signal.name;
            signalcheck.IsChecked = true;
            signalcheck.Foreground = signal.getStroke();
            signalcheck.FontWeight = System.Windows.FontWeights.Bold;
            signalcheck.Checked += new System.Windows.RoutedEventHandler(signalcheck_Checked);
            signalcheck.Unchecked += new System.Windows.RoutedEventHandler(signalcheck_Checked);
            window.signalToggles.Children.Add(signalcheck);

            if (fake == true)
            {
                updatePlot();
            }
        }
       
        // Detect if new data is received.
        private void arduinoPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            dataDispatcher.Invoke(DispatcherPriority.Normal, ReadSerialData);
        }
        // Process received data.
        // Via delegate ReadSerialData.
        private void DoUpdate()
        {
            // Number of bytes in the receive buffer.
            if (arduinoPort.IsOpen)
            {
                int iNBytes = arduinoPort.BytesToRead;
                int N_BYTES = iNBytes;
                if (iNBytes == N_BYTES)
                {
                    // Get bytes.
                    string line = "";
                    try
                    {
                        line = arduinoPort.ReadLine();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Timeout, closing connection");
                        Thread t = new Thread(closePort);
                        t.Start();
                        window.connectButton.Content = "Connect";
                        window.connectButton.IsEnabled = true;
                        window.ExportBtn.IsEnabled = false;
                        window.isConnected = false;
                        return;
                    }
                    Console.WriteLine(line);
                    MatchCollection matches = Regex.Matches(line, "[\\-0-9\\.]+");
                    if (matches.Count > 0)
                    {
                        while (signals.Count < matches.Count) //Mismatch between input data and data streams, add Signal
                        {
                            addSignal(); //Add signal, checkbox, events etc.
                        }
                        if (matches.Count == signals.Count)
                        {
                            for (int i = 0; i < signals.Count; i++)
                            {
                                Signal signal = signals[i];
                                double inputValue = double.Parse(matches[i].Value, System.Globalization.CultureInfo.InvariantCulture); //Apparantly extremely important in parsing the dot right
                                if (inputValue > view.YMAX) view.YMAX = inputValue;
                                else if (inputValue < view.YMIN) view.YMIN = inputValue;
                                //Console.WriteLine("added value " + matches[i] + " to signal " + i);
                                signal.addValue(inputValue);
                            }
                        }
                    }
                }
            }
        }

        //Event Handler when clicking the checkbox with the signal
        private void signalcheck_Checked(object sender, RoutedEventArgs e)
        {
            //Uncheck/check checkbox
            CheckBox cb = (CheckBox)sender;
            string name = cb.Content.ToString();
            name = Regex.Match(name,"[0-9]+").Value;
            int id = int.Parse(name);
            id -= 1;
            System.Nullable<Boolean> check = cb.IsChecked;
            if (check == true) { signals[id].setVisibility(true); } else { signals[id].setVisibility(false); }
        }

        //Inital drawing of the grid
        private void preparePlot()
        {
            Canvas plot = window.plot;
            //RenderOptions.SetEdgeMode(plot, EdgeMode.Aliased);
            for (int i = 0; i < view.XMAX; i += xRes) //vertical lines
            {
                gridLine line = createVLine(i);
            }
            for (int i = 0; i < view.YMAX; i += yRes) //horizontals lines
            {
               gridLine line = createHLine(i);
            }
        }

        //Worker for delegate, will loop the updatePlot function FOREVER 
        void drawWorker()
        {
            while (true)
            {
                //we use this.Invoke to send information back to our UI thread with a delegate
                //if we were to try to access the text box on the UI thread directly from a different thread, there would be problems
                window.Dispatcher.Invoke(new updatePlotDelegate(updatePlot));
                Thread.Sleep(50); //FPS
            }
        }

        //Function updating the plot graphics
        public void updatePlot()
        {
            //Horizontalls
            int nrlines = (int) Math.Ceiling((double)(view.YMAX - view.YMIN) / yRes);
            try
            {
                int hornr = horizontals.Count;
                while (nrlines > hornr)
                {
                    gridLine line = createHLine(0);
                    hornr = horizontals.Count;
                }
                while (nrlines < hornr)
                {
                    gridLine temp = horizontals[horizontals.Count - 1]; //instance of gridLine to remove
                    temp.remove();
                    horizontals.Remove(temp);
                    hornr = horizontals.Count;
                }
                int count = 0;
                foreach (gridLine line in horizontals)
                {
                    double Yvalue = yRes* count;
                    //line.changeValue((int)Extra.map(Yvalue, (int)view.MIN, (int) view.MAX, 0, (int)window.plot.Height),Yvalue);
                    line.changeValue((int)Yvalue,yRes*count);
                    line.setThickness(factor);
                    count++;
                }
            }
            catch (NullReferenceException) {  }

            //Verticalls
            nrlines = (int)Math.Ceiling((double)(view.XMAX - view.XMIN) / xRes);
            nrlines = Math.Abs(nrlines);
            try
            {
                int vertnr = verticals.Count;
                while (nrlines > vertnr)
                {
                    gridLine line = createVLine(0);
                    vertnr = verticals.Count;
                }
                while (nrlines < vertnr)
                {
                    gridLine temp = verticals[verticals.Count - 1];
                    temp.remove();
                    verticals.Remove(temp);
                    vertnr = verticals.Count;
                }
                int count = 0;
                foreach (gridLine line in verticals)
                {
                    int Xvalue = (int) xRes * count;
                    line.changeValue(Xvalue,Xvalue);
                    line.setThickness(factor);
                    count++;
                }
            }
            catch (NullReferenceException) { }

            //PlotLines based on mouse position
            if (drawmouse == true)
            {
                plotlineH.changeValue((int)mousePosition.Y, (int) mousePosition.Y);
                plotlineV.changeValue((int)mousePosition.X, (int) mousePosition.X);
                //Light part of the graph
                foreach (Signal signal in signals) {
                    double result = signal.getValue((int)mousePosition.X, true);
                    signal.movePointer((int)mousePosition.X,(int) Math.Floor((decimal) result));
                    signal.check.Content = signal.name + ": " + signal.getValue((int)mousePosition.X).ToString();
                }
                plotlineH.setThickness(factor);
                plotlineV.setThickness(factor);
            }
            else
            {
                plotlineH.changeValue(0,0);
                plotlineV.changeValue(0,0);
            }
            foreach (Signal signal in signals) //Draw the real signals
            {
                signal.createGeometry();
            }

            //Fit plot in in canvas, using slider's values
            window.resetTransform(true);
        }

        //Create horizontal gridlines with labels
        private gridLine createHLine(int y, bool addtogroup=true) {
            gridLine hline = new gridLine(y, LineDirection.Horizontal, window);
            if (addtogroup == true) horizontals.Add(hline);
            return hline;
        }

        //Create vertical gridlines with labels
        private gridLine createVLine(int x, bool addtogroup=true)
        {
            gridLine vline = new gridLine(x, LineDirection.Vertical, window);
            if (addtogroup == true) verticals.Add(vline);
            return vline;

        }
    
        //Method for scaling the strokes when zoomed in, so we won't have fat lines
        public void scaleSignalStrokes(ScaleTransform transform) {
            this.factor = transform.ScaleX;
            double thickness = 1/factor;
            //If scaling is 2x, thickness should be 1/2 = 0.5
            foreach(Signal signal in signals) {
                signal.setThickness(thickness);
            }
        }
        public void updateLabels()
        {
            foreach (gridLine line in horizontals)
            {
                line.updateLabel();
            }
            foreach (gridLine line in verticals)
            {
                line.updateLabel();
            }
        }
    }
}
