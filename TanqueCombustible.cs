using System;

namespace ESP32.BasicExample
{
    public class TanqueCombustible
    {
        public int Id { get; private set; }
        public double NivelActual { get; private set; }
        public DateTime UltimaLectura { get; private set; }

        public TanqueCombustible(int id)
        {
            Id = id;
            NivelActual = 0;
            UltimaLectura = DateTime.MinValue;
        }

        public void ActualizarNivel(double nuevoNivel)
        {
            NivelActual = nuevoNivel;
            UltimaLectura = DateTime.UtcNow;
        }

        public string ObtenerEstadoJson()
        {
            return $"{{\"tanqueId\":{Id},\"nivel\":{NivelActual},\"timestamp\":\"{UltimaLectura}\"}}";
        }
    }
}