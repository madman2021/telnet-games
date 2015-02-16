﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelnetGames
{
    class VT100
    {
        public enum ColorEnum
        {
            Black,
            Red,
            Green,
            Yellow,
            Blue,
            Magneta,
            Cyan,
            White
        }

        public enum Direction
        {
            Horizontal,
            Vertical
        }

        public struct ColorStruct
        {
            public bool Bright;
            public ColorEnum Color;
        }

        public enum ClearMode
        {
            CursorToBeginning,
            Entire,
            BeginningToCursor
        }

        private NetworkStream networkStream;
        private MemoryStream stream;
        private bool isEscapeCode = false;

        public VT100(NetworkStream networkStream)
        {
            this.networkStream = networkStream;
            stream = new MemoryStream();
            stream.Write(new byte[6] { 0xFF, 0xFB, 0x01, 0xFF, 0xFB, 0x03 }, 0, 6);
            Flush();
        }

        public void Close()
        {
            stream.Dispose();
        }

        public void Flush()
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            networkStream.Write(buffer, 0, buffer.Length);
            stream.SetLength(0);
        }

        public void DrawLine(int x, int y, Direction direction, int length)
        {
            SetCursor(x, y);
            if (direction == Direction.Horizontal)
            {
                for (int i = 0; i < length; i++)
                    stream.Write(new byte[1] { 32 }, 0, 1);
            }
            if (direction == Direction.Vertical)
            {
                for (int i = 1; i <= length; i++)
                {
                    stream.Write(new byte[1] { 32 }, 0, 1);
                    SetCursor(x, y + i);
                }
            }
        }

        public void SetBackgroundColor(ColorStruct color)
        {
            byte[] buffer;
            if (color.Bright)
                buffer = new byte[6] { 27, 91, 49, 48, (byte)(48 + color.Color), 109 };
            else
                buffer = new byte[5] { 27, 91, 52, (byte)(48 + color.Color), 109 };
            stream.Write(buffer, 0, buffer.Length);
        }

        public void SetForegroundColor(ColorStruct color)
        {
            byte[] buffer;
            buffer = new byte[5] { 27, 91, 51, (byte)(48 + color.Color), 109 };
            if (color.Bright)
                buffer[2] = 57;
            stream.Write(buffer, 0, buffer.Length);
        }

        public void SetCursor(int x, int y)
        {
            byte[] xBytes = Encoding.ASCII.GetBytes((y + 1).ToString() + ";");
            byte[] yBytes = Encoding.ASCII.GetBytes((x + 1).ToString());
            byte[] buffer = new byte[3 + xBytes.Length + yBytes.Length];
            buffer[0] = 27;
            buffer[1] = 91;
            Array.Copy(xBytes, 0, buffer, 2, xBytes.Length);
            Array.Copy(yBytes, 0, buffer, 2 + xBytes.Length, yBytes.Length);
            buffer[2 + xBytes.Length + yBytes.Length] = 72;
            stream.Write(buffer, 0, buffer.Length);
        }

        public void SetCursorVisiblity(bool visiblity)
        {
            byte[] buffer = new byte[6] { 27, 91, 63, 50, 53, 104 };
            if (!visiblity)
                buffer[5] = 108;
            stream.Write(buffer, 0, buffer.Length);
        }

        public void Bell()
        {
            stream.Write(new byte[1] { 7 }, 0, 1);
        }

        public void ClearScreen()
        {
            ClearScreen(ClearMode.Entire);
        }

        public void ClearScreen(ClearMode clearMode)
        {
            int clearCode = 47 + (int)clearMode;
            byte[] buffer = new byte[4] { 27, 91, (byte)clearCode, 74 };
            stream.Write(buffer, 0, buffer.Length);
        }

        public void ClearLine()
        {
            byte[] buffer = new byte[4] { 27, 91, 50, 75 };
            stream.Write(buffer, 0, buffer.Length);
        }

        public void WriteText(string text)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(text);
            stream.Write(buffer, 0, buffer.Length);
        }

        public char? ReadChar()
        {
            while (true)
            {
                if (!networkStream.DataAvailable)
                    return null;
                int? temp = networkStream.ReadByte();
                if (isEscapeCode && ((temp > 64 && temp < 91) || (temp > 96 && temp < 123)))
                    isEscapeCode = false;
                else if (temp == 27) 
                    isEscapeCode = true;
                else if (isEscapeCode == false)
                    return (char?)temp;
            }
        }

        public string ReadLine()
        {
            StringBuilder builder = new StringBuilder();
            char? temp;
            while ((temp = ReadChar()) != 13)
            {
                if (temp > 31 && temp < 127)
                {
                    builder.Append(temp);
                    stream.Write(new byte[1] { (byte)temp }, 0, 1);
                }
                else if (temp == 8)
                {
                    stream.Write(new byte[1] { 8 }, 0, 1);
                    builder.Remove(builder.Length - 1, 1);
                }
            }
            return builder.ToString();
        }
    }
}
