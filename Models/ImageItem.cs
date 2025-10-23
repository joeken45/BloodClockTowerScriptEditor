using System.ComponentModel;

namespace BloodClockTowerScriptEditor.Models
{
    public class ImageItem : INotifyPropertyChanged
    {
        private string _url = string.Empty;

        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged(nameof(Url));
                }
            }
        }

        public ImageItem(string url = "")
        {
            _url = url;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}