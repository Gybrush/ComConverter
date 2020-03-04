using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Configuration;
using System.Collections.Specialized;

public class ComConverter
{
    static bool _continue = true;
    static bool _echoMode = false;
    static bool _binaryMode = false;
    static SerialPort _readSerialPort;
    static SerialPort _writeSerialPort;

    public static void Main()
    {
        Console.WriteLine("################################");
        Console.WriteLine("#   Serial Command Converter   #");
        Console.WriteLine("################################\n");

        NameValueCollection conf;

        // Read Configuration
        try
        {
            conf = SetConfiguration();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("<< Press any key to close. >>");
            Console.ReadKey();
            return;
        }

        //DisplayCurrentSetting(conf);

        // Serial Port Configuration
        SetSerialPort(conf);

        // Serial Port Open
        try
        {
            OpenSerialPort();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("<< Press any key to close. >>");
            Console.ReadKey();
            CloseSerialPort();
            return;
        }

        // Flushing read serial port
        FlushReadPort();

        DisplayCommand();

        Thread readThread = new Thread(Read);

        readThread.Start();

        byte[] buffer = new byte[256];
        int nBuffer = 0;

        while (_continue)
        {
            string message = Console.ReadLine();

            if (message.ToLower() == "quit")
            {
                _continue = false;
            }
            else if (message.ToLower() == "echo on")
            {
                _echoMode = true;
                Console.WriteLine(">>> echo mode on");
            }
            else if (message.ToLower() == "echo off")
            {
                _echoMode = false;
                Console.WriteLine(">>> echo mode off");
            }
            else if (message.ToLower() == "binary on")
            {
                _binaryMode = true;
                Console.WriteLine(">>> binary mode on");
            }
            else if (message.ToLower() == "binary off")
            {
                _binaryMode = false;
                Console.WriteLine(">>> binary mode off");
            }
            else if (message.ToLower() == "show")
            {
                DisplayCurrentSetting(conf);
            }
            else if (message.ToLower() == "?" || message.ToLower() == "help")
            {
                DisplayCommand();
            }
            else if (message.ToLower() == "send")
            {
                Console.Write("message (hexa code): ");

                string s = Console.ReadLine();
                if (s != null && s.Length > 0)
                {
                    nBuffer = 0;

                    string[] tok = s.Split(' ');
                    foreach (string t in tok)
                    {
                        buffer[nBuffer++] = Convert.ToByte(Convert.ToInt32(t, 16));
                    }

                    if (nBuffer > 0)
                        _writeSerialPort.Write(buffer, 0, nBuffer);
                    //_writeSerialPort.WriteLine(s);
                    //if (_echoMode)
                    //    Console.WriteLine("WRITE>>> " + s);
                }
            }
        }

        readThread.Join();

        CloseSerialPort();
    }

    public static void DisplayCommand()
    {
        Console.WriteLine("");
        Console.WriteLine("+-----------------------------------------------+");
        Console.WriteLine("|  quit          : to exit ComConverter         |");
        Console.WriteLine("|  echo on/off   : to turn on/off echo mode     |");
        //Console.WriteLine("|  binary on/off : to turn on/off binary mode   |");
        Console.WriteLine("|  show          : to show the current setting  |");
        Console.WriteLine("|  send          : to send message              |");
        Console.WriteLine("|  ? or help     : to show this screen          |");
        Console.WriteLine("+-----------------------------------------------+");
        Console.WriteLine("");
    }

    public static NameValueCollection SetConfiguration()
    {
        NameValueCollection conf = ConfigurationManager.AppSettings;
        if (conf.Count == 0)
        {
            throw new Exception("Error: There is no ComConverter.exe.config file or there is no configuration in it.");
        }

        if (conf.Get("ReadPortName") == null || !(conf.Get("ReadPortName").ToUpper().StartsWith("COM")))
        {
            throw new Exception("Error: There is no ReadPortName or invalid port name.");
        }

        if (conf.Get("ReadBaudRate") == null)
            conf.Set("ReadBaudRate", "9600");

        if (conf.Get("ReadParity") == null)
            conf.Set("ReadParity", "None");

        if (conf.Get("ReadDataBits") == null)
            conf.Set("ReadDataBits", "8");

        if (conf.Get("ReadStopBits") == null)
            conf.Set("ReadStopBits", "One");

        if (conf.Get("ReadHandShake") == null)
            conf.Set("ReadHandShake", "None");

        if (conf.Get("ReadTimeout") == null)
            conf.Set("ReadTimeout", "500");

        if (conf.Get("WritePortName") == null || !(conf.Get("WritePortName").ToUpper().StartsWith("COM")))
        {
            throw new Exception("Error: There is no WritePortName or invalid port name.");
        }

        if (conf.Get("WriteBaudRate") == null)
            conf.Set("WriteBaudRate", "9600");

        if (conf.Get("WriteParity") == null)
            conf.Set("WriteParity", "None");

        if (conf.Get("WriteDataBits") == null)
            conf.Set("WriteDataBits", "8");

        if (conf.Get("WriteStopBits") == null)
            conf.Set("WriteStopBits", "One");

        if (conf.Get("WriteHandShake") == null)
            conf.Set("WriteHandShake", "None");

        if (conf.Get("WriteTimeout") == null)
            conf.Set("WriteTimeout", "500");

        return conf;
    }

