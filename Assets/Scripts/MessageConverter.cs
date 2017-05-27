using System;

public static class MessageConverter
{
    private static readonly string DIVDER = "//";

    // Converts a message type to a string that can be sent over the network
    public static string messageToString(IMessage mess)
    {
        string str = mess.getMessageID();

        foreach(string data in mess)
        {
            str += DIVDER + data;
        }

        return str;
    }

    // Converts a string back into a message type
    public static MessageGeneric stringToMessage(string str)
    {
        string[] dataSet = str.Split(new string[] { DIVDER }, StringSplitOptions.None);

        MessageGeneric mess = new MessageGeneric(dataSet.Length - 1, dataSet[0]);

        for(int i = 1; i < dataSet.Length; i++)
        {
            mess.setDataAt(i - 1, dataSet[i]);
        }

        return mess;
    }
}