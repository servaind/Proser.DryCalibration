using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Proser.DryCalibration.util
{
    public class DataStore<T>
    {
     
        private List<T> data;
        private readonly int capacity;
        private int counter;
        private readonly object objLock;

        public List<T> Data
        {
            get
            {
                List<T> result;
                lock (objLock)
                {
                    result = new List<T>(data);
                }

                return result;
            }
        }


        public DataStore(int capacity)
        {
            objLock = new object();

            data = new List<T>(capacity);
            this.capacity = capacity - 1;
            counter = 0;

            Clear();
        }

        public void Clear()
        {
            lock (objLock)
            {
                data.Clear();
                for (var i = 0; i < capacity; i++) data.Add(default(T));
                counter = 0;
            }
        }

        public void Add(T value)
        {
            lock (objLock)
            {
                if (counter == capacity) counter = 0;

                data[counter] = value;

                counter++;
            }
        }
    }
}
