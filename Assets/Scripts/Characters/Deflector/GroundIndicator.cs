using UnityEngine;
using System.Collections.Generic;

public class GroundIndicator : MonoBehaviour
{
    const float indicatorLength = 250.0f;

    [SerializeField] float minHeightToActivate = 7.5f;
    [SerializeField] float lineBuffer = 4.0f; // to account for scale, line goes a little further than the raycast hits;

    [SerializeField] List<Gradient> lineGradients;
    [SerializeField] float minScale = 1.25f;
    [SerializeField] float maxScale = 5.0f;
    [SerializeField] float distanceForMaxScale = 50.0f;
    LayerMask groundMask;
  
    
    public LineRenderer lineRenderer;
    public MeshRenderer groundIndicator;
    Vector3 defaultScale;

    private void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        if (groundIndicator == null)
        {
            groundIndicator = GetComponentInChildren<MeshRenderer>();
        }

        groundMask = LayerMask.GetMask("Ground");
        defaultScale = groundIndicator.transform.localScale;
    }

    public void Init(Material mat, int index)
    {
        lineRenderer.colorGradient = lineGradients[index - 1];
        groundIndicator.material.SetColor("rimColor", mat.color);
    }

    private void Update()
    {
        Ray ray = new(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, indicatorLength, groundMask))
        {
            bool tooClose = hit.distance < minHeightToActivate;
                lineRenderer.enabled = !tooClose;
                groundIndicator.enabled = !tooClose;
            if (tooClose) return;
            
            lineRenderer.SetPosition(0, new Vector3(transform.position.x, transform.position.y, transform.position.z));
            lineRenderer.SetPosition(1, transform.position + (Vector3.down * (hit.distance + lineBuffer)));

            float distanceAsPercent = Mathf.Clamp01(hit.distance / distanceForMaxScale);
           Vector3 newScale = Vector3.one * Mathf.Lerp(maxScale, minScale, distanceAsPercent);
            newScale.y = defaultScale.y;
            groundIndicator.transform.localScale = newScale;
            Vector3 shadowPoint = hit.point;
            shadowPoint.y += 1; // don't let it clip into ground
            groundIndicator.transform.position = shadowPoint;
         
        }
        
    }
}
