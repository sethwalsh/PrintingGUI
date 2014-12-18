using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Diagnostics;
using System.Management;
using System.Net;
using System.IO;

namespace PrinterManager
{
    public partial class Form1 : Form
    {
        List<string> _printers = new List<string>();
        List<string> _currentPrinters = new List<string>();

        private string current_server = "", location_property_name = "", working_directory = "", download_path = "", key_value_filename = "", printer_list_filename = "";
        private string[] servers = new string[] { };

        public Form1()
        {
            // Read config file
            ReadConfigFile();
            
            // Get all available printers and add them into the available list
            RetrievePrinterList();

            this.listView1 = new ListView();
            GetCurrentPrinterList();

            InitializeComponent();

            listBox1.DataSource = _printers;            
        }

        public void GetCurrentPrinterList()
        {            
            // Search all installed printers and retrieve the comment property string that will identify which printers this service previously installed on the system
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Printer");
            foreach (System.Management.ManagementObject printer in searcher.Get())
            {
                string location = "";
                if (printer["Location"] != null)
                    location = printer["Location"].ToString();
                if (location.Contains(location_property_name) && printer["Location"] != null)
                {
                    // This is one of our network printers
                    listView1.Items.Add(printer["Name"].ToString());
                }
            }            
        }

        public void ReadConfigFile()
        {
            // Get print servers from config file
            if (File.Exists(@"C:\Tools\Printing\config.txt"))
            {
                System.IO.StreamReader file = new System.IO.StreamReader(@"C:\Tools\Printing\config.txt");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] words;
                    if (line.Contains("[server_list]"))
                    {
                        string[] delim = new string[] { "= " };
                        words = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                        servers = words[1].Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        //Console.WriteLine("PrintingService.PrimaryServer: " + primary_server);
                    }                    
                    if (line.Contains("[location_property_name]"))
                    {
                        words = line.Split((string[])null, 3, StringSplitOptions.RemoveEmptyEntries);
                        location_property_name = words[2];
                    }
                    if (line.Contains("[working_directory]"))
                    {
                        words = line.Split((string[])null, 3, StringSplitOptions.RemoveEmptyEntries);
                        working_directory = words[2];
                    }
                    if (line.Contains("[download_path]"))
                    {
                        words = line.Split((string[])null, 3, StringSplitOptions.RemoveEmptyEntries);
                        download_path = words[2];
                    }
                    if (line.Contains("[key_value_filename]"))
                    {
                        words = line.Split((string[])null, 3, StringSplitOptions.RemoveEmptyEntries);
                        key_value_filename = words[2];
                    }
                    if (line.Contains("[printer_list_filename]"))
                    {
                        words = line.Split((string[])null, 3, StringSplitOptions.RemoveEmptyEntries);
                        printer_list_filename = words[2];
                    }
                }
                file.Close();
            }
        }

        public void RetrievePrinterList()
        {
            // Download the key value printer => port file from the Server
            WebClient client = new WebClient();
            string url = "http://" + servers[0] + "/printing/" + key_value_filename;
            try
            {
                client.DownloadFile(new Uri(url), "printer_list.txt");

                if (File.Exists(@"printer_list.txt"))
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(@"printer_list.txt");
                    string line;
                    
                    while ((line = file.ReadLine()) != null)
                    {
                        // Split the line and keep just the printer name
                        string[] split = line.Split((string[])null, 3, StringSplitOptions.RemoveEmptyEntries);
                        _printers.Add(split[0]);                        
                    }
                    file.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void AddPrinter(string printer)
        {
            string command = @"C:\Windows\System32\spool\tools\PrintBrm.exe";
            ProcessStartInfo startInfo = new ProcessStartInfo(command);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(startInfo);
            startInfo.Arguments = @"-r -f C:\Tools\Printing\" + printer + ".brm -noacl -o force";
            var process = Process.Start(startInfo);
            process.WaitForExit();
            MessageBox.Show("Printer: " + printer + " has successfully been added.");
        }

        public void RemovePrinter(string printer)
        {
            System.Management.ManagementScope oManagementScope = new System.Management.ManagementScope(System.Management.ManagementPath.DefaultPath);
            oManagementScope.Connect();
            System.Management.SelectQuery query = new System.Management.SelectQuery("SELECT * FROM Win32_Printer");
            System.Management.ManagementObjectSearcher search = new System.Management.ManagementObjectSearcher(oManagementScope, query);
            System.Management.ManagementObjectCollection printers = search.Get();
            foreach (System.Management.ManagementObject p in printers)
            {
                string pName = p["Name"].ToString().ToLower();
                if (pName.Equals(printer.ToLower()))
                {
                    p.Delete();
                    break;
                }
            }
            MessageBox.Show("Printer: " + printer + " has successfully been removed.");
        }

        private void button1_Click(object sender, EventArgs e)
        {
           

            AddPrinter("CMS-5");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RemovePrinter("CMS-5");
        }
    }
}
