using UnityEngine;
using UnityEngine.Networking;

namespace Test_Tools {

public sealed class ImageSource : MonoBehaviour
{
    #region Public property

    public Texture Texture => texture;

    #endregion

    #region Editable attributes

    // Source type options
    public enum SourceType { Webcam, Card }
    [SerializeField] SourceType _sourceType = SourceType.Card;

    // Webcam options
    [SerializeField] string _webcamName = "";
    [SerializeField] Vector2Int _webcamResolution = new(1920, 1080);
    [SerializeField] int _webcamFrameRate = 30;

    // Output options
    [SerializeField] RenderTexture _outputTexture;
    [SerializeField] Vector2Int _outputResolution = new(1024, 1024);

    #endregion

    #region Package asset reference

    [SerializeField, HideInInspector] Shader _shader;

    #endregion

    #region Private members

    UnityWebRequest _webTexture;
    WebCamTexture _webcam;
    Material _material;
    RenderTexture _buffer;

    RenderTexture texture
      => _outputTexture != null ? _outputTexture : _buffer;

    // Blit a texture into the output buffer with aspect ratio compensation.
    void Blit(Texture source, bool vflip = false)
    {
        if (source == null) return;

        var aspect1 = (float)source.width / source.height;
        var aspect2 = (float)texture.width / texture.height;

        var scale = new Vector2(aspect2 / aspect1, aspect1 / aspect2);
        scale = Vector2.Min(Vector2.one, scale);
        if (vflip) scale.y *= -1;

        var offset = (Vector2.one - scale) / 2;

        Graphics.Blit(source, texture, scale, offset);
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // Allocate a render texture if no output texture has been given.
        if (_outputTexture == null)
            _buffer = new RenderTexture
              (_outputResolution.x, _outputResolution.y, 0);

        // Create a material for the shader (only on Card and Gradient)
        if (_sourceType == SourceType.Card )
            _material = new Material(_shader);

        // Webcam source type:
        // Create a WebCamTexture and start capturing.
        if (_sourceType == SourceType.Webcam)
        {
            _webcam = new WebCamTexture
              (_webcamName,
               _webcamResolution.x, _webcamResolution.y, _webcamFrameRate);
            _webcam.Play();
        }

        // Card source type:
        // Run the card shader to generate a test card image.
        if (_sourceType == SourceType.Card)
        {
            var dims = new Vector2(texture.width, texture.height);
            _material.SetVector("_Resolution", dims);
            Graphics.Blit(null, texture, _material, 0);
        }
    }

    void OnDestroy()
    {
        if (_webcam != null) Destroy(_webcam);
        if (_buffer != null) Destroy(_buffer);
        if (_material != null) Destroy(_material);
    }

    void Update()
    {

        if (_sourceType == SourceType.Webcam && _webcam.didUpdateThisFrame)
            Blit(_webcam, _webcam.videoVerticallyMirrored);

        // Asynchronous image downloading
        if (_webTexture != null && _webTexture.isDone)
        {
            var texture = DownloadHandlerTexture.GetContent(_webTexture);
            _webTexture.Dispose();
            _webTexture = null;
            Blit(texture);
            Destroy(texture);
        }
    }

    #endregion
}

} // namespace Klak.TestTools