    public static void DisplayCurrentSetting(NameValueCollection conf)
    {
        Console.WriteLine("");
        Console.WriteLine("== Serial Port Setting ==");
        foreach (string s in conf.AllKeys)
            Console.WriteLine(s + " : " + conf.Get(s));

        Console.WriteLine("");
        Console.WriteLine("== Echo Mode : {0} ==", _echoMode ? "on" : "off");
        Console.WriteLine("== Binary Mode : {0} ==", _binaryMode ? "on" : "off");
        Console.WriteLine("");
    }

    public static void SetSerialPort(NameValueCollection conf)
    {
        _readSerialPort = new SerialPort();

        _readSerialPort.PortName = conf.Get("ReadPortName");
        _readSerialPort.BaudRate = int.Parse(conf.Get("ReadBaudRate"));
        _readSerialPort.Parity = (Parity)Enum.Parse(typeof(Parity), conf.Get("ReadParity"), true);
        _readSerialPort.DataBits = int.Parse(conf.Get("ReadDataBits"));
        _readSerialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), conf.Get("ReadStopBits"), true);
        _readSerialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), conf.Get("ReadHandShake"), true);
        _readSerialPort.ReadTimeout = int.Parse(conf.Get("ReadTimeout"));

        _writeSerialPort = new SerialPort();

        _writeSerialPort.PortName = conf.Get("WritePortName");
        _writeSerialPort.BaudRate = int.Parse(conf.Get("WriteBaudRate"));
        _writeSerialPort.Parity = (Parity)Enum.Parse(typeof(Parity), conf.Get("WriteParity"), true);
        _writeSerialPort.DataBits = int.Parse(conf.Get("WriteDataBits"));
        _writeSerialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), conf.Get("WriteStopBits"), true);
        _writeSerialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), conf.Get("WriteHandShake"), true);
        _writeSerialPort.ReadTimeout = int.Parse(conf.Get("WriteTimeout"));
    }

    public static void OpenSerialPort()
    {
        try
        {
            _readSerialPort.Open();
        }
        catch (UnauthorizedAccessException)
        {
            throw new Exception("Error: Read port is not accessible. It may not exist or it could be being occupied by other process.");
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new Exception("Error: Some of read port configuration(baudrate, parity, databits, stopbits, handshake, timeout) are invalid.");
        }
        catch (ArgumentException)
        {
            throw new Exception("Error: Read port name is invalid");
        }
        catch (IOException)
        {
            throw new Exception("Error: Read port is not normal.");
        }

        try
        {
            _writeSerialPort.Open();
        }
        catch (UnauthorizedAccessException)
        {
            throw new Exception("Error: Write port is not accessible. It may not exist or it could be being occupied by other process.");
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new Exception("Error: Some of write port configuration values(baudrate, parity, databits, stopbits, handshake, timeout) are invalid.");
        }
        catch (ArgumentException)
        {
            throw new Exception("Error: Write port name is invalid");
        }
        catch (IOException)
        {
            throw new Exception("Error: Write port is not normal.");
        }
    }

    public static void FlushReadPort()
    {
        while (true)
        {
            try
            {
                string message = _readSerialPort.ReadLine();
                if (message == null || message.Length == 0)
                    break;
            }
            catch (TimeoutException)
            {
                return;
            }
        }
    }

    public static void CloseSerialPort()
    {
        if (_readSerialPort.IsOpen)
            _readSerialPort.Close();

        if (_writeSerialPort.IsOpen)
            _writeSerialPort.Close();
    }

    public static void Read()
    {
        byte[] buffer = new byte[256];
        int nBuffer = 0;

        while (_continue)
        {
            try
            {
                nBuffer = _readSerialPort.Read(buffer, 0, 256);
                if (nBuffer > 0)
                {
                    if (_echoMode)
                    {
                        Console.Write("READ>>> ");
                        for (int i = 0; i < nBuffer; ++i)
                        {
                            Console.Write("0x" + buffer[i].ToString("X2") + " ");
                        }
                        Console.WriteLine("");
                    }

                }
            }
            catch (TimeoutException) { }
        }
    }
}
