using System;
using NNUtils;
using Unity.Sentis;
using UnityEngine;

namespace MediaPipe.FaceLandmark {

//
// Face landmark detector class
//
public sealed class FaceLandmarkDetector : IDisposable
{
    #region Public methods/properties

    public const int VertexCount = 468;

    public FaceLandmarkDetector(ResourceSet resources)
      => AllocateObjects(resources);

    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture image)
      => RunModel(image);

    public GraphicsBuffer VertexBuffer
      => _output;

    public ReadOnlySpan<Vector4> VertexArray
      => _readCache.Cached;

    #endregion

    #region Private objects

    // Input image size (defined by the model)
    const int ImageSize = 192;

    ResourceSet _resources;
    IWorker _worker;
    ImagePreprocess _preprocess;
    GraphicsBuffer _output;
    BufferReader<Vector4> _readCache;
    
    void AllocateObjects(ResourceSet resources)
    {
        _resources = resources;
        _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, ModelLoader.Load(resources.model));
        // Preprocessing buffer
        _preprocess = new ImagePreprocess(ImageSize, ImageSize, nchwFix: true);

        // Output buffer
        _output = BufferUtil.NewStructured<Vector4>(VertexCount);

        // Read cache
        _readCache = new BufferReader<Vector4>(_output, VertexCount);
    }

    void DeallocateObjects()
    {
        _worker?.Dispose();
        _worker = null;

        _preprocess?.Dispose();
        _preprocess = null;

        _output?.Dispose();
        _output = null;
    }

    #endregion

    #region Neural network inference function

    void RunModel(Texture source)
    {
        // Preprocessing
        _preprocess.Dispatch(source, _resources.preprocess);

        // Run the BlazeFace model.
        _worker.Execute(_preprocess.Tensor);

        // Postprocessing
        var post = _resources.postprocess;
        post.SetBuffer(0, "_Tensor", _worker.PeekOutputBuffer());
        post.SetBuffer(0, "_Vertices", _output);
        post.DispatchThreads(0, VertexCount, 1, 1);

        // Cache data invalidation
        _readCache.InvalidateCache();
    }

    #endregion
}

} // namespace MediaPipe.FaceLandmark
