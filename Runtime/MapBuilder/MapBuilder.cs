using UnityEngine;
using UnityEngine.Serialization;

namespace Procrain.MapBuilder
{
    /// <summary>
    /// Base class for map generation builders in the Procrain system.
    /// Provides common functionality for both synchronous and parallel map generation.
    /// Inherits from ScriptableObject to allow configuration through the Unity Inspector.
    /// </summary>
    public class MapBuilder: ScriptableObject
    {
        /// <summary>
        /// Determines if the map generation should use parallel processing.
        /// When true, the generation tasks will be executed using Unity's Job System.
        /// </summary>
        [SerializeField, Tooltip("When enabled, generation tasks will be executed in parallel using Unity's Job System")]
        protected bool paralelized;

        /// <summary>
        /// Enables debug information output during map generation.
        /// When true, performance metrics and other debug info will be logged.
        /// </summary>
        [FormerlySerializedAs("debugInfo")] [SerializeField, Tooltip("When enabled, shows performance metrics and debug info during generation")] 
        protected bool debug;
    }
}
