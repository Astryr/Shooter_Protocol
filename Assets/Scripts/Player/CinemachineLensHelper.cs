using Unity.Cinemachine;
using UnityEngine;

public static class CinemachineLensHelper
{
    public static float GetVerticalFov(CinemachineVirtualCameraBase vcam)
    {
        if (vcam == null)
            return 40f;

        if (vcam is CinemachineCamera camera)
            return camera.Lens.FieldOfView;

#pragma warning disable CS0618
        if (vcam is CinemachineVirtualCamera legacy)
            return legacy.m_Lens.FieldOfView;
#pragma warning restore CS0618

        return 40f;
    }

    public static void SetVerticalFov(CinemachineVirtualCameraBase vcam, float fov)
    {
        if (vcam == null)
            return;

        if (vcam is CinemachineCamera camera)
        {
            LensSettings lens = camera.Lens;
            lens.FieldOfView = fov;
            camera.Lens = lens;
            return;
        }

#pragma warning disable CS0618
        if (vcam is CinemachineVirtualCamera legacy)
        {
            var lens = legacy.m_Lens;
            lens.FieldOfView = fov;
            legacy.m_Lens = lens;
        }
#pragma warning restore CS0618
    }

    public static CinemachineVirtualCameraBase FindFirstVirtualCamera()
    {
        CinemachineCamera camera = Object.FindFirstObjectByType<CinemachineCamera>();
        return camera != null ? camera : Object.FindFirstObjectByType<CinemachineVirtualCameraBase>();
    }
}
