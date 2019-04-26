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
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Net.WebSockets;
using System.Threading;

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
        Choices frases = new Choices();

        bool esperandoPorZona = false;
        bool esparandoPorDispositivo = false;

        Dtos.Devices[] devices;
        Dtos.Zones[] zones;


        //tmp
        string clientaddress = "http://colegiolaredencion.ddns.net:8010/api/";
        string userId = "auth0|5cb7a44bd7c2f8109ec501d6";
        ClientWebSocket ws = new ClientWebSocket();


        const string encender = "enciende";
        const string apagar = "apaga";

        public MainWindow()
        {
            InitializeComponent();
            devices = GetDispositivos();
            zones = GetZonas();
            ws.ConnectAsync(new Uri("ws://colegiolaredencion.ddns.net:8010/ws"), CancellationToken.None);
        }



        private void InitChoices()
        {

            //frases.Add("kira");

            /*comandos*/
            //frases.Add(encender);
            //frases.Add(apagar);

            /*zonas*/
            foreach (var zone in zones)
            {

                foreach (var device in devices.Where(a => a.zoneId == zone.id))
                {
                    string frasecompleta = string.Format("kira {0} el {1} de la {2}", encender, device.name, zone.name);
                    frases.Add(frasecompleta);

                    frasecompleta = string.Format("kira {0} el {1} de la {2}", apagar, device.name, zone.name);
                    frases.Add(frasecompleta);
                }

                //frases.Add(item.name);
            }

        }

        private Dtos.Devices[] GetDispositivos()
        {

            Dtos.Devices[] devices;

            using (WebClient client = new WebClient())
            {
                var uri = string.Format("{0}/devices/", clientaddress);

                Stream data = client.OpenRead(uri);
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();

                devices = new JavaScriptSerializer().Deserialize<Dtos.Devices[]>(s);

                Console.WriteLine(s);
                data.Close();
                reader.Close();

            }

            return devices;

        }

        private Dtos.Zones[] GetZonas()
        {

            Dtos.Zones[] zones;

            using (WebClient client = new WebClient())
            {
                var uri = string.Format("{0}/zones/{1}", clientaddress, userId);

                Stream data = client.OpenRead(uri);
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();

                zones = new JavaScriptSerializer().Deserialize<Dtos.Zones[]>(s);

                Console.WriteLine(s);
                data.Close();
                reader.Close();

            }

            return zones;

        }

        private void RealizarAccion(Dtos.Devices device)
        {
            var sending = Task.Run(async () =>
            {
                string line = "{event : \"OnToogle\", d1 : " + device.pinNumber.ToString() + ", d2 : \"" + device.nodeId + "\"}";

                var bytes = Encoding.UTF8.GetBytes(line);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            });

            var receiving = Receiving(ws);
        }

        private async Task Receiving(ClientWebSocket client)
        {
            var buffer = new byte[1024 * 4];

            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                    lblTextoReconocido.Content = Encoding.UTF8.GetString(buffer, 0, result.Count);

                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.SetInputToDefaultAudioDevice();
            InitChoices();
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

            if (e.Result.Confidence > 0.68)
            {


                lblTextoReconocido.Content = e.Result.Text;

                if (e.Result.Text.Contains("kira"))
                {

                    if (e.Result.Text.Contains(encender))
                    {

                        var dispositivos = devices.Where(a => e.Result.Text.Contains(a.name));

                        if (!dispositivos.Any())
                        {
                            kira.Speak("No econtre ningun dispositivo con ese nombre");
                            return;
                        }

                        var zoneId = zones.FirstOrDefault(a => e.Result.Text.Contains(a.name));

                        if (zoneId == null)
                        {
                            kira.Speak("No econtre ninguna zona con ese nombre");
                            return;
                        }

                        var device = dispositivos.FirstOrDefault(a => a.zoneId == zoneId.id);

                        if (device != null)
                        {
                            kira.Speak("Ok, realizando la accion");
                            RealizarAccion(device);
                        }
                    }

                    switch (e.Result.Text)
                    {
                        case encender:
                            {

                                kira.Speak("de cual zona es el dispositivo");
                                break;
                            }
                        default:
                            break;
                    }


                    //switch (e.Result.Text)
                    //{

                    //    case "enciende el bombillo":
                    //        {
                    //            toggle("01");
                    //            kira.Speak("Muy bien jefe");
                    //            break;
                    //        }

                    //    case encender:
                    //        {

                    //            break;
                    //        }

                    //    case "hola":
                    //        kira.Speak("Hola jefe, como estas?");
                    //        break;
                    //    case "abre el facebook":
                    //        kira.SpeakAsync("abriendo el facebook");
                    //        System.Diagnostics.Process.Start("www.facebook.com");
                    //        kira.Speak("facebook abierto");
                    //        break;
                    //    case "gracias":
                    //        int valor = r.Next(1, 4);
                    //        if (valor == 1)
                    //        {
                    //            kira.Speak("para eso estamos");
                    //        }
                    //        if (valor == 2)
                    //        {
                    //            kira.Speak("de nada jefecito");
                    //        }
                    //        if (valor == 3)
                    //        {
                    //            kira.Speak("ahora me das las gracias, que cara dura");
                    //        }
                    //        break;
                    //    case "abre el paint":
                    //        kira.Speak("Abriendo el paint");
                    //        System.Diagnostics.Process.Start("mspaint.exe");
                    //        break;
                    //    default:
                    //        break;
                    //}
                }
            }
        }

        private void toggle(string pinNumber)
        {
            WebClient client = new WebClient();

            var uri = string.Format("{0}/gpio/{1}/", clientaddress, pinNumber);

            Stream data = client.OpenRead(uri);
            StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
            Console.WriteLine(s);
            data.Close();
            reader.Close();

        }
    }
}
