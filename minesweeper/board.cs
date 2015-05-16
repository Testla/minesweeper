using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.ComponentModel;

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;

namespace minesweeper
{
    class Board : INotifyPropertyChanged
    {
        public delegate void SomethingHappened();
        public SomethingHappened win;
        public SomethingHappened lose;
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;
        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        public vector[] actualBoardSize = new vector[] {
            new vector(9, 9),
            new vector(16, 16),
            new vector(16, 30)
        };
        private vector[] neighbours = new vector[] {
            new vector(-1, -1),
            new vector(-1, 0),
            new vector(-1, 1),
            new vector(0, -1),
            new vector(0, 1),
            new vector(1, -1),
            new vector(1, 0),
            new vector(1, 1)
        };
        private int[] numBomb = { 10, 40, 99 };
        private Random rand = new Random();
        public enum state
        {
            playing,
            waiting
        };
        public enum size
        {
            small,
            medium,
            big
        };
        private Zone[,] zones;
        public List<Zone> zoneList;
        public state currentBoardState { get; private set; }
        public size currentBoardSize { get; private set; }
        public int unRevealedLeft { get; private set; }
        private int _minesLeft;
        public int minesLeft
        {
            get
            {
                return _minesLeft;
            }
            set
            {
                _minesLeft = value;
                OnPropertyChanged("minesLeft");
            }
        }

        static private LinearGradientBrush grayBrush, transparentBrush;
        static private ImageBrush flagBrush, mineBrush, wrongMarkBrush;

        public Board()
        {
            // set state
            currentBoardState = state.waiting;
            setBoardSize(size.small);

            // initialize lose delegate
            lose = revealAllMines;

            // initialize background brush(pure color and image)
            GradientStopCollection gradientStopCollection = new GradientStopCollection();
            GradientStop gradientStop = new GradientStop();
            gradientStop.Color = Windows.UI.Colors.Gray;
            gradientStopCollection.Add(gradientStop);
            grayBrush = new LinearGradientBrush(gradientStopCollection, 0);

            transparentBrush = new LinearGradientBrush(new GradientStopCollection(), 0);

            flagBrush = new Windows.UI.Xaml.Media.ImageBrush();
            flagBrush.ImageSource = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/flag.png"));
            flagBrush.AlignmentX = AlignmentX.Center;
            flagBrush.AlignmentY = AlignmentY.Center;
            flagBrush.Stretch = Stretch.UniformToFill;

            mineBrush = new Windows.UI.Xaml.Media.ImageBrush();
            mineBrush.ImageSource = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/mine.png"));
            mineBrush.AlignmentX = AlignmentX.Center;
            mineBrush.AlignmentY = AlignmentY.Center;
            mineBrush.Stretch = Stretch.UniformToFill;

            wrongMarkBrush = new Windows.UI.Xaml.Media.ImageBrush();
            wrongMarkBrush.ImageSource = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/wrong mark.png"));
            wrongMarkBrush.AlignmentX = AlignmentX.Center;
            wrongMarkBrush.AlignmentY = AlignmentY.Center;
            wrongMarkBrush.Stretch = Stretch.UniformToFill;
        }
        
        // test if a position is in the board
        private bool isValid(vector v)
        {
            return v.x >= 0 && v.x < actualBoardSize[(int)currentBoardSize].x
                    && v.y >= 0 && v.y < actualBoardSize[(int)currentBoardSize].y;
        }
        
        public void setBoardSize(size newBoardSize)
        {
            currentBoardState = state.waiting;
            currentBoardSize = newBoardSize;
            reset();
        }
        
        // resets the whole board according to current boardSize
        public void reset()
        {
            resetZones();
            randomBomb();
            storeToList();
            currentBoardState = state.playing;
        }
        
        private void resetZones()
        {
            zones = new Zone[actualBoardSize[(int)currentBoardSize].x, actualBoardSize[(int)currentBoardSize].y];
            minesLeft = numBomb[(int)currentBoardSize];
            unRevealedLeft = actualBoardSize[(int)currentBoardSize].x * actualBoardSize[(int)currentBoardSize].y - minesLeft;
            // reset all zones
            for (int i = 0; i < actualBoardSize[(int)currentBoardSize].x; ++i)
                for (int j = 0; j < actualBoardSize[(int)currentBoardSize].y; ++j)
                {
                    zones[i, j] = new Zone(transparentBrush);
                    zones[i, j].extra = (i * actualBoardSize[(int)currentBoardSize].y + j).ToString();
                }
        }

        // randomly place bomb
        private void randomBomb()
        {
            for (int i = 0; i < numBomb[(int)currentBoardSize]; ++i)
            {
                int toSetX, toSetY;
                do
                {
                    toSetX = rand.Next(actualBoardSize[(int)currentBoardSize].x);
                    toSetY = rand.Next(actualBoardSize[(int)currentBoardSize].y);
                } while (zones[toSetX, toSetY].state == true);
                placeBomb(new vector(toSetX, toSetY));
            }
        }

        // place a bomb and update its neighbour
        private void placeBomb(vector target)
        {
            zones[target.x, target.y].state = true;
            for (int i = 0; i < neighbours.Length; ++i)
                if (isValid(new vector(target.x + neighbours[i].x, target.y + neighbours[i].y)))
                    zones[target.x + neighbours[i].x, target.y + neighbours[i].y].zoneValue += 1;
        }

        // copy to zone_list
        private void storeToList()
        {
            zoneList = new List<Zone> { };
            for (int i = 0; i < actualBoardSize[(int)currentBoardSize].x; ++i)
                for (int j = 0; j < actualBoardSize[(int)currentBoardSize].y; ++j)
                    zoneList.Add(zones[i, j]);
        }

        public void open(int zoneToOpen) {
            if (currentBoardState == state.playing) { 
                int x = zoneToOpen / actualBoardSize[(int)currentBoardSize].y,
                    y = zoneToOpen % actualBoardSize[(int)currentBoardSize].y;
                if (!zones[x, y].revealed && !zones[x, y].marked)
                {
                    zones[x, y].revealed = true;
                    zoneList[zoneToOpen].background = grayBrush;
                    if (zones[x, y].state)
                    {
                        // lose
                        zoneList[zoneToOpen].revealed = true;
                        zoneList[zoneToOpen].background = mineBrush;
                        currentBoardState = state.waiting;
                        lose();
                        return;
                    }
                    if (zones[x, y].zoneValue != 0)
                    {
                        zoneList[zoneToOpen].content = zones[x, y].zoneValue.ToString();
                    }
                    else
                    {
                        // zoneValue == 0, auto open
                        for (int i = 0; i < neighbours.Length; ++i)
                            if (isValid(new vector(x + neighbours[i].x, y + neighbours[i].y)))
                                open(zoneToOpen + neighbours[i].x * actualBoardSize[(int)currentBoardSize].y + neighbours[i].y);
                    }
                    --unRevealedLeft;
                    if (unRevealedLeft == 0)
                    {
                        // win
                        currentBoardState = state.waiting;
                        win();
                    }
                    return;
                }
            }
            return;
        }

        public void mark(int zoneToMark)
        {
            if (currentBoardState == state.playing)
            {
                int x = zoneToMark / actualBoardSize[(int)currentBoardSize].y,
                    y = zoneToMark % actualBoardSize[(int)currentBoardSize].y;
                if (!zones[x, y].revealed)
                {
                    zones[x, y].marked = !zones[x, y].marked;
                    if (zones[x, y].marked)
                        zoneList[zoneToMark].background = flagBrush;
                    else
                        zoneList[zoneToMark].background = transparentBrush;
                    minesLeft += zones[x, y].marked ? -1 : 1;
                }
            }
        }

        private void revealAllMines()
        {
            for (int i = 0; i < actualBoardSize[(int)currentBoardSize].x; ++i)
                for (int j = 0; j < actualBoardSize[(int)currentBoardSize].y; ++j)
                    if (zones[i, j].state)
                        zoneList[i * actualBoardSize[(int)currentBoardSize].y + j].background = mineBrush;
        }

        public void explore(int zoneToExplore) {
            if (currentBoardState == state.playing)
            {
                int x = zoneToExplore / actualBoardSize[(int)currentBoardSize].y,
                    y = zoneToExplore % actualBoardSize[(int)currentBoardSize].y;
                if (zones[x, y].revealed) {
                    bool safe = true;
                    for (int i = 0; i < neighbours.Length; ++i)
                        if (isValid(new vector(x + neighbours[i].x, y + neighbours[i].y))) 
                        {
                            Zone targetZone = zones[x + neighbours[i].x, y + neighbours[i].y];
                            if (!targetZone.revealed)
                            {
                                if (targetZone.state && !targetZone.marked)
                                {
                                    safe = false;
                                }
                                // wrong mark
                                if (targetZone.marked && !targetZone.state)
                                {
                                    currentBoardState = state.waiting;
                                    zoneList[int.Parse(targetZone.extra)].background = wrongMarkBrush;
                                    // lose
                                    lose();
                                }
                            }
                        }
                    if (safe)
                        for (int i = 0; i < neighbours.Length; ++i)
                            if (isValid(new vector(x + neighbours[i].x, y + neighbours[i].y))) 
                            {
                                Zone targetZone = zones[x + neighbours[i].x, y + neighbours[i].y];
                                if (!targetZone.revealed)
                                    open(int.Parse(targetZone.extra));
                            }
                }
            }
        }

        // 用来发给客户端
        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < actualBoardSize[(int)currentBoardSize].x; ++i)
                for (int j = 0; j < actualBoardSize[(int)currentBoardSize].y; ++j)
                    result += zones[i, j].state ? "1" : "0";
            return result;
        }
        // 接收地图
        public void FromString(string source)
        {
            currentBoardState = state.waiting;
            for (int i = 0; i <= (int)size.big; ++i)
                if (source.Length == actualBoardSize[i].x * actualBoardSize[i].y)
                    currentBoardSize = (size)i;
            resetZones();
            for (int i = 0; i < actualBoardSize[(int)currentBoardSize].x; ++i)
                for (int j = 0; j < actualBoardSize[(int)currentBoardSize].y; ++j)
                    if (source.ElementAt(i * actualBoardSize[(int)currentBoardSize].y + j) == '1')
                        placeBomb(new vector(i, j));
            storeToList();
            currentBoardState = state.playing;
        }
    }
}
