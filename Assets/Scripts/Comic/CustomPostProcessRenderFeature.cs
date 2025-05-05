using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class CustomPostProcessRenderFeature : ScriptableRendererFeature
{
    [SerializeField]
    private Shader bloomShader;
    [SerializeField]
    private Shader compositShader;
    private Material bloomMaterial;
    private Material compositeMaterial;
    private CustomPostProcessPass customPass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //  if (renderingData.cameraData.cameraType == CameraType.Game)
        // {
            renderer.EnqueuePass(customPass);
        //}
        
    }

    public override void Create()
    {
        bloomMaterial = CoreUtils.CreateEngineMaterial(bloomShader);
        compositeMaterial = CoreUtils.CreateEngineMaterial(compositShader);
        customPass = new CustomPostProcessPass(bloomMaterial, compositeMaterial);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(bloomMaterial);
        CoreUtils.Destroy(compositeMaterial);
    }
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if(renderingData.cameraData.cameraType == CameraType.Game){
            customPass.ConfigureInput(ScriptableRenderPassInput.Color);
            customPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            customPass.SetTarget(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);

        }
    }
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
