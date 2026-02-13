using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] Transform chargeMeterOver;
    [SerializeField] Transform chargeMeterUnder;
    [SerializeField] MeshRenderer chargeMeterOverMesh;
    [SerializeField] Material chargeMeterMax;
    [SerializeField] Material chargeMeterProgress;

    [SerializeField] bool enableOnAwake = false;
    float originalSize = 0.0f;

    private void Awake()
    {
        originalSize = chargeMeterOver.localScale.x;
        chargeMeterOver.gameObject.SetActive(enableOnAwake);
        chargeMeterUnder.gameObject.SetActive(enableOnAwake);
    }

    public void SetProgress(float percent)
    {
        Vector3 newScale = chargeMeterOver.localScale;
        newScale.x = originalSize * percent;
        chargeMeterOver.localScale = newScale;
        Vector3 newPos = chargeMeterOver.transform.localPosition;
        newPos.x = (originalSize - newScale.x) / 2.0f;
        chargeMeterOver.transform.localPosition = newPos;

        chargeMeterOverMesh.material = percent >= 0.999f ? chargeMeterMax : chargeMeterProgress;

        //Vector3 newScale = chargeMeterOver.localScale;
        //newScale.x = originalSize * chargeAsPercent;
        //chargeMeterOver.localScale = newScale;
        //Vector3 newPos = chargeMeterOver.transform.localPosition;
        //newPos.x = (originalSize - newScale.x) / 2.0f;
        //chargeMeterOver.transform.localPosition = newPos;

        //chargeMeterOverMesh.material = percent > 0.99f ? chargeMeterMax : chargeMeterProgress;
    }

    public void SetDisplayStatus(bool status)
    {
        chargeMeterOver.gameObject.SetActive(status);
        chargeMeterUnder.gameObject.SetActive(status);
    }
}
