using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlatformTrigger : MonoBehaviour
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private Renderer platformRenderer;

    public event Action OnPlayerReachedPlatform;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || !other.CompareTag("Player"))
            return;

        Debug.Log("Player reached the platform.");
        OnPlayerReachedPlatform?.Invoke();
    }

    public void SetActive(bool value)
    {
        isActive = value;
    }

    public void SetVisible(bool visible)
    {
        if (platformRenderer != null)
            platformRenderer.enabled = visible;
    }
}