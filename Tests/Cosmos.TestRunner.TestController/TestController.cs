﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cosmos.Debug.Kernel;

namespace Cosmos.TestRunner
{
    public static class TestController
    {
        private static Debugger mDebugger;

        internal static Debugger Debugger
        {
            get
            {
                return new Debugger("Tests", "TestController");
            }
        }

        public const byte TestChannel = 255;
        public static unsafe void Completed()
        {
            Debugger.SendChannelCommand(TestChannel, (byte)TestChannelCommandEnum.TestCompleted);
            while (true)
                ;
        }

        public static void Failed()
        {
            Debugger.Send("Failed");
            Debugger.SendChannelCommand(TestChannel, (byte)TestChannelCommandEnum.TestFailed);
            while (true)
                ;
        }

        internal static void AssertionSucceeded()
        {
            Debugger.SendChannelCommand(TestChannel, (byte)TestChannelCommandEnum.AssertionSucceeded);
        }
    }
}
