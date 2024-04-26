using System;
using NNUtils;
using Unity.Sentis;
using UnityEngine;

namespace BlazeFace {

public sealed class FaceDetector : IDisposable
{
    readonly ResourceSet _resources;
    private ImagePreprocess _preprocess;
    IWorker _worker;
    (GraphicsBuffer post1, GraphicsBuffer post2, GraphicsBuffer count) _output;
    readonly CountedBufferReader<Detection> _detection;
    private int _size;

    public FaceDetector(ResourceSet resources)
    {
        _resources = resources;
        var model = ModelLoader.Load(_resources.model);
        _size = model.inputs[0].GetTensorShape().GetWidth();
        _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);
        _preprocess = new ImagePreprocess(_size, _size);
        _output.post1 = new GraphicsBuffer(GraphicsBuffer.Target.Append, Detection.Max, Detection.Size);
        _output.post2 = new GraphicsBuffer(GraphicsBuffer.Target.Append, Detection.Max, Detection.Size);
        _output.count = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1, sizeof(uint));
        _detection = new CountedBufferReader<Detection>(_output.post2, _output.count, Detection.Max);
    }
    
    public void ProcessImage(Texture image, float threshold = 0.75f)
    {
        // Reset the compute buffer counters.
        _output.post1.SetCounterValue(0);
        _output.post2.SetCounterValue(0);

        // Preprocessing
        _preprocess.Dispatch(image, _resources.preprocess);

        // Run the BlazeFace model.
        _worker.Execute(_preprocess.Tensor);

        // 1st postprocess (bounding box aggregation)
        var post1 = _resources.postprocess1;
        post1.SetFloat("_ImageSize", _size);
        post1.SetFloat("_Threshold", threshold);

        post1.SetBuffer(0, "_Scores", _worker.PeekOutputBuffer("Identity"));
        post1.SetBuffer(0, "_Boxes", _worker.PeekOutputBuffer("Identity_2"));
        post1.SetBuffer(0, "_Output", _output.post1);
        post1.Dispatch(0, 1, 1, 1);

        post1.SetBuffer(1, "_Scores", _worker.PeekOutputBuffer("Identity_1"));
        post1.SetBuffer(1, "_Boxes", _worker.PeekOutputBuffer("Identity_3"));
        post1.SetBuffer(1, "_Output", _output.post1);
        post1.Dispatch(1, 1, 1, 1);

        // Retrieve the bounding box count.
        GraphicsBuffer.CopyCount(_output.post1, _output.count, 0);

        // 2nd postprocess (overlap removal)
        var post2 = _resources.postprocess2;
        post2.SetBuffer(0, "_Input", _output.post1);
        post2.SetBuffer(0, "_Count", _output.count);
        post2.SetBuffer(0, "_Output", _output.post2);
        post2.Dispatch(0, 1, 1, 1);

        // Retrieve the bounding box count after removal.
        GraphicsBuffer.CopyCount(_output.post2, _output.count, 0);

        // Cache data invalidation
        _detection.InvalidateCache();
    }

    public void Dispose()
    {
        _worker?.Dispose();
        _worker = null;

        _preprocess?.Dispose();
        _preprocess = null;

        _output.post1?.Dispose();
        _output.post2?.Dispose();
        _output.count?.Dispose();
        _output = (null, null, null);
    }
    
    public ReadOnlySpan<Detection> Detections
      => _detection.Cached;
    
}

}
