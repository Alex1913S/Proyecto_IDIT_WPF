using System;
using System.Collections.Generic;
using System.Text;

namespace Presentación
{
    // Esta interfaz obliga a cualquier UserControl a saber cómo pintarse en ambos modos
    public interface IThemeable
    {
        void AplicarTema(bool modoClaro);
    }
}
