using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Packet class used for data representation - used for reading and writing data that will be send or were received, includes methods for de-/serialization of different data types
/// </summary>
public class Packet
{
    /// <summary>
    /// Dynamic data list
    /// </summary>
    private List<byte> packetDataBuffer; 
    /// <summary>
    /// Byte array for reading data
    /// </summary>
    private byte[] dataBuffer;
  
    private int readPosition; 

  
    public Packet() 
    {
        readPosition = 0; 
        packetDataBuffer = new List<byte>(); 
    }
    public Packet(int packetId) 
    {
        readPosition = 0;  
        packetDataBuffer = new List<byte>(); 
        Write(packetId); 
    }
    public Packet(byte[] data) 
    {
        readPosition = 0;  
        packetDataBuffer = new List<byte>(); 
        AddBytesToBuffers(data); 
    }

    /// <summary>
    /// Write string data
    /// </summary>
    public void Write(string text) 
    {
        Write(text.Length); 
        packetDataBuffer.AddRange(Encoding.ASCII.GetBytes(text));
    }
    /// <summary>
    /// Read string data
    /// </summary>
    public string ReadString(bool ChangeReadPosition = true)
    {
        try
        {
            int textLength = ReadInt(); 
            string text = Encoding.ASCII.GetString(dataBuffer, readPosition, textLength); 
            if (ChangeReadPosition == true && text.Length > 0) 
            {
                readPosition += textLength; 
            }
            return text; 
        }
        catch
        {
            throw new Exception("Error reading string");
        }
    }

    /// <summary>
    /// Write int data
    /// </summary>
    public void Write(int data) 
    {
        packetDataBuffer.AddRange(BitConverter.GetBytes(data));
    }
    /// <summary>
    /// Read int data
    /// </summary>
    public int ReadInt(bool changeReadPosition = true)
    {
        if (packetDataBuffer.Count > readPosition) 
        {
            int data = BitConverter.ToInt32(dataBuffer, readPosition); 
            if (changeReadPosition == true)
            {
                readPosition += 4;
            }
            return data; 
        }
        else
        {
            throw new Exception("Error reading int");
        }
    }

    /// <summary>
    /// Write float data
    /// </summary>
    public void Write(float data) 
    {
        packetDataBuffer.AddRange(BitConverter.GetBytes(data));
    }
    /// <summary>
    /// Read float data
    /// </summary>
    public float ReadFloat(bool changeReadPosition = true)
    {
        if (packetDataBuffer.Count > readPosition) 
        {
            float floatingPointNum = BitConverter.ToSingle(dataBuffer, readPosition);
            if (changeReadPosition == true)
            {
                readPosition += 4;
            }
            return floatingPointNum; 
        }
        else
        {
            throw new Exception("Error reading float");
        }
    }

    /// <summary>
    /// Write bool data
    /// </summary>
    public void Write(bool data) 
    {
        packetDataBuffer.AddRange(BitConverter.GetBytes(data));
    }
    /// <summary>
    /// Read bool data
    /// </summary>
    public bool ReadBool(bool changeReadPosition = true)
    {
        if (packetDataBuffer.Count > readPosition) 
        {
            bool data = BitConverter.ToBoolean(dataBuffer, readPosition);
            if (changeReadPosition)
            {
                readPosition += 1;
            }
            return data; 
        }
        else
        {
            throw new Exception("Error reading bool");
        }
    }

    /// <summary>
    /// Write byte data
    /// </summary>
    public void Write(byte data) 
    {
        packetDataBuffer.Add(data);
    }
    /// <summary>
    /// Read byte data
    /// </summary>
    public byte ReadByte(bool changeReadPosition = true)
    {
        if (packetDataBuffer.Count > readPosition)
        {
            byte data = dataBuffer[readPosition];
            if (changeReadPosition == true)
            {
                readPosition += 1;
            }
            return data;
        }
        else
        {
            throw new Exception("Error reading byte");
        }
    }

    /// <summary>
    /// Write byte array data
    /// </summary>
    public void Write(byte[] data) 
    {
        packetDataBuffer.AddRange(data);
    }
    /// <summary>
    /// read byte array data
    /// </summary>
    public byte[] ReadBytes(int arrayLength, bool changeReadPosition = true)
    {
        if (packetDataBuffer.Count > readPosition)
        {
            byte[] ByteArray = packetDataBuffer.GetRange(readPosition, arrayLength).ToArray();
            if (changeReadPosition == true)
            {
                readPosition += arrayLength;
            }
            return ByteArray;
        }
        else
        {
            throw new Exception("Error reading byte array");
        }
    }

