using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelloDrone_NDI_Send_ConsoleApp
{
    class UDPReceiver
    {

        private readonly int ReceiveStatePort;
        private readonly int ReceiveVideoPort;

        public UDPReceiver(int receiveStatePort, int receiveVideoPort)
        {
            ReceiveStatePort = receiveStatePort;            
			ReceiveVideoPort = receiveVideoPort;    
        }




        private string GetTelloStateString()
        {

            byte[] receiveBuffer = new byte[2048];

            using (UdpClient client = new UdpClient(ReceiveStatePort))
            {
                IPEndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);
                receiveBuffer = client.Receive(ref senderEP);
                
                return Encoding.ASCII.GetString(receiveBuffer); // Converts bytes to string and returns it
            }

        }



        public List<KeyValuePair<string,float>> GetTelloInfoList()
        {
            string telloAllInfoString = GetTelloStateString();
            string[] separatedInfoArray = telloAllInfoString.Split(';');
            var telloAllInfoList = new List<KeyValuePair<string, float>>();

            foreach (string s in separatedInfoArray)
            {
                string title = s.Split(':')[0];
                if (title != "\r\n")
                { 
                    float value = float.Parse(s.Split(':')[1]);
                
                    telloAllInfoList.Add(new KeyValuePair<string, float>(title,value));
                }
            }

            
            return telloAllInfoList;
        }




        public float GetTelloInfoValue(string title)
        {
            var infoList = GetTelloInfoList();
            return infoList.First(kvp => kvp.Key == title).Value;            
        }



        public string GetTelloInfoValueString(string title)
        {
            return GetTelloInfoValue(title).ToString();
        }





		public byte[] GetVideoData()
        {

            byte[] receiveBuffer = new byte[2048];

            using (UdpClient client = new UdpClient(ReceiveVideoPort))
            {
                IPEndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);
                receiveBuffer = client.Receive(ref senderEP);
                
                return receiveBuffer; 
            }

        }




    }
}
