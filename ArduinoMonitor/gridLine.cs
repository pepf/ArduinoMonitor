using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using ArduinoMonitor;

namespace System.Windows.Shapes
{
    class gridLine
    {
        public TextBlock label;
        public Path line;
        private int value;
        private Canvas canvas;
        private ViewParams view;
        public string direction;
        private StreamGeometry geometry;

        //gridLine constructor
        public gridLine(int value, string dir, MainWindow window)
        {
            // Create a path to draw a geometry with.
            line = new Path();
            line.Stroke = Brushes.WhiteSmoke;
            line.StrokeThickness = 0.5;
            line.SnapsToDevicePixels = true;

            //this.line = new Line();
            this.canvas = window.plot;
            this.view = window.view;
            this.direction = dir;

            creategeometry(value);

            if (dir == LineDirection.Horizontal)
            {
                TextLabel(5, value, value.ToString());
            }
            else
            {
                TextLabel(value + 1, 5, value.ToString());
            }


            canvas.Children.Add(line);
        }

        //Redraw data
        private void creategeometry(int value)
        {
            // Create a StreamGeometry to use to specify myPath.
            geometry = new StreamGeometry();
            geometry.Open();
            // Open a StreamGeometryContext that can be used to describe this StreamGeometry  
            // object's contents. 
            using (StreamGeometryContext geo = geometry.Open())
            {
                if (direction == LineDirection.Horizontal)
                {
                    geo.BeginFigure(new Point(0, value), false, false);
                    geo.LineTo(new Point(view.XMAX, value), true, false);

                }
                else if (direction == LineDirection.Vertical)
                {
                    geo.BeginFigure(new Point(value, 0), false, false);
                    geo.LineTo(new Point(value, view.YMAX), true, false);
                }
            }
            geometry.Freeze();
            line.Data = geometry;
        }

        //Change gridlines coordinates
        public void changeValue(int val, int realVal)
        {
            this.value = val;
            if (direction == LineDirection.Horizontal)
            {
                creategeometry(val);
                Canvas.SetTop(label, val); //change y of label along
            }
            else
            {
                creategeometry(val);
                Canvas.SetLeft(label, val);
            }
            label.Text = realVal.ToString(); //Change text of the label itself
        }

        //Set gridline thickness
        public void setThickness(double factor) {
            double thickness = 0.5/factor;
            line.StrokeThickness= thickness;
            label.FontSize = 16 * thickness;
        }

        //Hide the label
        public void hideLabel()
        {
            canvas.Children.Remove(this.label);
        }

        //Create the label
        private void TextLabel(double x, double y, string text)
        {
            TextBlock textBlock = new TextBlock();
            this.label = textBlock;
            textBlock.Text = text;
            textBlock.FontSize = 8;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.Foreground = System.Windows.Media.Brushes.WhiteSmoke;
            textBlock.RenderTransformOrigin = new Point(0.5,0.5);
            ScaleTransform scale = new ScaleTransform();
            scale.ScaleY = -1;
            textBlock.RenderTransform = scale;
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            canvas.Children.Add(label);
        }

        //Remove line
        internal void remove()
        {
            canvas.Children.Remove(line);
            canvas.Children.Remove(label);
        }
    }

    //Class created in order to create static strings describing direction
    static class LineDirection
    {
        public static string Horizontal
        {
            get { return (string)"horizontal".ToString(); }
        }
        public static string Vertical
        {
            get { return (string)"vertical".ToString(); }
        }
    }
}
