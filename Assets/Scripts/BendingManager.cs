using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class BendingManager : MonoBehaviour
{
    #region Constants

    private const string BENDING_FEATURE = "ENABLE_BENDING";

    private static readonly int BENDING_AMOUNT =
      Shader.PropertyToID("_BendingAmount");

    #endregion


    #region Inspector

    [SerializeField]
    [Range(0.005f, 0.1f)]
    private float bendingAmount = 0.015f;
    [SerializeField]
    private bool Activate = true;

    #endregion


    #region Fields

    private float _prevAmount;
    private bool _prevActivate;

    #endregion


    #region MonoBehaviour

    private void Awake()
    {
        if (Application.isPlaying && Activate)
            Shader.EnableKeyword(BENDING_FEATURE);
        else
            Shader.DisableKeyword(BENDING_FEATURE);

        UpdateBendingAmount();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void Update()
    {
        if (Math.Abs(_prevAmount - bendingAmount) > Mathf.Epsilon)
            UpdateBendingAmount();

        if (Activate != _prevActivate)
            UpdateBendingActivate();
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        UpdateBendingActivate();
    }

    #endregion


    #region Methods

    private void UpdateBendingAmount()
    {
        _prevAmount = bendingAmount;
        Shader.SetGlobalFloat(BENDING_AMOUNT, bendingAmount);
    }
    private void UpdateBendingActivate()
    {
        _prevActivate = Activate;
        if (Application.isPlaying && Activate)
            Shader.EnableKeyword(BENDING_FEATURE);
        else
            Shader.DisableKeyword(BENDING_FEATURE);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext ctx,
                                                Camera cam)
    {

        if (Application.isPlaying && Activate)
            cam.cullingMatrix = Matrix4x4.Ortho(-99, 99, -99, 99, 0.001f, 99) *
                                cam.worldToCameraMatrix;
    }

    private static void OnEndCameraRendering(ScriptableRenderContext ctx,
                                              Camera cam)
    {
        cam.ResetCullingMatrix();
    }

    #endregion
}