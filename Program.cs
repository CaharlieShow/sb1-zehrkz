using System;
using System.Threading;
using System.Device.Gpio;
using System.IO.Ports;
using System.Net;
using System.Text;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Networking;
using nanoFramework.Device.Gpio;
using nanoFramework.WebServer;

namespace ESP32.BasicExample
{
    public class Program
    {
        private static GpioPin _ledIndicador;
        private static SerialPort _puertoSerie;
        private static WebServer _servidorWeb;
        private static GestorTanques _gestorTanques;
        private static ClienteMqtt _clienteMqtt;
        private static Timer _timerLectura;
        
        // Configuración WiFi
        private const string WIFI_SSID = "ESP32-AP";
        private const string WIFI_PASSWORD = "password123";
        
        // Credenciales de acceso web
        private const string USUARIO_WEB = "admin";
        private const string PASSWORD_WEB = "Asp123";
        
        // Configuración del puerto serie RS232
        private const int PIN_TRANSMISION = 17;  // Pin TX
        private const int PIN_RECEPCION = 16;    // Pin RX

        // Configuración MQTT
        private const string BROKER_MQTT = "192.168.1.100"; // Cambiar por la IP de tu broker
        private const int PUERTO_MQTT = 1883;
        
        public static void Main()
        {
            InicializarPuertos();
            ConfigurarPuntoAccesoWiFi();
            IniciarServidorWeb();
            IniciarMonitoreoTanques();
            
            Thread.Sleep(Timeout.Infinite);
        }

        private static void InicializarPuertos()
        {
            // Configuración del LED indicador
            _ledIndicador = GpioController.GetDefault().OpenPin(2, PinMode.Output);

            // Configuración de pines para comunicación serie
            Configuration.SetPinFunction(PIN_TRANSMISION, DeviceFunction.COM2_TX);
            Configuration.SetPinFunction(PIN_RECEPCION, DeviceFunction.COM2_RX);

            // Inicialización del puerto serie RS232
            _puertoSerie = new SerialPort("COM2")
            {
                BaudRate = 115200,
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None
            };

            try
            {
                _puertoSerie.Open();
                Console.WriteLine("Puerto serie RS232 iniciado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar puerto serie: {ex.Message}");
            }
        }

        private static void IniciarMonitoreoTanques()
        {
            _gestorTanques = new GestorTanques(_puertoSerie);
            _clienteMqtt = new ClienteMqtt(BROKER_MQTT, PUERTO_MQTT);

            // Iniciar timer para lectura cada 30 segundos
            _timerLectura = new Timer(RealizarLecturaYPublicacion, null, 0, 30000);
        }

        private static void RealizarLecturaYPublicacion(object state)
        {
            _gestorTanques.RealizarLecturaTanques();
            string estadoJson = _gestorTanques.ObtenerEstadoTanquesJson();
            _clienteMqtt.PublicarEstadoTanques(estadoJson);
        }

        private static void ConfigurarPuntoAccesoWiFi()
        {
            var configuracionWiFi = new WifiAPConfiguration()
            {
                Ssid = WIFI_SSID,
                Password = WIFI_PASSWORD,
                Authentication = AuthenticationType.WPA2,
                MaxConnections = 10
            };

            var conexionExitosa = WifiNetworkHelper.SetupNetwork(configuracionWiFi);
            if (conexionExitosa)
            {
                var direccionIP = WifiNetworkHelper.GetCurrentIPAddress();
                Console.WriteLine($"Punto de acceso WiFi iniciado en: {direccionIP}");
            }
            else
            {
                Console.WriteLine("Error al iniciar el punto de acceso WiFi");
            }
        }

        private static void IniciarServidorWeb()
        {
            _servidorWeb = new WebServer(80, HttpProtocol.Http);

            _servidorWeb.AddRoute("/", HttpMethod.Get, VerificarAutenticacion(ManejadorPaginaPrincipal));
            _servidorWeb.AddRoute("/led", HttpMethod.Get, VerificarAutenticacion(ManejadorControlLED));
            _servidorWeb.AddRoute("/estado", HttpMethod.Get, VerificarAutenticacion(ManejadorEstadoSistema));
            _servidorWeb.AddRoute("/tanques", HttpMethod.Get, VerificarAutenticacion(ManejadorEstadoTanques));

            _servidorWeb.Start();
            Console.WriteLine("Servidor web iniciado correctamente");
        }

