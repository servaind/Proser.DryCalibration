
using Modbus.IO;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unme.Common;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.IO
{
    public class SerialPortAdapter : Modbus.IO.IStreamResource
    {
        private const string NewLine = "\r\n";
        private SerialPort serialPort;

        public SerialPortAdapter(SerialPort serialPort)
        {
            this.serialPort = serialPort;
            this.serialPort.NewLine = NewLine;

        }

        public int InfiniteTimeout
        {
            get { return SerialPort.InfiniteTimeout; }
        }

        public int ReadTimeout
        {
            get { return serialPort.ReadTimeout; }
            set { serialPort.ReadTimeout = value; }
        }

        public int WriteTimeout
        {
            get { return serialPort.WriteTimeout; }
            set { serialPort.WriteTimeout = value; }
        }

        public void DiscardInBuffer()
        {
            try
            {
                serialPort.DiscardInBuffer();
            }
            catch { }
         
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return serialPort.Read(buffer, offset, count);
            }
            catch (TimeoutException e)
            {
                throw new TimeoutException(e.Message);
            }
            catch (Exception) 
            {
                return 0;
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            serialPort.Write(buffer, offset, count);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                serialPort?.Dispose();
                serialPort = null;
            }
        }
    }
}
