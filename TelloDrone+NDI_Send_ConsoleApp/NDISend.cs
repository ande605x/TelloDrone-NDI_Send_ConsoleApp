using NewTek;
using NewTek.NDI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;



namespace TelloDrone_NDI_Send_ConsoleApp
{
    class NDISend
    {

        private readonly int ReceiveStatePort;
        private readonly int ReceiveVideoPort;
        private readonly string FullTempImagePath;


        public NDISend(int receiveStatePort, int receiveVideoPort, string fullTempImagePath)
        {
            ReceiveStatePort = receiveStatePort;
            ReceiveVideoPort = receiveVideoPort;
            FullTempImagePath = fullTempImagePath;
        }






        static void DrawText(Graphics graphics, String text, float size, FontFamily family, Point origin, StringFormat format, Brush fill)
        {
			// outline pen
			Pen noOutlinePen = new Pen(Color.Black, 0.0f);
		
            // make a text path
            GraphicsPath path = new GraphicsPath();
            path.AddString(text, family, 0, size, origin, format);

            // Draw the pretty text
            graphics.FillPath(fill, path);
            graphics.DrawPath(noOutlinePen, path);
        }

    





        public void NDISendFrame()
        {
            UDPReceiver udpService = new UDPReceiver(ReceiveStatePort,ReceiveVideoPort);
            
            String failoverName = String.Format("{0} (Tello NDI)", System.Net.Dns.GetHostName());

           	using (Sender sendInstance = new Sender("Tello NDI", true, false, null, failoverName))
            {
                using (VideoFrame videoFrame = new VideoFrame(960, 720, (4.0f / 3.0f), 30000, 1200))
                {
                    using (Bitmap bmp = new Bitmap(videoFrame.Width, videoFrame.Height, videoFrame.Stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, videoFrame.BufferPtr))
                    using (Graphics graphics = Graphics.FromImage(bmp))
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                       
                        StringFormat textFormat = new StringFormat();
                        textFormat.Alignment = StringAlignment.Center;
                        textFormat.LineAlignment = StringAlignment.Center;

                        FontFamily fontFamily = new FontFamily("Verdana");

                        int fileNumberInt = 1;
						string fileNumberString;                        

                        for (int frameNumber = 1; frameNumber < 10000; frameNumber++)
                        {                            
                            if (sendInstance.GetConnections(10000) < 1)  		// any connections ?
                            {
                                Console.WriteLine("No current connections, so no rendering needed."); 		// if not: No rendering
                                System.Threading.Thread.Sleep(50);   // Wait a bit, otherwise it will end before you can connect to it 
                            }
                            else
                            {
                                if (fileNumberInt==1)
                                    Program.measureTimeList.Add("Before writing first NDI videoframe", DateTime.Now);
                                if (frameNumber == 25 || frameNumber == 50 || frameNumber == 75)
                                    Program.measureTimeList.Add("Before writing NDI videoframe " + frameNumber + " Filenumber: " + fileNumberInt, DateTime.Now);
                              
                                graphics.Clear(Color.DarkBlue);   // clear with background color
                                                        
                                if (frameNumber <= 9)
                                    fileNumberString = "00" + fileNumberInt;
                                else if (fileNumberInt >= 10 && fileNumberInt <= 99)
                                    fileNumberString = "0" + fileNumberInt;
                                else fileNumberString = fileNumberInt.ToString();

                                string imagefilename = "$tello"+fileNumberString+".bmp";
                                string imagePath = FullTempImagePath + imagefilename;
                                Image image = Image.FromFile(imagePath);
                                
                                graphics.DrawImage(image, 0,0);

                                string infoString = "Tello Drone Info:"+
                                               "\r\n Height: " + udpService.GetTelloInfoValueString("h") + " cm" +
                                               "\r\n Barometer: " + udpService.GetTelloInfoValueString("baro") + " cm" +
                                               "\r\n Battery: " + udpService.GetTelloInfoValueString("bat") + " %" +
                                               "\r\n Temperature: " + udpService.GetTelloInfoValueString("temph") + " °C";

                                DrawText(graphics, infoString, 30.0f, fontFamily, new Point(480, 100), textFormat, Brushes.Lime);

                                string frameAndFile = String.Format("Frame {0}   File: {1}", frameNumber.ToString(), imagefilename);
                                DrawText(graphics, frameAndFile, 30.0f, fontFamily, new Point(480, 350), textFormat, Brushes.White);

                                int yPos = 400;
                                foreach(var mt in Program.measureTimeList)
                                {
                                    TimeSpan counterFromStart = mt.Value.Subtract(Program.measureTimeList[Program.startingTimeKey]);
                                    string displayKvp = mt.Key + " - " + counterFromStart.ToString(@"mm\:ss\:fff"); 
                                    DrawText(graphics, displayKvp, 20.0f, fontFamily, new Point(480, yPos), textFormat, Brushes.Yellow);
                                    yPos += 20;
                                }

                                sendInstance.Send(videoFrame);		// Submitting frame to NDI. Clocked at 25 fps

                                Console.WriteLine("Frame number {0} sent to NDI. File used: {1}", frameNumber, imagefilename);

                                fileNumberInt++;
                            }
                        } 
                    } 
                } 
            } 
        }



    }
}
