using Unity.Barracuda;
using UnityEngine;

namespace BlazeFace {
public sealed class ResourceSet : ScriptableObject
{
    public NNModel model;
    public ComputeShader preprocess;
    public ComputeShader postprocess1;
    public ComputeShader postprocess2;
}
}