        private static WebServerEventHandler VerificarAutenticacion(WebServerEventHandler manejador)
        {
            return (WebServerEventArgs e) =>
            {
                string autorizacion = e.Context.Request.Headers["Authorization"];
                
                if (string.IsNullOrEmpty(autorizacion))
                {
                    SolicitarAutenticacion(e);
                    return;
                }

                try
                {
                    string credencialesBase64 = autorizacion.Substring("Basic ".Length);
                    byte[] credencialesBytes = Convert.FromBase64String(credencialesBase64);
                    string credenciales = new string(Encoding.UTF8.GetChars(credencialesBytes));
                    string[] partes = credenciales.Split(':');

                    if (partes.Length == 2 && 
                        partes[0] == USUARIO_WEB && 
                        partes[1] == PASSWORD_WEB)
                    {
                        manejador(e);
                        return;
                    }
                }
                catch
                {
                    // Error al decodificar las credenciales
                }

                SolicitarAutenticacion(e);
            };
        }

        private static void SolicitarAutenticacion(WebServerEventArgs e)
        {
            e.Context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"ESP32 Control Panel\"");
            e.Context.Response.StatusCode = 401;
            string mensajeError = "<html><body><h1>Acceso Denegado</h1><p>Se requiere autenticación.</p></body></html>";
            EnviarRespuestaHTML(e, mensajeError);
        }

        private static void ManejadorPaginaPrincipal(WebServerEventArgs e)
        {
            string paginaHTML = @"<html><body>
                <h1>Panel de Control ESP32</h1>
                <p><a href='/led'>Controlar LED</a></p>
                <p><a href='/estado'>Ver Estado del Sistema</a></p>
                <p><a href='/tanques'>Ver Estado de Tanques</a></p>
                </body></html>";
            
            EnviarRespuestaHTML(e, paginaHTML);
        }

        private static void ManejadorControlLED(WebServerEventArgs e)
        {
            _ledIndicador.Toggle();
            string estadoLED = _ledIndicador.Read() == PinValue.High ? "encendido" : "apagado";
            
            string paginaHTML = $@"<html><body>
                <h1>Control de LED</h1>
                <p>El LED está {estadoLED}</p>
                <p><a href='/'>Volver al inicio</a></p>
                </body></html>";
            
            EnviarRespuestaHTML(e, paginaHTML);
        }

        private static void ManejadorEstadoSistema(WebServerEventArgs e)
        {
            string paginaHTML = $@"<html><body>
                <h1>Estado del Sistema</h1>
                <p>LED: {(_ledIndicador.Read() == PinValue.High ? "Encendido" : "Apagado")}</p>
                <p>Puerto Serie RS232: {(_puertoSerie.IsOpen ? "Conectado" : "Desconectado")}</p>
                <p>Nombre de red WiFi: {WIFI_SSID}</p>
                <p>Dirección IP: {WifiNetworkHelper.GetCurrentIPAddress()}</p>
                <p><a href='/'>Volver al inicio</a></p>
                </body></html>";
            
            EnviarRespuestaHTML(e, paginaHTML);
        }

        private static void ManejadorEstadoTanques(WebServerEventArgs e)
        {
            string estadoJson = _gestorTanques.ObtenerEstadoTanquesJson();
            string paginaHTML = $@"<html><body>
                <h1>Estado de Tanques</h1>
                <pre>{estadoJson}</pre>
                <p><a href='/'>Volver al inicio</a></p>
                </body></html>";
            
            EnviarRespuestaHTML(e, paginaHTML);
        }

        private static void EnviarRespuestaHTML(WebServerEventArgs e, string contenidoHTML)
        {
            e.Context.Response.ContentType = "text/html";
            e.Context.Response.ContentLength = contenidoHTML.Length;
            WebServer.OutPutStream(e.Context.Response, contenidoHTML);
        }
    }
}