using System;
using System.Text;
using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;

namespace ESP32.BasicExample
{
    public class ClienteMqtt
    {
        private readonly MqttClient _clienteMqtt;
        private const string TOPIC_TANQUES = "esp32/tanques/niveles";
        
        public ClienteMqtt(string brokerHost, int puerto = 1883)
        {
            try
            {
                _clienteMqtt = new MqttClient(brokerHost, puerto, false, null, null, MqttSslProtocols.None);
                _clienteMqtt.ConnectionClosed += ManejadorDesconexion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear cliente MQTT: {ex.Message}");
                throw;
            }
        }

        public bool Conectar(string clienteId = "ESP32_Tanques")
        {
            try
            {
                if (!_clienteMqtt.IsConnected)
                {
                    _clienteMqtt.Connect(clienteId);
                }
                return _clienteMqtt.IsConnected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al conectar MQTT: {ex.Message}");
                return false;
            }
        }

        public void PublicarEstadoTanques(string estadoJson)
        {
            try
            {
                if (!_clienteMqtt.IsConnected && !Conectar())
                {
                    Console.WriteLine("No se pudo conectar al broker MQTT");
                    return;
                }

                _clienteMqtt.Publish(
                    TOPIC_TANQUES,
                    Encoding.UTF8.GetBytes(estadoJson),
                    MqttQoSLevel.ExactlyOnce,
                    false
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al publicar en MQTT: {ex.Message}");
            }
        }

        private void ManejadorDesconexion(object sender, EventArgs e)
        {
            Console.WriteLine("Conexión MQTT cerrada. Intentando reconectar...");
            Thread.Sleep(5000); // Esperar antes de reconectar
            Conectar();
        }

        public void Desconectar()
        {
            if (_clienteMqtt.IsConnected)
            {
                _clienteMqtt.Disconnect();
            }
        }
    }
}