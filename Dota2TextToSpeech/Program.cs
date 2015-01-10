using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Speech.Synthesis;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Management;
 

namespace Dota2TextToSpeech
{
    class Program
    {
        SpeechSynthesizer reader;
        private Thread PacketCaptureThread;
        public static Encoding Utf8 = Encoding.UTF8;
        public bool ON = true;

        private Boolean HasDefaultAdapter = false;
        private String DefaultAdapterMAC = "";

        // The "recommended" adapter. The field Found will be null if there is no recommended adapter.
        private Adapter DefaultAdapter;

        // All adapters found. Used in the advances layout.
        private Adapter[] AllAdapters;

        // Struct which stores the information about the adapter.
        struct Adapter
        {
            // The index used to retrieve the actual device.
            public int Index;
            // The MAC address of the adapter.
            public String MAC;
            // The IP address of the adapter.
            public String IP;
            // The description of the adapter.
            public String Description;
            // Whether or not this adapter is valid.
            public Boolean Found;
        };

        public struct Dota_ChatMessage
        {
            public int Type;
            public string Sender;
            public string Message;
        }

        public delegate void Dota_ChatMessageCallback(Dota_ChatMessage data);

        [DllImport("Dota2ChatDLL.dll")]
        public static extern void StartDevice(IntPtr device, Dota_ChatMessageCallback callback);

        // Returns a list of available devices.
        [DllImport("Dota2ChatDLL.dll")]
        [return: MarshalAs(UnmanagedType.BStr)]
        public static extern string GetDeviceList();

        // Returns a pointer to the specified device.
        [DllImport("Dota2ChatDLL.dll")]
        public static extern IntPtr GetDevice(int num);

        private IntPtr Device;

        public Program()
        {
            reader = new SpeechSynthesizer();
            reader.SelectVoice("Microsoft Hazel Desktop");
            LoadAdapters();
            GetDefaultAdapter(AllAdapters);
            pcap(DefaultAdapter.Index);
            while (true)
            {
                getInput();
            }
        }

        public void getInput()
        {
            String s = Console.ReadLine();
            s = s.ToLower();
            if (s.Equals("on"))
            {
                if (!ON)
                    reader.SpeakAsync("Turning voice on.");
                else
                    reader.SpeakAsync("Voice is already on.");
                ON = true;
            }
            if (s.Equals("off"))
            {
                if (!ON)
                    reader.SpeakAsync("Voice is already off.");
                else
                    reader.SpeakAsync("Turning voice off.");
                ON = false;
            }
            if (s.Equals("exit"))
            {
                Console.WriteLine("Goodbye.");
                reader.SpeakAsync("Goodbye.");
                System.Environment.Exit(0);
            }
        }

        private void LoadAdapters()
        {
            String data = null;
            try
            {
                data = GetDeviceList();
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("Error: File not found! Microsoft Visual C++ 2010 Redistributable is not installed.");
                return;
            }
            catch (DllNotFoundException)
            {
                Console.WriteLine("Error: File not found! WinPcap is not installed.");
                return;
            }

            String[] deviceData = data.Split('\n');

            AllAdapters = new Adapter[(deviceData.Length - 1) / 3];
            for (int i = 0; i < AllAdapters.Length; i++)
            {
                Adapter adapter = new Adapter();
                adapter.Index = i;
                adapter.MAC = deviceData[i * 3];
                adapter.IP = deviceData[i * 3 + 1];
                adapter.Description = deviceData[i * 3 + 2];
                adapter.Found = true;

                if (HasDefaultAdapter)
                {
                    if (adapter.MAC.ToLower().Equals(DefaultAdapterMAC.ToLower()))
                    {
                        return;
                    }
                }

                AllAdapters[i] = adapter;
            }

            // Attempt to find the default adapter.
            DefaultAdapter = GetDefaultAdapter(AllAdapters);

            if (HasDefaultAdapter)
            {
                // No match for the saved default adapter was found.
            }
        }

        private Adapter GetDefaultAdapter(Adapter[] adapters)
        {
            try
            {
                // Select all adapters in the system.
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select MACAddress,PNPDeviceID FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL AND PNPDeviceID IS NOT NULL");
                ManagementObjectCollection mObject = searcher.Get();

                // Iterate over the adapters.
                foreach (ManagementObject obj in mObject)
                {
                    string pnp = obj["PNPDeviceID"].ToString();

                    // Only check against real world adapters (Hamachi, etc will be excluded).
                    if (pnp.Contains("PCI\\"))
                    {
                        // Retrieve the MAC address and check against the adapters from winpcap.
                        string mac = obj["MACAddress"].ToString();
                        mac = mac.Replace(":", string.Empty);

                        foreach (Adapter adapter in adapters)
                        {
                            // Return the adapter if the MAC addresses matches.
                            if (adapter.MAC.ToLower().Equals(mac.ToLower()))
                                return adapter;
                        }
                    }
                }
            }
            catch (COMException)
            {
                Console.WriteLine("Error: Service is missing.");
            }
            catch (Exception)
            {
                Console.WriteLine("Error: Unkown exception.");
                // Make sure no other exception is thrown.
            }

            // Return an empty adapter (Found = False).
            return new Adapter();
        }

        public void pcap(int deviceIndex)
        {
            IntPtr devicePointer = GetDevice(deviceIndex);
            Device = devicePointer;
            PacketCaptureThread = new Thread(new ThreadStart(StartPacketCapture));
            PacketCaptureThread.Start();
        }

        // Starts capturing packets.
        public void StartPacketCapture()
        {
            Console.WriteLine("Starting packet capture.");
            StartDevice(Device, OnMessageReceived);
        }

        // Called by the C++ code when a message has been received or a status has changed.
        public void OnMessageReceived(Dota_ChatMessage data)
        {
            switch (data.Type)
            {
                // Hide status has been changed, report to DLL.
                case -2:
                case -1:
                    //InjectionHelper.SendHideShowMessage(data.Type == -1);

                    break;

                // A message has been received, display in main window and send to DLL.
                case 0:
                case 1:
                case 2:
                    // Scope is determined by the type value: 0 = All, 1 = Team.
                    String scope = (data.Type == 0) ? "ALL" : ((data.Type == 1) ? "TEAM" : "TV");

                    // Make sure the data is read as UTF8.
                    String sender = data.Sender;
                    String message = data.Message;
                    Console.WriteLine("Got message: " + message);
                    if (ON)
                        reader.SpeakAsync(message);
                    // Translate the message and add it.
                    //new Thread(new ParameterizedThreadStart(TranslateMessageAndAdd)).Start(new object[] { scope, sender, message });

                    break;

            }
        }

        // Decodes the UTF8 message from a C++ wstring.
        private static String ToUTF(String str)
        {
            // Convert the string to a char array.
            char[] chars = str.ToCharArray();

            // Create a byte array and fill it with the byte values from the char array.
            byte[] bytes = new byte[chars.Length];
            int i = 0;
            foreach (char c in chars)
            {
                bytes[i++] = (byte)c;
            }
            foreach (byte b in bytes)
            {
                Console.WriteLine("Byte: " + (int)b);
            }
            // Decode the byte array using UTF8.
            return Utf8.GetString(bytes);
        }

        static void Main(string[] args)
        {
            new Program();
        }
    }
}
