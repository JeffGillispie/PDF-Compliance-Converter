using System;
using System.ComponentModel;

namespace PDF_Compliance_Converter
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var args = new PropertyChangedEventArgs(propertyName);
            PropertyChanged?.Invoke(this, args);
        }
    }
}
