using System;
using System.IO;
using System.Net;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Media;
using System.Text;
using System.Windows.Shapes;

namespace WeatherApp {

    public partial class MainWindow : Window {
        private static XmlSerializer serializer;
        private static System.Collections.ObjectModel.ObservableCollection<City> Cities { get; set; }
        public MainWindow() {
            InitializeComponent();

            serializer = new XmlSerializer(typeof(ObservableCollection<City>));
            LoadSavedData();
            DataGrid.ItemsSource = Cities;

            if (!InternetAvailable) {
                System.Windows.MessageBox.Show("You have no internet connection!");
                return;
            }

            RefreshData();
        }

        private static bool InternetAvailable {
            get {
                try {
                    using (var client = new WebClient())
                    using (client.OpenRead("http://google.com/generate_204"))
                        return true;
                } catch {
                    return false;
                }
            }
        }

        private static void LoadSavedData() {
            try {
                using (StreamReader reader = new StreamReader("cities.xml")) {
                    Cities = serializer.Deserialize(reader) as ObservableCollection<City>;
                }
            } catch (Exception) {
                Cities = new ObservableCollection<City>();
            }
        }

        private static string GetData(string city) {
            string apiKey = "b62285d4e0f2a71e16f18edbd9af39d0";
            string uriString = "https://api.openweathermap.org/data/2.5/weather?q=" + city + "&appid=" + apiKey;
            try {
                WebRequest request = WebRequest.Create(uriString);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                return responseFromServer;
            } catch (Exception) {
                return null;
            }
        }

        private static City GetCity(string city) {
            try {
                string weatherData = GetData(city);
                City newCity = City.FromJson(weatherData);
                newCity.Main.Temp -= 273.15;
                newCity.Main.Temp = (int)Math.Round(newCity.Main.Temp, 0);
                return newCity;
            } catch (Exception) {
                return null;
            }
        }

        private static void RefreshData() {
            for (int i = 0; i < Cities.Count; i++) {
                Cities[i] = GetCity(Cities[i].Name);
                Thread.Sleep(50);
            }
            SaveCities();
        }

        private static void SaveCities() {
            using (StreamWriter writer = new StreamWriter("cities.xml")) {
                serializer.Serialize(writer, Cities);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e) {
            TextBox.Clear();
        }

        private void AddCityButton_Click(object sender, RoutedEventArgs e) {
            string text = TextBox.Text;
            if (!(text.Length == 0 || text == "Enter city")) {
                City newCity = GetCity(text);
                if (newCity == null) {
                    System.Windows.MessageBox.Show("Oops, something went wrong!");
                    return;
                }
                foreach (var city in Cities) {
                    if (city.Name.ToLower() == newCity.Name.ToLower()) {
                        System.Windows.MessageBox.Show("City already added!");
                        return;
                    }
                }
                Cities.Add(newCity);
                SaveCities();
                TextBox.Clear();
            } else {
                System.Windows.MessageBox.Show("Enter a city first!");
            }
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                AddCityButton_Click(this, new RoutedEventArgs());
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) {
            RefreshData();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) {
            City cityToDelete = ((FrameworkElement)sender).DataContext as City;
            Cities.Remove(cityToDelete);
            SaveCities();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.OriginalSource.ToString().ToLower().Contains("rectangle")) {
                this.DragMove();
            }            
        }

        private void PART_MAXIMIZE_RESTORE_Click(object sender, RoutedEventArgs e) {
            if (this.WindowState == System.Windows.WindowState.Normal) {
                this.WindowState = System.Windows.WindowState.Maximized;
            } else {
                this.WindowState = System.Windows.WindowState.Normal;
            }
            
        }

        private void PART_MINIMIZE_Click(object sender, RoutedEventArgs e) {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void PART_CLOSE_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        bool Resizing = false;
        private void Resize_Init(object sender, MouseButtonEventArgs e) {
            Rectangle senderRect = sender as Rectangle;
            if (sender != null) {
                Resizing = true;
                senderRect.CaptureMouse();
            }
        }

        private void Resize_End(object sender, MouseButtonEventArgs e) {
            Rectangle senderRect = sender as Rectangle;
            if (senderRect != null) {
                Resizing = false; ;
                senderRect.ReleaseMouseCapture();
            }
        }

        private void Resizing_Form(object sender, System.Windows.Input.MouseEventArgs e) {
            if (Resizing) {
                Rectangle senderRect = sender as Rectangle;
                Window mainWindow = senderRect.Tag as Window;
                if (senderRect != null) {
                    double width = e.GetPosition(mainWindow).X;
                    double height = e.GetPosition(mainWindow).Y;
                    senderRect.CaptureMouse();
                    if (senderRect.Name.ToLower().Contains("right")) {
                        width += 5;
                        if (width > 0) { 
                            mainWindow.Width = width;
                        }
                    }
                    if (senderRect.Name.ToLower().Contains("left")) {
                        width -= 5;
                        mainWindow.Left += width;
                        width = mainWindow.Width - width;
                        if (width > 0) {
                            mainWindow.Width = width;
                        }
                    }
                    if (senderRect.Name.ToLower().Contains("bottom")) {
                        height += 5;
                        if (height > 0) {
                            mainWindow.Height = height;
                        }
                    }
                    if (senderRect.Name.ToLower().Contains("top")) {
                        height -= 5;
                        mainWindow.Top += height;
                        height = mainWindow.Height - height;
                        if (height > 0) {
                            mainWindow.Height = height;
                        }
                    }
                }
            }
        }
    }
}
