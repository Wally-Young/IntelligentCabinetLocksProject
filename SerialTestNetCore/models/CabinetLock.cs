using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using flyfire.IO.Ports;
namespace SerialTestNetCore
    
{
    public class CabinetLock
    {
        static public byte[] UnlockCmd(int code)
        {
            byte location = 0;
            byte[] unLockCmd = { 0x8A, 0x01, 0x01, 0x11, 0x9B };
            if (code > 12)
            {
                location = 2;
                code = code - 12;
            }
            else
            {
                location = 1;
            }
            unLockCmd[1] = location;
            unLockCmd[2] = (byte)code;
            byte sum = 0;
            for (int i = 0; i < unLockCmd.Length - 1; i++)
            {
                sum ^= unLockCmd[i];
            }
            unLockCmd[unLockCmd.Length - 1] = sum;
            return unLockCmd;

        }

        static public byte[] ReadLockState(int code)
        {
            byte location = 0;
            byte[] unLockCmd = { 0x80, 0x01, 0x01, 0x33, 0x9B };
            if (code > 12)
            {
                location = 2;
                code = code - 12;
            }
            else
            {
                location = 1;
            }
            unLockCmd[1] = location;
            unLockCmd[2] = (byte)code;
            byte sum = 0;
            for (int i = 0; i < unLockCmd.Length - 1; i++)
            {
                sum ^= unLockCmd[i];
            }
            unLockCmd[unLockCmd.Length - 1] = sum;
            return unLockCmd;

        }

        static public string  DealWithLocks(int code,string cmd,CustomSerialPort serial)
        {
            if(cmd[2]=='O')
            {
                serial.Write(UnlockCmd(code));
                return "0";
            }
            else if(cmd[0]=='O'||cmd.StartsWith("A-R"))
            {
                serial.Write(ReadLockState(code));
                return "R";
            }
            return "E";
        
        }
     
    }
}
