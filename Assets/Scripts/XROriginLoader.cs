using Unity.XR.CoreUtils;
using UnityEngine;

public class XROriginLoader : MonoBehaviour
{
    public GameObject xrOriginPrefab;

    private void Start()
    {
        if (FindObjectOfType<XROrigin>() == null)
        {
            // ¶¯Ì¬ÊµÀý»¯ XR Origin
            Instantiate(xrOriginPrefab, Vector3.zero, Quaternion.identity);
        }
    }
}
