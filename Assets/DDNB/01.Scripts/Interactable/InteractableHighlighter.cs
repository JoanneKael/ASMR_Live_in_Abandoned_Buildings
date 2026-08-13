using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조준 시 흰색 아웃라인만 표시 (전체 Emission 하이라이트 대체).
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    private const string OutlineObjectName = "__DDNB_Outline";
    private const string OutlineShaderName = "DDNB/WhiteOutline";

    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 0.025f;

    private static Material _sharedOutlineMaterial;

    private readonly List<GameObject> _outlineObjects = new List<GameObject>();
    private bool _highlighted;
    private bool _built;

    public void SetHighlight(bool on)
    {
        EnsureOutlineObjects();

        if (_highlighted == on) return;
        _highlighted = on;

        for (int i = 0; i < _outlineObjects.Count; i++)
        {
            if (_outlineObjects[i] != null)
                _outlineObjects[i].SetActive(on);
        }
    }

    private void OnDisable()
    {
        if (_highlighted)
            SetHighlight(false);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _outlineObjects.Count; i++)
        {
            if (_outlineObjects[i] != null)
                Destroy(_outlineObjects[i]);
        }
        _outlineObjects.Clear();
    }

    private void EnsureOutlineObjects()
    {
        if (_built) return;
        _built = true;

        Material mat = GetOutlineMaterial();
        if (mat == null)
        {
            Debug.LogWarning("[InteractableHighlighter] DDNB/WhiteOutline 셰이더를 찾을 수 없습니다.");
            return;
        }

        Renderer[] sourceRenderers = CollectSourceRenderers();
        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer src = sourceRenderers[i];
            if (src == null) continue;

            Mesh mesh = null;
            Transform host = src.transform;

            if (src is MeshRenderer)
            {
                MeshFilter filter = src.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                mesh = filter.sharedMesh;
            }
            else if (src is SkinnedMeshRenderer skinned)
            {
                mesh = skinned.sharedMesh;
                if (mesh == null) continue;
            }
            else
            {
                continue;
            }

            GameObject outlineGo = new GameObject(OutlineObjectName);
            outlineGo.transform.SetParent(host, false);
            outlineGo.transform.localPosition = Vector3.zero;
            outlineGo.transform.localRotation = Quaternion.identity;
            outlineGo.transform.localScale = Vector3.one;
            outlineGo.layer = host.gameObject.layer;
            outlineGo.SetActive(false);

            MeshFilter mf = outlineGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = outlineGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            _outlineObjects.Add(outlineGo);
        }
    }

    private Renderer[] CollectSourceRenderers()
    {
        if (TryGetComponent(out InteractableDoor door) && door.DoorMesh != null)
            return door.DoorMesh.GetComponentsInChildren<Renderer>(true);

        Transform root = transform.parent != null ? transform.parent : transform;
        Renderer[] all = root.GetComponentsInChildren<Renderer>(true);

        // 이미 만든 아웃라인 렌더러는 제외
        List<Renderer> filtered = new List<Renderer>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;
            if (r.gameObject.name == OutlineObjectName) continue;
            filtered.Add(r);
        }
        return filtered.ToArray();
    }

    private Material GetOutlineMaterial()
    {
        if (_sharedOutlineMaterial != null)
            return _sharedOutlineMaterial;

        Shader shader = Shader.Find(OutlineShaderName);
        if (shader == null)
            return null;

        _sharedOutlineMaterial = new Material(shader)
        {
            name = "DDNB_WhiteOutline_Runtime",
            hideFlags = HideFlags.DontSave
        };
        _sharedOutlineMaterial.SetColor("_OutlineColor", outlineColor);
        _sharedOutlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        return _sharedOutlineMaterial;
    }
}
