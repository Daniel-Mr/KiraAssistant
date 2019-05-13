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


    //numerop 2
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


            frases.Add("kira abre el facebook");
            frases.Add("kira hola");
            frases.Add("kira Que hora es");
            frases.Add("kira desactivate");
            frases.Add("kira Prepárame un sandwich");
            frases.Add("kira te puedo contar un secreto");
            frases.Add("kira yo te amo ");
            frases.Add("kira abre el google ");
            frases.Add("kira presenta por mi porfavor");
            frases.Add("kira ve a la siguiente habitacion ");
            frases.Add("kira verdad que esta habitacion es tu favorita");
            frases.Add("kira saluda a nuestros invitados  ");
           
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
            //Choices frases = new Choices(new string[] { "hola", "enciende el bombillo", "como estas", "gracias", "abre el facebook", "abre el paint" });
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
                    switch (e.Result.Text)
                    {
                        case "kira hola":
                            int Hola = r.Next(1, 8);
                            if (Hola == 1)
                            {
                                kira.Speak("Bonito día Señor");
                            }
                            if (Hola == 2)
                            {
                                kira.Speak("Holaa");
                            }
                            if (Hola == 3)
                            {
                                kira.Speak("¿Le puedo ayudar en algo?");
                            }
                            if (Hola == 4)
                            {
                                kira.Speak("Ordene señor");
                            }
                            if (Hola == 5)
                            {
                                kira.Speak("Digame");
                            }
                            if (Hola == 6)
                            {
                                kira.Speak("¿Que onda?");
                            }
                            if (Hola == 7)
                            {
                                kira.Speak("Que lo que");
                            }
                            if (Hola == 8)
                            {
                                kira.Speak("Dime a ver");
                            }
                            break;

                        case "kira Que hora es":
                            kira.SpeakAsync("Son las " + DateTime.Now.ToShortTimeString() + ",algo mas señor ");
                            break;

                        case "kira desactivate":
                            kira.SpeakAsync("como ordene señor");
                            Close();
                            break;

                        case "kira Prepárame un sandwich":
                            kira.SpeakAsync("que tal si mejor enciendo el gas de la cocina y te lo haces tu");
                            break;

                        case "kira":
                            kira.SpeakAsync("digame señor");
                            break;

                        case "kira te puedo contar un secreto":
                            kira.SpeakAsync("si puedes contarme lo que sea");
                            break;

                        case"kira yo te amo":
                            kira.SpeakAsync("ahh pues, si buenas es el nueve once, si le llamo porque tengo a un negro aqui en la casa, vengan rapido a sacarlo ");
                            break;

                        case "kira abre el google":
                            kira.SpeakAsync("abriendo el google");
                            System.Diagnostics.Process.Start("www.google.com");
                            kira.Speak("google abierto, algo mas con lo que pueda ayudarle señor");
                            break;

                        case "kira presenta por mi porfavor":
                            kira.SpeakAsync("ok con gusto, ok niños y niñas, mi nombre es kira estamos actualmente en la cocina, aqui como todos sabran se cocinan los platillos para una familia normal, lo cual este men no es, pero bueno eso es otra cosa, aqui yo estoy encargada de ayudar y facilitar cualquier herramienta que el cocinero necsite, ya sea que encienda el gas de la cocina, o encienda el bombillo de esta,  ");
                            break;

                        case "kira ve a la siguiente habitacion ":
                            kira.SpeakAsync("a que parte de la cas iremos");                           
                            break;

                        case "kira verdad que esta habitacion es tu favorita":
                            kira.SpeakAsync("no a decir verdad prefiero mas la sala, puedo ver peliculas sin que me llames cada segundo");
                            break;

                        case "kira saluda a nuestros invitados ":
                            kira.SpeakAsync("hola es un placer estar aqui con ustedes espero que le guste su estadia aqui ");
                            break;




                    }

                    if (e.Result.Text.Contains(encender))
                    {

                        var dispositivos = devices.Where(a => e.Result.Text.Contains(a.name));

                        if (!dispositivos.Any())
                        {
                            kira.SpeakAsync("No encontre ningun dispositivo con ese nombre");
                            return;
                        }

                        var zoneId = zones.FirstOrDefault(a => e.Result.Text.Contains(a.name));

                        if (zoneId == null)
                        {
                            kira.SpeakAsync("No econtre ninguna zona con ese nombre");
                            return;
                        }

                        var device = dispositivos.FirstOrDefault(a => a.zoneId == zoneId.id);

                        if (device != null)
                        {
                            kira.SpeakAsync("Ok, realizando la accion señor");
                            RealizarAccion(device);
                        }
                    }
                    
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
