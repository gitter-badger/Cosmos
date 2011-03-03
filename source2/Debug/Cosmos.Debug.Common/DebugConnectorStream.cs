﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cosmos.Compiler.Debug;

namespace Cosmos.Debug.Common {
    public abstract class DebugConnectorStream : DebugConnector {
        private Stream mStream;
        
        protected class Incoming {
            public Stream Stream;
            // Buffer to hold incoming message
            public byte[] Packet;
            // Current # of bytes in mPacket
            public int CurrentPos = 0;
            public Action<byte[]> Completed;
        }

        private static string BytesToString(byte[] bytes, int index, int count)
        {
            if (count > 100)
            {
                return String.Empty;
            }
            var xSB = new StringBuilder(2 + (bytes.Length * 2));
            xSB.Append("0x");
            for (int i = index; i < index+count; i++)
            {
                xSB.Append(bytes[i].ToString("X2").ToString());
            }
            return xSB.ToString();
        }

        protected override void SendRawData(byte[] aBytes) {
            System.Diagnostics.Debug.WriteLine(String.Format("DC - sending: {0}", BytesToString(aBytes, 0, aBytes.Length)));
            mStream.Write(aBytes, 0, aBytes.Length);
        }

        public override bool Connected {
            get { return mStream != null; }
        }

        // Start is not in ctor, because for servers we have to wait
        // for the callback.
        protected void Start(Stream aStream) {
            mStream = aStream;
            Next(1, WaitForSignature);
        }

        public override void Dispose()
        {
            if (mStream != null)
            {
                mStream.Close();
                mStream = null;
            }
            base.Dispose();
        }
        
        protected override void Next(int aPacketSize, Action<byte[]> aCompleted) {
            var xIncoming = new Incoming() {
                Packet = new byte[aPacketSize]
                , Stream = mStream
                , Completed = aCompleted
            };
            mStream.BeginRead(xIncoming.Packet, 0, aPacketSize, new AsyncCallback(DoRead), xIncoming);
        }
        
        protected void DoRead(IAsyncResult aResult) {
            try
            {
                var xIncoming = (Incoming)aResult.AsyncState;
                int xCount = xIncoming.Stream.EndRead(aResult);
                
                System.Diagnostics.Debug.WriteLine(String.Format("DC - Received: {0}", BytesToString(xIncoming.Packet, xIncoming.CurrentPos, xCount)));
                xIncoming.CurrentPos += xCount;
                // If 0, end of stream then just exit without calling BeginRead again
                if (xCount == 0)
                {
                    return;
                    // Packet is not full yet, read more data
                }
                else if (xIncoming.CurrentPos < xIncoming.Packet.Length)
                {
                    xIncoming.Stream.BeginRead(xIncoming.Packet, xIncoming.CurrentPos
                        , xIncoming.Packet.Length - xIncoming.CurrentPos
                        , new AsyncCallback(DoRead), xIncoming);
                    // Full packet received, process it
                }
                else
                {
                    xIncoming.Completed(xIncoming.Packet);
                }
            }
            catch (System.IO.IOException ex)
            {
                ConnectionLost(ex);
            }
        }

   }
}