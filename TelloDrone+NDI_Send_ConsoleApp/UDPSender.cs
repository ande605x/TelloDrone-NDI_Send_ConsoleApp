using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelloDrone_NDI_Send_ConsoleApp
{

    class UDPSender
    {
	
	
        private readonly int Port;
        private readonly string Ip;

        UdpClient client = new UdpClient();


        public UDPSender(string ip, int port)
        {
            Port = port;
            Ip = ip;
            
        }

   






		public bool SendCommandToTello(string command)
		{
			 //using (UdpClient client = new UdpClient())
            {
                IPEndPoint ReceiverEP = new IPEndPoint(IPAddress.Parse(Ip), Port);

                byte[] buffer = Encoding.ASCII.GetBytes(command);
                client.Send(buffer, buffer.Length, ReceiverEP);

                // receive 
                byte[] inBuffer = new byte[2048];
                inBuffer = client.Receive(ref ReceiverEP);
                String incommingStr = Encoding.ASCII.GetString(inBuffer);
                
				if (incommingStr == "ok")
				{
					Console.WriteLine("Connect status: OK");
					return true;
				}
				else
				{
					Console.WriteLine("Error sending command to drone: " + incommingStr);					
					return false;
				}
			}
		}





        

        }

    }

