using System;
using System.Text;
using System.IO.Ports;

namespace ESP32.BasicExample
{
    public class GestorTanques
    {
        private readonly SerialPort _puertoSerie;
        private readonly TanqueCombustible[] _tanques;
        private const string COMANDO_LECTURA = "READ_NIVEL";
        
        public GestorTanques(SerialPort puertoSerie)
        {
            _puertoSerie = puertoSerie;
            _tanques = new TanqueCombustible[4]
            {
                new TanqueCombustible(1),
                new TanqueCombustible(2),
                new TanqueCombustible(3),
                new TanqueCombustible(4)
            };
        }

        public void RealizarLecturaTanques()
        {
            if (!_puertoSerie.IsOpen)
            {
                Console.WriteLine("Error: Puerto serie no está abierto");
                return;
            }

            try
            {
                // Enviar comando de lectura
                _puertoSerie.WriteLine(COMANDO_LECTURA);
                Thread.Sleep(500); // Esperar respuesta

                if (_puertoSerie.BytesToRead > 0)
                {
                    string respuesta = _puertoSerie.ReadLine();
                    ProcesarRespuesta(respuesta);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer tanques: {ex.Message}");
            }
        }

        private void ProcesarRespuesta(string respuesta)
        {
            // Formato esperado: "T1:XX.XX;T2:XX.XX;T3:XX.XX;T4:XX.XX"
            try
            {
                string[] lecturas = respuesta.Split(';');
                for (int i = 0; i < lecturas.Length && i < _tanques.Length; i++)
                {
                    string[] partes = lecturas[i].Split(':');
                    if (partes.Length == 2 && double.TryParse(partes[1], out double nivel))
                    {
                        _tanques[i].ActualizarNivel(nivel);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar respuesta: {ex.Message}");
            }
        }

        public string ObtenerEstadoTanquesJson()
        {
            StringBuilder json = new StringBuilder("[");
            for (int i = 0; i < _tanques.Length; i++)
            {
                json.Append(_tanques[i].ObtenerEstadoJson());
                if (i < _tanques.Length - 1)
                    json.Append(",");
            }
            json.Append("]");
            return json.ToString();
        }
    }
}