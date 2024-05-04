using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellGenerator.app.tag
{
    public class LoraLayer
    {
        public string name { get; set; }
        public double[] layers = new double[17];
        public bool isEmpty = false;

        public void SetAll()
        {
            for (int i = 0; i < 17; i++)
            {
                layers[i] = 1;
            }
        }

        public bool IsEqual(LoraLayer l2)
        {
            if(isEmpty)
            {
                return true;
            }
            for(int i=0; i<17; i++)
            {
                if (Math.Abs(layers[i] - l2.layers[i]) > 0.0001)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsAll()
        {
            for (int i = 0; i < 17; i++)
            {
                if (Math.Abs(layers[i] - 1) > 0.0001)
                {
                    return false;
                }
            }
            return true;
        }

        public void CopyFrom(LoraLayer l2)
        {
            l2.layers.CopyTo(layers, 0);
        }

        public void FromString(string s)
        {
            string[] sp = s.Split(",");
            for(int i=0; i<17; i++)
            {
                if(i<sp.Length)
                {
                    layers[i] = double.Parse(sp[i]);
                }
                else
                {
                    layers[i] = i > 0 ? layers[i - 1] : 0;
                }
            }
        }

        public string ToString()
        {
            string rs = "";
            for(int i=0; i<17; i++)
            {
                if(i>0)
                {
                    rs += ",";
                }
                rs += layers[i];
            }
            return rs;
        }
    }
}
