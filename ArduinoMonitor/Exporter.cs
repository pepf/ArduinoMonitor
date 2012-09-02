using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArduinoMonitor
{
    class Exporter
    {
        private string filecontents;
        private string directory = @"C:\";
        private string file;
        public Exporter(SignalCollection signals, String filename)
        {
            //Combine the new file name with the path
            file = System.IO.Path.Combine(directory, filename);
            foreach(Signal signal in signals.signals) 
            {
                filecontents = filecontents + signal.name + ", ";
                foreach (int value in signal.values)
                {
                    filecontents = filecontents + value.ToString() + ", ";
                }
                filecontents = filecontents + "\n";
            }
            //writeFile();
        }

        private void writeFile()
        {
            // Create the file and write to it. 
            // DANGER: System.IO.File.Create will overwrite the file 
            // if it already exists. This can occur even with 
            // random file names. 
            byte[] bytearray = System.Text.Encoding.ASCII.GetBytes("blaat");
            try
            {
                bytearray = System.Text.Encoding.ASCII.GetBytes(filecontents);
            }
            catch (NullReferenceException) { Console.WriteLine("nothing to export"); }
            int i = 0;
            string basename = System.IO.Path.GetFileNameWithoutExtension(file);
            while (System.IO.File.Exists(file))
            {
                string newfilename = basename + i.ToString() + System.IO.Path.GetExtension(file);
                file = System.IO.Path.Combine(directory, newfilename);
                i++;
            }
            if (!System.IO.File.Exists(file))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(file))
                {
                    foreach(byte character in bytearray)
                    {
                        fs.WriteByte(character);
                    }
                }
            }
            else
            {
                throw new SystemException("File already exists");
            }
        }
        public void writeFileStream(System.IO.FileStream fs) {
            byte[] bytearray = System.Text.Encoding.ASCII.GetBytes("blaat");
            try
            {
                bytearray = System.Text.Encoding.ASCII.GetBytes(filecontents);
            }
            catch (NullReferenceException) { Console.WriteLine("nothing to export"); }
            foreach (byte character in bytearray)
            {
                fs.WriteByte(character);
            }
            fs.Close();

        }
    }
}
