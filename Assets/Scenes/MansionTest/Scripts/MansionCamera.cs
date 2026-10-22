using UnityEngine;
using System.Collections.Generic;

public class MansionCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 9f, -7f);
    readonly List<Renderer> hidden = new List<Renderer>();

    void LateUpdate()
    {
        if (!target) return;
        Restore();
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 0.5f);
        Vector3 origin = target.position + Vector3.up * 0.5f;
        Vector3 path = transform.position - origin;
        foreach (var hit in Physics.SphereCastAll(origin, 0.3f, path.normalized, path.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
            var renderer = hit.collider.GetComponent<Renderer>();
            if (renderer && !renderer.forceRenderingOff)
            {
                renderer.forceRenderingOff = true;
                hidden.Add(renderer);
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
