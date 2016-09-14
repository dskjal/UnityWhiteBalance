using UnityEngine;

// 白色点の変更方法については以下の記事を参照
// http://technorgb.blogspot.jp/2015/08/blog-post_22.html
// http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html
// https://ssodelta.wordpress.com/tag/rgb-to-lms/
// https://github.com/keijiro/ColorSuite/blob/master/Assets/ColorSuite/ColorSuite.cs

namespace UnityStandardAssets.ImageEffects {
    [ExecuteInEditMode]
    public class WhiteBalanceEffect : PostEffectsBase {

        public Color WhiteBalance = Color.white;

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
        }

        public override bool CheckResources() {
            wbMaterial = CheckShaderAndCreateMaterial(whiteBalanceShader, wbMaterial);

            if (!isSupported) {
                ReportAutoDisable();
            }

            return isSupported;
        }

        private static Matrix4x4 rgbToXYZ = new Matrix4x4();
        private static Matrix4x4 xyzToLMS = new Matrix4x4();
        private static Matrix4x4 rgbToLMS;
        private static Matrix4x4 lmsToRGB;
        private Vector4 RGBtoLMS(Color rgb) {
            var v = new Vector4(rgb.r, rgb.g, rgb.b, 0f);
            return rgbToLMS*v;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination) {
            if (!CheckResources()) {
                Graphics.Blit(source, destination);
                return;
            }

            // 白色点 D65 に変換
            var srcLMS = RGBtoLMS(WhiteBalance);
            var ca = new Matrix4x4();
            ca[0, 0] =  0.9414285f / srcLMS.x;
            ca[1, 1] =  1.0404175f / srcLMS.y;
            ca[2, 2] =  1.0895327f / srcLMS.z;

            wbMaterial.SetMatrix("ChromaticAdaptation", (lmsToRGB * (ca * rgbToLMS)));

            Graphics.Blit(source, destination, wbMaterial);
        }
    }

}