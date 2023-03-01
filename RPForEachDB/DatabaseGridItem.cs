using System.ComponentModel;

namespace RPForEachDB;

public class DatabaseGridItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public string Name { get; set; }
    private string status;
    public string Status
    {
        get => status;
        set => status = value;
    }
    public bool Checked { get; set; }
    private string lastMessage;
    public string LastMessage
    {
        get => lastMessage;
        set => lastMessage = value;
    }

}