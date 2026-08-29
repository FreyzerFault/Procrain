using System;
using UnityEngine;

namespace Procrain
{
    // Interfaz de la que debe heredar el Player para poder optimizar los calculos dinámicos del Terreno
    // SOLO cuando se desplaza
    public interface IPlayer
    {
        public static IPlayer Player => GameObject.FindWithTag("Player")?.GetComponent<IPlayer>();
        
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        
        public event Action<Vector2> OnPlayerMove;
    }
}
