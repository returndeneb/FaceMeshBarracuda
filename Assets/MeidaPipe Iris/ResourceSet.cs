using Unity.Sentis;
using UnityEngine;

namespace MediaPipe.Iris {

//
// ScriptableObject class used to hold references to internal assets
//
[CreateAssetMenu(fileName = "Iris",
                 menuName = "ScriptableObjects/MediaPipe/Iris Resource Set")]
public sealed class ResourceSet : ScriptableObject
{
    public ModelAsset model;
    public ComputeShader preprocess;
    public ComputeShader postprocess;
}

} // namespace MediaPipe.Iris
