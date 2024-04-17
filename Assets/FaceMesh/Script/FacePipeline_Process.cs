using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MediaPipe.FaceMesh {

//
// Image processing part of the face pipeline class
//

partial class FacePipeline
{
    // Face/eye region trackers
    FaceRegion _faceRegion = new();
    EyeRegion _leyeRegion = new();
    EyeRegion _reyeRegion = new(true);
    
    // Vertex retrieval from the face landmark detector
    float4 GetFaceVertex(int index)
      => _landmarkDetector.face.VertexArray[index];
    public static readonly int[] FACE_LANDMARK = {
        10, 297, 284, 389, 454, 361, 397, 378, 152, 149, 172, 132, 234, 162, 54, 67, 159, 157, 133, 154, 145, 163, 
        33, 161, 386, 388, 263, 390, 374, 381, 362, 384, 12, 271, 291, 403, 15, 179, 61, 41, 164, //473, 468
    };
    void RunPipeline(Texture input)
    {
        // Face detection
        _faceDetector.ProcessImage(input);

        // Cancel if the face detection score is too low.
        if (_faceDetector.Detections.IsEmpty) return;
        var face = _faceDetector.Detections[0];
        if (face.score < 0.5f) return;

        // Try updating the face region with the detection result. It's
        // actually updated only when there is a noticeable jump from the last
        // frame.
        _faceRegion.TryUpdateWithDetection(face);

        // Face region cropping
        _preprocess.SetMatrix("_Xform", _faceRegion.CropMatrix);
        Graphics.Blit(input, _cropRT.face, _preprocess, 0);

        // Face landmark detection
        _landmarkDetector.face.ProcessImage(_cropRT.face);

        // Key points from the face landmark
        var mouth    = _faceRegion.Transform(GetFaceVertex( 13)).xy;
        var mid_eyes = _faceRegion.Transform(GetFaceVertex(168)).xy;
        var eye_l0   = _faceRegion.Transform(GetFaceVertex( 33)).xy;
        var eye_l1   = _faceRegion.Transform(GetFaceVertex(133)).xy;
        var eye_r0   = _faceRegion.Transform(GetFaceVertex(362)).xy;
        var eye_r1   = _faceRegion.Transform(GetFaceVertex(263)).xy;
        List<Vector2> face_landmark = new List<Vector2>();
        for (int i = 0; i < FACE_LANDMARK.Length; i += 2)
        {
            int landmarkIndex = FACE_LANDMARK[i];
            face_landmark.Add(GetFaceVertex(landmarkIndex).xy);
        }
        // Eye region update
        _leyeRegion.Update(eye_l0, eye_l1, _faceRegion.RotationMatrix);
        _reyeRegion.Update(eye_r0, eye_r1, _faceRegion.RotationMatrix);

        // Eye region cropping
        _preprocess.SetMatrix("_Xform", _leyeRegion.CropMatrix);
        Graphics.Blit(input, _cropRT.eyeL, _preprocess, 0);

        _preprocess.SetMatrix("_Xform", _reyeRegion.CropMatrix);
        Graphics.Blit(input, _cropRT.eyeR, _preprocess, 0);

        // Eye landmark detection
        _landmarkDetector.eyeL.ProcessImage(_cropRT.eyeL);
        _landmarkDetector.eyeR.ProcessImage(_cropRT.eyeR);

        // Postprocess for face mesh construction
        var post = _resources.postprocessCompute;

        post.SetMatrix("_fx_xform", _faceRegion.CropMatrix);
        post.SetBuffer(0, "_fx_input", _landmarkDetector.face.VertexBuffer);
        post.SetBuffer(0, "_fx_output", _computeBuffer.post);
        post.SetBuffer(0, "_fx_bbox", _computeBuffer.bbox);
        post.Dispatch(0, 1, 1, 1);

        post.SetBuffer(1, "_e2f_index_table", _computeBuffer.eyeToFace);
        post.SetBuffer(1, "_e2f_eye_l", _landmarkDetector.eyeL.VertexBuffer);
        post.SetBuffer(1, "_e2f_eye_r", _landmarkDetector.eyeR.VertexBuffer);
        post.SetMatrix("_e2f_xform_l", _leyeRegion.CropMatrix);
        post.SetMatrix("_e2f_xform_r", _reyeRegion.CropMatrix);
        post.SetBuffer(1, "_e2f_face", _computeBuffer.post);
        post.Dispatch(1, 1, 1, 1);

        post.SetBuffer(2, "_lpf_input", _computeBuffer.post);
        post.SetBuffer(2, "_lpf_output", _computeBuffer.filter);
        post.SetFloat("_lpf_beta", 30.0f);
        post.SetFloat("_lpf_cutoff_min", 1.5f);
        post.SetFloat("_lpf_t_e", Time.deltaTime);
        post.Dispatch(2, 468 / 52, 1, 1);
        
        // Face region update based on the postprocessed face mesh
        _faceRegion.Step
          (_computeBuffer.bbox.GetBoundingBoxData(), mid_eyes - mouth);
    }
}

} // namespace MediaPipe.FaceMesh
