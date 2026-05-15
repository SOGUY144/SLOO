using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

public class RenderPipelineConverter
{
    private const string MenuPath = "Tools/Convert Materials/";

    [MenuItem(MenuPath + "Convert to HDRP Lit")]
    private static void ConvertToHDRP()
    {
        Shader hdrpLitShader = Shader.Find("HDRP/Lit");
        if (hdrpLitShader == null)
        {
            EditorUtility.DisplayDialog("Error", "HDRP/Lit shader not found. Please ensure HDRP is installed.", "OK");
            return;
        }
        PerformConversion(hdrpLitShader, "HDRP/Lit", true);
    }

    [MenuItem(MenuPath + "Convert to URP Lit")]
    private static void ConvertToURP()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            EditorUtility.DisplayDialog("Error", "URP Lit shader not found. Please ensure URP is installed.", "OK");
            return;
        }
        PerformConversion(urpLitShader, "Universal Render Pipeline/Lit", false);
    }

    private static void PerformConversion(Shader targetShader, string targetShaderName, bool isHDRP)
    {
        string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
        int convertedCount = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Material Conversion", "Scanning materials...", 0f);

            for (int i = 0; i < allMaterialGuids.Length; i++)
            {
                string guid = allMaterialGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                EditorUtility.DisplayProgressBar(
                    "Material Conversion",
                    $"Processing: {Path.GetFileName(assetPath)}",
                    (float)i / allMaterialGuids.Length);

                Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                if (material != null && material.shader.name == "Standard")
                {
                    Texture albedoTexture = material.GetTexture("_MainTex");
                    Color baseColor = material.GetColor("_Color");
                    Texture metallicGlossMap = material.GetTexture("_MetallicGlossMap");
                    Texture normalMap = material.GetTexture("_BumpMap");
                    float metallicValue = material.GetFloat("_Metallic");
                    float smoothnessValue = material.GetFloat("_Glossiness");
                    int renderMode = material.GetInt("_Mode");

                    material.shader = targetShader;

                    if (isHDRP)
                    {
                        material.SetTexture("_BaseColorMap", albedoTexture);
                        material.SetColor("_BaseColor", baseColor);
                        material.SetTexture("_MaskMap", metallicGlossMap);
                        material.SetTexture("_NormalMap", normalMap);
                        material.SetFloat("_Metallic", metallicValue);
                        material.SetFloat("_Smoothness", smoothnessValue);

                        EditorApplication.delayCall += () =>
                        {
                            if (material != null)
                            {
                                material.SetFloat("_MetallicRemapMin", 0f);
                                material.SetFloat("_MetallicRemapMax", 1f);
                                material.SetFloat("_SmoothnessRemapMin", 0f);
                                material.SetFloat("_SmoothnessRemapMax", 1f);
                                EditorUtility.SetDirty(material);
                                AssetDatabase.SaveAssets();
                            }
                        };
                    }
                    else
                    {
                        material.SetTexture("_BaseMap", albedoTexture);
                        material.SetColor("_BaseColor", baseColor);
                        material.SetTexture("_MetallicGlossMap", metallicGlossMap);
                        material.SetTexture("_BumpMap", normalMap);
                        material.SetFloat("_Metallic", metallicValue);
                        material.SetFloat("_Smoothness", 1f);
                    }

                    if (renderMode == 0)
                    {
                        material.SetFloat("_Surface", 0);
                        material.SetFloat("_AlphaClip", 0);
                        if (isHDRP) material.SetFloat("_AlphaCutoffEnable", 0);
                    }
                    else if (renderMode == 1)
                    {
                        material.SetFloat("_Surface", 0);
                        material.SetFloat("_AlphaClip", 1);
                        if (isHDRP) material.SetFloat("_AlphaCutoffEnable", 1);
                    }
                    else if (renderMode == 2)
                    {
                        if (!isHDRP)
                        {
                            material.SetFloat("_Surface", 1);
                            material.SetFloat("_AlphaClip", 0);
                            material.SetFloat("_PreserveSpecular", 0);
                            material.SetFloat("_Cull", 2);
                        }
                        else
                        {
                            material.SetFloat("_SurfaceType", 1);
                            material.SetFloat("_BlendMode", 0);
                            material.SetFloat("_AlphaCutoffEnable", 0);
                            material.SetFloat("_TransparentBackfaceEnable", 1);
                            material.SetFloat("_EnableBlendModePreserveSpecularLighting", 0);
                        }
                    }

                    EditorUtility.SetDirty(material);
                    convertedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog(
            "Conversion Complete",
            $"{convertedCount} Standard materials were converted to {targetShaderName}.",
            "OK");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif