
using Gma.System.MouseKeyHook;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace MultimediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // đặt sự kiện sau khi chơi hết một bài hát
            player.MediaEnded += Player_MediaEnded;
            // đặt khoảng và sự kiện sau mỗi nhịp đồng hồ
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            // đặt nguồn cho ListBox hiển thị các bài hát
            listSongListBox.ItemsSource = playList;
            // đặt sự kiện cho phím bắt được
            _hook = Hook.GlobalEvents();
            _hook.KeyUp += _hook_KeyUp;
            // đặt DataContext cho cửa sổ
            this.DataContext = this;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _hook.KeyUp -= _hook_KeyUp;
            _hook.Dispose();
        }

        MediaPlayer player = new MediaPlayer();
        // bộ đếm thời gian cho bài hát
        DispatcherTimer timer = new DispatcherTimer();
        // danh sách các bài hát
        BindingList<FileInfo> playList = new BindingList<FileInfo>();
        // cho phép thay đổi slider bằng tay
        bool isManuelValueChanged = true;

        private IKeyboardMouseEvents _hook;
        // chế đô chơi lại các bài hát
        private int againMode = 0;

        // có đang chơi nhạc hay không
        private bool isPlaying = false;

        // chế độ chơi ngẫu nhiên các bài hát
        private bool randomMode = false;

        #region set, get
        public int AgainMode
        {
            get { return againMode; }
            set
            {
                againMode = value;
                OnPropertyChanged(nameof(AgainMode));
            }
        }

        public bool Playing
        {
            get { return isPlaying; }
            set
            {
                isPlaying = value;
                OnPropertyChanged(nameof(Playing));
            }
        }

        public bool RandomMode
        {
            get { return randomMode; }
            set
            {
                randomMode = value;
                OnPropertyChanged(nameof(randomMode));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
        #endregion

        #region hook
        private void _hook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            // phím Ctrl + shift + J - chơi lại bài hát hiện tại
            if (e.Control && e.Shift && e.KeyCode == System.Windows.Forms.Keys.J)
            {
                previouButton_Click(null, null);
                return;
            }

            // phím Ctrl + shift + J - chơi bài hát
            if (e.Control && e.Shift && e.KeyCode == System.Windows.Forms.Keys.K)
            {
                playButton_Click(null, null);
                return;
            }

            // phím Ctrl + shift + J - chơi bài hát tiếp theo
            if (e.Control && e.Shift && e.KeyCode == System.Windows.Forms.Keys.L)
            {
                playNextButton_Click(null, null);
                return;
            }
        }
        #endregion

        #region Media Player
        /// <summary>
        /// sự kiện khi chơi hết một bài hát
        /// </summary>
        private void Player_MediaEnded(object sender, EventArgs e)
        {
            int currentIndex = listSongListBox.SelectedIndex;

            switch (againMode)
            {
                case 0: // không chơi lại
                    if (currentIndex == playList.Count - 1)
                    {
                        // dừng phát khi chơi xong bài hát cuói cùng trong danh sách
                        player.Stop();
                        return;
                    }
                    else
                    {
                        // chơi bài tiếp theo
                        currentIndex++;
                    }
                    break;

                case 1: // chế độ chơi lại danh sách bài hát
                    if (currentIndex == playList.Count - 1)
                    {
                        // chơi lại từ đầu khi chơi xong bài hát cuối cùng
                        currentIndex = 0;
                    }
                    else
                    {
                        // chơi bài hát tiếp theo
                        currentIndex++;
                    }
                    break;

                case 2: // chơi lại bài hát vừa chơi xong
                    player.Stop();
                    player.Play();
                    return;

                default:
                    break;

            }

            // chơi bài hát ở chế độ chơi ngẫu nhiên
            if (randomMode)
            {
                Random random = new Random();
                currentIndex = random.Next(0, playList.Count);
            }

            playSong(currentIndex);
        }

        /// <summary>
        /// chơi bài một bài hát
        /// </summary>
        /// <param name="index">chỉ mục của bài hát được chơi</param>
        private void playSong(int index)
        {
            // mở bài hát
            player.Open(new Uri(playList[index].FullName, UriKind.Absolute));
            // đặt chỉ mục được chọn cho danh sách
            listSongListBox.SelectedIndex = index;
            // chờ đợi tải xong thông tin bài hát
            Thread.Sleep(1500);
            //chơi bài hát
            player.Play();
            // lấy và hiển thị thời lượng của bài hát
            songSlider.Maximum = (int)player.NaturalDuration.TimeSpan.TotalSeconds;
            durationSongTextBlock.Text = player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
        }

        /// <summary>
        /// chơi một bài hát
        /// </summary>
        /// <param name="selectedSong">thông tin của bài hát</param>
        private void playSong(FileInfo selectedSong)
        {
            // mở bài hát
            player.Open(new Uri(selectedSong.FullName, UriKind.Absolute));
            // chờ đợi tải xong thông tin bài hát
            Thread.Sleep(1500);
            // chơi bài hát
            player.Play();
            // lấy và hiển thị thời lượng của bài hát
            songSlider.Maximum = (int)player.NaturalDuration.TimeSpan.TotalSeconds;
            durationSongTextBlock.Text = player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
        }
        #endregion

        #region timer
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                isManuelValueChanged = false;
                songSlider.Value = (int)player.Position.TotalSeconds;
                currentPositionTextBlock.Text = player.Position.ToString(@"mm\:ss");
                isManuelValueChanged = true;
            }
        }
        #endregion

        #region Slider
        /// <summary>
        /// sự kiện khi thay đổi giá trị slider bằng tay
        /// </summary>
        private void songSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isManuelValueChanged)
            {
                // thay đổi vị trí đang phát của bài hát
                player.Position = TimeSpan.FromSeconds((int)songSlider.Value);
            }
        }
        #endregion

        #region Button
        /// <summary>
        /// sự kiện click - thêm bài hát vào playList
        /// </summary>
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            // mở một hộp thoại để chọn các file
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "MP3 files (*.mp3)|*.mp3|All files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                // lấy các file được chọn
                String[] fileNames = dialog.FileNames;

                foreach (String fileName in fileNames)
                {
                    playList.Add(new FileInfo(fileName));
                }
            }

        }

        /// <summary>
        /// sư kiện click - chơi bài hát tiếp theo
        /// </summary>
        private void playNextButton_Click(object sender, RoutedEventArgs e)
        {
            // chơi chọn và chơi bài hát tùy theo các chế độ
            if (listSongListBox.SelectedItem != null)
            {
                Player_MediaEnded(null, null);
            }
        }

        /// <summary>
        /// sự kiện click - chơi bài hát được chọn
        /// </summary>
        /// <param name="sender">nút play của bài hát được chọn</param>
        private void playSong_Click(object sender, RoutedEventArgs e)
        {

            Button button = sender as Button;
            FileInfo fileInfo = button.DataContext as FileInfo;

            // kiểm tra có phải là bài hát đang chơi
            if (listSongListBox.SelectedItem == fileInfo)
            {
                if (isPlaying)
                {
                    // dừng nếu đang chơi
                    player.Pause();
                }
                else
                {
                    // chơi tiếp nếu đang dừng
                    player.Play();
                }

                // chuyển trạng thái
                Playing = !Playing;

            }
            else
            {
                // chơi bài hát khác
                listSongListBox.SelectedItem = fileInfo;
                playSong(listSongListBox.SelectedItem as FileInfo);
                Playing = true;
            }

        }

        /// <summary>
        /// sự kiện click - chuyển đổi chế độ chơi lại
        /// </summary>
        private void playAgainButton_Click(object sender, RoutedEventArgs e)
        {
            // 0: không chơi lại
            // 1: chơi lại danh sách bài hát
            // 2: chơi lại một bài hát
            AgainMode = (AgainMode + 1) % 3;
        }

        /// <summary>
        /// sự kiện click - chuyển đối chế độ chơi ngẫu nhiên
        /// </summary>
        private void playRandomButton_Click(object sender, RoutedEventArgs e)
        {
            RandomMode = !RandomMode;
        }

        /// <summary>
        /// sự kiện click - chơi lại bài hát trước
        /// </summary>
        private void previouButton_Click(object sender, RoutedEventArgs e)
        {
            if (listSongListBox.SelectedItem != null)
            {
                player.Stop();
                player.Play();
            }
        }

        /// <summary>
        /// sự kiện click - chơi hoặc dừng các bài hát trong playList
        /// </summary>
        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            // kiểm tra playList có bài hát hay không
            if (playList.Count == 0)
            {
                MessageBox.Show("You don't have any song in the playlist!", "The Playlist Is Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // kiểm tra đã chọn bài hát muốn chơi hay chưa
            if (listSongListBox.SelectedItem == null)
            {
                // nếu chưa chọn bài hát, chơi bài hát đâu tiên
                playSong(0);
                Playing = true;
            }
            else
            {
                // kiểm tra đang chơi hay tạm dừng
                if (isPlaying)
                {
                    // dừng nếu đang chơi
                    player.Pause();
                }
                else
                {
                    // chơi nếu đang tạm dừng
                    player.Play();
                }

                // chuyển đổi trạng thái
                Playing = !Playing;
            }
        }

        /// <summary>
        /// sự kiện click - xóa một bài hát khỏi danh sách
        /// </summary>
        private void removeSongButton_Click(object sender, RoutedEventArgs e)
        {
            // lấy thông tin bài hát cần xóa
            Button button = sender as Button;
            FileInfo selectedItem = button.DataContext as FileInfo;
            // xóa bài hát
            playList.Remove(selectedItem);
        }
    }
    #endregion


}
