using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;

namespace Trading.Utilities.Indicators
{
    public class SimpleVolume : IIndicator
    {
        public bool[] Increasing { get; private set; }

        public bool[] Decreasing { get; private set; }

        public int[] VolumeLevel { get; private set; }


        public SimpleVolume()
        {
            this.Increasing = new bool[0];
            this.Decreasing = new bool[0];
            this.VolumeLevel = new int[0];
        }

        public void Calculate(DataSet data)
        {
            this.Calculate(data.Volume);
        }

        public void Calculate(double[] data)
        {
            this.Increasing = new bool[data.Length];
            this.Decreasing = new bool[data.Length];
            this.VolumeLevel = new int[data.Length];

            for (int i = 1; i < data.Length; i++)
            {
                this.Increasing[i] = (data[i] > data[i - 1]);
                this.Decreasing[i] = (data[i] < data[i - 1]);

                var prevVol = this.VolumeLevel[i - 1];

                if (this.Increasing[i])
                {
                    if (prevVol < 0)
                        this.VolumeLevel[i] = 1;
                    else
                        this.VolumeLevel[i] = prevVol + 1;
                }
                else if (this.Decreasing[i])
                {
                    if (prevVol > 0)
                        this.VolumeLevel[i] = -1;
                    else
                        this.VolumeLevel[i] = prevVol - 1;
                }
                else
                {
                    this.VolumeLevel[i] = prevVol;
                }
            }
        }

        public string GetOutputString(int index)
        {
            return this.VolumeLevel[index].ToString();
        }
    }
}
