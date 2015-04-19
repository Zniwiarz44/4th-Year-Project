using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Project_v1._0._2
{
    class PacketWrapper
    {
        public RawCapture p;

        public int Count { get; private set; }
        public PosixTimeval Timeval { get { return p.Timeval; } }
        public LinkLayers LinkLayerType { get { return p.LinkLayerType; } }
        public int Length { get { return p.Data.Length; } }
        //ref http://www.cisco.com/univercd/cc/td/doc/product/lan/trsrb/frames.htm#xtocid12
        public string Mac_Origen { get { return getValores(6, 11, p); } }
        public string Plataforma { get { return getEntre2Valores("0006", "0002", p); } }
        public string IP { get { return getEntre2ValoresIP("cc00", "0003", p); } }
        public string Port_Que_Envia_CDP { get { return getEntre2Valores("0003", "0004", p).Substring(2); } }
        public string Duplex { get { return getValorDuplex("000b", p); } }

        public string getEntre2Valores(string valor1, string valor2, RawCapture datos)
        {
            string cadena = "";
            string subcadena = "";
            for (int i = 0; i < datos.Data.Length - 1; i++)
            {
                cadena += String.Format("{0:x2}", datos.Data.GetValue(i));
            }
            try
            {
                int pos1 = cadena.IndexOf(valor1, 64);
                int pos2 = cadena.IndexOf(valor2);
                int incremento = 64;
                while (pos2 < pos1 || incremento >= cadena.Length)
                {
                    pos2 = cadena.IndexOf(valor2, incremento);
                    incremento = incremento + 2;
                }

                int lon = valor1.Length;
                subcadena = cadena.Substring(pos1 + lon, pos2 - pos1 - lon);
                subcadena = HexString2Ascii(subcadena);
                return subcadena;
            }
            catch { return "????"; }

        }

        public string getValorDuplex(string valor1, RawCapture datos)
        {
            string cadena = "";
            string subcadena = "";
            for (int i = 0; i < datos.Data.Length - 1; i++)
            {
                cadena += String.Format("{0:x2}", datos.Data.GetValue(i));
            }
            try
            {
                int pos1 = cadena.IndexOf(valor1, 64);
                try { subcadena = cadena.Substring(pos1 + 8, 2); }
                catch { subcadena = "Half (¿duplex mismatch?)"; }
                if (subcadena.Equals("00")) subcadena = "Half";
                if (subcadena.Equals("01")) subcadena = "Full";
                return subcadena;
            }
            catch { return "Duplex ????"; }

        }

        public string getEntre2ValoresIP(string valor1, string valor2, RawCapture datos)
        {
            //int=0;
            string cadena = "";
            string subcadena = "";
            for (int i = 0; i < datos.Data.Length - 1; i++)
            {
                cadena += String.Format("{0:x2}", datos.Data.GetValue(i));
            }
            try
            {
                int pos1 = cadena.IndexOf(valor1, 64);
                int pos2 = cadena.IndexOf(valor2);
                int lon = valor1.Length;
                subcadena = (cadena.Substring(pos1 + lon, pos2 - pos1 - lon)).Substring(2);
                string[] lasips = subcadena.Split(new String[] { "cc00" }, StringSplitOptions.RemoveEmptyEntries);
                string devolver = "";
                //subcadena = subcadena.Substring(2);
                foreach (string elemento in lasips)
                {
                    char[] componentes = elemento.ToCharArray();
                    string ipa = (int.Parse((componentes[0].ToString() + componentes[1].ToString()), NumberStyles.HexNumber)).ToString();
                    string ipb = (int.Parse((componentes[2].ToString() + componentes[3].ToString()), NumberStyles.HexNumber)).ToString();
                    string ipc = (int.Parse((componentes[4].ToString() + componentes[5].ToString()), NumberStyles.HexNumber)).ToString();
                    string ipd = (int.Parse((componentes[6].ToString() + componentes[7].ToString()), NumberStyles.HexNumber)).ToString();
                    devolver += ipa + "." + ipb + "." + ipc + "." + ipd + " ";
                }
                return devolver;
            }
            catch { return "IP ???"; }

        }


        public string getValores(int indice1, int indice2, RawCapture datos)
        {
            string cadena = "";
            for (int i = indice1; i <= indice2; i++)
            {
                string hex = String.Format("{0:x2}", datos.Data.GetValue(i));
                cadena += hex + ":";
            }
            return cadena.Substring(0, cadena.Length - 1);
        }

        public string getPlataforma(int indice2, RawCapture datos)
        {
            string cadena = "";
            int i = indice2;
            string compara = "";

            while (!compara.Equals("0006000e"))
            {
                //00  06 00 0e
                string hex0 = String.Format("{0:x2}", datos.Data.GetValue(i));
                string hex1 = String.Format("{0:x2}", datos.Data.GetValue(i + 1));
                string hex2 = String.Format("{0:x2}", datos.Data.GetValue(i + 1));
                string hex3 = String.Format("{0:x2}", datos.Data.GetValue(i + 1));
                compara = hex0 + hex1;
                if (compara.Equals("00020011")) { MessageBox.Show(cadena); break; }
                cadena += hex0;
                i++;
            }

            return HexString2Ascii(cadena);
        }

        private string HexString2Ascii(string hexString)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= hexString.Length - 2; i += 2)
            {
                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(hexString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber))));
            }
            return sb.ToString();
        }

        public PacketWrapper(int count, RawCapture p)
        {
            this.Count = count;
            this.p = p;
        }
    }
}
