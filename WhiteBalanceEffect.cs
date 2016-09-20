using UnityEngine;

// 白色点の変更方法については以下の記事を参照
// http://technorgb.blogspot.jp/2015/08/blog-post_22.html
// http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html
// https://ssodelta.wordpress.com/tag/rgb-to-lms/
// https://github.com/keijiro/ColorSuite/blob/master/Assets/ColorSuite/ColorSuite.cs

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class WhiteBalanceEffect : MonoBehaviour {

    public Color WhiteBalance = Color.white;
    private Color oldWhiteBalance = Color.white;    // ホワイトバランスの色を変化させたときのみ行列をアップデートする

    private Material wbMaterial;
    public Shader whiteBalanceShader = null;

    public void Awake() {
        rgbToXYZ.SetRow(0, new Vector4(0.412391f, 0.357584f, 0.180481f));
        rgbToXYZ.SetRow(1, new Vector4(0.212639f, 0.715169f, 0.072192f));
        rgbToXYZ.SetRow(2, new Vector4(0.019331f, 0.119195f, 0.950532f));
        rgbToXYZ.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

        // Bradford
        xyzToLMS.SetRow(0, new Vector4(0.8951000f, 0.2664000f, -0.1614000f));
        xyzToLMS.SetRow(1, new Vector4(-0.7502000f, 1.7135000f, 0.0367000f));
        xyzToLMS.SetRow(2, new Vector4(0.0389000f, -0.0685000f, 1.0296000f));
        xyzToLMS.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

        rgbToLMS = xyzToLMS * rgbToXYZ;
        lmsToRGB = rgbToLMS.inverse;

        chromaticAdaptation = getChromaticAdaptationMatrix(WhiteBalance);
    }

    public bool CheckResources() {
        if (whiteBalanceShader == null) {
            Debug.Log("Missing shader in " + ToString());
            return false;
        }

        if(wbMaterial== null) {
            wbMaterial = new Material(whiteBalanceShader);
        }

        if (!whiteBalanceShader.isSupported) {
            Debug.LogWarning("The image effect " + ToString() + " has been disabled as it's not supported on the current platform.");
        }

        return whiteBalanceShader.isSupported;
    }

    private static Matrix4x4 rgbToXYZ = new Matrix4x4();
    private static Matrix4x4 xyzToLMS = new Matrix4x4();
    private static Matrix4x4 rgbToLMS;
    private static Matrix4x4 lmsToRGB;
    private static Matrix4x4 chromaticAdaptation;
    private Vector4 RGBtoLMS(Color rgb) {
        var v = new Vector4(rgb.r, rgb.g, rgb.b, 0f);
        return rgbToLMS*v;
    }

    private Matrix4x4 getChromaticAdaptationMatrix(Color whiteBalance) {
        var srcLMS = RGBtoLMS(WhiteBalance);
        // 白色点 D65 に変換
        var ca = new Matrix4x4();
        ca[0, 0] =  0.9414285f / srcLMS.x;
        ca[1, 1] =  1.0404175f / srcLMS.y;
        ca[2, 2] =  1.0895327f / srcLMS.z;

        return lmsToRGB * (ca * rgbToLMS);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (!CheckResources()) {
            Graphics.Blit(source, destination);
            return;
        }

        if (WhiteBalance != oldWhiteBalance) {
            chromaticAdaptation = getChromaticAdaptationMatrix(WhiteBalance);
            oldWhiteBalance = WhiteBalance;
        }

        wbMaterial.SetMatrix("ChromaticAdaptation", chromaticAdaptation);

        Graphics.Blit(source, destination, wbMaterial);
    }
}
