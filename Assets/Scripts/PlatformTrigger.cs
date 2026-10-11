using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlatformTrigger : MonoBehaviour
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private Renderer platformRenderer;

    private Collider platformCollider;

    public event Action OnPlayerReachedPlatform;

    private void Awake()
    {
        platformCollider = GetComponent<Collider>();
        platformCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || !IsPlayerCollider(other))
            return;

        Debug.Log(
            "Player reached the platform through trigger. " +
            $"Collider: {other.name}, tag: {other.tag}");

        OnPlayerReachedPlatform?.Invoke();
    }

    /// <summary>
    /// Quest/Android fallback: checks physical overlap each frame so the
    /// trial does not depend entirely on an OnTriggerEnter callback.
    /// It uses the same trigger collider and the player's controller radius.
    /// </summary>
    public void CheckPlayerProximity(Transform playerTransform)
    {
        if (!isActive ||
            platformCollider == null ||
            playerTransform == null)
        {
            return;
        }

        CharacterController playerController =
            playerTransform.GetComponent<CharacterController>();

        float playerRadius =
            playerController != null
                ? playerController.radius
                : 0.5f;

        Bounds playerBounds =
            playerController != null
                ? playerController.bounds
                : playerTransform.GetComponent<Collider>() != null
                    ? playerTransform.GetComponent<Collider>().bounds
                    : new Bounds(playerTransform.position, Vector3.one);

        Bounds triggerBounds = platformCollider.bounds;

        // Require vertical overlap as well as horizontal proximity.
        if (playerBounds.max.y < triggerBounds.min.y ||
            playerBounds.min.y > triggerBounds.max.y)
        {
            return;
        }

        Vector3 closestPoint =
            platformCollider.ClosestPoint(playerTransform.position);

        Vector3 horizontalOffset =
            playerTransform.position - closestPoint;

        horizontalOffset.y = 0f;

        if (horizontalOffset.sqrMagnitude >
            playerRadius * playerRadius)
        {
            return;
        }

        Debug.Log(
            "Player reached the platform through proximity fallback.");

        OnPlayerReachedPlatform?.Invoke();
    }

    private static bool IsPlayerCollider(Collider other)
    {
        // A child collider may be Untagged even when its Player root is tagged.
        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag("Player"))
                return true;

            current = current.parent;
        }

        return false;
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