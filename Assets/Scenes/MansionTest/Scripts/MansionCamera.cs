using UnityEngine;
using System.Collections.Generic;

public class MansionCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 9f, -7f);
    public bool purelyTopDown;
    public float topDownSize = 11f;
    readonly List<Renderer> hidden = new List<Renderer>();
    readonly HashSet<Renderer> occluders = new HashSet<Renderer>();
    readonly RaycastHit[] hits = new RaycastHit[64];
    Camera viewCamera;

    void LateUpdate()
    {
        if (!target) return;
        if (!viewCamera) viewCamera = GetComponent<Camera>();
        transform.position = target.position + offset;
        if (purelyTopDown)
        {
            transform.position = target.position + Vector3.up * 30f;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = topDownSize;
        }
        else transform.LookAt(target.position + Vector3.up * 0.5f);
        Vector3 origin = target.position + Vector3.up * 0.5f;
        Vector3 path = transform.position - origin;
        occluders.Clear();
        int count = Physics.SphereCastNonAlloc(origin, 0.3f, path.normalized, hits, path.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
            var renderer = hit.collider.GetComponent<Renderer>();
            if (renderer) occluders.Add(renderer);
            if (renderer && !renderer.forceRenderingOff)
            {
                renderer.forceRenderingOff = true;
                hidden.Add(renderer);
            }
        }
        for (int i = hidden.Count - 1; i >= 0; i--)
        {
            var renderer = hidden[i];
            if (!renderer || !occluders.Contains(renderer))
            {
                if (renderer) renderer.forceRenderingOff = false;
                hidden.RemoveAt(i);
            }
        }
    }

    void Restore()
    {
        foreach (var renderer in hidden) if (renderer) renderer.forceRenderingOff = false;
        hidden.Clear();
    }

    void OnDisable() => Restore();
}
