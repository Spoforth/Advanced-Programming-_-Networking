using System;
using System.Collections;
using System.Collections.Generic;

public class MessageGeneric : IMessage
{
    private readonly string[] rawData;
    private readonly string idName;

    public MessageGeneric(int size, string idName)
    {
        this.rawData = new string[size];
        this.idName = idName;
    }

    public string getMessageID()
    {
        return idName;
    }

    public int getSize()
    {
        return rawData.Length;
    }

    public string getDataAt(int index)
    {
        return rawData[index];
    }

    public void setDataAt(int index, string data)
    {
        this.rawData[index] = data;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)rawData).GetEnumerator();
    }
}