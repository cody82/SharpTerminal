using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace SharpTerminal
{
    public class SerialData : BindableBase
    {
        public DateTime timestamp;
        public DateTime TimeStamp
        {
            get
            {
                return timestamp;
            }
            set
            {
                SetProperty(ref timestamp, value);
            }
        }

        byte[] data;
        public byte[] Data
        {
            get
            {
                return data;
            }
            set
            {
                SetProperty(ref data, value);
                Hex = Data.Select(x => x.ToString("X2")).Aggregate((a, c) => a + " " + c);
                ASCII = Encoding.ASCII.GetString(data);
            }
        }

        string hex;
        public string Hex
        {
            get
            {
                return hex;
            }
            private set
            {
                SetProperty(ref hex, value);
            }
        }

        string ascii;
        public string ASCII
        {
            get
            {
                return ascii;
            } 
            private set
            {
                SetProperty(ref ascii, value);
            }
        }

    }

    public class PortInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Name + " - " + Description;
        }
    }

    public class Serial : BindableBase
    {
        SerialPort port;

        public SerialPort Port
        {
            get { return port; }
            private set { SetProperty(ref port, value); }
        }

        public Serial()
        {
            Ports = new ObservableCollection<PortInfo>();
            BaudRates = new ObservableCollection<int>(new int[] { 9600, 19200, 38400, 57600, 115200 });
            Paritys = new ObservableCollection<Parity>(Enum.GetValues(typeof(Parity)).Cast<Parity>());
            StopBitsList = new ObservableCollection<StopBits>(Enum.GetValues(typeof(StopBits)).Cast<StopBits>());
            UpdatePorts();
            if (Ports.Any())
                PortName = Ports.First();

            ReceivedData = new ObservableCollection<SerialData>();
            SentData = new ObservableCollection<SerialData>();


        }

        public async void Open()
        {
            if(port != null)
            {
                port.Dispose();
            }
            Port = new SerialPort(PortName.Name, BaudRate, Parity, 8, StopBits);
            port.Open();
            IsOpen = true;

            byte[] buffer = new byte[64 * 1024];
            while(true)
            {
                try
                {
                    int r = await port.BaseStream.ReadAsync(buffer, 0, buffer.Length);
                    if (r <= 0)
                        break;
                    
                    var now = DateTime.Now;
                    bool add = true;
                    if(ReceivedData.Any())
                    {
                        var last = ReceivedData.Last();
                        if (now - last.TimeStamp < TimeSpan.FromMilliseconds(20))
                        {
                            var tmp = new byte[last.Data.Length + r];
                            Array.Copy(last.Data, tmp, last.Data.Length);
                            Array.Copy(buffer,0, tmp, last.Data.Length, r);
                            last.Data = tmp;
                            add = false;
                        }
                    }
                    
                    if(add)
                        ReceivedData.Add(new SerialData(){TimeStamp = now, Data = buffer.Take(r).ToArray()});

                    ReceivedString = ReceivedString + encoding.GetString(buffer, 0, r);
                }
                catch(IOException)
                {
                    break;
                }
            }
        }


        ObservableCollection<SerialData> sentdata;
        public ObservableCollection<SerialData> SentData
        {
            get
            {
                return sentdata;
            }
            set
            {
                SetProperty(ref sentdata, value);
            }
        }

        ObservableCollection<SerialData> receiveddata;
        public ObservableCollection<SerialData> ReceivedData
        {
            get
            {
                return receiveddata;
            }
            set
            {
                SetProperty(ref receiveddata, value);
            }
        }

        string receivestring = "";
        public string ReceivedString
        {
            get
            {
                return receivestring;
            }
            set
            {
                SetProperty(ref receivestring, value);
            }
        }


        string sentstring = "";
        public string SentString
        {
            get
            {
                return sentstring;
            }
            set
            {
                SetProperty(ref sentstring, value);
            }
        }

        public void Close()
        {
            if (port != null)
            {
                port.Dispose();
                Port = null;
                IsOpen = false;
            }
        }

        Encoding encoding = Encoding.ASCII;

        public async Task SendAsync(byte[] buffer)
        {
            if (!IsOpen)
                return;

            SentData.Add(new SerialData() { TimeStamp = DateTime.Now, Data = buffer });
            SentString += encoding.GetString(buffer);
            await port.BaseStream.WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task SendAsync(string s)
        {
            if (!IsOpen)
                return;

            byte[] buffer = encoding.GetBytes(s);
            SentData.Add(new SerialData(){TimeStamp = DateTime.Now, Data=buffer});
            SentString += s;
            await port.BaseStream.WriteAsync(buffer, 0, buffer.Length);
        }

        public bool IsOpen
        {
            get
            {
                return port != null && port.IsOpen;
            }
            private set
            {
                OnPropertyChanged<bool>(() => IsOpen);
                OnPropertyChanged<bool>(() => IsClosed);
            }
        }

        public bool IsClosed
        {
            get
            {
                return !IsOpen;
            }
        }

        public void UpdatePorts()
        {
            var portnames = SerialPort.GetPortNames();

            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM WIN32_SerialPort"))
                {
                    var ports = searcher.Get().Cast<ManagementBaseObject>().ToList();
                    var tList = (from n in portnames
                                 join p in ports on n equals p["DeviceID"].ToString()
                                 select new PortInfo() { Name = n, Description = (string)p["Caption"] }).ToList();

                    foreach (var p in portnames)
                    {
                        if (!Ports.Any(x => x.Name == p))
                        {
                            var tmp = tList.SingleOrDefault(x => x.Name == p);
                            if (tmp != null)
                                Ports.Add(tmp);
                            else
                                Ports.Add(new PortInfo() { Name = p, Description = "" });
                        }
                    }

                    foreach (var p in Ports.ToArray())
                    {
                        if (!portnames.Any(x => x == p.Name))
                            Ports.Remove(p);
                    }
                }
            }
        }


        PortInfo portname;
        public PortInfo PortName
        {
            get
            {
                return portname;
            }
            set
            {

                SetProperty(ref portname, value);
            }
        }

        int baudrate = 9600;
        public int BaudRate
        {
            get
            {
                return baudrate;
            }
            set
            {

                SetProperty(ref baudrate, value);
            }
        }

        Parity parity = Parity.None;
        public Parity Parity
        {
            get
            {
                return parity;
            }
            set
            {

                SetProperty(ref parity, value);
            }
        }

        StopBits stopbits = StopBits.One;
        public StopBits StopBits
        {
            get
            {
                return stopbits;
            }
            set
            {

                SetProperty(ref stopbits, value);
            }
        }

        ObservableCollection<PortInfo> ports;
        public ObservableCollection<PortInfo> Ports
        {
            get
            {
                return ports;
            }
            set
            {
                SetProperty(ref ports, value);
            }
        }

        ObservableCollection<int> baudrates;
        public ObservableCollection<int> BaudRates
        {
            get
            {
                return baudrates;
            }
            set
            {
                SetProperty(ref baudrates, value);
            }
        }

        ObservableCollection<Parity> paritys;
        public ObservableCollection<Parity> Paritys
        {
            get
            {
                return paritys;
            }
            set
            {
                SetProperty(ref paritys, value);
            }
        }

        ObservableCollection<StopBits> stopbitslist;
        public ObservableCollection<StopBits> StopBitsList
        {
            get
            {
                return stopbitslist;
            }
            set
            {
                SetProperty(ref stopbitslist, value);
            }
        }

        public void ClearSentData()
        {
            SentData.Clear();
            SentString = "";
        }

        public void ClearReceivedData()
        {
            ReceivedData.Clear();
            ReceivedString = "";
        }
    }
}
