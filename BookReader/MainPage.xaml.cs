using UglyToad.PdfPig;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using System.Threading;

namespace BookReader {
    public partial class MainPage : ContentPage {
        string[] words;
        int currentIndex = 0;
        CancellationTokenSource cts;
        bool isPaused = false;
        string fileName = string.Empty;
        public MainPage() {
            InitializeComponent();
        }

        private async void OnPickPdfClicked(object sender, EventArgs e) {
            try {
                var file = await FilePicker.PickAsync(new PickOptions {
                    FileTypes = FilePickerFileType.Pdf,
                    PickerTitle = "Pick a PDF file"
                });

                if (file == null) return;
                fileName = file.FileName;
                using var stream = await file.OpenReadAsync();
                var text = ReadTextFromPdf(stream);
                words = text.Split(' ', '\n');
                currentIndex = Preferences.Default.Get(fileName, 0);

                TextLabel.Text = text;
                PauseButton.IsEnabled = true;
                ResumeButton.IsEnabled = false;

                cts = new CancellationTokenSource();
                await SpeakWords(cts.Token);
            }
            catch (Exception ex) {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
        private string ReadTextFromPdf(Stream pdfStream) {
            using var pdf = PdfDocument.Open(pdfStream);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString();
        }

        private async Task SpeakWords(CancellationToken token) {
            while (currentIndex < words.Length && !token.IsCancellationRequested) {
                string word = words[currentIndex];
                if (!string.IsNullOrEmpty(word)) {
                    HighlightWord(word);
                    await TextToSpeech.SpeakAsync(word, cancelToken: token);
                    await Task.Delay(100, token); // optional: adjust delay (100–500ms) for pacing
                    currentIndex++;
                    Preferences.Default.Set(fileName, currentIndex);
                }
                else currentIndex++;
            }

            PauseButton.IsEnabled = false;
            RestartButton.IsEnabled = true;
            ResumeButton.IsEnabled = false;
        }

        private void HighlightWord(string word) {
            var fullText = string.Join(" ", words);
            var highlighted = fullText.Replace(word, $"[h]{word}[/h]");
            TextLabel.Text = FormatHighlightedText(highlighted);
        }

        private string FormatHighlightedText(string raw) {
            // Not rich text, so simulate with uppercase or emoji markers
            return raw.Replace("[h]", "").Replace("[/h]", ""); // Or color using custom Label handler
        }

        private async void OnRestartClicked(object sender, EventArgs e) {
            cts.Cancel();
            PauseButton.IsEnabled = true;
            RestartButton.IsEnabled = false;
            ResumeButton.IsEnabled = false;
            currentIndex = 0;
            Preferences.Default.Set(fileName, currentIndex);
            cts = new CancellationTokenSource();
            await SpeakWords(cts.Token);
        }
        private void OnPauseClicked(object sender, EventArgs e) {
            isPaused = true;
            cts.Cancel();
            PauseButton.IsEnabled = false;
            ResumeButton.IsEnabled = true;
        }

        private async void OnResumeClicked(object sender, EventArgs e) {
            isPaused = false;
            cts = new CancellationTokenSource();
            PauseButton.IsEnabled = true;
            ResumeButton.IsEnabled = false;
            await SpeakWords(cts.Token);
        }
    }
}
