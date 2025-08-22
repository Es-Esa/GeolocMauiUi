using ClientApp.Core.Detection;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Media;
using System.IO;

namespace ClientApp.Views
{
   
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Tämä luokka on pääsivun määrittely joka sisältää mallin alustuksen ja analysoinnin.
        /// </summary>
        private readonly IObjectDetector _objectDetector;
        private bool _isModelInitialized = false;
        private Stream? _selectedImageStream = null;

        /// <summary>
        /// MainPage constructor. Tämän luokka on pääsivun määrittely.
        /// </summary>
        /// <param name="objectDetector"></param>
        public MainPage(IObjectDetector objectDetector)
        {
            InitializeComponent();
            _objectDetector = objectDetector;
            ResultsListView.ItemsSource = new ObservableCollection<string>();
        }


        /// <summary>
        /// Tämä metodi alustaa mallin ja tarkistaa onko se valmis analysoimaan kuvia.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_isModelInitialized)
            {
                await InitializeModelAsync();
            }
        }
        /// <summary>
        /// InitializeModelAsync metodi alustaa mallin ja tarkistaa onko se valmis analysoimaan kuvia.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeModelAsync()
        {
            // Näytetään latausindikaattori
            // ja estetään nappien käyttö mallin alustuksen aikana.
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            SelectImageBtn.IsEnabled = false;
            AnalyzeBtn.IsEnabled = false;

            // Yritetään alustaa malli ja tarkistaa onko se valmis analysoimaan kuvia.
            try
            {
                await _objectDetector.InitializeAsync();
                _isModelInitialized = true;
                SelectImageBtn.IsEnabled = true;
                await DisplayAlert("Success", "Model initialized successfully.", "OK");
            }
            catch (Exception ex)
            {
                _isModelInitialized = false;
                 await DisplayAlert("Error", $"Model initialization failed: {ex.ToString()}", "OK"); 
                 SelectImageBtn.IsEnabled = false;
                 AnalyzeBtn.IsEnabled = false;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                 AnalyzeBtn.IsEnabled = _isModelInitialized && _selectedImageStream != null;
            }
        }

        /// <summary>
        /// Tämä metodi avaa valokuvan valitsimen ja valitsee kuvan analysoitavaksi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnSelectImageClicked(object sender, EventArgs e)
        {

             if (!MediaPicker.Default.IsCaptureSupported)
            {
                
                 await DisplayAlert("Error", "Photo capture/picking may not be fully supported on this device.", "OK");

            }

            try
            {
                FileResult photo = await MediaPicker.Default.PickPhotoAsync();

                if (photo != null)
                {
                    var resultsCollection = ResultsListView.ItemsSource as ObservableCollection<string>;
                    resultsCollection?.Clear();

                     
                     _selectedImageStream?.Dispose();

              
                    _selectedImageStream = await photo.OpenReadAsync();

                 
                    var memoryStream = new MemoryStream();
                    await _selectedImageStream.CopyToAsync(memoryStream);
                    _selectedImageStream.Dispose();
                    memoryStream.Position = 0; 
                    _selectedImageStream = memoryStream;

                    
                    SelectedImage.Source = ImageSource.FromStream(() => {
                        
                        var newStream = new MemoryStream();
                        _selectedImageStream.Position = 0; 
                        _selectedImageStream.CopyTo(newStream);
                        _selectedImageStream.Position = 0;
                        newStream.Position = 0;
                        return newStream;
                     });

                    AnalyzeBtn.IsEnabled = _isModelInitialized;
                }
                else
                {
                     _selectedImageStream?.Dispose();
                     _selectedImageStream = null;
                     SelectedImage.Source = null;
                     AnalyzeBtn.IsEnabled = false;
                }
            }
             catch (PermissionException pEx)
            {
                 await DisplayAlert("Permission Error", $"Permission needed to access photos: {pEx.Message}", "OK");
                 _selectedImageStream?.Dispose();
                 _selectedImageStream = null;
                 SelectedImage.Source = null;
                 AnalyzeBtn.IsEnabled = false;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to select image: {ex.Message}", "OK");
                 _selectedImageStream?.Dispose();
                 _selectedImageStream = null;
                 SelectedImage.Source = null;
                 AnalyzeBtn.IsEnabled = false;
            }
        }

        /// <summary>
        /// Tämä metodi analysoi valitun kuvan ja näyttää tulokset.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnAnalyzeClicked(object sender, EventArgs e)
        {
            if (!_isModelInitialized)
            {
                await DisplayAlert("Error", "Model is not initialized.", "OK");
                return;
            }
            if (_selectedImageStream == null || !_selectedImageStream.CanRead || _selectedImageStream.Length == 0)
            {
                 await DisplayAlert("Error", "Please select a valid image first.", "OK");
                 return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            SelectImageBtn.IsEnabled = false;
            AnalyzeBtn.IsEnabled = false;
            var resultsCollection = ResultsListView.ItemsSource as ObservableCollection<string>; 
            resultsCollection?.Clear();

            try
            {
                 _selectedImageStream.Position = 0;

                List<YoloBoundingBox> detections = await _objectDetector.DetectAsync(_selectedImageStream);

                if (detections != null && detections.Any())
                {
                    foreach (var detection in detections)
                    {
                         
                         string resultString = $"{detection.Label}: {detection.Score:P1} [X:{detection.TopLeftX},Y:{detection.TopLeftY},W:{detection.BottomRightX - detection.TopLeftX},H:{detection.BottomRightY - detection.TopLeftY}]";
                         resultsCollection?.Add(resultString);
                    }
                }
                else
                {
                    resultsCollection?.Add("No detections found.");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Analysis failed: {ex.ToString()}", "OK");
                resultsCollection?.Add("Analysis error.");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                SelectImageBtn.IsEnabled = true;
                AnalyzeBtn.IsEnabled = true;
                 
                 if (_selectedImageStream != null && _selectedImageStream.CanSeek)
                 {
                     _selectedImageStream.Position = 0;
                 }
            }
        }


        /// <summary>
        /// Tämä metodi avaa kameran ja live havaintoonnin.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnLiveDetectClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(CameraDetectionPage));
        }

        /// <summary>
        /// Tämä metodi avaa karttasivun ja näyttää havaintotiedot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnViewMapClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(MapPage));
        }

        /// <summary>
        /// Tämä metodi vapauttaa resurssit kun sivu suljetaan.
        /// </summary>
        protected override void OnDisappearing()
         {
             base.OnDisappearing();
             _selectedImageStream?.Dispose();
             _selectedImageStream = null;
         }
    }
}
