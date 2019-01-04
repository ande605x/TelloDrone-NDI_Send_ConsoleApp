using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelloDrone_NDI_Send_ConsoleApp
{
    class Program
    {
		// hardcoded constants
        private const string TelloIP = "192.168.10.1";
		private const int CommandPort = 8889;
		private const int TelloStatePort = 8890;
		private const int VideoPort = 11111;
        private const string TempImagePath = @"\tempimage\";
        private const string RawVideoFile = "telloRawH264buffer.raw";

        static public Dictionary<string, DateTime> measureTimeList = new Dictionary<string, DateTime>();
        static public string startingTimeKey = "Starting time (before FFmpeg)";




        static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Append, FileAccess.Write))  //Append: Write bytes from the current end of file. 
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in process of writing to file: {0}", ex);
                return false;
            }
        }




        static void Main(string[] args)
        {

            string startupPath = Environment.CurrentDirectory;  // this projects bin directory (here: ..\bin\x86\Debug)

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("TELLO DRONE - NDI");
            Console.WriteLine("1) Please turn on Tello Drone so it blinks yellow");
            Console.WriteLine("2) Connect to Tello Wifi (TELLO-C77E11) and wait until connected");
            Console.WriteLine("3) If already connected, please turn drone off and on, and take step 1) + 2) again.");
            Console.WriteLine("4) Then press enter.");
            Console.WriteLine();
            Console.ReadLine();

				
            UDPSender udpSenderService = new UDPSender(TelloIP, CommandPort);
			UDPReceiver udpReceiverService = new UDPReceiver(TelloStatePort, VideoPort);


            if (udpSenderService.SendCommandToTello("command"))
                Console.WriteLine("Tello ready for your input.");
			
			
			string userMenuInput = "1";  // default			
 
            while (userMenuInput.ToUpper() != "Q")
            {				
	    	    Console.WriteLine();
                Console.WriteLine("----------------------------------------------------------------------------------------");                    
                Console.WriteLine("Please enter menu choice ('Q' to quit):");
                Console.WriteLine("  1: Control Command");
                Console.WriteLine("  2: Set Command");
                Console.WriteLine("  3: Read Command (get all info)");
                Console.WriteLine("  4: Show video stream data and save in file");
                Console.WriteLine("  5: Send video stream data to NDI");
				Console.WriteLine();
				userMenuInput = Console.ReadLine();                                  	  
	  
	                              					
                if (userMenuInput == "1")
                {
                    Console.WriteLine("Find list of commands here: https://dl-cdn.ryzerobotics.com/downloads/tello/20180910/Tello%20SDK%20Documentation%20EN_1.3.pdf");
                    Console.WriteLine();
                    Console.WriteLine("Please write command to drone: ");
					string userCommand = "";
						
			    	Console.ForegroundColor = ConsoleColor.Yellow;
		    		userCommand = Console.ReadLine();
					Console.ForegroundColor = ConsoleColor.White;

                    if (udpSenderService.SendCommandToTello(userCommand))
                        Console.WriteLine("Command " + userCommand + " executed succesfully.");						
				}				
		
					
				if (userMenuInput == "2")
                {
                    Console.WriteLine("Set Commands is UNDER CONSTRUCTION in this App!");
                }


                if (userMenuInput == "3")
                {            
                    var list = udpReceiverService.GetTelloInfoList();
							
				    Console.ForegroundColor = ConsoleColor.Cyan;
		    		foreach(var kvp in list)
					{
						Console.WriteLine(kvp.Key + ": " + kvp.Value);	
					}                  
                            
					Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();                    
                }
					

                if (userMenuInput == "4")
                {
                    string rawVideoFileFullPath = startupPath + TempImagePath + RawVideoFile;

                    if (udpSenderService.SendCommandToTello("streamon"))
                        Console.WriteLine("Stream turned on.");
						
		    		Console.ForegroundColor = ConsoleColor.Cyan;						
                        
				    while (!Console.KeyAvailable)  // until key is pressed
                    {
	    				var videoData = udpReceiverService.GetVideoData();
								 
                        Console.Clear();                                                               
                        Console.WriteLine("Video data: ");                                
                        Console.WriteLine(Encoding.ASCII.GetString(videoData));								
		    			Console.WriteLine();
                        Console.WriteLine("Writing to file...");
                                
                        ByteArrayToFile(rawVideoFileFullPath, videoData);

                        Console.WriteLine("File writing finished.");
                        Console.WriteLine();
                    }
    		    	Console.ForegroundColor = ConsoleColor.White;                      
                }


                if (userMenuInput=="5")
                {
                    if (udpSenderService.SendCommandToTello("streamon")) 
                        Console.WriteLine("Stream turned on.");
    
                    Console.WriteLine("Please open NDI Studio Monitor.");
                    
                    measureTimeList.Add(startingTimeKey, DateTime.Now);

                    //FFMPEG PATHS:
                    //ffmpeg -i udp://127.0.0.1:11111 -f image2 C:/SOURCE/$tello%03d.bmp
                    //string cPath = @"C:\SOURCE\TelloDrone+NDI_Send_ConsoleApp\TelloDrone+NDI_Send_ConsoleApp\bin\x86\Debug\ffmpeg\";
                    //string cParams = "-y -i udp://127.0.0.1:11111 -f image2 "+TempImagePath+"$tello%03d.bmp     //-y overwriting files

                    string fullTempImagePath = startupPath + TempImagePath;
                    string cPath = startupPath + @"\ffmpeg\";
                    string cParams = "-y -i udp://127.0.0.1:11111 -f image2 " + fullTempImagePath + "$tello%03d.bmp";   //-y overwriting files
                    string filename = Path.Combine(cPath, "ffmpeg.exe");
                    var proc = System.Diagnostics.Process.Start(filename, cParams);

                    string firstFilePath = fullTempImagePath + "$tello001.bmp";
                    bool firstFileCreated = false;

                    measureTimeList.Add("FFmpeg started (no bitmapfile yet)", DateTime.Now);

                    while (!firstFileCreated)                                                               // wait until ffmpeg creates first file
                    {
                        if (File.Exists(firstFilePath))                                                     // if file exists 
                            if (File.GetLastWriteTime(firstFilePath) > measureTimeList[startingTimeKey])    // check if file is new (the file should be created after I started the timer)
                                firstFileCreated = true;
                        Console.WriteLine("Waiting for FFmpeg to create first bitmap file...");
                    }

                    DateTime lastWriteTimeFileOne = File.GetLastWriteTime(firstFilePath);
                    measureTimeList.Add("Creation timestamp of first file", lastWriteTimeFileOne);
                    measureTimeList.Add("First FFmpeg file created (before NDI connection)", DateTime.Now);

                    NDISend NDISendInstans = new NDISend(TelloStatePort, VideoPort, fullTempImagePath);
                    NDISendInstans.NDISendFrame();                     
                }
					
            }	
            Console.ReadLine();
        }
    }
}
