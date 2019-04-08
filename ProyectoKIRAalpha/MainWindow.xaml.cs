using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Speech.Recognition; // reconocedor
using System.Speech.Synthesis; // kira
using System.Globalization;
namespace ProyectoKIRAalpha
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SpeechRecognitionEngine rec = new SpeechRecognitionEngine();
        SpeechSynthesizer kira = new SpeechSynthesizer();
        Random r = new Random();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.SetInputToDefaultAudioDevice();

            Choices frases = new Choices(new string[] { "hola", "como estas", "gracias", "abre el facebook","abre el paint" });

            GrammarBuilder gb = new GrammarBuilder();
            gb.Append(frases);

            Grammar gramaticas = new Grammar(gb);

            rec.LoadGrammar(gramaticas);
            rec.RecognizeAsync(RecognizeMode.Multiple);

            rec.SpeechRecognized += Rec_SpeechRecognized;
            rec.AudioLevelUpdated += Rec_AudioLevelUpdated;
        }

        private void Rec_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            pbAudio.Value = e.AudioLevel;
        }

        private void Rec_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //kira.Speak(e.Result.Text);
            lblTextoReconocido.Content = e.Result.Text;
            switch (e.Result.Text)
            {
                case "hola":
                    kira.Speak("Hola jefe, como estas?");
                    break;
                case "abre el facebook":
                    kira.SpeakAsync("abriendo el facebook");
                    System.Diagnostics.Process.Start("www.facebook.com");
                    kira.Speak("facebook abierto");
                    break;
                case "gracias":
                    int valor = r.Next(1,4);
                    if (valor == 1)
                    {
                        kira.Speak("para eso estamos");
                    }
                    if (valor == 2)
                    {
                        kira.Speak("de nada jefecito");
                    }
                    if (valor == 3)
                    {
                        kira.Speak("ahora me das las gracias, que cara dura");
                    }
                    break;
                case "abre el paint":
                    kira.Speak("Abriendo el paint");
                    System.Diagnostics.Process.Start("mspaint.exe");
                    break;
                default:
                    break;
            }
        }
    }
}
