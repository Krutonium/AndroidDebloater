using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SharpAdbClient;
using SharpAdbClient.DeviceCommands;

namespace AndroidDebloat
{
    class Program
    {
        static AdbClient client = new AdbClient();
        private static string adb_location;
        static void Main(string[] args)
        {
            AdbServer server = new AdbServer();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                server.StartServer("/usr/bin/adb", restartServerIfNewer: false);
                adb_location = "/usr/bin/adb";
                Console.WriteLine("Running on Linux");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                server.StartServer(@"C:\Program Files (x86)\android-sdk\platform-tools\adb.exe", restartServerIfNewer: false);
                adb_location = @"C:\Program Files (x86)\android-sdk\platform-tools\adb.exe";
                Console.WriteLine("Running on Windows");
            }
            
            var devices = client.GetDevices();
            var targetDevice = devices.First();
            Console.WriteLine("Found " + devices.Count + " device(s).");
            Console.WriteLine("Using first device: " + targetDevice.Model);
            //var AppList = currentAppList(devices.First());
            
            //We have our info, we should now ask the user what they wish to do.
            MainMenu(targetDevice);
        }

        private static Dictionary<string, string> currentAppList(DeviceData device)
        {
            PackageManager manager = new PackageManager(client, device, false);
            return manager.Packages;
        }

        private static void removeApp(string app, DeviceData device)
        {
            client.ExecuteRemoteCommand("pm uninstall --user 0 " + app, device, new ConsoleOutputReceiver());
            Console.WriteLine("Removed " + app);
        }

        private static void MainMenu(DeviceData targetDevice)
        {
#if !DEBUG
            Console.Clear();
#endif
            Console.WriteLine("=====================");
            Console.WriteLine("| Android Debloater |");
            Console.WriteLine("=====================");
            Console.WriteLine();
            Console.WriteLine("    1. List Packages");
            Console.WriteLine("    2. Remove Packages by Name");
            Console.WriteLine("    3. Remove Packages by List");
            Console.WriteLine("    4. Exit & Reboot Phone");
            Console.WriteLine();
            Console.Write("Please select an Option: ");
            var Reply = Console.ReadKey();
            Console.WriteLine();
            
            string r = Reply.KeyChar.ToString();
            switch (r)
            {
                case "1":
                    var apps = currentAppList(targetDevice);
                    foreach (var app in apps)
                    {
                        Console.WriteLine(app.Key);
                    }
                    MainMenu(targetDevice);
                    break;
                
                case "2":
                    Console.WriteLine("What app would you like to remove?");
                    string toRemove = Console.ReadLine();
                    removeApp(toRemove, targetDevice);
                    MainMenu(targetDevice);
                    break;
                case "3":
                    //Goto a Menu that lets you select from Package Lists
                    listRemovalsMenu(targetDevice);
                    MainMenu(targetDevice);
                    break;
                case "4":
                    client.Reboot(targetDevice);
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid Input");
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                    MainMenu(targetDevice);
                    break;
            }
        }

        private static void listRemovalsMenu(DeviceData targetDevice)
        {
            if (Directory.Exists("./lists") == false)
            {
                Directory.CreateDirectory("./lists");
            }
            var lists = Directory.GetFiles("./lists");
            if (lists.Length == 0)
            {
                Console.WriteLine("There is no lists, go get some!");
            }
            else
            {
                int listIndex = 0;
                foreach (var list in lists)
                {
                    string listName = list.Substring(list.LastIndexOf("/") + 1);
                    Console.WriteLine(listIndex + ": " + listName);
                }

                Console.WriteLine();
                Console.WriteLine("Please select the list you would like to run: ");
                var Reply = Console.ReadKey();
                Console.WriteLine();
                string r = Reply.KeyChar.ToString();
                
                //the value of r, if it's in integer, will be the index of the list in `lists`
                int value;
                if (int.TryParse(r, out value))
                {
                    if (value <= lists.Length && value >= 0)
                    {
                        //Value should be within range
                        string[] RemovalList = File.ReadAllLines(lists[value]);
                        var InstalledApps = currentAppList(targetDevice);
                        foreach (var key in InstalledApps.Keys)
                        {
                            if (RemovalList.Contains(key))
                            {
                                removeApp(key, targetDevice);
                            }
                        }
                        Console.WriteLine("Everything Removed");
                    }
                    else
                    {
                        Console.WriteLine("Missed");
                    }
                }
            }
        }
    }
    
    
    
}