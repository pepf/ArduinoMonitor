using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace ArduinoMonitor
{
    class Signal
    {
        public List<int> values;
        private StreamGeometry geometry;
        private MainWindow window;
        private Path line;
        private Ellipse pointer;
        public CheckBox check;
        public String name;
        private ViewParams view;

        public Signal(MainWindow main, bool fake=false)
        {
            this.view = main.view; //Refer to the view parameters
            values = new List<int>(0);
            window = main;
            line = new Path(); //Path, to be filled with streamgeometry
            line.Stroke = Brushes.Red;
            line.SnapsToDevicePixels = true;
            line.Visibility = Visibility.Visible;
            line.RenderTransform = new ScaleTransform(1.0, view.LINESCALE);
            createGeometry();
            window.plot.Children.Add(line);

            //Create pointer
            pointer = new Ellipse();
            pointer.Fill = line.Stroke;
            pointer.Width = 5;
            pointer.Height = 5;
            pointer.Name = "signalPointer";
            window.plot.Children.Add(pointer);
            setThickness(1 / (main.plot.Height / view.YMAX));
            Canvas.SetLeft(pointer, -10);
            Canvas.SetTop(pointer, -10);


            if (fake) { //Create dummy graph
                Random random = new Random();
                //Fill the signal with 200 random data points
                for (int i = 0; i < 2000; i++)
                {
                    int randomNumber = (int) Math.Floor(500 * Math.Sin(i * (Math.PI / 180))) + 500;
                    addValue(randomNumber);
                }
            }

        }

        //Add value to the signal and redraw
        public void addValue(int inputValue) //Probably ranges from 0-1024
        {
            values.Add(inputValue);
            if (values.Count() > view.XMAX) {
                view.XMAX = values.Count();
            }
            createGeometry();
        }

        //Redraw the streamgeometry
        private void createGeometry() {
            // Create a StreamGeometry to use to specify myPath.
            geometry = new StreamGeometry();
            // Open a StreamGeometryContext that can be used to describe this StreamGeometry  
            // object's contents. 
            using (StreamGeometryContext geo = geometry.Open())
            {
                double xmax = window.plot.Width/window.scale.ScaleX; //views xmax
                int xres = (int)Math.Ceiling(xmax / window.plot.Width);
                double xScale = window.scale.ScaleX;
                geo.BeginFigure(new Point(0, 0), false, false);
                for (int i = 0; i < xmax; i+=xres)
                {
                    int value;
                    if (i < (values.Count() - 1))
                    {
                        value = values[i];
                    }
                    else { break; }
                    geo.LineTo(new Point(i, value), true, false);
                    i++;
                }
            }
            geometry.Freeze(); //Freeze to free resources
            line.Data = geometry;
        }

        //Clear all data stored in the signal
        public void clear()
        {
            while (values.Count > 0)
            {
                values.RemoveAt(0);
            }
            view.XMAX = window.plot.Width;
            createGeometry();
        }

        //Return latest signal
        public int latest()
        {
            if (values.Count > 0) { return values[values.Count - 1]; } else { return 0; }
        }
        //Return second to latest signal
        public int previous()
        {
            if (values.Count > 1) { return values[values.Count - 2]; } else { return 0; }
        }
        //Set brush for the stroke and pointer
        public void setStroke(Brush brush)
        {
            line.Stroke = brush;
            pointer.Fill = brush;
            brush.Freeze();
        }
        //Get Brush for the stroke and pointer
        public Brush getStroke()
        {
            return line.Stroke;
        }
        //Get line thickness 
        public double getThickness()
        {
            return line.StrokeThickness;
        }
        
        //Set line thickness and pointer size
        public void setThickness(double thickness)
        {
            line.StrokeThickness = thickness;

            ScaleTransform scaletrans = window.scale;
            ScaleTransform scale = new ScaleTransform();
            scale.ScaleX = 1/scaletrans.ScaleX;
            scale.ScaleY = 1/scaletrans.ScaleY;
            pointer.RenderTransform = scale;

        }
        
        //Set line visibility
        public void setVisibility(bool vis) {
            if (vis == true) { line.Visibility = Visibility.Visible; pointer.Visibility = Visibility.Visible; }
            else { line.Visibility = Visibility.Hidden; pointer.Visibility = Visibility.Hidden; }
        }

        //Get value at particular x coordinate
        public int getValue(int x, bool mapped=false) {
            if (x < values.Count && x>=0)
            {
                return values[x];

            }
            else return 0;
        }

        //Move pointer to the right position on the line
        public void movePointer(int x, int y)
        {
            Canvas.SetLeft(pointer, x);
            Canvas.SetTop(pointer, y-(int)Math.Floor(pointer.Height/2));
        }
    }

}
