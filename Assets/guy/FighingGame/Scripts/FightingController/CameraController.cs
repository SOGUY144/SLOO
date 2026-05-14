using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform[] targets;
    public float smonthSpeed = 0.125f;
    public Vector3 offset;

    [Header("Stage Bounds")]
    public bool usePositionLimits = true;
    public Vector2 minPosition = new Vector2(-10f, -10f);
    public Vector2 maxPosition = new Vector2(10f, 10f);

    [Header("Wall Collision")]
    public bool avoidWalls = true;
    public LayerMask wallLayers;
    public float collisionRadius = 0.35f;
    public float wallPadding = 0.35f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (targets == null || targets.Length == 0) return;
        Transform activeTarget = FindActiveTarget();
        if (activeTarget == null) return;

        Vector3 desiredPosition = activeTarget.position + offset;
        desiredPosition.y = transform.position.y;

        desiredPosition = ApplyWallCollision(activeTarget.position, desiredPosition);
        desiredPosition = ClampToStage(desiredPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref currentVelocity, smonthSpeed);
    }

    Transform FindActiveTarget()
    {
        foreach (Transform target in targets)
            if (target != null && target.gameObject.activeInHierarchy)
                return target;
        return null;
    }

    Vector3 ClampToStage(Vector3 position)
    {
        if (!usePositionLimits) return position;
        position.x = Mathf.Clamp(position.x, minPosition.x, maxPosition.x);
        position.z = Mathf.Clamp(position.z, minPosition.y, maxPosition.y);
        return position;
    }

    Vector3 ApplyWallCollision(Vector3 targetPosition, Vector3 desiredPosition)
    {
        if (!avoidWalls || wallLayers.value == 0) return desiredPosition;

        // Cast จากตัวละคร → ตำแหน่งกล้องที่ต้องการ
        Vector3 origin = targetPosition;
        origin.y = desiredPosition.y;

        Vector3 direction = desiredPosition - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f) return desiredPosition;

        if (Physics.SphereCast(
            origin,
            collisionRadius,
            direction.normalized,
            out RaycastHit hit,
            distance,
            wallLayers,
            QueryTriggerInteraction.Ignore))
        {
            // หยุดกล้องก่อนชน wall โดยดันออกมาตาม normal ของผิว
            Vector3 safePos = origin + direction.normalized * (hit.distance - wallPadding);
            safePos += hit.normal * wallPadding;
            return safePos;
        }

        return desiredPosition;
    }

    void OnDrawGizmosSelected()
    {
        if (!usePositionLimits) return;
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(
            (minPosition.x + maxPosition.x) * 0.5f,
            transform.position.y,
            (minPosition.y + maxPosition.y) * 0.5f);
        Vector3 size = new Vector3(
            Mathf.Abs(maxPosition.x - minPosition.x),
            0.1f,
            Mathf.Abs(maxPosition.y - minPosition.y));
        Gizmos.DrawWireCube(center, size);
    }
}