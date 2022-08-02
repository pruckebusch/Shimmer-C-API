﻿using System;
using System.Collections.Generic;

using Hoho.Android.UsbSerial.Driver;
using Hoho.Android.UsbSerial.Extensions;
using Hoho.Android.UsbSerial.Util;
using Android.Content;
using Android.Hardware.Usb;
using shimmer.Communications;
using System.Threading.Tasks;

namespace ShimmerBLEAPI.Android.Communications
{
    public class SerialPortByteCommunicationAndroid : IVerisenseByteCommunication
    {
        public UsbManager usbManager;
        UsbSerialPort port;
        UsbSerialDriver driver;
        SerialInputOutputManager serialIoManager;
        public static Context context { get; set; }

        public Guid Asm_uuid { get; set; }
        public string id { get; set; }

        public event EventHandler<ByteLevelCommunicationEvent> CommunicationEvent;
        public String ComPort { get; set; }

        public static void setContext(Context context1)
        {
            context = context1;
        }

        public async Task<ConnectivityState> Connect()
        {
            if(context == null)
            {
                return ConnectivityState.Disconnected;
            }
            usbManager = context.GetSystemService(Context.UsbService) as UsbManager;
            var drivers = await FindAllDriversAsync(usbManager);
            // get first driver for now
            foreach(var x in drivers)
            {
                driver = (UsbSerialDriver)x;
                break;
            }
            port = driver.Ports[0];
            port.SetDTR(true);
            port.SetRTS(true);
            var permissionGranted = await usbManager.RequestPermissionAsync(port.Driver.Device, context);
            if (permissionGranted)
            {
                serialIoManager = new SerialInputOutputManager(port)
                {
                    BaudRate = 115200,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                };
                serialIoManager.DataReceived += (sender, e) => {
                    DataReceived(e.Data);
                };

                try
                {
                    serialIoManager.Open(usbManager);
                }
                catch (Java.IO.IOException e)
                {
                    return ConnectivityState.Disconnected;
                }
                return ConnectivityState.Connected;
            }
            
            return ConnectivityState.Disconnected;
        }

        internal static Task<IList<IUsbSerialDriver>> FindAllDriversAsync(UsbManager usbManager)
        {
            var table = UsbSerialProber.DefaultProbeTable;

            //Verisense
            table.AddProduct(0x1915, 0x520F, typeof(CdcAcmSerialDriver));

            var prober = new UsbSerialProber(table);

            return prober.FindAllDriversAsync(usbManager);
        }

        public async Task<ConnectivityState> Disconnect()
        {
            throw new NotImplementedException();
        }

        void DataReceived(byte[] data)
        {
            var temp = "Read " + data.Length + " bytes: \n"
                + HexDump.DumpHexString(data) + "\n\n";
        }

        public ConnectivityState GetConnectivityState()
        {
            return ConnectivityState.Connected;
        }

        public async Task<bool> WriteBytes(byte[] bytes)
        {
            if (serialIoManager.IsOpen)
            {
                port.Write(bytes, 100);
            }
            return true;
        }
    }
}
