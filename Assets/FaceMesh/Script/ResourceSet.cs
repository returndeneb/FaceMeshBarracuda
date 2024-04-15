using UnityEngine;

namespace MediaPipe.FaceMesh {

//
// ScriptableObject class used to hold references to internal assets
//
[CreateAssetMenu(fileName = "ResourceSet",
                 menuName = "ScriptableObjects/MediaPipe/FaceMesh/Resource Set")]
public sealed class ResourceSet : ScriptableObject
{
    public global::BlazeFace.ResourceSet blazeFace;
    public FaceLandmark.ResourceSet faceLandmark;
    public Iris.ResourceSet iris;

    public Shader preprocessShader;
    public ComputeShader postprocessCompute;

    public Mesh faceMeshTemplate;
    public Mesh faceLineTemplate;
}

} // namespace MediaPipe.FaceMesh
