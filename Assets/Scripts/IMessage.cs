using System.Collections.Generic;

// IEnumerable allows for iteration through data via foreach loops
public interface IMessage : IEnumerable<string>
{
    string getMessageID(); // The ID name of this message type

    int getSize(); // Returns number of data entries excluding the message's ID
    string getDataAt(int index); // Returns data entry from the given index
    void setDataAt(int index, string data); // Overwrite's data entry at the given index
}