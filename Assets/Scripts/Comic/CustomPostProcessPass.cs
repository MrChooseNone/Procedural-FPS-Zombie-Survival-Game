//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomPostProcessPass : ScriptableRenderPass
{
    private Material m_bloomMaterial;
    private Material compositeMaterial;
    RenderTextureDescriptor m_Descriptor;
    RTHandle m_CameraColorTarget;
    RTHandle m_CameraDepthTarget;

    const int k_maxPyramidSize = 16;
    private int[] _BloomMipUp;
    private int[] _BloomMipDown;
    private RTHandle[] m_BloomMipUp;
    private RTHandle[] m_BloomMipDown;
    private GraphicsFormat hdrFormat;
    private BenDayBloomEffect m_BloomEffect;

    public CustomPostProcessPass(Material bloomMat, Material compositeMat){

        m_bloomMaterial = bloomMat;
        compositeMaterial = compositeMat;
        if (m_bloomMaterial == null || compositeMaterial == null)
        {
            Debug.LogError("Materials not initialized");
            return;
        }

        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        _BloomMipUp = new int[k_maxPyramidSize];
        _BloomMipDown = new int[k_maxPyramidSize];
        m_BloomMipUp = new RTHandle[k_maxPyramidSize];
        m_BloomMipDown = new RTHandle[k_maxPyramidSize];

        for(int i = 0; i < k_maxPyramidSize; i++){
            _BloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
            _BloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
            m_BloomMipUp[i] = RTHandles.Alloc(_BloomMipUp[i], name: "_BloomMipUp" + i);
            m_BloomMipDown[i] = RTHandles.Alloc(_BloomMipDown[i], name: "_BloomMipDown" + i);
        }
        const FormatUsage usage = FormatUsage.Linear | FormatUsage.Render;
        
        if(SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, usage)){
            hdrFormat = GraphicsFormat.B10G11R11_UFloatPack32;
        }else{
            hdrFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? GraphicsFormat.R8G8B8A8_SRGB
                : GraphicsFormat.R8G8B8A8_UNorm;
        }

        if (!SystemInfo.IsFormatSupported(hdrFormat, FormatUsage.Render)) {
            Debug.LogError($"HDR format {hdrFormat} is not supported on this device.");
        }
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
        
    }
    public void SetTarget(RTHandle camerColorTargetHandle, RTHandle cameraDepthTargetHandle){
        m_CameraColorTarget = camerColorTargetHandle;
        m_CameraDepthTarget = cameraDepthTargetHandle;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // if (renderingData.cameraData.cameraType != CameraType.Game)
        // return;
        VolumeStack stack = VolumeManager.instance.stack;
        m_BloomEffect = stack.GetComponent<BenDayBloomEffect>();

        CommandBuffer cmd = CommandBufferPool.Get();

        using (new ProfilingScope(cmd, new ProfilingSampler("custom post processing"))){
            
            SetupBloom(cmd, m_CameraColorTarget);

            compositeMaterial.SetFloat("_Cutoff", m_BloomEffect.dotsCutoff.value);
            compositeMaterial.SetFloat("_Density", m_BloomEffect.dotsDensity.value);
            compositeMaterial.SetVector("_Direction", m_BloomEffect.scrollDirection.value);
            //compositeMaterial.SetTexture("_Bloom_Texture", m_BloomMipUp[0]);
            Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, compositeMaterial, 0);

        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

    private void SetupBloom(CommandBuffer cmd, RTHandle source){
        //Debug.Log("the source: "  + source.name);
        int downres = 1;
        int tw = m_Descriptor.width >> downres;
        int th = m_Descriptor.height >> downres;

        int maxSize = Mathf.Max(tw, th);
        int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) -1);
        int mipCount = Mathf.Clamp(iterations, 1, m_BloomEffect.maxIterations.value);
        float clamp = m_BloomEffect.clamp.value;
        float threshold = Mathf.GammaToLinearSpace(m_BloomEffect.threshold.value);
        float thresholdKnee = threshold * 0.5f;

        float scatter = Mathf.Lerp(0.05f, 0.95f, m_BloomEffect.scatter.value);
        var bloomMaterialNew = m_bloomMaterial;
        bloomMaterialNew.SetVector("_Params", new Vector4(scatter, clamp, threshold, thresholdKnee));
        //Debug.Log($"Shader Params: Scatter={scatter}, Clamp={clamp}, Threshold={threshold}, ThresholdKnee={thresholdKnee}");


        var desc = GetCompatibleDescriptor(tw, th, hdrFormat);
        for(int i = 0; i < mipCount; i++){
            //Debug.Log($"Allocating texture {m_BloomMipDown[0].name} with dimensions {desc.width}x{desc.height} and format {desc.graphicsFormat}");

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_BloomMipUp[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_BloomMipUp[i].name);
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_BloomMipDown[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_BloomMipDown[i].name);
            desc.width = Mathf.Max(1, desc.width >> 1);
            desc.height = Mathf.Max(1, desc.height >> 1);
        }
        //Debug.Log($"Source texture: {source.name}, Width: {source.rt.width}, Height: {source.rt.height}");

        Blitter.BlitCameraTexture(cmd, source, m_BloomMipDown[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterialNew, 0);
        //Debug.Log("the m_BloomMipDown[0]: "  + m_BloomMipDown[0].name);

        var lastDown = m_BloomMipDown[0];
         //Debug.Log("the lastDown: "  + lastDown.name);
        for(int i = 1; i < mipCount; i++){
            //Debug.Log("the lastDown inside loop: "  + lastDown.name);
            Blitter.BlitCameraTexture(cmd, lastDown ,m_BloomMipUp[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterialNew, 1);
            //Debug.Log("the lastDown inside loop 2: "  + lastDown.name);
            Blitter.BlitCameraTexture(cmd, m_BloomMipUp[i] ,m_BloomMipDown[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterialNew, 2);

            lastDown = m_BloomMipDown[i];
            //Debug.Log("the lastDown inside loop 3: "  + lastDown.name);
        }

        for(int i = (mipCount -2); i >= 0; i--){
            var lowMip = (i == mipCount - 2) ? m_BloomMipDown[i+1] : m_BloomMipUp[i+1];
            var highMip = m_BloomMipDown[i];
            var dst = m_BloomMipUp[i];
            // Debug.Log($"SourceLowMip texture: {lowMip.name}, Width: {lowMip.rt.width}, Height: {lowMip.rt.height}");
            // Debug.Log($"lowMip Format: {lowMip.rt.graphicsFormat}, highMip Format: {highMip.rt.graphicsFormat}, dst Format: {dst.rt.graphicsFormat}");

            //Blitter.BlitCameraTexture(cmd, lowMip, m_CameraColorTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterialNew, 3);
            //Debug.Log($"Pass {i}: lowMip = {lowMip?.name ?? "null"}, Valid: {lowMip != null}");


            cmd.SetGlobalTexture("_SourceTexLowMip", lowMip);
            Blitter.BlitCameraTexture(cmd, highMip ,dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterialNew, 3);

        }
        //compositeMaterial.SetTexture("_Bloom_Texture", m_BloomMipUp[0]);
        //cmd.SetGlobalTexture("_Screen_Texture", m_BloomMipUp[0]);
        cmd.SetGlobalTexture("_Bloom_Texture", m_BloomMipUp[0]);
        cmd.SetGlobalFloat("_BloomIntensity", m_BloomEffect.intensity.value);
    }

    RenderTextureDescriptor GetCompatibleDescriptor()
        => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

    RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None)
        => GetCompatibleDescriptor(m_Descriptor, width, height, format, depthBufferBits);

    internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None){
        desc.depthBufferBits = (int)depthBufferBits;
        desc.msaaSamples = 1;
        desc.width = width;
        desc.height = height;
        desc.graphicsFormat = format;
        return desc;
    }

    // internal void SetTarget(RTHandle camerColorTargetHandle, RTHandle cameraDepthTargetHandle){
    //     m_CameraColorTarget = camerColorTargetHandle;
    //     m_CameraDepthTarget = cameraDepthTargetHandle;
    // }

    // // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start()
    // {
        
    // }

    // // Update is called once per frame
    // void Update()
    // {
        
    // }
}