    /// <summary>
    /// Write short data
    /// </summary>
    public void Write(short data)
    {
        packetDataBuffer.AddRange(BitConverter.GetBytes(data));
    }
    /// <summary>
    /// Read short data
    /// </summary>
    public short ReadShort(bool changeReadPos = true)
    {
        if (packetDataBuffer.Count > readPosition)
        {
            short data = BitConverter.ToInt16(dataBuffer, readPosition);
            if (changeReadPos == true)
            {
                readPosition += 2;
            }
            return data;
        }
        else
        {
            throw new Exception("Error reading short");
        }
    }
    /// <summary>
    /// Write Vector3 data
    /// </summary>
    public void Write(Vector3 vector)
    {
        Write(vector.x);
        Write(vector.y);
        Write(vector.z);
    }
    /// <summary>
    /// Read Vector3 data
    /// </summary>
    public Vector3 ReadVector3(bool changeReadPos = true)
    {
        float x = ReadFloat(changeReadPos);
        float y = ReadFloat(changeReadPos);
        float z = ReadFloat(changeReadPos);
        return new Vector3(x, y, z);
    }
    /// <summary>
    /// Write Quaternion data
    /// </summary>
    public void Write(Quaternion data)
    {
        Write(data.x);
        Write(data.y);
        Write(data.z);
        Write(data.w);
    }
    /// <summary>
    /// Read Quaternion data
    /// </summary>
    public Quaternion ReadQuaternion(bool changeReadPos = true)
    {
        float x = ReadFloat(changeReadPos);
        float y = ReadFloat(changeReadPos);
        float z = ReadFloat(changeReadPos);
        float w = ReadFloat(changeReadPos);
        return new Quaternion(x, y, z, w);
    }


   
    public void ClearPacket(bool clear) 
    {
        if (clear == false)
        {
            readPosition -= 4; 
        }
        else
        {
            packetDataBuffer.Clear();
            readPosition = 0;
            dataBuffer = null;
        }
    }
    public byte[] ConvertToArray() 
    {
        dataBuffer = packetDataBuffer.ToArray();
        return dataBuffer;
    }
    public void AddIntAtStart(int number) 
    {
        packetDataBuffer.InsertRange(0, BitConverter.GetBytes(number)); 
    }
    public void SizeOfDataAtStart() 
    {
        packetDataBuffer.InsertRange(0, BitConverter.GetBytes(packetDataBuffer.Count)); 
    }
    public int DataLength() 
    {
        return packetDataBuffer.Count;
    }
    public int UnreadLength() 
    {
        int actual_lenghth = DataLength();
        return actual_lenghth - readPosition; 
    }
    public void AddBytesToBuffers(byte[] Data) 
    {
        Write(Data); 
        dataBuffer = packetDataBuffer.ToArray(); 
    }
    /// <summary>
    /// Return data without HMAC
    /// </summary>
    public byte[] GetDataWithoutHmac()
    {
        return packetDataBuffer.GetRange(0, packetDataBuffer.Count - 32).ToArray();
    }
    /// <summary>
    /// return HMAC from data
    /// </summary>
    public byte[] GetHmac()
    {
        return packetDataBuffer.GetRange(packetDataBuffer.Count - 32, 32).ToArray();
    }

    /// <summary>
    /// Defined packets sent to Clients by Server
    /// </summary>
    public enum ServerPackets 
    {
        welcome = 1, 
        SpawnUser,
        DisconnectUser,
        Latency,
        Message,
        HmacKey,
        AuthToken,
        MessageAll,
        TransformUpdate,
        AnimationUpdate,
        BandwidthReply,
        ThroughputReply
    }

    /// <summary>
    /// Defined packets sent to Server by Client
    /// </summary>
    public enum ClientPackets 
    {
        welcomeReceived = 1, 
        MeassureRequest,
        DisconnectRequest,
        MessageToUserRequest,
        MessageToAllRequest,
        HmacKey,
        AutToken,
        TransformChange,
        AnimationChange,
        BandwidthRequest,
        ThroughputRequest
    }
}
