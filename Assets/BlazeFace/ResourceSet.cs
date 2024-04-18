using Unity.Sentis;
using UnityEngine;

namespace BlazeFace {
public sealed class ResourceSet : ScriptableObject
{
    public ModelAsset model;
    public ComputeShader preprocess;
    public ComputeShader postprocess1;
    public ComputeShader postprocess2;
}
}