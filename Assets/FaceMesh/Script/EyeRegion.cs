using Unity.Mathematics;

namespace MediaPipe.FaceMesh
{
    sealed class EyeRegion
    {
        public float4x4 CropMatrix { get; private set; }
        bool _flipped;
        public EyeRegion(bool flipped = false)
            => _flipped = flipped;
        public void Update(float2 p0, float2 p1, float4x4 rotation)
        {
            var box = BoundingBox.CenterExtent
                ((p0 + p1) * 0.5f, math.distance(p0, p1) * 1.4f);
           
            CropMatrix = math.mul(box.CropMatrix, rotation);
            if (_flipped)
                CropMatrix = math.mul(CropMatrix, MathUtil.HorizontalFlip());
        }

    }
}