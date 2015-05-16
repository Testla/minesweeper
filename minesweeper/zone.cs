using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI.Xaml.Media;

namespace minesweeper
{
    class Zone : INotifyPropertyChanged
    {
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;
        private string _content;

        public string content
        {
            get { return _content; }
            set 
            {
                _content = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("content");
            }
        }

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public string extra { get; set; }
        // whether it contains a mine or not
        public bool state { get; set; }

        // if it's revealed, the user has opened the zone
        public bool revealed { get; set; }
        /*public string color {
            get
            {
                return revealed ? "Gray" : "Transparent";
            }
        }*/

        private Brush _background;

        public Brush background
        {
            get
            {
                return _background;
            }
            set
            {
                _background = value;
                OnPropertyChanged("background");
            }
        }

        // whether the user has marked the zone as containing a mine
        public bool marked { get; set; }

        // the number of mines in the surrounding zones
        public int zoneValue { get; set; }

        public Zone(Brush brush)
        {
            background = brush;
        }
    }
}
